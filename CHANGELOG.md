# Changelog

All notable changes to **MasterBuilderHeeler** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-11-02

### Added

- `ResetBuildSettingsKey`, F10 by default. Puts rotation, steps and grid back to defaults.

### Changed

- Putting the hammer away doesn't reset your rotation and build settings anymore. Did that on purpose originally but
  people complained about it, so it's gone. Nudge offset clears when you put it away, dying or logging out resets everything.
- Five step presets now, UltraFine to Coarse, `-` and `=` to go down and up. Fine is the default preset. Standard is 22.5
  so it matches the game.
- Step size settings and the step keys got renamed, if you changed or rebound them you'll need to redo it.
- Finishing an in-place edit gives the ghost its old rotation back.

## [1.1.0] - 2026-11-01

### Fixed

- Repair and dismantle in Build Camera check workbench range at the piece now, same as placing. No more repairing stuff
  way out of range just because you're standing next to a workbench.
- `ValidateStationRangeAtPieceLocation` now covers repair and dismantle on foot too.

### Changed

- Infinity Hammer is flagged as incompatible, so this won't load alongside it. A HarmonyX bug in the version BepInEx ships
  breaks rotate and nudge in Precision Mode when both are installed, and BepInEx isn't updating 5.x. Details in the README.
- Fixed a few config descriptions that drifted during dev, and added a settings table and more setup info to the README.
- Build tooling now matches the rest of my mods.

### Removed

- `LockConfiguration`. Never actually implemented it and forgot to take it out.

## [1.0.1] - 2026-10-31

*The one where the first bug rears its head*

### Added

- For Searica: `GizmoScale` setting in `General`. Sets the rotation gizmo size from 0.25x to 4x. Applies in real time.

### Changed

- The build menu cannot be opened during an in-place edit. This is really more of a result of the fix below for
  the placement bug, but wanted to call it out. Never should have been opening in edit mode anyways.

### Fixed

- Clicking during an in-place edit no longer builds the selected piece if one was selected prior to entering edit mode.
  Repair, dismantle, and copy are also blocked until the edit ends. No more extra walls in your portals, kacdaddy.
- `RequireHammerForHoverInspection` now defaults to on. Existing configs are whatever you already had them set to.
  This was bugging people as a default, and I agree, so swapped the default here. Thanks, kacdaddy.

## [1.0.0] - 2026-10-30

Initial release.

### Added

- Everything. This is the initial release. There are bound to be bugs. Please give feedback.
- This is a culmination of over a weeks work of long night sessions crunching on code. There will be bugs. Please give feedback.
- Did I mention feedback? :D
- Motivation: Hopefully this helps everyone move on, and gives thunderstore users a solid, reliable, upkept solution for Building Placement and Build Hammers Camera.
- Thanks Searica - Really truly, thanks. It's crazy, but having more fun crunching on code than I have in a long time.
