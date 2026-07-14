---
name: playmode-logs-help
description: Explain Play Mode Log Saver, its installed skills, settings and output paths, capture filters, saved-log format, menus, and privacy-safe read-only diagnostics.
---

# Play Mode Log Saver Help

Answer in the user's language. Explain workflows without entering Play Mode, creating settings, saving Console logs, changing filters, or reading log contents unless the user separately requests a summary.

1. Read `PACKAGE_SKILLS.md` first. Treat its generated package identity, complete related-skill table, `$skill-name` invocations, descriptions, and access values as authoritative.
2. Read `Packages/com.actionfit.playmodelogsaver/README.md` and `AI_GUIDE.md` when available. If downloaded, resolve `Library/PackageCache/com.actionfit.playmodelogsaver@*` without editing it.
3. Explain the settings asset, default `Assets/Logs` output, PlayMode capture lifecycle, timestamped `*_PlayLog.txt` files, stack-trace option, and error/warning-only filter.
4. Separate privacy-safe latest-log summarization from state-changing actions such as entering Play Mode, changing settings, saving Console logs, creating folders, or deleting logs.
5. List `Open Window`, `Setting SO`, and `README` under `Tools > Package > Play Mode Log Saver`.

State that summaries must not reproduce whole logs, credentials, tokens, user identifiers, payloads, private URLs, or machine-specific home paths.
