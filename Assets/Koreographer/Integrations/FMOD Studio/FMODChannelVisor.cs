//----------------------------------------------
//            	   Koreographer                 
//    Copyright © 2014-2021 Sonic Bloom, LLC    
//----------------------------------------------

using System.Collections.Generic;


namespace SonicBloom.Koreo.Players.FMODStudio
{
    /// <summary>
    /// <para>
    /// The FMODChannelVisor is the heart of the FMOD Integration. It performs all of the heavy
    /// lifting of sound file position monitoring. This is accomplished by monitoring up to two
    /// FMOD Sound instances played through a single FMOD Channel (thus "Channel Visor"). This
    /// allows the FMODChannelVisor to detect seeks within the same sound file.
    /// </para>
    /// <para>
    /// As a low level visor, it also performs its own environment-appropriate estimation based
    /// upon reported read position updates. It is also [mostly?] prepared for using the default
    /// estimation system, should the need arise.
    /// </para>
    /// </summary>
    public class FMODChannelVisor
    {
        #region Enumerations


        /// <summary>
        /// Enumeration used to describe the playback status of the Visor.
        /// </summary>
        public enum PlaybackStatus
        {
            Stopped,
            Playing,
            Paused,
        }


        #endregion
        #region Structs


        /// <summary>
        /// Data type used to track sample updates between frames.
        /// </summary>
        struct SampleRangeRecord
        {
            /// <summary>
            /// The first played sample in the record.
            /// </summary>
            public int firstSample;
            /// <summary>
            /// The last played sample in the record.
            /// </summary>
            public int lastSample;
            /// <summary>
            /// The playback speed of samples in the record.
            /// </summary>
            public float speed;

            /// <summary>
            /// Creates a new record.
            /// </summary>
            /// <param name="first">The first sample of the new record.</param>
            /// <param name="last">The last sample of the new record.</param>
            /// <param name="spd">The speed of the samples in the new record.</param>
            public SampleRangeRecord(int first, int last, float spd)
            {
                firstSample = first;
                lastSample = last;
                speed = spd;
            }

            /// <summary>
            /// Whether this record is considered empty or not.
            /// </summary>
            /// <returns><c>true</c> if this record is empty, <c>false</c> otherwise.</returns>
            public bool IsEmpty()
            {
                return lastSample == -1;
            }

            /// <summary>
            /// Sets the record to be considered "empty".
            /// </summary>
            public void SetEmpty()
            {
                lastSample = -1;
            }
        }

        #endregion
        #region Data Fields


        /// <summary>
        /// The parent ChannelGroup for Channels tracked by this visor.
        /// </summary>
        FMOD.ChannelGroup parentGroup;

        /// <summary>
        /// The Sound data being tracked by this visor.
        /// </summary>
        FMOD.Sound trackedSound;

        /// <summary>
        /// The main channel actively playing.
        /// </summary>
        FMOD.Channel playingChannel;

        /// <summary>
        /// The queued channel, if any, that will begin playing soon.
        /// </summary>
        FMOD.Channel queuedChannel;

        /// <summary>
        /// The name of the audio file being played in the tracked channel instance(s).
        /// </summary>
        string audioName;

        /// <summary>
        /// The total length of the sound in samples. This value is driven by the runtime sample
        /// rate (not normalized).
        /// </summary>
        int soundSampleLength = 0;


        #endregion
        #region Runtime Fields


        /// <summary>
        /// The Koreographer to report audio time updates to.
        /// </summary>
        Koreographer koreographerCom = null;

        /// <summary>
        /// The current sound's sample rate (aka frequency).
        /// </summary>
        int soundSampleRate = 0;

        /// <summary>
        /// The most recent audio position received from FMOD in samples. This value is driven by
        /// the runtime sample rate (not normalized).
        /// </summary>
        int fmodSampleTime = -1;

        /// <summary>
        /// Whether or not a loop has been detected.
        /// </summary>
        bool bLoopDetected = false;

        /// <summary>
        /// Whether or not playback is currently paused.
        /// </summary>
        PlaybackStatus playbackStatus = PlaybackStatus.Stopped;

        /// <summary>
        /// The sample position at which playback started. This value is normalized to the original
        /// sample rate stored in the relevant Koreography.
        /// </summary>
        int soundPlaybackStartSample = 0;

        /// <summary>
        /// The sample position at which playback will end (or at which a loop will occur). This
        /// value is normalized to the original sample rate stored in the relevant Koreography.
        /// </summary>
        int soundPlaybackEndSample = 0;

        /// <summary>
        /// The current playback speed of the Channel.
        /// </summary>
        float playbackSpeed = 1f;

        /// <summary>
        /// A queue for buffering sample position updates from the underlying audio engine. Records
        /// contained within are used for processing Koreography updates.
        /// </summary>
        Queue<SampleRangeRecord> playbackBuffer = new Queue<SampleRangeRecord>(4);

        /// <summary>
        /// The Sample Range Record being actively consumed. Possibly empty.
        /// </summary>
        SampleRangeRecord recordInProcess = new SampleRangeRecord(-1, -1, 1f);

        /// <summary>
        /// The most recent processed audio playback sample position. This value is normalized to
        /// the original sample rate stored in the relevant Koreography.
        /// </summary>
        int processedPlaybackSampleTime = -1;

        /// <summary>
        /// The ratio of playback (runtime) sample rate to recorded (edit time) sample rate.
        /// </summary>
        double playbackSampleRateRatio = 1d;


        #endregion
        #region Initializers


        /// <summary>
        /// Initializes the <c>FMODChannelVisor</c>.
        /// </summary>
        /// <param name="_group">The FMOD ChannelGroup parent of the Channel(s) that this visor
        /// will track.</param>
        /// <param name="_channel">The [initial] FMOD Channel that this visor will track.</param>
        /// <param name="_sound">The FMOD Sound used by the Channel(s) that this visor will track.
        /// </param>
        /// <param name="_audioName">The name of the audio file being played back.</param>
        /// <param name="originalSampleRate">The original sample rate of the sound as recorded
        /// during Koreography creation. Possibly different from runtime sample rate.</param>
        /// <param name="targetKoreographer">The Koreographer to use. If not specified, uses the
        /// default singleton.</param>
        public void Init(FMOD.ChannelGroup _group, FMOD.Channel _channel, FMOD.Sound _sound, string _audioName, int originalSampleRate, Koreographer targetKoreographer = null)
        {
            // Setting up data.
            parentGroup = _group;
            trackedSound = _sound;
            audioName = _audioName;

            // Set sound frequency defaults.
            {
                float frequency;
                int dummy;
                _sound.getDefaults(out frequency, out dummy);
                soundSampleRate = (int)frequency;
            }

            // Set sound playback sample rate ratio.
            {
                playbackSampleRateRatio = (double)originalSampleRate / (double)soundSampleRate;
            }

            // Initialize channel state.
            {
                bool isPlaying = false;
                _channel.isPlaying(out isPlaying);
                if (isPlaying)
                {
                    playingChannel = _channel;
                }
                else
                {
                    queuedChannel = _channel;
                }
            }

            // Approximate end extent.
            {
                uint totalLength;
                _sound.getLength(out totalLength, FMOD.TIMEUNIT.PCM);
                soundSampleLength = (int)((double)totalLength * playbackSampleRateRatio);
                soundPlaybackEndSample = soundSampleLength - 1;
            }

            // Initialize extents based on Loop Points. Defaults to full extents so this should be safe.
            SetExtentsToLoopExtents();

            // Set initial position.
            {
                uint position;
                _channel.getPosition(out position, FMOD.TIMEUNIT.PCM);
                fmodSampleTime = (int)position;
            }

            // Koreographer connections.
            koreographerCom = targetKoreographer != null ? targetKoreographer : Koreographer.Instance;
        }

        /// <summary>
        /// Resets this <c>FMODChannelVisor</c> instance for reuse.
        /// </summary>
        public void Reset()
        {
            // Reset data fields.
            parentGroup.clearHandle();
            trackedSound.clearHandle();
            playingChannel.clearHandle();
            queuedChannel.clearHandle();
            audioName = string.Empty;
            soundSampleLength = 0;

            // Reset runtime fields.
            fmodSampleTime = -1;
            playbackBuffer.Clear();
            recordInProcess.SetEmpty();
            processedPlaybackSampleTime = -1;
            playbackSampleRateRatio = 1d;

            // Koreographer connections.
            koreographerCom = null;
        }

        /// <summary>
        /// Checks if the visor is watching or will watch the provided Channel instance. The Sound and
        /// ChannelGroup are used to verify whether this visor instance should potentially queue the
        /// Channel as a transition target.
        /// </summary>
        /// <param name="_group">The FMOD ChannelGroup parent of the Channel to check.</param>
        /// <param name="_channel">The FMOD Channel to check.</param>
        /// <param name="_sound">The FMOD Sound used by the provided Channel.</param>
        /// <returns><c>true</c> if the visor instance is or will watch the provided Channel instance,
        /// <c>false</c> otherwise.</returns>
        public bool IsWatchingOrWillWatchChannel(FMOD.ChannelGroup _group, FMOD.Channel _channel, FMOD.Sound _sound)
        {
            // Ensure the data we're comparing against is valid.
            if (!parentGroup.hasHandle() || !playingChannel.hasHandle() || !trackedSound.hasHandle())
            {
                return false;
            }

            // Ensure that we're not already tracking the channel in question.
            if (playingChannel.handle == _channel.handle ||
                (queuedChannel.hasHandle() && queuedChannel.handle == _channel.handle))
            {
                return true;
            }

            // Ensure the Sounds are the same. This should have been checked prior to this getting called,
            //  but the added caution shouldn't be harmful.
            if (trackedSound.handle != _sound.handle)
            {
                return false;
            }

            // Ensure the Channels have the same parent ChannelGroup. Jumps/loops should not occur (??)
            //  for sounds from different groups.
            if (parentGroup.handle != _group.handle)
            {
                return false;
            }

            ulong delayed_start, delayed_end, dummy;
            playingChannel.getDelay(out dummy, out delayed_end);

            // If our playing channel doesn't have a scheduled end, then this isn't a jump or loop.
            if (delayed_end == 0)
            {
                return false;
            }

            _channel.getDelay(out delayed_start, out dummy);

            // If our playing channel ends at a different time than the new one starts, then this isn't
            //  a jump or loop.
            if (delayed_end != delayed_start)
            {
                return false;
            }

            // Loop/jump *very* likely detected! Queue it up!
            queuedChannel = _channel;

            // Also update our upcoming Start Extent.
            uint pos;
            queuedChannel.getPosition(out pos, FMOD.TIMEUNIT.PCM);
            soundPlaybackStartSample = (int)((double)pos * playbackSampleRateRatio);

            return true;
        }

        /// <summary>
        /// Sets the playback extents of the watched Channel(s) to match the Loop Points.
        /// </summary>
        void SetExtentsToLoopExtents()
        {
            uint loopstart = (uint)soundPlaybackStartSample;
            uint loopend = (uint)soundPlaybackEndSample;
            if (playingChannel.hasHandle())
            {
                playingChannel.getLoopPoints(out loopstart, FMOD.TIMEUNIT.PCM, out loopend, FMOD.TIMEUNIT.PCM);
            }
            else if (queuedChannel.hasHandle())
            {
                // Use the Queued Channel as a fallback.
                queuedChannel.getLoopPoints(out loopstart, FMOD.TIMEUNIT.PCM, out loopend, FMOD.TIMEUNIT.PCM);
            }
            // else { keep as is }
            soundPlaybackStartSample = (int)((double)loopstart * playbackSampleRateRatio);
            soundPlaybackEndSample = (int)((double)loopend * playbackSampleRateRatio);
        }


        #endregion
        #region Methods


        /// <summary>
        /// Checks the state of the watched Channel instance(s) for sample position updates and
        /// processes sample updates, calling ProcessKoreography as necessary. This is a very
        /// custom Visor implementation that takes advantage of the deep insight available through
        /// FMOD Core APIs (combined with certain "guaranteed" usage patterns provided by FMOD
        /// Studio).
        /// 
        /// This method works by recording position updates into a "playback buffer" and then, in a
        /// second phase, processing that buffer based on unscaled frame time.
        /// </summary>
        /// <param name="rootGroup">The root of the channels' <c>ChannelGroup</c> hierarchy.
        /// </param>
        /// <param name="rootSpeed">The playback speed of the channels' root <c>ChannelGroup</c>
        /// instance.</param>
        /// <returns>The playback status of the Visor.</returns>
        public PlaybackStatus ProcessUpdate(FMOD.ChannelGroup rootGroup, float rootSpeed)
        {
            // Reset any detected loops.
            bLoopDetected = false;

            // Accumulate any sample strides as consumed by FMOD's internal systems, storing the
            //  playback status in the processing.
            PlaybackStatus channelStatus = AccumulatePlaybackBuffer(rootGroup, rootSpeed);

            // Consume playback buffer using unscaled frame time. This will trigger Koreography
            //  processing.
            ConsumePlaybackBuffer();

            // Get the parent playback status.
            PlaybackStatus parentStatus = GetParentGroupPlaybackStatus();

            // Prefer the playing status if we have sample buffer to consume.
            if (!recordInProcess.IsEmpty() || playbackBuffer.Count > 0)
            {
                playbackStatus = PlaybackStatus.Playing;
            }
            // Second preference is the parent status. If stopped or paused, then the channels will
            //  not matter.
            else if (parentStatus != PlaybackStatus.Playing)
            {
                playbackStatus = parentStatus;
            }
            // If everything else is checked, then we verify what the channels are up to.
            else
            {
                playbackStatus = channelStatus;
            }

            return playbackStatus;
        }

        /// <summary>
        /// Record any position updates for tracked channel(s). This is responsible for queueing
        /// <c>SampleRangeRecord</c>s into the playback buffer.
        /// </summary>
        /// <param name="rootGroup">The root of the channels' <c>ChannelGroup</c> hierarchy.
        /// </param>
        /// <param name="rootSpeed">The playback speed of the channels' root <c>ChannelGroup</c>
        /// instance.</param>
        /// <returns>The playback status of the tracked channel(s).</returns>
        PlaybackStatus AccumulatePlaybackBuffer(FMOD.ChannelGroup rootGroup, float rootSpeed)
        {
            int lastPositionNormalized = (int)((double)fmodSampleTime * playbackSampleRateRatio);
            bool bPaused, bMainPlaying = false, bQueuedPlaying = false;
            uint position = 0;

            int newPosRaw = fmodSampleTime;

            // Assume paused until shown otherwise.
            PlaybackStatus status = PlaybackStatus.Paused;

            // Check the main playingChannel first.
            if (playingChannel.hasHandle())
            {
                playingChannel.isPlaying(out bMainPlaying);
                playingChannel.getPaused(out bPaused);

                // Check for time update only if in an unpaused playing state.
                if (bMainPlaying && !bPaused)
                {
                    // Confirmed playing.
                    status = PlaybackStatus.Playing;

                    playingChannel.getPosition(out position, FMOD.TIMEUNIT.PCM);
                    newPosRaw = (int)position;

                    float speed = rootSpeed * GetPlaybackSpeedOfChannel(playingChannel, rootGroup);

                    // Update playback speed.
                    playbackSpeed = speed;

                    // Check for looping.
                    if (newPosRaw < fmodSampleTime)
                    {
                        // Grab the loop extents for processing the position updates.
                        uint loopstart, loopend;
                        playingChannel.getLoopPoints(out loopstart, FMOD.TIMEUNIT.PCM, out loopend, FMOD.TIMEUNIT.PCM);
                        int loopstartNormalized = (int)((double)loopstart * playbackSampleRateRatio);
                        int loopendNormalized = (int)((double)loopend * playbackSampleRateRatio);

                        // UnityEngine.Debug.Log($"[{UnityEngine.Time.frameCount}] 1 - Adding {lastPositionNormalized} to {loopendNormalized}");
                        // We've looped. Enqueue to the end.
                        playbackBuffer.Enqueue(new SampleRangeRecord(lastPositionNormalized, (int)loopendNormalized, speed));

                        // Prep for the fall-through.
                        lastPositionNormalized = (int)loopstartNormalized;
                    }

                    // TODO: Handle position-perfect loop?
                    if (newPosRaw != fmodSampleTime)
                    {
                        int newPosNormalized = (int)((double)newPosRaw * playbackSampleRateRatio);
                        // UnityEngine.Debug.Log($"[{UnityEngine.Time.frameCount}] 2 - Adding {lastPositionNormalized} to {newPosNormalized - 1}");
                        // Enqueue to the end.
                        playbackBuffer.Enqueue(new SampleRangeRecord(lastPositionNormalized, newPosNormalized - 1, speed));

                        // Iterate the sample time forward.
                        fmodSampleTime = newPosRaw;
                    }
                }
            }

            // Check if we have a queued loop/jump transition coming.
            if (queuedChannel.hasHandle())
            {
                queuedChannel.isPlaying(out bQueuedPlaying);
                queuedChannel.getPaused(out bPaused);

                // Check if we're playing.
                if (bQueuedPlaying && !bPaused)
                {
                    // Confirmed playing.
                    status = PlaybackStatus.Playing;

                    ulong dspTime, parentDSPTime;
                    queuedChannel.getDSPClock(out dspTime, out parentDSPTime);

                    ulong delayStart, delayEnd;
                    queuedChannel.getDelay(out delayStart, out delayEnd);

                    // Get the time.
                    queuedChannel.getPosition(out position, FMOD.TIMEUNIT.PCM);
                    newPosRaw = (int)position;

                    float speed = rootSpeed * GetPlaybackSpeedOfChannel(queuedChannel, rootGroup);

                    // We're transitioning.
                    if (delayStart <= parentDSPTime)
                    {
                        int newPosNormalized = (int)((double)newPosRaw * playbackSampleRateRatio);

                        // Check for the odd scenario wherein we jump very close to the end of the
                        //  audio file and then loop around within this update.
                        if (newPosNormalized < soundPlaybackStartSample)
                        {
                            // We've jumped to the end but looped around!
                            uint loopstart, loopend;
                            queuedChannel.getLoopPoints(out loopstart, FMOD.TIMEUNIT.PCM, out loopend, FMOD.TIMEUNIT.PCM);
                            int newStartNormalized = (int)((double)loopstart * playbackSampleRateRatio);
                            int newEndNormalized = (int)((double)loopend * playbackSampleRateRatio);

                            // UnityEngine.Debug.Log($"[{UnityEngine.Time.frameCount}] 3 - Adding {soundPlaybackStartSample} to {newEndNormalized}");

                            // Enqueue to the end. When transitioning, the playback start sample
                            //  contains the sample position from which to start.
                            playbackBuffer.Enqueue(new SampleRangeRecord(soundPlaybackStartSample, newEndNormalized, speed));

                            // Iterate the position forward.
                            soundPlaybackStartSample = newStartNormalized;
                            soundPlaybackEndSample = newEndNormalized;
                        }

                        // UnityEngine.Debug.Log($"[{UnityEngine.Time.frameCount}] 4 - Adding {soundPlaybackStartSample} to {newPosNormalized - 1}");

                        // Accumulate the last bit.
                        playbackBuffer.Enqueue(new SampleRangeRecord(soundPlaybackStartSample, newPosNormalized - 1, speed));

                        // Iterate the reported sample time forward.
                        fmodSampleTime = newPosRaw;

                        // Update playback speed.
                        playbackSpeed = speed;

                        // Promote the queued channel to the playing one!
                        playingChannel = queuedChannel;
                        queuedChannel.clearHandle();
                    }
                }
            }

            // Return the detected playback status, properly detecting a "Stopped" state.
            return (bMainPlaying || bQueuedPlaying) ? status : PlaybackStatus.Stopped;
        }

        /// <summary>
        /// Processes the playback buffer (if any exists) using the unscaled frame time as the
        /// amount to consume.
        /// </summary>
        void ConsumePlaybackBuffer()
        {
            if (recordInProcess.IsEmpty())
            {
                if (playbackBuffer.Count > 0)
                {
                    recordInProcess = playbackBuffer.Dequeue();
                    // UnityEngine.Debug.LogWarning($"[{UnityEngine.Time.frameCount}] Dequeued: {recordInProcess.firstSample} to {recordInProcess.lastSample}");
                }
                else
                {
                    return;
                }
            }

            // Run the check.

            double frameTime = GetRawFrameTime();
            double timeLeft = frameTime;

            // TODO: Handle the slice!!
            DeltaSlice slice = new DeltaSlice();
            slice.deltaLength = (float)frameTime;

            int playStartSample = recordInProcess.firstSample;

            while (timeLeft > 0d)
            {
                int totalSamples = recordInProcess.lastSample - recordInProcess.firstSample + 1;
                double timeInRecord = ((double)totalSamples / soundSampleRate) / (double)recordInProcess.speed;

                if (timeLeft < timeInRecord)
                {
                    // Consume and update the recordInProcess.
                    int numSamples = System.Convert.ToInt32(((double)soundSampleRate * timeLeft));
                    // Subtract one because the first sample position is included. This means that
                    //  if we are reading one sample, then start and end should be identical.
                    int playEndSample = recordInProcess.firstSample + numSamples - 1;
                    // UnityEngine.Debug.LogWarning($"[{UnityEngine.Time.frameCount}] A. Processing from: {playStartSample} to {playEndSample}");

                    // Process the Koreography.
                    koreographerCom.ProcessKoreography(audioName, playStartSample, playEndSample, slice);
                    processedPlaybackSampleTime = playEndSample;

                    // Iterate the recordInProcess.
                    recordInProcess.firstSample = playEndSample + 1;

                    // Done. End it.
                    break;
                }
                else if (timeLeft == timeInRecord)
                {
                    // UnityEngine.Debug.LogWarning($"[{UnityEngine.Time.frameCount}] B. Processing from: {playStartSample} to {recordInProcess.lastSample}");
                    // Consume what's left in the record!
                    koreographerCom.ProcessKoreography(audioName, playStartSample, recordInProcess.lastSample, slice);
                    processedPlaybackSampleTime = recordInProcess.lastSample;

                    // Set the sentinel record empty.
                    recordInProcess.SetEmpty();

                    // Done. End it.
                    break;
                }
                else    // timeLeft > timeInRecord
                {
                    if (playbackBuffer.Count > 0)
                    {
                        // Consume time from this record.
                        timeLeft -= timeInRecord;

                        SampleRangeRecord nextRecord = playbackBuffer.Peek();
                        if (recordInProcess.lastSample + 1 != nextRecord.firstSample)
                        {
                            // A jump! Consume the rest of this record and then set us up for the next.

                            // Calculate the overall slice length in seconds.
                            slice.deltaLength = (float)((slice.deltaOffset * frameTime) - timeLeft);

                            // UnityEngine.Debug.LogWarning($"[{UnityEngine.Time.frameCount}] C. Processing from: {playStartSample} to {recordInProcess.lastSample}");
                            // Process the Koreography!
                            koreographerCom.ProcessKoreography(audioName, playStartSample, recordInProcess.lastSample, slice);
                            processedPlaybackSampleTime = recordInProcess.lastSample;

                            // Store the new offset for the beginning of the next slice.
                            slice.deltaOffset = (float)(timeLeft / frameTime);
                            slice.deltaLength = (float)timeLeft;

                            // Update the new start sample position.
                            playStartSample = nextRecord.firstSample;

                            // For now, we consider any jump backwards to be a loop.
                            bLoopDetected = (recordInProcess.lastSample > nextRecord.firstSample);
                        }
                        // else - simple continuation; fall through!

                        // Iterate forward!
                        recordInProcess = playbackBuffer.Dequeue();
                        // UnityEngine.Debug.LogWarning($"[{UnityEngine.Time.frameCount}] Dequeued: {recordInProcess.firstSample} to {recordInProcess.lastSample}");
                    }
                    else
                    {
                        // Consume what's left! There's nowhere left to go here so we'll probably
                        //  stop after this.
                        slice.deltaLength = (float)((slice.deltaOffset * frameTime) - timeLeft);

                        // UnityEngine.Debug.LogError($"[{UnityEngine.Time.frameCount}] D. Processing from: {playStartSample} to {recordInProcess.lastSample}");
                        koreographerCom.ProcessKoreography(audioName, playStartSample, recordInProcess.lastSample, slice);
                        processedPlaybackSampleTime = recordInProcess.lastSample;

                        // Set the sentinel record empty.
                        recordInProcess.SetEmpty();

                        // Done. End it.
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the playback state of the parent ChannelGroup of the tracked Channel(s).
        /// </summary>
        /// <returns>The playback status of the parent ChannelGroup.</returns>
        PlaybackStatus GetParentGroupPlaybackStatus()
        {
            bool bPaused, bPlaying;

            // Skip update if the parent group is paused.
            if (parentGroup.hasHandle())
            {
                // Prefer stopped over paused: being paused and stopped means that we're still
                //  stopped.
                parentGroup.isPlaying(out bPlaying);
                if (!bPlaying)
                {
                    return PlaybackStatus.Stopped;
                }
                parentGroup.getPaused(out bPaused);
                if (bPaused)
                {
                    return PlaybackStatus.Paused;
                }
            }
            // No parent means that we're likely in a broken state. We consider this equivalent to
            //  Stopped.
            else
            {
                return PlaybackStatus.Stopped;
            }

            return PlaybackStatus.Playing;
        }

        /// <summary>
        /// Calculates the effective playback speed of the <paramref name="channel">, rather than
        /// simply the specific speed of the Channel itself (and possibly its parent). This walks
        /// the entire ChannelGroup hierarchy up to either the specified
        /// <paramref name="rootGroup"/> or, if <paramref name="rootGroup"/> is "null", the
        /// absolute root to look for speed changes. This can be a relatively heavy operation.
        /// Limiting this method to getting called once per frame at max is advised.
        /// <paramref name="rootGroup"/>.
        /// </summary>
        /// <param name="channel">The channel for which to retrieve the playback speed.</param>
        /// <param name="rootGroup">The root of the hierarchy at which to stop. Specify <c>null</c>
        /// to process the entire hierarchy.</param>
        /// <returns></returns>
        float GetPlaybackSpeedOfChannel(FMOD.Channel channel, FMOD.ChannelGroup rootGroup)
        {
            float speed = 1f;

            // Incorporate potential frequency changes.
            float freq;
            channel.getFrequency(out freq);
            speed *= (freq / (float)soundSampleRate);

            float pitch;

            // Incorporate ancestral pitches.
            FMOD.ChannelGroup group = parentGroup;

            while (group.handle != rootGroup.handle &&
                   group.handle != System.IntPtr.Zero)
            {
                group.getPitch(out pitch);
                speed *= pitch;
                group.getParentGroup(out group);
            }

            // Incorporate direct channel pitch.
            channel.getPitch(out pitch);
            speed *= pitch;

            return speed;
        }

        /// <summary>
        /// Calculates the effective playback speed of the currently active (main) Channel
        /// instance.
        /// </summary>
        /// <param name="rootGroup">The root of the hierarchy at which to stop. Specify <c>null</c>
        /// to process the entire hierarchy.</param>
        /// <returns></returns>
        float GetChannelPlaybackSpeed(FMOD.ChannelGroup rootGroup)
        {
            // TODO: We MAY be able to optimize this a bit by storing a reference to the parent
            //  EventInstance's direct ChannelGroup and stop crawling parent groups at that point.
            //  Depending on the mix setup, however, this could _theoretically_ cause us to miss
            //  certain speed multipliers. This would likely need to be checked with FMOD
            //  engineers.

            float speed = 1f;

            if (playingChannel.hasHandle())
            {
                return GetPlaybackSpeedOfChannel(playingChannel, rootGroup);
            }
            else if (queuedChannel.hasHandle())
            {
                return GetPlaybackSpeedOfChannel(queuedChannel, rootGroup);
            }
            else
            {
                return speed;
            }
        }

        /// <summary>
        /// Whether this visor is actively monitoring a channel or not.
        /// </summary>
        /// <returns><c>true</c> if actively tracking, <c>false</c> otherwise.</returns>
        public bool IsTrackingChannel()
        {
            return playingChannel.hasHandle() || queuedChannel.hasHandle();
        }

        // NOTE: The algorithm below was the first written. It took the standard visor approach of
        //  extending VisorBase to access the shared estimation system. During integration it
        //  became apparent that using that system would result in throwing away useful
        //  information. The version below was "deprecated" in favor of using a completely separate
        //  processing approach for the FMOD visor; one that takes advantage of the deeper insights
        //  available in the FMOD Core API (than most other systems).
        //
        //  It is possible that the version below may be useful in certain circumstances for which
        //  the FMOD Studio-specific version will not suffice. Such circumstances might include:
        //      - Programmer-made Sounds
        //      - Manual channel playback control (i.e. controlled by programmer).
        //      - FMOD Core based use cases (read: not FMOD Studio).
        //
        //  The logic in the algorithm below has not been thoroughly tested and very likely
        //  contains some bugs that have since been worked out/cleared up with the newer version.
        //  In the event that the method below is used again, it is highly recommended that it be
        //  updated to generally follow the processing as performed in the currently used method.

        /// <summary>
        /// Checks the state of the watched Channel instance(s) and reports updates to Koreographer
        /// via the core visor system. This method is responsible for calling the internal Visor
        /// Update process which, in turn, calls ProcessKoreography.
        /// 
        /// When this method returns `PlaybackStatus.Stopped`, it is considered "finished" and
        /// should be Reset.
        /// </summary>
        /// <param name="rootGroup">The root of the channels' <c>ChannelGroup</c> hierarchy.
        /// </param>
        /// <param name="rootSpeed">The playback speed of the channels' root <c>ChannelGroup</c>
        /// instance.</param>
        /// <returns>The playback status of the Visor.</returns>
        // public PlaybackStatus ProcessUpdateWithCore(FMOD.ChannelGroup rootGroup, float rootSpeed)
        // {
        //     playbackStatus = GetParentGroupPlaybackStatus();
        //     if (playbackStatus != PlaybackStatus.Playing)
        //     {
        //         return playbackStatus;
        //     }

        //     // Assume playback of core Channels is paused and set it otherwise when processing
        //     //  occurs.
        //     playbackStatus = PlaybackStatus.Paused;

        //     bool bPaused, bMainPlaying = false, bQueuedPlaying = false;
        //     uint position = 0;

        //     int fmodDeltaSamples = 0;

        //     // Check the main playingChannel first.
        //     if (playingChannel.hasHandle())
        //     {
        //         playingChannel.isPlaying(out bMainPlaying);
        //         playingChannel.getPaused(out bPaused);

        //         // Check for time update only if in an unpaused playing state.
        //         if (bMainPlaying && !bPaused)
        //         {
        //             // Set status to playing.
        //             playbackStatus = PlaybackStatus.Playing;

        //             playingChannel.getPosition(out position, FMOD.TIMEUNIT.PCM);
        //             int pos = (int)position;

        //             // Check for looping.
        //             if (pos < fmodSampleTime)
        //             {
        //                 // TODO: Determine if the following check is necessary. Currently it seems
        //                 //  as though a "jump" is properly processed via the "queuedChannel" stuff
        //                 //  found below. In "full blown" loops, however, the queuedChannel isn't
        //                 //  set and we need to look for loop situations here. What isn't clear is
        //                 //  how FMOD Studio handles looping within Event Instances. Do they bother
        //                 //  with the "loop start/end" points? Do they properly track "loop count"?
        //                 //  Testing has shown that some Channels report "Loop Count 0" while in a
        //                 //  looping mode (which shouldn't be feasible)?
        //                 //  For now, we simply assume that if we find ourselves in this situation,
        //                 //  then we've encountered a loop and don't bother to check first.
        //                 // if (GetIsAudioLooping())
        //                 fmodDeltaSamples = (soundPlaybackEndSample - fmodSampleTime) + (pos - soundPlaybackStartSample);
        //                 bLoopDetected = true;
        //             }
        //             else
        //             {
        //                 // Update the delta samples. This may be added to later if we're in a
        //                 //  transition frame.
        //                 fmodDeltaSamples = pos - fmodSampleTime;
        //             }

        //             // Update the sample time. This may be overridden later if we're in a
        //             //  transition frame.
        //             fmodSampleTime = pos;
        //         }
        //     }

        //     // Check if we have a queued loop/jump transition coming.
        //     if (queuedChannel.hasHandle())
        //     {
        //         queuedChannel.isPlaying(out bQueuedPlaying);
        //         queuedChannel.getPaused(out bPaused);

        //         // Check if we're playing.
        //         if (bQueuedPlaying && !bPaused)
        //         {
        //             // Set status to playing.
        //             playbackStatus = PlaybackStatus.Playing;

        //             ulong dspTime, parentDSPTime;
        //             queuedChannel.getDSPClock(out dspTime, out parentDSPTime);

        //             ulong delayStart, delayEnd;
        //             queuedChannel.getDelay(out delayStart, out delayEnd);

        //             // Get the time.
        //             queuedChannel.getPosition(out position, FMOD.TIMEUNIT.PCM);
        //             int pos = (int)position;

        //             if (delayStart <= parentDSPTime)
        //             {
        //                 // Use the position grabbed from the previous "playing channel" and use
        //                 //  it as the end extent.
        //                 // TODO: Check what happens here if we jump forward...
        //                 soundPlaybackEndSample = fmodSampleTime;

        //                 // Update delta.
        //                 // Check for the odd scenario wherein we jump very close to the end of the
        //                 //  audio file and then loop around within this update.
        //                 if (pos < soundPlaybackStartSample)
        //                 {
        //                     // We've jumped to the end but looped around!
        //                     uint loopstart, loopend;
        //                     queuedChannel.getLoopPoints(out loopstart, FMOD.TIMEUNIT.PCM, out loopend, FMOD.TIMEUNIT.PCM);

        //                     fmodDeltaSamples += ((int)loopend - soundPlaybackStartSample);
        //                     fmodDeltaSamples += (pos - ((int)loopstart));
        //                     bLoopDetected = true;
        //                 }
        //                 else
        //                 {
        //                     // The simple case.
        //                     fmodDeltaSamples += (int)position - soundPlaybackStartSample;
        //                 }

        //                 // Update the sample time.
        //                 fmodSampleTime = pos;

        //                 // Promote the queued channel to the playing one!
        //                 playingChannel = queuedChannel;
        //                 queuedChannel.clearHandle();
        //             }
        //         }
        //     }
        //     else if (bMainPlaying)
        //     {
        //         // If we get to this point then we do not have a transition happening and are
        //         //  still playing. We should check to see if we've reached the end.

        //         ulong delayStart, delayEnd;
        //         playingChannel.getDelay(out delayStart, out delayEnd);

        //         // Check that playback end has been scheduled.
        //         if (delayEnd > 0)
        //         {
        //             ulong dspTime, parentDSPTime;
        //             playingChannel.getDSPClock(out dspTime, out parentDSPTime);

        //             // Check that the scheduled end has been reached.
        //             if (delayEnd <= parentDSPTime)
        //             {
        //                 // Update the end extent to assist in estimation.
        //                 soundPlaybackEndSample = fmodSampleTime;

        //                 // NOTE: We don't yet say that we're stopped because we might have some
        //                 //  estimation to do before we get to a truly "stopped" state. This is
        //                 //  handled further down.
        //             }
        //         }
        //     }

        //     // We are still playing if the estimated position is still catching up to the source
        //     //  position even when our underlying Channels are done. PlaybackStatus is set to
        //     //  Stopped after the call to Update if the timing is correct.
        //     if (!bMainPlaying && !bQueuedPlaying)
        //     {
        //         playbackStatus = PlaybackStatus.Playing;
        //     }

        //     // Store previous sample positions for post-estimation comparison. The underlying
        //     //  VisorBase logic currently looks to this for *source* position looping, not
        //     //  estimated position looping.
        //     // TODO: Verify that this is still needed/applies when we unify visor estimation logic.
        //     int oldSourceSampleTime = lastFrameStats.sourceSampleTime;
        //     int oldSampleTime = sampleTime;

        //     // Update playback speed prior to entering code that may use it.
        //     playbackSpeed = rootSpeed * GetChannelPlaybackSpeed(rootGroup);

        //     base.Update();

        //     // The sample position either looped or jumped this frame. Handle special processing.
        //     if (lastFrameStats.sourceSampleTime < oldSourceSampleTime)
        //     {
        //         // Reset Extents to Loop Points.
        //         // WARNING: It is unclear whether FMOD Studio actually makes use of these settings
        //         //  or not. We rely on them to be safe for now.
        //         SetExtentsToLoopExtents();

        //         // TODO: This should(?) become unecessary once we unify the underlying estimation
        //         //  systems.
        //         bLoopDetected = false;
        //     }
        //     // If neither channel is playing, then we should stop watching. In addition, we
        //     //  ensure that the estimated time has processed at least as far as the reported sample
        //     //  position.
        //     else if (!bMainPlaying && !bQueuedPlaying &&
        //              (sampleTime >= fmodSampleTime ||   // Estimated to-or-beyond source time.
        //               // HACK?? This should be checked once we replace the estimation system.
        //               sampleTime == oldSampleTime))     // Estimation didn't progress at all.
        //     {
        //         playbackStatus = PlaybackStatus.Stopped;
        //     }

        //     return playbackStatus;
        // }


        #endregion
        #region State Accessors


        /// <summary>
        /// Gets the name of the playing Sound.
        /// </summary>
        /// <returns>The name of the playing Sound.</returns>
        public string GetSoundName()
        {
            return audioName;
        }

        /// <summary>
        /// The processed Playback position in samples of the Sound within the monitored
        /// Channel(s).
        /// </summary>
        /// <returns>The processed Playback position in samples.</returns>
        public int GetPlaybackSamplePosition()
        {
            return processedPlaybackSampleTime;
        }

        /// <summary>
        /// The length-in-samples of the Sound instance being monitored by this visor.
        /// </summary>
        /// <returns>The total length-in-samples of the Sound.</returns>
        public int GetSoundSampleLength()
        {
            return soundSampleLength;
        }

        /// <summary>
        /// Whether the Channel(s) monitored by this visor is/are playing or not.
        /// </summary>
        /// <returns><c>true</c> if the Channel(s) is/are actively playing, <c>false</c> otherwise.
        /// </returns>
        public bool GetIsPlaying()
        {
            return playbackStatus == PlaybackStatus.Playing;
        }

        /// <summary>
        /// The current playback speed of the Sound within the monitored Channel(s).
        /// </summary>
        /// <returns>The playback speed of the Sound within the monitored Chaannel(s).</returns>
        public float GetPlaybackSpeed()
        {
            return playbackSpeed;
        }


        #endregion
        #region VisorBase-related


        // TODO: Remove this implementation if this class is adjusted to extend VisorBase.

        /// <summary>
        /// Gets the deltaTime used to process the frame *without* modification from Time.timeScale.
        /// </summary>
        /// <returns>The raw frame time.</returns>
        float GetRawFrameTime()
        {
            float rawFrameTime = UnityEngine.Time.unscaledDeltaTime;

            // There is one known edge case that we check for here:
            //   1) When the Editor is paused (moved to background or the user presses the Pause button).
            //  Most of the time we want to use unscaled time for calculations. This is because the audio
            //  timeline moves at a fixed rate (sample rate); it is unaffected by the game simulation rate.
            //  That said, the unscaled delta time does not stop accumulating when the Editor pauses in the
            //  manners outlined in #1 above (at least). In these cases, the first frame re-processed when
            //  returning from the paused state is typically very short, falling underneath the maximum
            //  delta time. If we detect that the unscaled time is above the maximum delta but that the
            //  standard deltaTime is less than the maximum delta, then we prefer the standard deltaTime.
            // TODO: Determine if this effect is Editor-only or if it happens at game time as well. If
            //  Editor-only, we could add Preprocessor directives around it. The best possible solution
            //  for this, though, would be to determine a way to tell if the game had resumed from
            //  pause or not...
            // In the future, we may want to use EditorApplication.playmodeStateChanged to detect "play
            //  restarting" and use that to set (and then clear?) a flag. For now, this appears to work
            //  well enough.
            if (UnityEngine.Time.unscaledDeltaTime >= UnityEngine.Time.maximumDeltaTime &&
                UnityEngine.Time.deltaTime < UnityEngine.Time.maximumDeltaTime &&
                UnityEngine.Time.timeScale != 0f)
            {
                rawFrameTime = UnityEngine.Time.deltaTime / UnityEngine.Time.timeScale;
            }

            return rawFrameTime;
        }


        #endregion
    }
}
