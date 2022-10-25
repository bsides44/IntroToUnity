# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.6-preview] - 2021-12-01
### Changed
- Update package version to create a signed package

## [0.1.5-preview] - 2021-11-02
### Added
- Fix issue with spinner in 2021.2 or newer

## [0.1.4-preview] - 2021-10-06
### Added
- Support for URL's ending in `.tgz` for tarball package installs via direct download

### Fixed
 - Content packs and products that are imported before content manager is installed will now be discoverable by content manager.
 - Fixed a case where trying to install multiple samples at the same time would result in some samples being skipped.

### Changed
 - A custom registry for content-packs is now installed by default with Content-Manager installed.

## [0.1.3-preview] - 2020-02-23

### Changed
- Preview packages are detected as in preview and can have customized labels.
- Local content packs are now written with local paths to play nice with source control.

## [0.1.2-preview] - 2020-02-04

### Added
 - Update icons for content packs that have updates or different versions that will be installed. A new content pack has been added to test this feature.
 - Initial release of Content Manager.  Includes 4 test packs in the 'Test' product.
