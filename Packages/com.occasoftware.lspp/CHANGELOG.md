# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [3.2.0] - 2023-12-06

This version of LSPP is compatible with Unity 2022.3.0f1.

### Changed

- Improved support for orthographic cameras.

## [3.1.1] - 2023-09-18

This version of LSPP is compatible with Unity 2022.3.0f1.

### Changed

- Updated readme.

## [3.1.0] - 2023-08-30

This version of LSPP is compatible with Unity 2022.3.0f1.

### Added

- Added option to control the occlusion over distance amount.

## [3.0.0] - 2023-06-28

This version of LSPP is compatible with Unity 2022.3.0f1.

### Changed

- Changed to RTHandle and Blitter APIs.
- LSPP is now a UPM-style package.

### Added

- Added option to control the occlusion assumption.
- Added a component that allows you to override the default light settings.

## [2.2.0] - 2023-04-26

### Changed

- Changed the screen edge sampling algorithm to reduce shadow pop-in

### Added

- Added a falloff intensity slider so that you can control the falloff rate of the sun lighting.

## [2.1.3] - 2023-03-31

### Fixed

- Fixed an issue causing the Sample Count property to be ignored.

### Added

- Added assembly definition.

## [2.1.2] - 2023-03-29

### Fixed

- Fixed an issue causing LSPP to render even when the LSPP override was not in the scene.

### Changed

- Changed the default Fog Density from 4 to 0.

## [2.1.1] - 2023-03-29

### Fixed

- Fixed an issue causing Transparent materials to render incorrectly with LSPP.

## [2.1.0] - 2023-03-28

### Added

- Added support for XR Single Pass Instanced rendering

### Changed

- Changed target version from 2020.3 to 2021.3 LTS.
- Changed Shader Graphs to Custom Shaders to enable compatibility with XR SPI rendering requirements, improve performance, and improve compatibility with Unity 2022.x.

## [2.0.4] - 2022-11-07

### Fixed

- Fixed a bug causing Decals to render incorrectly in versions of Unity URP with Decal support.

## [2.0.3] - 2022-11-07

- Fixed a bug regarding the creation of the occlusion texture that could occur in Unity 2022+.

## [1.0.1] through [2.0.2] - N.D

- Some changenotes are missing from 1.0.1 through to 2.0.3. Sorry! :)

## [1.0.0] - 2022-03-08

- Initial Release
