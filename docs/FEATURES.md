# Feature matrix

| Area | Implemented control | Read-only diagnostics / guidance | Deferred and prerequisite |
|---|---|---|---|
| System | Ten visual/dragging preferences, four numeric input preferences, installed power plan, session keep-awake | CPU/RAM/free space, OS/CPU/GPU driver/storage/battery, startup inventory | Startup toggles: exact mechanism/absence/type/view capture and VM testing |
| Gaming | Reviewed opt-in session with process identity and exact undo | Display/driver/power readiness, frame-time CSV import; Game Mode, captures, graphics and display Settings shortcuts | Live capture integration and real game exit tests in disposable environments |
| Profiles | Five presets, desired-only custom import/export, persistent queue | Missing-plan explanation and machine-specific preview | Future schema migration when another profile schema exists; v1 is the first custom-profile format |
| Privacy | No direct Windows privacy mutation | Advertising/personalization and camera-permission shortcuts | Additional controls require stable documented policy-aware interfaces |
| Debloat | No app/package removal | User/machine uninstall-registry inventory; Windows uninstall workflow | Complete packaged/provisioned/framework classification, removal and explicit data-loss contract |
| Network | No DNS changes or resets | Adapter/DNS inventory, chosen-endpoint bounded ICMP samples | Per-adapter DHCP/static/VPN/policy-safe changes and rollback integration tests |
| Maintenance | No permanent deletion | Windows Storage Sense and Update shortcuts | Allowlisted app-owned cache cleanup with reparse safety and honest recovery |
| Recovery | Durable schema-2 journals, v0.1 migration, verification, compensation, cancellation, conflict retry | Recovery-only startup; provider inspection; reviewed System Restore helper | Real restore creation/frequency/denial VM testing; quarantine-adjudication tooling |
| Updates | Manual fixed-repository stable/preview checks | Official release page, offline/rate-limit/interruption states | Automatic executable replacement requires publisher signing and trusted signed metadata |
| Measurements | Timestamped actual samples and local two-window comparison | Benchmark repeat guidance; CPU/RAM limitations | Live capture and workload-completion instrumentation |
| Accessibility | Focus borders, native control peers/names, scrolling, high contrast resources and live status announcements | Four-theme/three-scale layout smoke and actual WPF renders | Full Narrator, actual OS scaling/high contrast, keyboard traversal and long-localized text audit |

All shortcuts are labeled as such and do not masquerade as implemented optimizations. No speculative tweak count, performance score or guaranteed gain is shown. Unknown battery/media/capability values remain unknown. OEM/Modern Standby availability is handled through existing installed plans rather than fabricating missing plans. Managed restrictions block execution rather than being bypassed.

## Milestones

1. Audited the public baseline commit and preserved its capture; kept WPF and chose supported .NET 10 LTS.
2. Split native settings from a migrated journal engine; regression coverage expanded from 8 to 29 checks.
3. Connected a persistent review queue to MVVM, exact preview, apply, progress, cancellation and retry across seven native pages.
4. Added conservative/custom profiles, cached hardware recommendations and supplementary restore-point integration.
5. Connected system/gaming/privacy/debloat/network/maintenance controls, inventories and clearly labeled shortcuts.
6. Added logs, reviewed exports, manual update safety, 39 default checks, portable packaging, layout renders and measured overhead. Remaining acceptance gaps are listed in VALIDATION.md rather than treated as passed.

## v0.3.0 additions

Frame-time import accepts `FrameTimeMs` or `MsBetweenPresents`, one process/swap chain, 30–200,000 positive samples, and at most 8 MB. Average FPS is 1000 / mean frame time. The 1% low is 1000 / mean of the slowest ceil(1%) frame times. Percentiles use nearest rank. CSVs are kept local. Matching workload labels do not prove matching experimental conditions.

Coding inspection reads a selected local folder with 25,000-entry, eight-level and three-second traversal limits. Linked entries are skipped. Reported logical sizes in common dependency/output folders are not a cleanup recommendation; no deletion is performed.

Native preferences use [documented Windows interfaces](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow). Input preferences affect desktop behavior, not game polling rates or raw-input latency. Modern apps may ignore classic effects.

### Feature comparison scope

[EXM](https://support.exmtweaks.com/en/using-exm) and [BoosterX](https://boosterx.org/en/howitworks/) describe profiles, Windows settings, gaming tools and diagnostics. EZoptimizer covers these categories with the implemented features above; this is not a claim of complete feature parity. Firmware changes, security disabling, blanket service changes, undocumented registry tweaks, arbitrary scripts, package removal and irreversible cleanup are excluded. Source code and UI are independently implemented.
