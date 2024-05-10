//----------------------------------------------
//            	   Koreographer                 
//    Copyright © 2014-2021 Sonic Bloom, LLC    
//----------------------------------------------

using FMODUnity;
using System.Collections.Generic;
using UnityEngine;


namespace SonicBloom.Koreo.Players.FMODStudio
{
    /// <summary>
    /// The KoreographedEventEmitter replaces the standard FMOD Studio Event Emitter component. It
    /// provides the same exact interface with the addition of Koreographer-specific features. When
    /// properly configured, audio played back through the FMOD Event can be Koreographed.
    /// </summary>
    [AddComponentMenu("Koreographer/FMOD Studio/Koreographed Event Emitter")]
    public class KoreographedEventEmitter : StudioEventEmitter, IKoreographedPlayer
    {
        #region Fields


        /// <summary>
        /// The FMODEventInstanceVisor that manages the actual FMOD EventInstance.
        /// </summary>
        FMODEventInstanceVisor visor = new FMODEventInstanceVisor();

        /// <summary>
        /// The set of Koreography for sounds expected to be played as part of the tracked FMOD
        /// EventInstance.
        /// </summary>
        [SerializeField]
        [Tooltip("Koreography for any sound expected to be played as part of the FMOD Event.")]
        FMODKoreographySet koreographySet = null;

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
        [Tooltip("Whether or not this emitter should act as the Music Player for the Target Koreographer component (or the singleton [global] instance if none is specified).")]
        bool isMusicPlayer = false;


        #endregion
        #region Methods


        /// <summary>
        /// Loads the Koreography specified in the FMODKoreographySet into the target Koreographer.
        /// </summary>
        void LoadKoreography()
        {
            if (targetKoreographer != null && koreographySet != null)
            {
                List<FMODKoreoEntry> koreographies = koreographySet.koreographies;

                for (int i = 0; i < koreographies.Count; ++i)
                {
                    targetKoreographer.LoadKoreography(koreographies[i].koreo);
                }
            }
        }

        /// <summary>
        /// Unloads the Koreography specified in the FMODKoreographySet from the target
        /// Koreographer.
        /// </summary>
        void UnloadKoreography()
        {
            if (targetKoreographer != null && koreographySet != null)
            {
                List<FMODKoreoEntry> koreographies = koreographySet.koreographies;

                for (int i = 0; i < koreographies.Count; ++i)
                {
                    targetKoreographer.UnloadKoreography(koreographies[i].koreo);
                }
            }
        }

        /// <summary>
        /// Handles component startup. Koreography loading occurs here.
        /// </summary>
        protected override void Start()
        {
            // Call parent version first.
            base.Start();

            if (targetKoreographer == null)
            {
                targetKoreographer = Koreographer.Instance;
            }

            if (targetKoreographer == null)
            {
                Debug.LogWarning("No Koreographer component specified and could not find the " +
                                 "singleton. Please add a Koreographer component to the scene " +
                                 "or make sure to specify a default Koreographer. Disabling " +
                                 "this Koreographed Event Emitter (on GameObject '" +
                                 gameObject.name + "').");
                enabled = false;
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
        /// Updates the FMODEventInstanceVisor if it's playing.
        /// </summary>
        void Update()
        {
            if (IsPlaying())
            {
                visor.Update();
            }
        }

        /// <summary>
        /// Handles component clean up. Koreography unloading occurs here.
        /// </summary>
        protected override void OnDestroy()
        {
            UnloadKoreography();

            // Reset Music Playback Controller, if necessary.
            if (isMusicPlayer && targetKoreographer != null &&
                targetKoreographer.musicPlaybackController == (IKoreographedPlayer) this)
            {
                targetKoreographer.musicPlaybackController = null;
            }

            base.OnDestroy();
        }

        /// <summary>
        /// Overrides (and calls) the internal event handler which is responsible for controlling
        /// playback. Sets up/tears down the visor at event-appropriate timing.
        /// </summary>
        /// <param name="gameEvent">The event phase to be handled.</param>
        protected override void HandleGameEvent(EmitterGameEvent gameEvent)
        {
            base.HandleGameEvent(gameEvent);

            if (gameEvent == this.PlayEvent)
            {
                // TODO: Handle OneShot stuff? FMOD clears out its EventInstance handle and simply
                //  lets OneShots play out there is an already playing instance (that is a oneShot).
                //  This might require managing multiple Event Instance Visors per Event Emitter...
                if (IsActive)
                {
                    visor.Initialize(instance, koreographySet, targetKoreographer);
                }
            }
            // Not needed. The visor takes care of resetting itself when the event is determined to
            // be stopped.
            // if (gameEvent == this.StopEvent)
            // {
            //     visor.Reset();
            // }
        }


        #endregion
        #region IKoreographedPlayer Interface Methods


        /// <summary>
        /// Retrieves the current sample position of the first playing instance of the Sound
        /// specified by <paramref name="clipName"/> in the Event Instance being monitored by this
        /// Koreographed Event Emitter.
        /// </summary>
        /// <param name="clipName">The name of the Sound to check.</param>
        /// <returns>The current sample position of the specified Sound. Will return <c>0</c> if
        /// the specified Sound is not found.</returns>
        public int GetSampleTimeForClip(string clipName)
        {
            return visor.GetSampleTimeForClip(clipName);
        }

        /// <summary>
        /// Gets the total sample time for the Sound with name <paramref name="clipName"/>.
        /// </summary>
        /// <param name="clipName">The name of the Sound to check.</param>
        /// <returns>The total sample time for the Sound with name <paramref name="clipName"/>, or
        /// <c>0</c> if the Sound is not found.</returns>
        public int GetTotalSampleTimeForClip(string clipName)
        {
            return visor.GetTotalSampleTimeForClip(clipName);
        }

        /// <summary>
        /// Determines whether the Sound with name <paramref name="clipName"/> is playing.
        /// </summary>
        /// <param name="clipName">The name of the Sound to check.</param>
        /// <returns><c>true</c> if the Sound with name <paramref name="clipName"/> is playing,
        /// <c>false</c> otherwise.</returns>
        public bool GetIsPlaying(string clipName)
        {
            return visor.GetIsPlaying(clipName);
        }

        /// <summary>
        /// Gets the pitch of the first playing instance of the Sound specified by
        /// <paramref name="clipName"/> in the Event Instance being monitored by this Koreographed
        /// Event Emitter that is not <c>1f</c>. If the Sound specified is found but found to have
        /// a pitch value of <c>1f</c>, then this will continue searching for a playing instance
        /// that is not <c>1f</c>. Will return <c>1f</c> if the Sound is not found.
        /// </summary>
        /// <param name="clipName"></param>
        /// <returns>The pitch of the first non-<c>1f</c> playing instance of the Sound indicated
        /// by <paramref name="clipName"/>. If the Sound is not found, this will return <c>1f</c>.
        /// </returns>
        public float GetPitch(string clipName)
        {
            return visor.GetPitch(clipName);
        }

        /// <summary>
        /// Gets the name of the current Sound. In practice this is the first Sound found to be
        /// actively monitored within the Event Instance being monitored by this Koreographed Event
        /// Emitter, if any.
        /// </summary>
        /// <returns>The name of the currently playing Sound (or the empty string if no Sounds are
        /// playing).</returns>
        public string GetCurrentClipName()
        {
            return visor.GetCurrentClipName();
        }


        #endregion
    }
}
