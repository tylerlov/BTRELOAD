# Version History:

## 2024.2: Optimized memory provider
- Feature: Added a reserved memory provider.
- Feature: Added a allocated memory provider.
- Feature: Added a mono managed memory provider.
- Improvement: Renamed the "MemoryProvider" class to "AllocatedMemoryProvider" class, which now measures the whole allocated memory including the footprint of all loaded assets (textures/materials/audio/...).
- QoL: All predefined performance components are now prefabs, which can be found at "Assets\GUPS\EasyPerformanceMonitor\Prefabs\Components".
- QoL: The memory component in the performance monitor now shows not only the allocated memory, but also the reserved and mono memory by default.

## 2024.1: Unified the versioning
- Improvement: Optimized the gpu frame time monitoring for unity 2021.
- Improvement: The ui windows are now repositioned when modified in the inspector.
- Improvement: The ui elements are now resized when modified in the inspector.

## 2.3.1:
- Fix: The Unity 2021 GpuFrameTime provider returned zero in release builds and no correct values in editor or development builds. This has been corrected and a fallback for release builds has been added. Unity 2022+ is not affected by this.

## 2.3:
- Breaking Change: Refactored the observer pattern between data providers and renderer, to allow a higher customization. If you wrote custom provider or renderer you need to refactor them.
- Feature: Added a minimalistic monitor in two variants showing even more information on a smaller space.
- QoL: The text renderer now has a new property 'Render Pattern', which allows you to easily set how the provided data and its unit are rendered.

## 2.2.1:
- Feature: Support of Unity 2021. [Thanks Andy]
- Fix: When the UnityProfilerProvider is opened in the Inspector while in play mode, the custom profiler state name will be reset. 

## 2.2:
- Improvement: Demo A now got a really demo scene.
- Feature: AR/VR ready free place able monitor + Demo Scenes.
- Feature: Included all Unity Profiler metrics as possible data provider.
- Feature: Included counting of Unity GameObjects by Tag as possible data provider.
- Fix: The mobile shader did not work on all mobile devices (purple image). [Thanks matzek92]
- Fix: CPU / GPU frame time measuring on mobile devices without a dedicated GPU.

## 2.1:
- Improvement: You can now click through both monitors.
- Improvement: The compact monitor now resized with the height of the device, instead of the width (better for mobile solutions).
- Feature: A system/device information monitor window.
- Feature: A log monitor window keeping you updated on Unity log messages.

## 2.0:
This version is a complete rework of the performance monitoring allowing a more appealing and optimized design. Please remove your old version before installing version 2.0 and above.
- Improvement: Using of shader instead of vertices rendering through the OnGui methods. [Thanks Slashbot64]
- Improvement: There is now an "bigger" monitor and a "compact" one, optimized for mobile usage.
- Feature: The machines current download and upload rate can now be tracked and monitored too.
- QoL: Graph data can now be "rated" allowing different colors on reaching fixed thresholds.
- QoL: Data fetching and rendering are now clearer separated allowing a more easy customization.

## 1.2.1:
- Improvement: Added a new renderer property 'PercentForMeanCalculation' to set the percentage of last rendered values (default 60%) used to calculate the mean value.  [Thanks again krakentanz]
- Fix: The persistent singleton Monitor could produce an error: Some objects were not cleaned up when closing the scene. (Did you spawn new GameObjects from OnDestroy?)

## 1.2:
- Feature: Now supports also the new Unity Input System (default key for activation is still F1). [Thanks krakentanz]
- Improvement: Added a minimalistic option to render only the mean performance values.
- QoL: Added a "ShowOnStart" toggle to enable or disable the monitor on start.

## 1.1:
- Improvement: Separated performance data providing and rendering. This allows to subscribe to the performance data for custom interpretation. [Thanks Drew]

When upgrading: You might need to remove and readd the 'Performance Monitor' prefab to your scene if you run into issues.

## 1.0: First official release of EasyPerformanceMonitor.