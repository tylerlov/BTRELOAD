# Changelog
All notable changes to this package will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.9.8] - 2024-10-22

### Fixed
- Material variation demo scene shader warning on Unity 6000.0.23f1.
- Material variation shader conversion error when using built-in shaders.

## [0.9.5] - 2024-09-09

### Added
- Prefab Manager Add/Remove instance performance improvements.

### Fixed
- Prototype could not be removed from the Prefab Manager if the GPUIPrefab component was manually deleted from a prefab.
- GPUIPrefab component was not automatically added to a prefab when using a variant of a model prefab as a prototype on the Prefab Manager.

## [0.9.0] - 2024-07-22

### Added
- Initial release.