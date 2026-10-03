# Baseline audit

Inspected 2026-10-03: 2310502c9f3b0d6a0c369690cabe8f49afeef8b7, origin/main and v0.1.0. No repository AGENTS.md was found. Read README, CONTRIBUTING, SECURITY, release workflow, all source and the eight regression checks; preserved the original overview capture as docs/v0.1-overview.png.

The brief's baseline description is accurate. Strengths are intent-before-write persistence, read-back verification, saved originals, external conflict preservation, standard-user startup and the narrow control set. Gaps: no journal schema or malformed-file isolation; no per-write revalidation; no compensating rollback or cancellation; first CPU sample incorrectly appears as 0%; expensive UI-thread reads; no cached inventory; monolithic window; no stored queue/custom profiles; no restore integration or update checking.

Retain the three proven controls. Broader categories use read-only diagnostics or explicit Windows links where a stable reversible mutation contract is unavailable. DNS rewriting, package removal and registry-based privacy edits are deliberately excluded.

Framework decision: .NET 8 reaches end of support 2026-11-10. Move to .NET 10 LTS (support through 2028-11-14), preserving WPF and portable x64. System.Management 10.0.12 is the only added package, for documented WMI inventory/System Restore APIs. SDK 10.0.401 was installed into the workspace for this build. Production machines must use a Windows release eligible for security servicing; the Windows 10 reference host's ESU enrollment is unknown.

References checked:
- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- https://support.exmtweaks.com/en/using-exm
- https://support.exmtweaks.com/en/exm-vs-competitors
- https://storage-asset.msi.com/files/pdf/X_BOOST_User_Guide_EN.pdf
- https://learn.microsoft.com/en-us/windows/win32/sr/using-system-restore
- https://learn.microsoft.com/en-us/windows/win32/sr/createrestorepoint-systemrestore

EXM's categories and review/recovery workflows inform product organization. Its counts and impact statements remain vendor claims, not evidence for this app. MSI X-BOOST is a separately identified MSI product; the intended third-party “XBoost” identity remains unverified. No proprietary code, artwork or tweak packs are used.
