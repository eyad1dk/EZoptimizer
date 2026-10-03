# EZoptimizer v0.3.0

- Removed the custom logo from the app interface and executable resources.
- Expanded to 15 documented Windows preference controls, with exact preview, verified apply and conflict-preserving undo.
- Added optional reduced desktop motion to profiles.
- Added display/driver/power readiness checks and bounded, read-only coding workspace inspection.
- Added frame-time CSV baseline/comparison: average FPS, 1% low, p95 and p99 frame times.
- Added unanswered-request percentage to endpoint measurements.

51 simulated regression checks and 84 layout combinations pass, alongside read-only native smoke checks. No Windows settings were changed during validation. Native mutation testing on disposable Windows 10/11 environments remains open. Unsigned x64 preview; no guaranteed performance gains or complete EXM/BoosterX feature parity.
