# Security and recovery boundaries

v0.2.0 is an unsigned preview. Download only from this repository's releases. A SHA-256 file alongside a binary is an integrity check, not independent proof of publisher identity. Code signing and trusted signed update metadata are not configured. The app has no executable-download or replacement path; a failed or unsigned check cannot install anything.

The ordinary app runs as the current user. The System Restore helper starts only after a separate review and UAC request. It accepts exactly one fixed switch, calls a fixed documented WMI method and verifies a new point by sequence and description. It accepts no paths, script, profile, executable target or writable privileged request file. Creation must still be integration-tested in a disposable VM.

Profiles are bounded JSON with a strict schema and three allowlisted operation IDs. Targets are Boolean preferences or a GUID for an installed plan. Unknown fields, IDs and versions are rejected. Power commands use the system executable and argument lists; there is no shell invocation. Managed restrictions are authoritative.

The engine records intent before writes, rechecks assumptions and policy, verifies writes and attempts compensating rollback. Active recovery is never pruned. External conflicts are preserved. Quarantine is not permission to delete records: unknown originals may still matter. Keep all history through upgrades.

Do not post tokens, passwords, personal paths or full reports in public issues. Use GitHub private vulnerability reporting if the owner has enabled it; otherwise contact the owner privately before public disclosure. Diagnostic exports are explicit and reviewed, and omit free-text operation results and identifying inventory fields by default. Logs record redacted types/HRESULTs with correlation IDs rather than exception messages.

No Defender/firewall/UAC/updates/VBS changes, arbitrary scripts, registry cleaners, broad service disabling, RAM purges, forced process termination, destructive cleanup, DNS rewrites or package-removal automation are included.
