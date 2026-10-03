# Validation and compatibility

## Local evidence · 2026-10-03

Baseline: 2310502c9f3b0d6a0c369690cabe8f49afeef8b7. Current preview: v0.2.0, .NET 10.0.12, SDK 10.0.401, Windows x64 portable.

| Check | Result | What it establishes |
|---|---|---|
| Release build | Pass, zero warnings/errors | Source and WPF resources compile |
| Default regression suite | 39 checks pass | Simulated recovery/migration/failures plus strict profiles, update/manual-install gates, endpoint validation and export/log redaction |
| Self-contained single-file publish | Pass | Packaged executable is produced with its own runtime |
| Packaged --smoke-test | Exit 0 | Native memory/power/preferences/inventories, UI construction and staging/preview; no Windows tuning writes |
| Packaged --ui-check | Exit 0, 84 layouts | Exactly the selected page is instantiated and measurable across 7 pages × 4 themes × 3 scales at 1366×768 equivalent; status has its automation name |
| --render-preview | Pass | Actual WPF content rendered from real local readings; overview, light 150%, high contrast 200% |
| Native desktop capture | Not completed | FrameArrived/window-capture timeouts persisted after one refresh/retry; built-in renders are labeled as such |
| Hosted GitHub Actions | Blocked | Account/billing restriction; no hosted CI success is claimed |

The 39 default checks preserve the original eight scenarios and cover saved originals, service restart, stale previews, write/read-back failures, compensation, interrupted undo, external conflicts, one-active ownership, schema migration, malformed/path-mismatched records, disk/permission/save failure, policy changes, cancellation, unsupported capabilities, strict profiles, unknown/disallowed operations, persistent desired values, update host/channel/offline/rate-limit/interruption handling, no unsigned install path, missing measurements, restore-result mapping, network target validation, redacted reports/logs, and consistency/retry after a rollback save failure.

No default check calls native tuning writes, creates a restore point, terminates a process, removes a package or sends a network probe. Restore creation tests verify result mapping, not real provider behavior. The unsigned-install test verifies there is no installation path; it is not a test of a configured signature verifier. Future registry absence/type/view operations and dependency graphs are deferred, not silently claimed tested.

## Measured application overhead

Measured the packaged preview with its own `--measure-performance` mode: one visible-idle and one minimized window, each sampled for 20 seconds after a 10-second settle period. [Raw measurements](performance.json).

| Phase | CPU, share of whole 6-core machine | Average working set | Peak working set |
|---|---:|---:|---:|
| Visible idle | 0.22% | 112.9 MiB | 124.7 MiB |
| Minimized | 0.21% | 113.3 MiB | 114.3 MiB |

Startup-handler entry to inventory-ready was 662 ms. This excludes process launch/extraction before managed startup and is **not** a cold-start measurement. WPF rendering tier was 2. Background apps, disk cache, graphics state and Windows services were uncontrolled. These are local app-overhead measurements, not evidence of gaming or coding gains.

Earlier visible runs showed 4–6% machine CPU while a desktop inspection tool was querying accessibility. A trace showed substantial automation-provider traversal. Pages now instantiate on selection, and capture attempts were stopped before the final run. That changes both UI cost and measurement conditions, so these numbers must not be presented as a controlled percentage speedup.

## Compatibility matrix

| Environment | Evidence | Status |
|---|---|---|
| Windows 10 Pro 22H2 x64, build 19045; i5-9400F; GTX 1660 SUPER; SATA SSD/HDD; AC | Local build/publish, read-only native checks, WPF rendering and measured overhead | Preview-tested for those reads/UI paths; ESU enrollment unknown |
| Windows 11 x64 | Supported API/runtime target and OS-build eligibility logic | Actual VM/hardware validation pending |
| Standard-user native mutation | Default startup does not request UAC; policy and verification gates implemented | Exact-write/undo integration in disposable VM pending |
| Admin restore-point helper | Fixed documented WMI method, separate review/UAC, verified sequence result logic | Real created/skipped/frequency/denied behavior pending disposable VM tests |
| OEM laptop / Modern Standby / hybrid GPU / battery / managed policy | Existing plans only, unknown states explicit, policy checks and conservative presets | Hardware/policy matrix testing pending |
| Game-session exit and app exit | Explicit PID+start identity and engine-backed restoration implemented | Real native game/session integration pending |
| ARM64 | No ARM64 release | Deferred until verified native compatibility |

## Accessibility limits and next release gates

Shared focus borders, native peers/names, keyboard-capable controls, a live-region status event, scalable WPF layout, Windows high-contrast brushes and scrolling are implemented. Layout checks emulate logical size and render DPI; they do not change the actual OS display setting. The minimum window is 640×360 DIPs. At 200% on 1366×768, navigation and content scroll. The review area has a bounded height and its own scroll path so it cannot consume the entire window.

Manual Narrator traversal, keyboard Tab/arrow/Enter sequences, real OS text/DPI/high-contrast transitions, long translated strings, restore UAC cancellation, unplugged/restricted WMI states and startup/app inventory completeness still need hands-on verification. Text size follows DPI; independent Windows text-size behavior is not fully verified. Do not call this preview fully accessibility-certified or universally compatible.

Before expanding mutations or calling a stable release, run disposable Windows 11 and supported-serviced Windows 10 VMs with snapshots. Exercise each real preference write/undo, denied policies, interrupted processes, helper frequency/creation outcomes, game exit, app exit, power changes and upgrade recovery. Keep the existing v0.1 fixtures and active journals through every test.
