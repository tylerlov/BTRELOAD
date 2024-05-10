//----------------------------------------------
//            	   Koreographer                 
//    Copyright © 2014-2021 Sonic Bloom, LLC    
//----------------------------------------------

using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace SonicBloom.Koreo.Players.FMODStudio
{
    /// <summary>
    /// The FMODEventDescriptionVisor is the FMOD Integration's closest thing to a "Super Visor".
    /// It provides a simple interface for allowing every single instance of an FMOD Event to be
    /// Koreographed, regardless of startup approach.
    /// </summary>
    [AddComponentMenu("Koreographer/FMOD Studio/FMOD Event Description Visor")]
    public class FMODEventDescriptionVisor : MonoBehaviour, IKoreographedPlayer
    {
        #region Custom Structs


        /// <summary>
        /// A pairing of FMOD Event Description to an FMODKoreographySet instance.
        /// </summary>
        [Serializable]
        struct EventDescPair
        {
            public EventReference Event;
            public FMODKoreographySet koreographySet;
            [NonSerialized]
            public EventDescription description;
        }


        #endregion
        #region Fields


        /// <summary>
        /// The set of Event Description and Koreography Set pairings for this visor to manage.
        /// </summary>
        [SerializeField]
        [Tooltip("Pairs of FMOD Studio Events and Koreography Sets. Whenever a specified Event is played, it will be Koreographed using the Koreography found in the Koreography Set.")]
        List<EventDescPair> koreographedEvents = new List<EventDescPair>();

        /// <summary>
        /// The target Koreographer component to use for event triggering.
        /// </summary>
        [SerializeField]
        [Tooltip("A specific Koreographer component to use for event triggering. If not specified, the singleton [global] instance will be used.")]
        Koreographer targetKoreographer = null;

        /// <summary>
        /// Whether or not this component should register itself as the music player for the target
        /// Koreographer component.
        /// </summary>
        [SerializeField]
        [Tooltip("Whether or not this visor should act as the Music Player for the Target Koreographer component (or the singleton [global] instance if none is specified).")]
        bool isMusicPlayer = false;

        /// <summary>
        /// Tracks allocated visors to cut down on in-game allocations (pooling).
        /// </summary>
        Stack<FMODEventInstanceVisor> availableVisors = new Stack<FMODEventInstanceVisor>();

        /// <summary>
        /// Active visors are updated in Update().
        /// </summary>
        List<FMODEventInstanceVisor> activeVisors = new List<FMODEventInstanceVisor>();


        #endregion
        #region Methods


        /// <summary>
        /// Loads the Koreography specified in the FMODKoreographySet(s) into the target
        /// Koreographer.
        /// </summary>
        void LoadKoreography()
        {
            if (targetKoreographer != null)
            {
                int numDescs = koreographedEvents.Count;
                for (int i = 0; i < numDescs; ++i)
                {
                    FMODKoreographySet set = koreographedEvents[i].koreographySet;
                    if (set != null)
                    {
                        List <FMODKoreoEntry> koreographies = set.koreographies;
                        int numKoreo = koreographies.Count;
                        for (int j = 0; j < numKoreo; ++j)
                        {
                            targetKoreographer.LoadKoreography(koreographies[j].koreo);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unloads the Koreography specified in the FMODKoreographySet(s) from the target
        /// Koreographer.
        /// </summary>
        void UnloadKoreography()
        {
            if (targetKoreographer != null)
            {
                int numDescs = koreographedEvents.Count;
                for (int i = 0; i < numDescs; ++i)
                {
                    FMODKoreographySet set = koreographedEvents[i].koreographySet;
                    if (set != null)
                    {
                        List <FMODKoreoEntry> koreographies = set.koreographies;
                        int numKoreo = koreographies.Count;
                        for (int j = 0; j < numKoreo; ++j)
                        {
                            targetKoreographer.UnloadKoreography(koreographies[j].koreo);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Grabs an EventInstance Visor from the pool (or creates a new one if the pool is empty).
        /// </summary>
        /// <returns>A valid EventInstance visor instance.</returns>
        FMODEventInstanceVisor GetAvailableEventInstanceVisor()
        {
            FMODEventInstanceVisor visor = null;

            if (availableVisors.Count > 0)
            {
                visor = availableVisors.Pop();
            }
            else
            {
                visor = new FMODEventInstanceVisor();
            }

            return visor;
        }

        /// <summary>
        /// Returns an EventInstance Visor to the 'available' pool.
        /// </summary>
        /// <param name="visor">The EventInstance Visor to return to the pool.</param>
        void DeactivateEventInstanceVisor(FMODEventInstanceVisor visor)
        {
            visor.Reset();

            activeVisors.Remove(visor);
            availableVisors.Push(visor);
        }

        /// <summary>
        /// Returns all 'active' EventInstance Visors to the 'available' pool.
        /// </summary>
        void DeactivateAllEventInstanceVisors()
        {
            // Reset and return all FMODEventInstanceVisors.
            int numActive = activeVisors.Count;
            for (int i = 0; i < numActive; ++i)
            {
                FMODEventInstanceVisor visor = activeVisors[i];

                visor.Reset();
                availableVisors.Push(visor);
            }

            // Clear out the active EventInstance visors.
            activeVisors.Clear();
        }

        /// <summary>
        /// Initializes the EventDescriptions under the visor's purview and registers for
        /// callbacks.
        /// </summary>
        void Awake()
        {
            // "Lookup" all event descriptions and set callbacks. These should be for something like
            //  "created".
            int numEvts = koreographedEvents.Count;
            for (int i = 0; i < numEvts; ++i)
            {
                EventDescPair item = koreographedEvents[i];

                // Get and store the EventDescription.
                item.description = RuntimeManager.GetEventDescription(item.Event);

                // Store the updated item.
                koreographedEvents[i] = item;
            }

            // Register this visor for callbacks. Registering the visor will also instruct the
            //  handler to provide it with the callback to apply to the EventDescription
            //  instances.
            FMODCallbackHandler.Instance.RegisterForEventDescriptionCallback(this);
        }

        /// <summary>
        /// Handles component startup. Koreography loading occurs here.
        /// </summary>
        void Start()
        {
            // Load Koreography.
            if (targetKoreographer == null)
            {
                targetKoreographer = Koreographer.Instance;
            }

            if (targetKoreographer == null)
            {
                Debug.LogWarning("No Koreographer component specified and could not find the " +
                                 "singleton. Please add a Koreographer component to the scene " +
                                 "or make sure to specify a default Koreographer. Disabling " +
                                 "this FMOD Event Description Visor (on GameObject '" +
                                 gameObject.name + ").");
                enabled = false;
                return;
            }
            else
            {
                if (isMusicPlayer)
                {
                    targetKoreographer.musicPlaybackController = this;
                }
                LoadKoreography();
            }
        }

        /// <summary>
        /// Updates the FMODEventInstanceVisor instances, if any are playing.
        /// </summary>
        void Update()
        {
            int numActive = activeVisors.Count;
            // Process in reverse order so that we can safely deactivate visors that are no longer
            //  running. This operation modifies the activeVisors list iterating in reverse order
            //  ensures that removal has no effect on the loop setup. If for some reason we need
            //  (or otherwise want) to process these in a forward direction, we must be sure to
            //  properly decrement the `numActive` and `i` variables so that the loop correctly
            //  covers all active visors.
            for (int i = numActive - 1; i >= 0; --i)
            {
                FMODEventInstanceVisor visor = activeVisors[i];
                visor.Update();

                if (!visor.IsEventInstancePlaying())
                {
                    DeactivateEventInstanceVisor(visor);
                }
            }
        }

        /// <summary>
        /// Handles component clean up. Koreography unloading occurs here.
        /// </summary>
        void OnDestroy()
        {
            FMODCallbackHandler.Instance.UnregisterForEventDescriptionCallback(this);

            DeactivateAllEventInstanceVisors();

            UnloadKoreography();

            // Reset Music Playback Controller, if necessary.
            if (isMusicPlayer && targetKoreographer != null &&
                targetKoreographer.musicPlaybackController == (IKoreographedPlayer)this)
            {
                targetKoreographer.musicPlaybackController = null;
            }
        }

        /// <summary>
        /// Determines whether this visor has any interest in the provided EventInstance. If so,
        /// the visor will "take ownership" of the EventInstance and initialize an
        /// FMODEventInstanceVisor on its behalf.
        /// </summary>
        /// <param name="desc">The EventDescription of the provided EventInstance.</param>
        /// <param name="inst">The EventInstance to possibly watch.</param>
        /// <returns><c>true</c> if the visor will watch the EventInstance, <c>false</c> otherwise.
        /// </returns>
        public bool WillWatchEventInstanceOfDescription(EventDescription desc, EventInstance inst)
        {
            int numEvts = koreographedEvents.Count;
            for (int i = 0; i < numEvts; ++i)
            {
                EventDescPair item = koreographedEvents[i];
                if (desc.handle == item.description.handle)
                {
                    // Grab available visor.
                    FMODEventInstanceVisor visor = GetAvailableEventInstanceVisor();
                    // Initialize an EventInstance visor.
                    visor.Initialize(inst, item.koreographySet, targetKoreographer);
                    // Watch the visor!
                    activeVisors.Add(visor);

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Applies the specified callback to all EventDescriptions under the purview of this
        /// FMODEventDescriptionVisor.
        /// </summary>
        /// <param name="cb"></param>
        /// <param name="mask"></param>
        public void SetDescriptionCallbacks(EVENT_CALLBACK cb, EVENT_CALLBACK_TYPE mask)
        {
            int numEvts = koreographedEvents.Count;
            for (int i = 0; i < numEvts; ++i)
            {
                koreographedEvents[i].description.setCallback(cb, mask);
            }
        }


        #endregion
        #region IKoreographedPlayer Interface Methods


        /// <summary>
        /// Retrieves the current sample position of the first playing instance of the Sound
        /// specified by <paramref name="clipName"/> amongst the active Event Instances being
        /// monitored by this visor.
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
                sampleTime = activeVisors[i].GetSampleTimeForClip(clipName);

                // We know we've had a response if the time is non-zero.
                if (sampleTime > 0)
                {
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
                totalTime = activeVisors[i].GetTotalSampleTimeForClip(clipName);

                // We know we've had a response if the time is non-zero.
                if (totalTime > 0)
                {
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
                if (activeVisors[i].GetIsPlaying(clipName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the pitch of the first playing instance of the Sound specified by
        /// <paramref name="clipName"/>amongst the active Event Instances being monitored by this
        /// visor that is not <c>1f</c>. If the Sound specified is found but found to have a pitch
        /// value of <c>1f</c>, then this will continue searching for a playing instance that is
        /// not <c>1f</c>. Will return <c>1f</c> if the Sound is not found.
        /// </summary>
        /// <param name="clipName"></param>
        /// <returns>The pitch of the first non-<c>1f</c> playing instance of the Sound indicated
        /// by <paramref name="clipName"/>. If the Sound is not found, this will return <c>1f</c>.
        /// </returns>
        public float GetPitch(string clipName)
        {
            float pitch = 1f;

            int numActive = activeVisors.Count;
            for (int i = 0; i < numActive; ++i)
            {
                pitch = activeVisors[i].GetPitch(clipName);

                // We know we've had a response if the pitch is non-one.
                if (pitch != 1f)
                {
                    break;
                }
            }

            return pitch;
        }

        /// <summary>
        /// Gets the name of the current Sound. In practice this is the first Sound found to be
        /// actively monitored within the first Event Instance being monitored by this visor, if
        /// any.
        /// </summary>
        /// <returns>The name of the currently playing Sound (or the empty string if no Sounds are
        /// playing).</returns>
        public string GetCurrentClipName()
        {
            return activeVisors.Count > 0 ? activeVisors[0].GetCurrentClipName() : string.Empty;
        }


        #endregion
    }
}
