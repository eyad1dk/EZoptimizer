# Feature matrix

| Area | Implemented control | Read-only diagnostics / guidance | Deferred and prerequisite |
|---|---|---|---|
| System | Two animation preferences, installed power plan, session keep-awake | CPU/RAM/free space, OS/CPU/GPU driver/storage/battery, startup inventory | Startup toggles: exact mechanism/absence/type/view capture and VM testing |
| Gaming | Reviewed opt-in session with process identity and exact undo | Game Mode, captures, graphics and display Settings shortcuts | FPS/frame-time integration and real game exit tests in disposable environments |
| Profiles | Five presets, desired-only custom import/export, persistent queue | Missing-plan explanation and machine-specific preview | Future schema migration when another profile schema exists; v1 is the first custom-profile format |
| Privacy | No direct Windows privacy mutation | Advertising/personalization and camera-permission shortcuts | Additional controls require stable documented policy-aware interfaces |
| Debloat | No app/package removal | User/machine uninstall-registry inventory; Windows uninstall workflow | Complete packaged/provisioned/framework classification, removal and explicit data-loss contract |
| Network | No DNS changes or resets | Adapter/DNS inventory, chosen-endpoint bounded ICMP samples | Per-adapter DHCP/static/VPN/policy-safe changes and rollback integration tests |
| Maintenance | No permanent deletion | Windows Storage Sense and Update shortcuts | Allowlisted app-owned cache cleanup with reparse safety and honest recovery |
| Recovery | Durable schema-2 journals, v0.1 migration, verification, compensation, cancellation, conflict retry | Recovery-only startup; provider inspection; reviewed System Restore helper | Real restore creation/frequency/denial VM testing; quarantine-adjudication tooling |
| Updates | Manual fixed-repository stable/preview checks | Official release page, offline/rate-limit/interruption states | Automatic executable replacement requires publisher signing and trusted signed metadata |
| Measurements | Timestamped actual samples and local two-window comparison | Benchmark repeat guidance; CPU/RAM limitations | Manual benchmark import and workload-completion instrumentation |
| Accessibility | Focus borders, native control peers/names, scrolling, high contrast resources and live status announcements | Four-theme/three-scale layout smoke and actual WPF renders | Full Narrator, actual OS scaling/high contrast, keyboard traversal and long-localized text audit |

All shortcuts are labeled as such and do not masquerade as implemented optimizations. No speculative tweak count, performance score or guaranteed gain is shown. Unknown battery/media/capability values remain unknown. OEM/Modern Standby availability is handled through existing installed plans rather than fabricating missing plans. Managed restrictions block execution rather than being bypassed.

## Milestones

1. Audited the public baseline commit and preserved its capture; kept WPF and chose supported .NET 10 LTS.
2. Split native settings from a migrated journal engine; regression coverage expanded from 8 to 29 checks.
3. Connected a persistent review queue to MVVM, exact preview, apply, progress, cancellation and retry across seven native pages.
4. Added conservative/custom profiles, cached hardware recommendations and supplementary restore-point integration.
5. Connected system/gaming/privacy/debloat/network/maintenance controls, inventories and clearly labeled shortcuts.
6. Added logs, reviewed exports, manual update safety, 39 default checks, portable packaging, layout renders and measured overhead. Remaining acceptance gaps are listed in VALIDATION.md rather than treated as passed.
