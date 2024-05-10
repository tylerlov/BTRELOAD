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
    /// <summary>
    /// <para>The FMODCallbackHandler is responsible for registering for and handling specific FMOD
    /// events. In particular it handles the following FMOD Studio System events:
    /// <list type="bullet">
    /// <item>SYSTEM_CALLBACK_TYPE.POSTUPDATE</item>
    /// </list>
    /// the following FMOD EventInstance events[1]:
    /// <list type="bullet">
    /// <item>EVENT_CALLBACK_TYPE.SOUND_PLAYED</item>
    /// <item>EVENT_CALLBACK_TYPE.STOPPED</item>
    /// <item>EVENT_CALLBACK_TYPE.DESTROYED</item>
    /// </list>
    /// and the following FMOD EventDescription events[2]:
    /// <list type="bullet">
    /// <item>EVENT_CALLBACK_TYPE.CREATED</item>
    /// </list>
    /// </para>
    /// <para>[1] The FMOD EventInstance events are forwaraded to the appropriate
    /// FMODEventInstanceVisors for processing.</para>
    /// <para>[2] The FMOD EventInstance events are forwaraded to the appropriate
    /// FMODEventDescriptionVisors for processing.</para>
    /// <para>If an integrating project needs to handle Studio System callbacks directly, then they
    /// may be disabled by setting the static <c>UseThirdPartyPostUpdateCallback</c> field to
    /// <c>true</c>. In that case, the singleton instance should be used to call the
    /// <c>DoFMODStudioPostUpdate</c> method during the FMOD Studio System's POSTUPDATE callback
    /// phase.</para>
    /// <para>If an integrating project needs to handle any FMOD Event Instance callbacks directly,
    /// then they may be disabled by setting the static <c>UseThirdPartyEventInstanceCallback</c>
    /// field to <c>true</c> and the callbacks should include the callback types specified in the
    /// <c>FMODCallbackHandler.EVENT_CALLBACK_TYPE_MASK</c> constant. In that case, the singleton
    /// instance should be used to call the <c>HandleEventInstanceCallback</c> method when a
    /// callback for one of the above types is received.</para>
    /// <para>If an integrating project needs to handle any FMOD Event Description callbacks
    /// directly, then they may be disabled by setting the static
    /// <c>UseThirdPartyEventDescriptionCallback</c> field to <c>true</c> and the callbacks should
    /// include the CREATED callback type. In that case, the singleton instance should be used to
    /// call the <c>HandleEventDescriptionCallback</c> method when a CREATED callback is received.
    /// </para>
    /// </summary>
    public class FMODCallbackHandler
    {
        #region Constants


        /// <summary>
        /// The callback Types that all FMODEventInstanceVisor instances care about.
        /// </summary>
        public const EVENT_CALLBACK_TYPE EVENT_CALLBACK_TYPE_MASK = EVENT_CALLBACK_TYPE.SOUND_PLAYED | EVENT_CALLBACK_TYPE.STOPPED | EVENT_CALLBACK_TYPE.DESTROYED;


        #endregion
        #region Static Fields


        /// <summary>
        /// The internal static instance of the System visor with which EventInstance visors
        /// will register themselves for PostUpdate callbacks.
        /// </summary>
        static FMODCallbackHandler instance = new FMODCallbackHandler();

        /// <summary>
        /// Whether the FMODCallbackHandler system should be driven by third party calls rather
        /// than direct callback registration.
        /// </summary>
        public static bool UseThirdPartyPostUpdateCallback = false;

        /// <summary>
        /// Whether the FMODCallbackHandler system should be driven by third party calls for event
        /// instance callbacks rather than direct callback registration.
        /// </summary>
        public static bool UseThirdPartyEventInstanceCallback = false;

        /// <summary>
        /// Whether the FMODCallbackHandler system should be driven by third party calls for event
        /// description callbacks rather than direct callback registration.
        /// </summary>
        public static bool UseThirdPartyEventDescriptionCallback = false;


        #endregion
        #region Static Properties


        /// <summary>
        /// Public accessor for the FMODCallbackHandler singleton instance.
        /// </summary>
        /// <value>A reference to the FMODCallbackHandler singleton instance.</value>
        public static FMODCallbackHandler Instance
        {
            get
            {
                return FMODCallbackHandler.instance;
            }
        }


        #endregion
        #region Member Fields


        // TODO: Verify the thread safety of all registration lists!

        /// <summary>
        /// This keeps the delegate's internal data used during managed-unmanaged boundary
        /// traversal from being freed.
        /// </summary>
        SYSTEM_CALLBACK postUpdateCallback = new SYSTEM_CALLBACK(OnFMODStudioPostUpdateCallback);

        /// <summary>
        /// The internal list of EventVisors that are awaiting a PostUpdate callback.
        /// </summary>
        List<FMODEventInstanceVisor> systemCallbackVisors = new List<FMODEventInstanceVisor>();

        /// <summary>
        /// Whether or not this instance is currently registered for the PostUpdate callback.
        /// </summary>
        bool isRegisteredForSystemUpdates = false;

        /// <summary>
        /// This keeps the delegate's internal data used during managed-unmanaged boundary
        /// traversal from being freed.
        /// </summary>
        EVENT_CALLBACK eventInstCallback = new EVENT_CALLBACK(OnFMODStudioEventInstanceCallbacks);

        /// <summary>
        /// The internal list of EventInstanceVisors that are awaiting EventInstance callbacks.
        /// </summary>
        List<FMODEventInstanceVisor> eventInstVisors = new List<FMODEventInstanceVisor>();

        /// <summary>
        /// This keeps the delegate's internal data used during managed-unmanaged boundary
        /// traversal from being freed.
        /// </summary>
        EVENT_CALLBACK eventDescCallback = new EVENT_CALLBACK(OnFMODStudioEventDescriptionCallbacks);

        /// <summary>
        /// The internal list of EventDescriptionVisors that are awaiting EventInstance callbacks.
        /// </summary>
        List<FMODEventDescriptionVisor> eventDescVisors = new List<FMODEventDescriptionVisor>();


        #endregion
        #region Static Methods


        /// <summary>
        /// Used to handle the POSTUPDATE FMOD System callback.
        /// </summary>
        /// <param name="system">A pointer to the active FMOD Studio System instance.</param>
        /// <param name="type">The type of callback that this call represents.</param>
        /// <param name="commanddata">A pointer to any data provided for the callback.</param>
        /// <param name="userdata">A pointer to any user data supplied to the callback.</param>
        /// <returns>Always returns the FMOD "OK" result.</returns>
        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        static FMOD.RESULT OnFMODStudioPostUpdateCallback(IntPtr system, SYSTEM_CALLBACK_TYPE type, IntPtr commanddata, IntPtr userdata)
        {
            FMOD.RESULT result = FMOD.RESULT.OK;

            try
            {
                if (type == SYSTEM_CALLBACK_TYPE.POSTUPDATE)
                {
                    FMODCallbackHandler.Instance.DoFMODStudioPostUpdate();

                    // Stop receiving callbacks.
                    FMOD.Studio.System sys = new FMOD.Studio.System(system);
                    result = sys.setCallback(null);
                    FMODCallbackHandler.Instance.isRegisteredForSystemUpdates = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return result;
        }

        /// <summary>
        /// Handles callbacks from registered EventInstance. It specifically handles the following
        /// callback types:
        /// <list type="bullet">
        ///     <item>SOUND_PLAYED</item>
        ///     <item>STOPPED</item>
        ///     <item>DESTROYED</item>
        /// </list>
        /// <para>If this FMODCallbackHandler was Initialized with the
        /// <c>UseThirdPartyEventInstanceCallback</c> static field set to <c>true</c> then the
        /// external callback handler is repsonsible for calling the
        /// <c>HandleEventInstanceCallback</c> method.
        /// </para>
        /// </summary>
        /// <param name="type">The FMOD event type.</param>
        /// <param name="_event">A pointer to the EventInstance for this callback.</param>
        /// <param name="parameters">A pointer to the parameters for this callback.</param>
        /// <returns>This will always return <c>FMOD.RESULT.OK</c>.</returns>
        [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
        static FMOD.RESULT OnFMODStudioEventInstanceCallbacks(EVENT_CALLBACK_TYPE type, IntPtr _event, IntPtr parameters)
        {
            try
            {
                FMODCallbackHandler.Instance.HandleEventInstanceCallback(type, _event, parameters);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return FMOD.RESULT.OK;
        }

        /// <summary>
        /// Handles EventInstance callbacks on behalf of registered EventDescriptions. It
        /// specifically handles the following callback types:
        /// <list type="bullet">
        ///     <item>CREATED</item>
        /// </list>
        /// <para>The EventInstances returned are handed to the appropriate
        /// <c>FMODEventDescriptionVisor</c> instance which will initialize a new
        /// <c>FMODEventInstanceVisor</c> on its behalf, overwriting the callback on the
        /// EventInstance itself.</para>
        /// <para>If this FMODCallbackHandler was Initialized with the
        /// <c>UseThirdPartyEventDescriptionCallback</c> static field set to <c>true</c> then the
        /// external callback handler is responsible for calling the
        /// <c>HandleEventDescriptionCallback</c> method.</para>
        /// </summary>
        /// <param name="type">The FMOD event type.</param>
        /// <param name="evt">A pointer to the EventInstance for this callback.</param>
        /// <param name="parameters">A pointer to the parameters for this callback.</param>
        /// <returns>This will always return <c>FMOD.RESULT.OK</c>.</returns>
        [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
        static FMOD.RESULT OnFMODStudioEventDescriptionCallbacks(EVENT_CALLBACK_TYPE type, IntPtr evt, IntPtr parameters)
        {
            try
            {
                FMODCallbackHandler.Instance.HandleEventDescriptionCallback(type, evt, parameters);
            }
            catch(Exception e)
            {
                Debug.LogError(e);
            }

            return FMOD.RESULT.OK;
        }


        #endregion
        #region Member Methods


        /// <summary>
        /// Registers the FMODEventInstanceVisor for FMOD Studio PostUpdate notification.
        /// </summary>
        /// <param name="visor">The visor to register for the callback.</param>
        public void RegisterForPostUpdateCallback(FMODEventInstanceVisor visor)
        {
            systemCallbackVisors.Add(visor);

            if (!FMODCallbackHandler.UseThirdPartyPostUpdateCallback && !isRegisteredForSystemUpdates)
            {
                // Register for callbacks.
                FMOD.Studio.System system = FMODUnity.RuntimeManager.StudioSystem;

                if (system.isValid())
                {
                    system.setCallback(postUpdateCallback, FMOD.Studio.SYSTEM_CALLBACK_TYPE.POSTUPDATE);
                    isRegisteredForSystemUpdates = true;
                }
            }
        }

        /// <summary>
        /// Unregisters the FMODEventInstanceVisor from FMOD Studio PostUpdate notification.
        /// </summary>
        /// <param name="visor">The visor to unregister from the callback.</param>
        public void UnregisterForPostUpdateCallback(FMODEventInstanceVisor visor)
        {
            systemCallbackVisors.Remove(visor);

            if (!FMODCallbackHandler.UseThirdPartyPostUpdateCallback && isRegisteredForSystemUpdates && systemCallbackVisors.Count == 0)
            {
                // Unregister for callbacks.
                FMOD.Studio.System system = FMODUnity.RuntimeManager.StudioSystem;

                if (system.isValid())
                {
                    system.setCallback(null);
                    isRegisteredForSystemUpdates = false;
                }
            }
        }

        /// <summary>
        /// Registers the FMODEventInstanceVisor for FMOD EventInstance callbacks.
        /// </summary>
        /// <param name="visor">The visor to register for the callbacks.</param>
        public void RegisterForEventInstanceCallback(FMODEventInstanceVisor visor)
        {
            eventInstVisors.Add(visor);

            if (!FMODCallbackHandler.UseThirdPartyEventInstanceCallback)
            {
                visor.GetEventInstance().setCallback(eventInstCallback, EVENT_CALLBACK_TYPE_MASK);
            }
        }

        /// <summary>
        /// Unegisters the FMODEventInstanceVisor from FMOD EventInstance callbacks.
        /// </summary>
        /// <param name="visor">The visor to unregister from the callbacks.</param>
        public void UnregisterForEventInstanceCallback(FMODEventInstanceVisor visor)
        {
            eventInstVisors.Remove(visor);

            if (!FMODCallbackHandler.UseThirdPartyEventInstanceCallback)
            {
                // Clear the callback handler.
                EventInstance evtInst = visor.GetEventInstance();
                if (evtInst.hasHandle())
                {
                    evtInst.setCallback(null);
                }
            }
        }

        /// <summary>
        /// Registers the FMODEventDescriptionVisor and its monitored EventDescriptions for FMOD
        /// EventInstance callbacks.
        /// </summary>
        /// <param name="visor">The visor to registor for the callbacks.</param>
        public void RegisterForEventDescriptionCallback(FMODEventDescriptionVisor visor)
        {
            eventDescVisors.Add(visor);

            if (!FMODCallbackHandler.UseThirdPartyEventDescriptionCallback)
            {
                visor.SetDescriptionCallbacks(OnFMODStudioEventDescriptionCallbacks, EVENT_CALLBACK_TYPE.CREATED);
            }
        }

        /// <summary>
        /// Unregisters the FMODEventDescriptionVisor and its monitored EventDescriptions from FMOD
        /// EventInstance callbacks.
        /// </summary>
        /// <param name="visor">The visor to unregister from the callbacks.</param>
        public void UnregisterForEventDescriptionCallback(FMODEventDescriptionVisor visor)
        {
            if (!FMODCallbackHandler.UseThirdPartyEventDescriptionCallback)
            {
                visor.SetDescriptionCallbacks(null, EVENT_CALLBACK_TYPE.ALL);
            }

            eventDescVisors.Remove(visor);
        }

        /// <summary>
        /// Performs any PostUpdate work necessary. This is expected to be called during the
        /// POSTUPDATE callback phase triggered by the FMOD Studio System. 
        /// </summary>
        public void DoFMODStudioPostUpdate()
        {
            int numToUpdate = systemCallbackVisors.Count;
            for (int i = 0; i < numToUpdate; ++i)
            {
                systemCallbackVisors[i].OnPostUpdate();
            }
            systemCallbackVisors.Clear();
        }

        /// <summary>
        /// Handles the event callbacks expected by registered FMODEventInstanceVisors.
        /// </summary>
        /// <param name="type">The FMOD event type.</param>
        /// <param name="_event">A pointer to the EventInstance for this callback.</param>
        /// <param name="parameters">A pointer to the parameters for this callback.</param>
        public void HandleEventInstanceCallback(EVENT_CALLBACK_TYPE type, IntPtr _event, IntPtr parameters)
        {
            if ((type & EVENT_CALLBACK_TYPE_MASK) != 0)
            {
                int numVisors = eventInstVisors.Count;

                // Count backwards as the list may be modified during the callback phase. This
                //  is a defensive measure, however, as we *currently* break before continuing
                //  if the callback is handled by a visor instance.
                for (int i = numVisors - 1; i >= 0; --i)
                {
                    FMODEventInstanceVisor visor = eventInstVisors[i];
                    if (visor.GetEventInstance().handle == _event)
                    {
                        visor.OnEventInstanceCallback(type, parameters);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the event callbacks expected by registered FMODEventDescriptionVisors.
        /// </summary>
        /// <param name="type">The FMOD event type.</param>
        /// <param name="_event">A pointer to the EventInstance for this callback.</param>
        /// <param name="parameters">A pointer to the parameters for this callback.</param>
        public void HandleEventDescriptionCallback(EVENT_CALLBACK_TYPE type, IntPtr _event, IntPtr parameters)
        {
            // TODO: Check thread safety of this process.
            if (type == EVENT_CALLBACK_TYPE.CREATED)
            {
                EventInstance inst = new EventInstance(_event);
                EventDescription desc;
                inst.getDescription(out desc);

                // Find a visor to handle this EventInstance.
                int numVisors = eventDescVisors.Count;
                for (int i = 0; i < numVisors; ++i)
                {
                    if (eventDescVisors[i].WillWatchEventInstanceOfDescription(desc, inst))
                    {
                        break;
                    }
                }
            }
        }


        #endregion
    }
}
