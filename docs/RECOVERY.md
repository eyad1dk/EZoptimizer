# Recovery

1. Keep `%LOCALAPPDATA%\EZoptimizer\history` when replacing the executable. Active journals are never pruned.
2. Open **Recovery & History**, or launch `EZoptimizer.exe --recovery` if hardware probing prevents normal startup.
3. Expand operation results. **Undo / retry exact restoration** restores only saved originals that still match the applied target, or confirms values already equal to the original.
4. If another app changed a value, it is a conflict. EZoptimizer preserves it. Review that external change in Windows. A retry succeeds only when it is safe; there is no force-original override in this preview.
5. A failed/cancelled batch attempts compensation. Any failed restoration remains durably active for retry. Disk access/policy may still block recovery; retain the records and report the correlation ID.

v0.1 unversioned records migrate in memory to schema 2 without replacing original data on read. A later verified save writes the new schema. Unknown future versions and malformed files are isolated in `history\quarantine` with random names; valid records load independently. New applies are blocked while quarantine exists. Do not clear quarantine until its originals and ownership have been reviewed; support tooling for safe adjudication is deferred.

App and menu preference values are explicit On/Off; power originals are GUIDs. These operations use documented Windows APIs and do not edit optional registry values. General absent/present registry recovery is intentionally not implemented because arbitrary registry operations are not supported.

Only one session may remain active. A per-user global mutex prevents normal simultaneous app instances across Windows sessions; the engine serializes batches within the process. Custom engine hosts must provide their own single-instance boundary.

Game sessions track an explicit PID plus process-start identity to avoid PID reuse. Settings are restored on observed exit or normal app exit. If shutdown/crash prevents restoration, the saved game-session journal remains available on next startup; monitoring does not silently resume.

System Restore is optional supplementary protection. Inspection reports provider accessibility and retained points, not full drive protection coverage. Creation respects Windows frequency behavior, does not enable protection or change quotas, and verifies a new sequence/description before claiming success. A skipped or unverified result is never shown as created. Use Windows System Protection / recovery tools for broad restoration; the app opens them but never initiates it automatically.
