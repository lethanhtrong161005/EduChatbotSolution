# Admin Reports & Experiment Pages — Stub Services Note

**Author:** Teammate C's agent  
**Date:** 2026-07-13  
**Purpose:** Track what must be removed / replaced when real Business layer service implementations arrive.

---

## What the Stub Is

Three files were added temporarily so the admin frontend pages (`/admin/reports`, `/admin/experiments/results`, `/admin/experiments/compare`) can be viewed in the browser **before** the real Business layer services are implemented.

| File | Role |
|------|------|
| `PresentationLayer/Services/Stubs/StubAdminReportService.cs` | Implements `IAdminReportService` — returns hard-coded dashboard fixture data |
| `PresentationLayer/Services/Stubs/StubExperimentService.cs` | Implements `IExperimentService` — returns hard-coded experiment list / result / comparison fixture data |
| Registration block in `PresentationLayer/Program.cs` (~line 95) | Registers both stubs with the DI container |

---

## What to Delete When Real Services Are Ready

### Step 1 — Delete the two stub files

```
PresentationLayer/Services/Stubs/StubAdminReportService.cs   ← DELETE
PresentationLayer/Services/Stubs/StubExperimentService.cs    ← DELETE
```

If the `Stubs/` folder is empty after deletion, delete it too.

### Step 2 — Remove the registration block from `Program.cs`

Find and remove these lines (clearly marked with a `STUB` comment):

```csharp
// ── STUB: Frontend preview — TODO: remove when real services are registered ─
// Added 2026-07-13 by Teammate C's agent so admin pages load without the
// Business layer implementations. Replace both lines with real registrations.
builder.Services.AddScoped<IAdminReportService, Presentation.Services.Stubs.StubAdminReportService>();
builder.Services.AddScoped<IExperimentService, Presentation.Services.Stubs.StubExperimentService>();
// ────────────────────────────────────────────────────────────────────────────
```

### Step 3 — Register the real implementations

Replace the removed block with the real scoped registrations, for example:

```csharp
builder.Services.AddScoped<IAdminReportService, AdminReportService>();
builder.Services.AddScoped<IExperimentService, ExperimentService>();
```

(Exact class names depend on what the Business layer owner provides.)

---

## Stub Behaviour Summary

| Page | Stub returns |
|------|-------------|
| `/admin/reports` | 7-day window, 74 active users, 216 completions, 2 subjects (DB201/AI301), 2 hot documents, 3 indexing issues |
| `/admin/experiments/results` | 3 experiments: 2 Completed (IDs `20000000-...-001`, `20000000-...-002`), 1 PreparingIndex (ID `20000000-...-003`) |
| `/admin/experiments/compare?leftId=...&rightId=...` | Pairwise comparison of the 2 completed experiments, 4 metrics, 3 questions |

---

## Why 500 Errors Happened Before This Fix

The **original** stub implementations tried to resolve fixture JSON files by walking up from `AppContext.BaseDirectory` at runtime. This worked locally but **failed in Docker** because the `.agents/` folder is not copied into the container image. The rewrite uses **fully embedded inline C# data** — no filesystem access at all.

---

## Service Interfaces (for reference)

```csharp
// Domain/Contracts/IAdminReportService.cs
public interface IAdminReportService
{
    Task<AdminReportDashboardDto> GetDashboardAsync(
        ReportRange range, ReportRoleFilter role, int? trendSubjectId,
        CancellationToken cancellationToken = default);
}

// Domain/Contracts/IExperimentService.cs
public interface IExperimentService
{
    Task<ExperimentCreateOptionsDto?> GetCreateOptionsAsync(int subjectId, CancellationToken cancellationToken = default);
    Task<ExperimentIndexPreflightDto?> PreflightAsync(ExperimentIndexPreflightRequest request, CancellationToken cancellationToken = default);
    Task<CreateExperimentResponse> CreateAsync(CreateExperimentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExperimentSummaryDto>> GetSummariesAsync(CancellationToken cancellationToken = default);
    Task<ExperimentResultDto?> GetResultAsync(Guid experimentId, CancellationToken cancellationToken = default);
    Task<ExperimentComparisonDto?> CompareAsync(Guid leftExperimentId, Guid rightExperimentId, CancellationToken cancellationToken = default);
}
```
