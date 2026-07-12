# Background Jobs and User Import Process

This document explains the implementation of background jobs using Hangfire in EduChatAI, specifically focusing on the User Import feature and how to monitor it.

## Overview

We use **Hangfire** for reliable background processing. It is backed by PostgreSQL, ensuring jobs are persisted and can survive application restarts.

### Key Components

- **Hangfire Storage:** Configured in `PresentationLayer/Program.cs` to use PostgreSQL.
- **Hangfire Dashboard:** Accessible at `/admin/hangfire` (requires admin privileges).
- **Background Job Client:** Injected via `IBackgroundJobClient` to enqueue tasks.

## User Import Feature

The User Import feature allows administrators to upload an Excel file containing a list of users to be imported in the background.

### The Flow

1. **Upload & Validation:** The admin selects an `.xlsx` file. `UserManage.cshtml.cs` validates the file size and extension, then uses OpenXML to parse and validate the rows.
2. **Batch Creation:** Valid rows are stored in the `UserImportRows` table, linked to a `UserImportBatches` record. The file is uploaded to Supabase Storage.
3. **Job Enqueue:** The `UserManage.cshtml.cs` enqueues a Hangfire job:
   ```csharp
   backgroundJobs.Enqueue<UserImportJob>(job => job.ExecuteAsync(batch.Id, CancellationToken.None));
   ```
4. **Background Processing (`UserImportJob.cs`):** 
   - The job retrieves the pending batch and rows from the database.
   - It iterates through each row, calling `IUserManagementService.CreateUserAsync`.
   - After processing each row, it pushes real-time progress updates to connected SignalR clients using the `import-batch` resource type.
   - The UI listens for these SignalR events to update the progress bar.
   - Upon completion, it marks the batch as completed.

### SignalR Real-time Progress

The client subscribes to the `"import-batch"` SignalR group. When a progress update is pushed, the UI updates the progress bar and shows any errors that occurred for specific rows in real-time.

## Viewing Hangfire Logs and Job Status

As a developer or administrator, you might need to inspect the background jobs if something goes wrong.

### 1. Using the Hangfire Dashboard

The most user-friendly way to view background jobs is through the built-in Hangfire Dashboard:

1. Log in to the application with an **Admin** account.
2. Navigate to `https://<your-domain>/hangfire`. (Note: The URL path depends on your exact setup in `Program.cs`, but usually it is mapped to `/hangfire` or similar).
3. The dashboard provides tabs for:
   - **Enqueued:** Jobs waiting to be processed.
   - **Processing:** Jobs currently running.
   - **Succeeded:** Jobs that completed successfully.
   - **Failed:** Jobs that encountered an unhandled exception.

In the **Failed** tab, you can click on a specific job to see the full stack trace and error message.

### 2. Monitoring the Import History UI

For the User Import feature specifically, you do not need to check Hangfire for basic errors. The import history modal tracks all outcomes:
- Click the **"View Import History"** button in the Import Users modal.
- Click **"Details"** on any past import to see exactly which row failed and the associated error message (e.g., "Email already exists" or "Invalid role").

### 3. Database Inspection

All Hangfire data is stored in the same PostgreSQL database, prefixed with `hangfire`.
- Table `hangfire.job`: Contains job definitions, arguments, and current state.
- Table `hangfire.state`: Contains the history of state transitions (Enqueued -> Processing -> Succeeded/Failed).
- If a job is stuck or needs to be purged, you can do so via the Hangfire Dashboard or directly in the database (though the dashboard is highly recommended).

## Troubleshooting Import Jobs

**Symptom:** The import progress bar in the UI is stuck at 0%, but the file uploaded successfully.
**Possible Causes:**
1. The Hangfire Server is not running. Ensure `builder.Services.AddHangfireServer()` is present and active in `Program.cs`.
2. The job failed instantly. Check the Hangfire Dashboard's "Failed" tab.
3. SignalR is disconnected. Check the browser console for SignalR connection errors. The job might actually be progressing, but the UI isn't receiving updates.

**Symptom:** "An error occurred while creating the user."
**Possible Cause:** This is typically logged inside the Import Detail UI. It means `CreateUserAsync` returned an error (e.g. invalid email format, user already exists, or missing password configuration). Check the "Error" column in the batch details.
