# Changelog
All notable changes to this package will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.9.10] - 2024-10-29

### New
- Added a Scene View overlay to chose between different rendering modes for the Scene camera. The Scene View camera now has the option the make its own visibility calculations at runtime. Allowing users to see the objects that are culled by the Game camera.
- Added a Runtime Settings option to select the Depth texture retrieval method for the Occlusion Culling system.
- The rendering system now respects the Maximum LOD Level quality setting and avoids rendering LODs higher than the specified level. Additionally users can set a Maximum LOD Level through Profile settings to have different settings for diffferent prototypes.
- Added an editor setting to prevent Unity from including shader variants with both DOTS instancing and procedural instancing keywords in builds.

### Changed
- Redesigned the Occlusion Culling system for improved compatibility with future Unity changes.
- Added various quality-of-life improvements to the user interface.

### Fixed
- Resolved Occlusion Culling issues in Unity 6000 URP and HDRP.

## [0.9.9] - 2024-10-23

### Fixed
- Tree Proxy shader not automatically included in builds, causing errors with SpeedTrees.

## [0.9.8] - 2024-10-22

### Fixed
- Compile error caused by Input System reference in Unity 6000.0.23f1.
- Obsolete HDRP light intensity warning in Unity 6000.0.23f1.
- Shader warning in the Material Variation Demo Scene in Unity 6000.0.23f1.
- Shader conversion error in Material Variation when using built-in shaders.

## [0.9.7] - 2024-10-03

### Added
- New demo showcasing how to use custom Compute Shaders.
- New API method to retrieve the GraphicsBuffer containing the Matrix4x4 transform data.

### Changed
- The Material Variations shader generator now uses relative paths for include files.
- Improved TransformBufferUtility methods to support multiple RenderSources using the same transform buffer.

### Fixed
- Prefabs with an LOD Group that has only one level and a culled percentage not being culled.
- Prefabs with an LOD Group not cross-fading to the culled level when using a culled percentage.
- Compute shader error that occurred when there were multiple RenderSources within a RenderSourceGroup and one RenderSource had a zero instance count.

## [0.9.6] - 2024-09-10

### Fixed
- Resolved rendering issues on devices with AMD GPUs.

## [0.9.5] - 2024-09-09

### Added
- Prefab Manager Add/Remove instance performance improvements.
- In edit mode, the Tree and Detail Managers can now render terrain details and trees from other scenes.
- The Tree and Detail Managers now include an option to automatically add terrains from scenes loaded at runtime.
- Map Magic 2 integration component for runtime generated terrains.
- UI improvements for GPUI Managers.

### Changed
- Camera FOV value is no longer cached and is now updated automatically.
- Auto. Find Tree and Detail Manager options for GPUI Terrain is now enabled by default.

### Fixed
- Prototype could not be removed from the Prefab Manager if the GPUIPrefab component was manually deleted from a prefab.
- GPUIPrefab component was not automatically added to a prefab when using a variant of a model prefab as a prototype on the Prefab Manager.
- 'Add Active Terrains' button on Detail and Tree Managers would add references to terrains in other scenes when multiple scenes with terrains were loaded in edit mode, causing a 'Scene mismatch' error.

## [0.9.4] - 2024-08-10

### Added
- New RequireUpdate API methods for Tree and Detail Managers to handle runtime terrain modifications.

### Fixed
- Detail Manager IndexOutOfRangeException when using Coverage mode with multiple prototypes that have the same prefab or texture.

## [0.9.3] - 2024-07-24

### Fixed
- Managers not showing the correct package version number.

## [0.9.2] - 2024-07-24

### Fixed
- Wrong render pipeline is selected for rendering and importing demos when the Render Pipeline Asset in Quality settings is not set.

## [0.9.1] - 2024-07-23

### Fixed
- New Profile objects are not editable.
- Removed unused using statements.

## [0.9.0] - 2024-07-22

### Added
- Initial release.