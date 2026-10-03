# EZoptimizer v0.2.0 preview

A substantial native upgrade with a reviewed-change workflow and stronger recovery.

- Seven pages, shared design resources, dark/light/Windows/high-contrast themes and lazy page creation.
- Persistent desired-value queue and exact previews; stale assumptions and policy are rechecked before every write.
- Schema-2 per-operation journals, v0.1 history migration, malformed-history quarantine, cancellation, compensation and conflict-preserving retry.
- Conservative Gaming/Coding/Everyday/Quiet/Battery Saver presets and strict custom-profile import/export.
- Cached hardware inventory, timestamped measurements, explained recommendations, partial process/startup/app inventory and adapter/DNS inspection.
- Explicit running-game selection, automatic undo attempts on observed game exit or normal app exit, and durable recovery after interruption.
- Chosen-endpoint bounded ICMP tests, local before/after sample comparisons, reviewed redacted reports and rotating local logs.
- Optional supplementary System Restore inspection/creation with a fixed reviewed UAC helper; no forced protection or throttling changes.
- Manual official stable/preview release checks. No executable updates are downloaded or installed by the app.

39 default regression checks pass. Native read-only smoke and WPF layout/render checks pass on Windows 10 x64. Windows 11, mutating native integration, actual System Restore creation, full Narrator and physical DPI/high-contrast transitions remain unverified. See docs/VALIDATION.md and docs/FEATURES.md for evidence and deferred work.

The portable executable includes .NET 10 and is unsigned. SHA256SUMS.txt checks file integrity, not independent publisher identity. Preserve %LOCALAPPDATA%\EZoptimizer\history through upgrades. This release was built locally; GitHub Actions execution is blocked by the account's billing restriction.
