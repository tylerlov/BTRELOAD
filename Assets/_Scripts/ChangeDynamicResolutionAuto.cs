using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

public class ChangeDynamicResolutionAuto : MonoBehaviour {

    private enum RenderScaleMode {
        DynamicResolution,
        RenderPipelineAsset,
    }


    [SerializeField] private TextMeshProUGUI screenText;
    [SerializeField] private RenderScaleMode renderScaleMode;


    private FrameTiming[] frameTimings = new FrameTiming[3];

    private float renderScale = 1.0f;
    private uint frameCount = 0;
    private const uint kNumFrameTimings = 2;

    private double gpuFrameTime;
    private double cpuFrameTime;

    private UniversalRenderPipelineAsset universalRenderPipelineAsset;
    private float lastCalculateTime;

    private void Awake() {
        universalRenderPipelineAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;

        if (universalRenderPipelineAsset == null) {
            // No override for this Quality
            universalRenderPipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline;
        }
    }

    private void Update() {
        float oldRenderScale = renderScale;

        GetFrameTimings();

        if (Input.GetKeyDown(KeyCode.U)) {
            renderScale = .1f;
        }
        if (Input.GetKeyDown(KeyCode.I)) {
            renderScale = 1f;
        }

        TryCalculateResolutionBasedOnFPS();

        if (renderScale != oldRenderScale) {
            ChangeRenderScale(renderScale);
        }

        int rezWidth = (int)Mathf.Ceil(ScalableBufferManager.widthScaleFactor * renderScale * Screen.currentResolution.width);
        int rezHeight = (int)Mathf.Ceil(ScalableBufferManager.heightScaleFactor * renderScale * Screen.currentResolution.height);

        screenText.text = string.Format("Scale: {0:F3}\nResolution: {1}x{2}\nGPU: {3:F3} CPU: {4:F3}",
            renderScale,
            rezWidth,
            rezHeight,
            gpuFrameTime,
            cpuFrameTime);
    }

    private void GetFrameTimings() {
        ++frameCount;
        if (frameCount <= kNumFrameTimings) {
            return;
        }

        FrameTimingManager.CaptureFrameTimings();
        FrameTimingManager.GetLatestTimings(kNumFrameTimings, frameTimings);
        if (frameTimings.Length < kNumFrameTimings) {
            Debug.LogFormat("Skipping frame {0}, didn't get enough frame timings.",
                frameCount);

            return;
        }

        gpuFrameTime = (double)frameTimings[0].gpuFrameTime;
        cpuFrameTime = (double)frameTimings[0].cpuFrameTime;
    }

    private void TryCalculateResolutionBasedOnFPS() {
        float timeSinceLastCalculate = Time.realtimeSinceStartup - lastCalculateTime;

        if (timeSinceLastCalculate < .2f) {
            // Calculated not long ago, don't calculate again
            return;
        }

        lastCalculateTime = Time.realtimeSinceStartup;

        float fps = Framerate.Instance.GetFPS();

        if (fps < 60) {
            renderScale -= .02f;
        } else {
            renderScale += .02f;
        }

        float renderScaleMin = .5f;
        renderScale = Mathf.Clamp(renderScale, renderScaleMin, 1f);
    }

    private void ChangeRenderScale(float renderScale) {
        switch (renderScaleMode) {
            default:
            case RenderScaleMode.RenderPipelineAsset:
                universalRenderPipelineAsset.renderScale = renderScale;
                break;
            case RenderScaleMode.DynamicResolution:
                ScalableBufferManager.ResizeBuffers(renderScale, renderScale);
                break;
        }
    }


}