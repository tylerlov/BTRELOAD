using UnityEngine;
using System;
using System.Runtime.InteropServices;
using FMOD.Studio;
using FMODUnity;

public class AudioSampler : MonoBehaviour
{
    private FMOD.System coreSystem;
    private FMOD.ChannelGroup channelGroup;
    private FMOD.DSP dsp;
    private FMOD.Sound recordedSound;
    private FMOD.Channel playbackChannel;

    private const int SAMPLE_RATE = 48000;
    private const int CHANNELS = 2;
    
    [SerializeField] private float recordDuration = 5f; // Duration to record in seconds
    [SerializeField] private int repeatCount = 1; // Number of times to repeat (0 for infinite)
    [SerializeField] private float playbackVolume = 0.5f;
    [SerializeField] private string busPath = "bus:/";

    private int BUFFER_SIZE;
    private float[] audioBuffer;
    private int writeIndex;
    private int readIndex;

    private bool isDestroyed = false;
    private int currentRepeatCount = 0;
    private bool isPlaying = false;

    private void Start()
    {
        BUFFER_SIZE = (int)(SAMPLE_RATE * CHANNELS * recordDuration);
        audioBuffer = new float[BUFFER_SIZE];
        coreSystem = RuntimeManager.CoreSystem;

        FMOD.RESULT result;

        result = coreSystem.getMasterChannelGroup(out channelGroup);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to get master channel group: {result}");
            return;
        }

        FMOD.DSP_DESCRIPTION dspDesc = new FMOD.DSP_DESCRIPTION();
        dspDesc.pluginsdkversion = FMOD.VERSION.number;
        dspDesc.name = System.Text.Encoding.UTF8.GetBytes("Audio Capture DSP\0");
        dspDesc.version = 0x00010000;
        dspDesc.numinputbuffers = 1;
        dspDesc.numoutputbuffers = 1;
        dspDesc.read = DSPReadCallback;

        result = coreSystem.createDSP(ref dspDesc, out dsp);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to create DSP: {result}");
            return;
        }

        result = channelGroup.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, dsp);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to add DSP to channel group: {result}");
            return;
        }

        result = dsp.setBypass(false);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to set DSP bypass: {result}");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPlaying)
            {
                StopPlayback();
            }
            else
            {
                PlaybackSampledAudio();
            }
        }

        if (isPlaying && playbackChannel.hasHandle())
        {
            bool isPlaying = false;
            playbackChannel.isPlaying(out isPlaying);
            if (!isPlaying)
            {
                currentRepeatCount++;
                if (repeatCount == 0 || currentRepeatCount < repeatCount)
                {
                    PlaybackSampledAudio();
                }
                else
                {
                    StopPlayback();
                }
            }
        }
    }

    private FMOD.RESULT DSPReadCallback(ref FMOD.DSP_STATE dspState, IntPtr inBuffer, IntPtr outBuffer, uint length, int inChannels, ref int outChannels)
    {
        if (isDestroyed || inBuffer == IntPtr.Zero || outBuffer == IntPtr.Zero)
        {
            return FMOD.RESULT.ERR_INVALID_PARAM;
        }

        float[] buffer = new float[length * inChannels];
        Marshal.Copy(inBuffer, buffer, 0, buffer.Length);

        lock (audioBuffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                audioBuffer[writeIndex] = buffer[i];
                writeIndex = (writeIndex + 1) % BUFFER_SIZE;
            }
        }

        Marshal.Copy(buffer, 0, outBuffer, buffer.Length);

        return FMOD.RESULT.OK;
    }

    private void PlaybackSampledAudio()
    {
        Debug.Log("Attempting to play sampled audio");

        FMOD.CREATESOUNDEXINFO exInfo = new FMOD.CREATESOUNDEXINFO();
        exInfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
        exInfo.numchannels = CHANNELS;
        exInfo.format = FMOD.SOUND_FORMAT.PCMFLOAT;
        exInfo.defaultfrequency = SAMPLE_RATE;
        exInfo.length = (uint)(SAMPLE_RATE * CHANNELS * sizeof(float) * recordDuration);
        exInfo.pcmreadcallback = PCMReadCallback;

        FMOD.RESULT result = coreSystem.createSound("", FMOD.MODE.OPENUSER | FMOD.MODE.LOOP_OFF, ref exInfo, out recordedSound);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to create sound: {result}");
            return;
        }

        result = coreSystem.playSound(recordedSound, channelGroup, false, out playbackChannel);
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"Failed to play sound: {result}");
        }
        else
        {
            playbackChannel.setVolume(playbackVolume);
            isPlaying = true;
            currentRepeatCount = 0;
            Debug.Log($"Playing sampled audio at volume {playbackVolume}");
        }
    }

    private FMOD.RESULT PCMReadCallback(IntPtr sound, IntPtr data, uint datalen)
    {
        if (isDestroyed)
        {
            return FMOD.RESULT.ERR_INVALID_PARAM;
        }

        float[] buffer = new float[datalen / sizeof(float)];

        lock (audioBuffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = audioBuffer[readIndex];
                readIndex = (readIndex + 1) % BUFFER_SIZE;
            }
        }

        Marshal.Copy(buffer, 0, data, buffer.Length);

        return FMOD.RESULT.OK;
    }

    private void StopPlayback()
    {
        if (playbackChannel.hasHandle())
        {
            playbackChannel.stop();
        }
        isPlaying = false;
        Debug.Log("Stopped audio playback");
    }

    private void OnDisable()
    {
        CleanUp();
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        isDestroyed = true;

        StopPlayback();

        if (dsp.hasHandle())
        {
            channelGroup.removeDSP(dsp);
            dsp.release();
        }

        if (recordedSound.hasHandle())
        {
            recordedSound.release();
        }
    }
}