//----------------------------------------------
//            	   Koreographer                 
//    Copyright © 2014-2021 Sonic Bloom, LLC    
//----------------------------------------------

using FMOD.Studio;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace SonicBloom.Koreo.Players.FMODStudio
{
    // NOTE: Thread Safety is a large concern here. FMOD's callbacks (of which several are used for
    //  efficiency) can occur on non-main threads. This can cause very strange side effects when
    //  reading/writing memory. The current thread handling takes "calculated" risks with member
    //  objects insofar as the expected potential inconsistencies _should_ not interfere with
    //  expected operation.
    //      If inexplicable things happen during the operation, a more whole-hearted thread safety
    //  approach should be implemented. Specifically, all externally accessible APIs (including the
    //  IKoreographedPlayer APIs) should be wrapped in a Lock (provided they do not call another
    //  locking API). In this manner we can ensure that only a single thread will have access to an
    //  instances state at any given time. This approach was not initially taken for efficiency
    //  (reduce the overhead of acquiring locks all the time).

    /// <summary>
    /// The FMODEventInstanceVisor manages synchronization for audio files played within the scope
    /// of a single FMOD EventInstance. When an EventInstance is created from an EventDefinition
    /// within FMOD Studio, all audio files played within it will be accessible from the
    /// EventInstance itself. The FMODEventInstanceVisor registers for "Sound Played" notifications
    /// from the EventInstance and checks to see if the Sound played is one for which interest has
    /// been registered (via the KoreographySet). This continues until the FMODEventInstanceVisor
    /// is manually stopped (via Reset()) or receives either a STOPPED or DESTROYED callback from
    /// the FMOD instance it watches.
    /// </summary>
    public class FMODEventInstanceVisor : IKoreographedPlayer
    {
        #region Fields


        /// <summary>
        /// The FMOD Studio EventInstance to watch.
        /// </summary>
        EventInstance eventInstance;

        /// <summary>
        /// The Koreographer component to use for event triggering.
        /// </summary>
        Koreographer koreographerCom = null;

        /// <summary>
        /// The set of Koreography that this visor is monitoring.
        /// </summary>
        [SerializeField]
        FMODKoreographySet koreoSet = null;

        /// <summary>
        /// Tracks allocated layers to cut down on in-game allocations (pooling).
        /// </summary>
        Stack<FMODChannelVisor> availableVisors = new Stack<FMODChannelVisor>();

        /// <summary>
        /// Active visors are updated in Update().
        /// </summary>
        List<FMODChannelVisor> activeVisors = new List<FMODChannelVisor>();

        /// <summary>
        /// Whether or not the visor is awaiting the FMOD Studio POSTUPDATE phase callback or not.
        /// </summary>
        bool needsPostUpdate = false;

        /// <summary>
        /// Whether or not the visor needs to scan the Event Instance's Channel hierarchy for
        /// Sounds to watch.
        /// </summary>
        bool needsReset = false;

        /// <summary>
        /// <para>A list of partially activated visors used for capturing state on the callback
        /// threads and ferrying that state to Unity's main thread.</para>
        /// <para>WARNING: Access this behind a lock!</para>
        /// </summary>
        List<FMODChannelVisor> activatingVisors = new List<FMODChannelVisor>();

        /// <summary>
        /// <para>A temporary list used for copying the <c>activeVisors</c> list on the callback
        /// threads. This is one location where things can get weird due to threads.</para>
        /// <para>WARNING: Access this behind a lock!</para>
        /// </summary>
        List<FMODChannelVisor> activeVisorsCallbackCopy = new List<FMODChannelVisor>();

        /// <summary>
        /// The object used for thread locking during callbacks (and to protect specific resource
        /// access on the main thread). This is used instead of a reference to a particular List
        /// for clarity.
        /// </summary>
        readonly object CallbackLock = new object();


        #endregion
        #region Constructors / Finalizer


        ~FMODEventInstanceVisor()
        {
            FMODCallbackHandler.Instance.UnregisterForPostUpdateCallback(this);
            FMODCallbackHandler.Instance.UnregisterForEventInstanceCallback(this);
        }


        #endregion
        #region Methods


        /// <summary>
        /// Retrieves the FMOD EventInstance being watched by this visor.
        /// </summary>
        /// <returns>The visor's FMOD EventInstance.</returns>
        public EventInstance GetEventInstance()
        {
            return eventInstance;
        }

        /// <summary>
        /// Grabs a Channel Visor from the pool (or creates a new one if the pool is empty).
        /// </summary>
        /// <returns>A valid Channel Visor instance.</returns>
        FMODChannelVisor GetAvailableChannelVisor()
        {
            FMODChannelVisor visor = null;

            if (availableVisors.Count > 0)
            {
                visor = availableVisors.Pop();
            }
            else
            {
                visor = new FMODChannelVisor();
            }

            return visor;
        }

        /// <summary>
        /// Returns a Channel Visor to the 'available' pool.
        /// </summary>
        /// <param name="visor">The Channel Visor to return to the pool.</param>
        void DeactivateChannelVisor(FMODChannelVisor visor)
        {
            visor.Reset();

            activeVisors.Remove(visor);
            availableVisors.Push(visor);
        }

        /// <summary>
        /// Returns all 'active' Channel Visors to the 'available' pool.
        /// </summary>
        void DeactivateActiveChannelVisors()
        {
            // Reset and return all FMODChannelVisors.
            int numActive = activeVisors.Count;
            for (int i = 0; i < numActive; ++i)
            {
                FMODChannelVisor visor = activeVisors[i];

                visor.Reset();
                availableVisors.Push(visor);
            }

            // Clear out the active Channel visors.
            activeVisors.Clear();
        }

        /// <summary>
        /// Initialize the FMODEventInstanceVisor.
        /// </summary>
        /// <param name="targetInstance">The EventInstance instance to watch.</param>
        /// <param name="koreographySet">The set of Koreography to be watched by this Visor.
        /// </param>
        /// <param name="targetKoreographer">An optional Koreographer component to which to send
        /// messages.</param>
        /// <param name="manualCallbacks">Whether the EventInstance callbacks will be handled by
        /// the caller or not.</param>
        public void Initialize(EventInstance targetInstance, FMODKoreographySet koreographySet, Koreographer targetKoreographer = null)
        {
            eventInstance = targetInstance;
            koreoSet = koreographySet;
            koreographerCom = targetKoreographer;

            // We do not know if the targetInstance was already playing prior to this so we do a
            //  search for items of interest to be safe.
            SearchForChannelsToWatch(activeVisors, activeVisors);

            FMODCallbackHandler.Instance.RegisterForEventInstanceCallback(this);
        }

        /// <summary>
        /// Resets the FMODEventInstanceVisor to its initial state (or near enough).
        /// </summary>
        public void Reset()
        {
            // Unregister callbacks (safer to be pedantic here).
            FMODCallbackHandler.Instance.UnregisterForEventInstanceCallback(this);

            // Clear relevant data.
            eventInstance.clearHandle();
            koreoSet = null;
            koreographerCom = null;

            // Ensure no ChannelVisors remain active.
            DeactivateActiveChannelVisors();

            // No longer need resetting.
            needsReset = false;
        }

        /// <summary>
        /// Updates all internal ChannelVisors, which may result in Koreography Event triggering.
        /// </summary>
        public void Update()
        {
            // Check if the reset flag has been scheduled. This helps handle Reset requests from
            //  callbacks on non-main threads.
            if (needsReset)
            {
                Reset();
            }

            // Check if there are activating visors (prepared on callback threads) that need to be
            //  fully activated.
            if (activatingVisors.Count > 0)
            {
                lock (CallbackLock)
                {
                    activeVisors.AddRange(activatingVisors);
                    activatingVisors.Clear();
                }
            }

            // Get data used by visors during update.
            FMOD.ChannelGroup evtGroup;
            eventInstance.getChannelGroup(out evtGroup);
            float speed;
            evtGroup.getPitch(out speed);

            int numActive = activeVisors.Count;
            // Process in reverse order so that we can safely deactivate visors that are no longer
            //  running. This operation modifies the activeVisors list iterating in reverse order
            //  ensures that removal has no effect on the loop setup. If for some reason we need
            //  (or otherwise want) to process these in a forward direction, we must be sure to
            //  properly decrement the `numActive` and `i` variables so that the loop correctly
            //  covers all active visors.
            for (int i = numActive - 1; i >= 0; --i)
            {
                FMODChannelVisor visor = activeVisors[i];
                FMODChannelVisor.PlaybackStatus status = visor.ProcessUpdate(evtGroup, speed);
                if (status == FMODChannelVisor.PlaybackStatus.Stopped)
                {
                    DeactivateChannelVisor(visor);
                }
            }
        }

        /// <summary>
        /// Whether or not the watched EventInstance is in a playing state.
        /// </summary>
        /// <returns><c>true</c> if playing or paused, <c>false</c> if stopped.</returns>
        public bool IsEventInstancePlaying()
        {
            if (eventInstance.isValid())
            {
                PLAYBACK_STATE playbackState;
                eventInstance.getPlaybackState(out playbackState);
                return (playbackState != FMOD.Studio.PLAYBACK_STATE.STOPPED);
            }
            return false;
        }

        /// <summary>
        /// Handles callbacks targeting the watched EventInstance.
        /// </summary>
        /// <param name="type">The type of callback being handled.</param>
        /// <param name="parameters">Any pootential parameters provided by the callback.</param>
        public void OnEventInstanceCallback(EVENT_CALLBACK_TYPE type, IntPtr parameters)
        {
            if (type == EVENT_CALLBACK_TYPE.SOUND_PLAYED)
            {
                lock (CallbackLock)
                {
                    // No need to do any comparison work if we already know that we have a sound to update
                    //  from this frame. This also stops us from requesting scheduling more than once.
                    if (!needsPostUpdate)
                    {
                        FMOD.Sound sound = new FMOD.Sound(parameters);
                        Koreography koreo = koreoSet.GetKoreoEntryForSound(sound);

                        if (koreo != null)
                        {
                            needsPostUpdate = true;
                            FMODCallbackHandler.Instance.RegisterForPostUpdateCallback(this);
                        }
                    }
                }
            }
            else if (type == EVENT_CALLBACK_TYPE.STOPPED)
            {
                needsReset = true;
            }
            else if (type == EVENT_CALLBACK_TYPE.DESTROYED)
            {
                needsReset = true;
            }
        }

        /// <summary>
        /// This method MUST be called in the FMODStudioSystem's POSTUPDATE phase. It is
        /// responsible for identifying newly created Channels to watch within the purview of the
        /// EventInstance watched by this FMODEventInstanceVisor. It is expected to only be called
        /// by the FMODCallbackHandler singleton.
        /// </summary>
        internal void OnPostUpdate()
        {
            lock (CallbackLock)
            {
                activeVisorsCallbackCopy.AddRange(activeVisors);

                SearchForChannelsToWatch(activeVisorsCallbackCopy, activatingVisors);

                activeVisorsCallbackCopy.Clear();

                // Unregister ourselves for the PostUpdate callback.
                needsPostUpdate = false;
                FMODCallbackHandler.Instance.UnregisterForPostUpdateCallback(this);
            }
        }

        /// <summary>
        /// Searches the EventInstance ChannelGroup/Channel hierarchy looking for unwatched
        /// Channels to watch.
        /// </summary>
        /// <param name="watchedVisors">A list of visors that are currently being watched.</param>
        /// <param name="visorsActivated">A list into which to put newly activated visors.</param>
        void SearchForChannelsToWatch(List<FMODChannelVisor> watchedVisors, List<FMODChannelVisor> visorsActivated)
        {
            // Scan for Channels containing sounds that we care about.
            FMOD.ChannelGroup mainGroup;
            eventInstance.getChannelGroup(out mainGroup);
            SearchChannelGroups(mainGroup, watchedVisors, visorsActivated);
        }

        /// <summary>
        /// Searches all Channels (and Subgroups) for newly created Sounds that we're interested
        /// in (based on Koreography contained within the provided KoreographySet).
        /// </summary>
        /// <param name="group">The group to search for Channels (and subgroups).</param>
        /// <param name="watchedVisors">A list of visors that are currently being watched.</param>
        /// <param name="visorsActivated">A list into which to put newly activated visors.</param>
        void SearchChannelGroups(FMOD.ChannelGroup group, List<FMODChannelVisor> watchedVisors, List<FMODChannelVisor> visorsActivated)
        {
            int numChannels;
            group.getNumChannels(out numChannels);

            // Check the channels in this ChannelGroup.
            for (int chanIdx = 0; chanIdx < numChannels; ++chanIdx)
            {
                FMOD.Channel channel;
                group.getChannel(chanIdx, out channel);

                FMOD.Sound sound;
                channel.getCurrentSound(out sound);

                bool bHandled = false;

                // Check if this Channel is already being watched or not.
                int numWatched = watchedVisors.Count;
                for (int watchedIdx = 0; watchedIdx < numWatched; ++watchedIdx)
                {
                    if (watchedVisors[watchedIdx].IsWatchingOrWillWatchChannel(group, channel, sound))
                    {
                        bHandled = true;
                        break;
                    }
                }

                if (!bHandled)
                {
                    // Check if the Sound is one we care about.
                    Koreography koreo = koreoSet.GetKoreoEntryForSound(sound);
                    if (koreo != null)
                    {
                        FMODChannelVisor visor = GetAvailableChannelVisor();
                        visor.Init(group, channel, sound, koreo.SourceClipName, koreo.SampleRate, koreographerCom);
                        visorsActivated.Add(visor);
                    }
                }
            }

            int numGroups;
            group.getNumGroups(out numGroups);

            // Check the ChannelGroups in this ChannelGroup.
            for (int grpIdx = 0; grpIdx < numGroups; ++grpIdx)
            {
                FMOD.ChannelGroup subGroup;
                group.getGroup(grpIdx, out subGroup);

                SearchChannelGroups(subGroup, watchedVisors, visorsActivated);
            }
        }


        #endregion
        #region IKoreographedPlayer Interface Methods


        /// <summary>
        /// Retrieves the playback sample position of the first playing instance of the Sound
        /// specified by <paramref name="clipName"/> in the monitored Event Instance.
        /// </summary>
        /// <param name="clipName">The name of the Sound to check.</param>
        /// <returns>The current sample position of the specified Sound. Will return <c>0</c> if
        /// the specified Sound is not found.</returns>
        public int GetSampleTimeForClip(string clipName)
        {
            int sampleTime = 0;

            int numActive = activeVisors.Count;
            for (int i = 0; i < numActive; ++i)
            {
                FMODChannelVisor visor = activeVisors[i];
                if (clipName == visor.GetSoundName())
                {
                    sampleTime = visor.GetPlaybackSamplePosition();
                    break;
                }
            }

            return sampleTime;
        }

        /// <summary>
        /// Gets the total sample time for the Sound with name <paramref name="clipName"/>.
        /// </summary>
        /// <param name="clipName">The name of the Sound to check.</param>
        /// <returns>The total sample time for the Sound with name <paramref name="clipName"/>, or
        /// <c>0</c> if the Sound is not found.</returns>
        public int GetTotalSampleTimeForClip(string clipName)
        {
            int totalTime = 0;

            int numActive = activeVisors.Count;
            for (int i = 0; i < numActive; ++i)
            {
                FMODChannelVisor visor = activeVisors[i];
                if (clipName == visor.GetSoundName())
                {
                    totalTime = visor.GetSoundSampleLength();
                    break;
                }
            }

            return totalTime;
        }

        /// <summary>
        /// Determines whether the Sound with name <paramref name="clipName"/> is playing.
        /// </summary>
        /// <param name="clipName">The name of the Sound to check.</param>
        /// <returns><c>true</c> if the Sound with name <paramref name="clipName"/> is playing,
        /// <c>false</c> otherwise.</returns>
        public bool GetIsPlaying(string clipName)
        {
            int numActive = activeVisors.Count;
            for (int i = 0; i < numActive; ++i)
            {
                FMODChannelVisor visor = activeVisors[i];
                if (clipName == visor.GetSoundName())
                {
                    return visor.GetIsPlaying();
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the playback speed (collectively referred to as pitch) of the Sound specified by
        /// <paramref name="clipName"/> in the monitored Event Instance. Will return <c>1f</c> if
        /// not found.
        /// </summary>
        /// <param name="clipName">The name of the Sound to check.</param>
        /// <returns>The speed (pitch) of the first playing instance of the Sound indicated by
        /// <paramref name="clipName"/>. If the Sound is not found, this will return <c>1f</c>
        /// </returns>
        public float GetPitch(string clipName)
        {
            int numActive = activeVisors.Count;
            for (int i = 0; i < numActive; ++i)
            {
                FMODChannelVisor visor = activeVisors[i];
                if (clipName == visor.GetSoundName())
                {
                    return visor.GetPlaybackSpeed();
                }
            }

            return 1f;
        }

        /// <summary>
        /// Gets the name of the current Sound. In practice this is the first Sound found to be
        /// actively monitored within the monitored Event Instance if any.
        /// </summary>
        /// <returns>The name of the currently playing Sound (or the empty string if no Sounds are
        /// playing).</returns>
        public string GetCurrentClipName()
        {
            return activeVisors.Count > 0 ? activeVisors[0].GetSoundName() : string.Empty;
        }


        #endregion
    }
}
