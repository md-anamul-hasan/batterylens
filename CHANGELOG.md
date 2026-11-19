# Changelog

All notable changes to BatteryLens. Versioning follows [Semantic Versioning](https://semver.org/); the version lives in `Directory.Build.props`.

## [1.2.0] - 2026-11-19

### Changed
- New look: Technical Glassmorphism v1.0, the design system of mdanamulhasan.com. Slate canvas, Precision Cobalt accent with PLM teal highlights in dark mode, hairline borders, crisp 6/10/14 px corners, restrained shadows.

## [1.1.0] - 2026-10-22

First open-source release (MIT). Earlier builds were a paid product; this release removes licensing entirely, so every feature is free.

### Added
- Reads battery design and full-charge capacity, cycles, charge state and power draw from Windows.
- Verdicts for the charger, the battery and the computer (KEEP / REPLACE / CALIBRATE) with typical costs.
- Simulator with sample batteries and live sliders; works on desktops and VMs.
- HTML report export and the official Windows battery report.
- Buying guide and calibration steps; light and dark themes; self-contained installer.

### Removed
- Licence-key activation, the licence dialog, evaluation limits and the licence-server connection. Everything is unlocked.
