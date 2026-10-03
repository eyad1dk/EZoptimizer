# Architecture

WPF and .NET 10 LTS remain the native stack. `MainWindow` contains view lifecycle, theme/UI interactions and validation hooks. `MainViewModel` owns navigation, command state, staged targets and async orchestration. It delegates Windows writes and recovery to services rather than holding native mutation code.

| Module | Responsibility |
|---|---|
| Design.xaml | Dynamic colors, typography, spacing, focus and native control templates |
| Presentation.cs | Adaptive card layout and presentation-only labels/icons; no operation execution |
| MainWindow.xaml | Scrollable views, cards, persistent review queue and accessible names |
| Domain.cs | Stable catalog IDs/versions, typed-value validation, planning, eligibility and recommendation rules |
| WindowsSettings.cs | Allowlisted SPI preferences and installed power plans; fixed system executable, no shell |
| Tuning.cs | Intent-before-write journals, v1 migration, atomic flush/rename, state transitions, conflict-aware compensation and retry |
| ProfileStore.cs | Strict desired-value profiles and persistent queue; no machine originals or executable content |
| SystemProbe.cs | Non-overlapping volatile samples, cached WMI inventory, partial process/startup/app inventories, policy checks and bounded endpoint diagnostics |
| LocalServices.cs | Structured logs, reviewed/redacted reports and dedicated System Restore helper |
| Updates.cs | Bounded fixed-host HTTPS metadata checks, channels and manual-only release flow |
| tests/ForgePC.Tests | Simulated recovery/contracts plus pure safety validation; no native mutation |

Boolean preferences use exact On/Off and power plans use GUIDs. Catalog metadata declares evidence, risk, dependencies, restart and recovery requirements. Unsupported system-level requirements are rejected by planning; no such mutation is exposed. The small catalog has no dependency graph beyond uniqueness and eligibility today. New dependency/conflict semantics need dedicated tests before expanding it.

Apply snapshots all originals before any write; each operation saves applying state, rechecks current value/capability, writes, reads back and saves the result. A failed batch stops and compensates in reverse order. The journal remains active until every owned setting is verified restored. Prepared/unattempted operations do not get written during compensation. Cancellation occurs at operation boundaries, not halfway through a native write.

No privileged IPC channel is exposed. The elevated helper accepts exactly `--create-restore-point`, not frontend configuration. All settings mutations remain ordinary-user operations. The Windows provider and installed plan/policy state remain authoritative.

Polling is serialized at 2 seconds while active and 10 seconds while inactive/minimized. Native power helper calls have an 8-second timeout. WMI enumeration has bounded timeouts/row counts; caller deadlines bound wait time, although Windows WMI work may finish in the background. Network probes have a 3-second overall wait per ICMP attempt and five attempts. Update metadata has a 12-second deadline and 1 MiB limit including streamed content. No temperature is inferred and no kernel driver is installed.

Logs rotate independently of history. Reports export only an explicit allowlist, excluding free-text operation results as well as process, startup, network and user-identifying fields. No data is sent automatically.
