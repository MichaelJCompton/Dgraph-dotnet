# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

Dgraph-dotnet versioning adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## 0.7.0 - [2019-09-19]

### Added

### Changed

*Breaking Changes*

- Upsert changed signature to cope with Dgraph changing how blank nodes are returned from mutations.  

There were a bunch of breaking changes in Dgraph v1.1, but Dgraph-dotnet shielded users from that.  However, Dgraph added a new kind of upsert in v1.1.  Dgraph-dotnet should be updated to include that.

## 0.6.0 - [2019-04-17]

### Added

- Added a DropAll.
- Finally added a changelog :-)
- End-to-end automated testing.

### Changed

*Breaking Changes*

- Everything goes Async.  Old synchronous calls have all been removed.
- SchemaQuery updated (Dgraph deprecated old response).  Now returns a schema object model. 
- Catch more exceptions and returns as errors (e.g. CheckVersion).
- Upsert becomes more generic and useful (signature changed).

*Other*

- Test projects being removed in faviour of end-to-end testing.
- Updated to new Dgraph transaction handling.

## 0.0.0 - [template]

### Added

Added for new features.

### Changed

Changed for changes in existing functionality.

### Deprecated

Deprecated for soon-to-be removed features.

### Removed

Removed for now removed features.

### Fixed

Fixed for any bug fixes.

### Security

Security in case of vulnerabilities.