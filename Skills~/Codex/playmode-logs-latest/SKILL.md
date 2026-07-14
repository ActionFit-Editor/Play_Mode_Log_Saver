---
name: playmode-logs-latest
description: Locate and summarize the latest saved PlayMode log without modifying, copying, uploading, deleting, or exposing sensitive log content. Use for recent runtime diagnostics.
---

# Summarize The Latest PlayMode Log

Read only the minimum local evidence needed and keep the settings asset and every log file unchanged.

1. Read repository instructions and the package `README.md` and `AI_GUIDE.md`. Resolve the exact absolute project or worktree and record `git status --short`.
2. Locate existing `PlayModeLogSettings.asset` files with `rg --files`. Read only the serialized `_logFolderPath` field needed to resolve the log directory. If no settings exists, inspect the documented default `Assets/Logs`.

Do not access `PlayModeLogSaver.Settings`; that property may create a settings asset and folders.

3. Within the resolved directory, select the newest regular file whose name ends in `_PlayLog.txt`. Use file modification time, with the timestamped filename only as supporting evidence. If the directory is outside the project, avoid exposing its parent path in the report.
4. Read the header counts and only enough `[WARNING]`, `[ERROR]`, and `[EXCEPTION]` entries to identify distinct failures. Group duplicates, cap examples, omit routine `[LOG]` noise, and do not reproduce full stack traces.
5. Redact credentials, authorization headers, tokens, emails, installation/player IDs, reward or backend payloads, URL query strings, and user/home paths. Report file basename, capture interval, total/log/warning/error counts, top distinct problems, likely first actionable failure, and whether no saved log exists.
6. Confirm the selected file size and modification time are unchanged and re-run `git status --short`.

Never enter or stop Play Mode, open the settings menu, save Console logs, touch timestamps, normalize line endings, copy or upload a log, delete files, or paste the full log into chat or an issue.
