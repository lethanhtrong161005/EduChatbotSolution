using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class UserImportBatch : NaturalEntity
{
    public string FileName { get; set; } = string.Empty;

    public string? StagingLocator { get; set; }

    public string? StorageLocator { get; set; }

    public int TotalRows { get; set; }

    public int ProcessedRows { get; set; }

    public int SuccessRows { get; set; }

    public int FailedRows { get; set; }

    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Pending;

    public Guid ImportedById { get; set; }

    [ForeignKey(nameof(ImportedById))]
    public virtual ApplicationUser? ImportedBy { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public virtual ICollection<UserImportRow> Rows { get; } = [];
}

public enum ImportBatchStatus
{
    Pending,
    Parsing,
    Validated,
    Processing,
    Completed,
    PartiallyCompleted,
    Failed,
}
