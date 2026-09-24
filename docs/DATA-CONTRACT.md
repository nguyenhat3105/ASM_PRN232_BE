# Existing schema contract

The supplied database is authoritative. No application startup creates, migrates, resets or seeds it.

- Departments: name required, max 100; description required, max 300.
- Projects: name required, max 200; start date required; end date nullable and not before start; status 0..3; department required.
- Tasks: title required, max 300; status and priority 0..3; project required; due date nullable.
- Tags: name required, max 50, unique; optional six-digit hex color.
- TaskTag is many-to-many with a composite primary key. Updates replace tags; omitted or empty TagIDs clears tags.
- Public reads hide inactive entities and children of inactive parents.
- Department and project deletion is physical only when there are no linked rows, including inactive children. Tag deletion checks all TaskTag rows, including inactive tasks.
- Task deletion is soft. Restore and status-only updates are additive schema-preserving endpoints.
- SQL timestamps are `timestamp without time zone`. Existing seed timestamps are displayed as recorded; new server timestamps use an explicitly documented UTC wall-clock convention without changing column types.
- Date-only values stay date-only. Overdue uses the configured business timezone, excludes Done/Cancelled, and has no dependency on browser UTC conversion.
- No time-series or historical completion metrics are fabricated from ModifiedDate.

## Pending schema extensions

Authentication, memberships, assignees, comments, attachments, durable notifications, audit history, dependencies and saved cross-device views require a later schema migration. They must not be simulated using unrelated columns or browser-only storage.
