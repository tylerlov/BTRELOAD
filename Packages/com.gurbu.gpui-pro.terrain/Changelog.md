# Changelog
All notable changes to this package will be documented in this file.
The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.9.5] - 2024-09-09

### Added
- In edit mode, the Tree and Detail Managers can now render terrain details and trees from other scenes.
- The Tree and Detail Managers now include an option to automatically add terrains from scenes loaded at runtime.
- Map Magic 2 integration component for runtime generated terrains.

### Fixed
- 'Add Active Terrains' button on Detail and Tree Managers would add references to terrains in other scenes when multiple scenes with terrains were loaded in edit mode, causing a 'Scene mismatch' error.

## [0.9.4] - 2024-08-10

### Added
- New RequireUpdate API methods for Tree and Detail Managers to handle runtime terrain modifications.

### Fixed
- Detail Manager IndexOutOfRangeException when using Coverage mode with multiple prototypes that have the same prefab or texture.

## [0.9.0] - 2024-07-22

### Added
- Initial release.