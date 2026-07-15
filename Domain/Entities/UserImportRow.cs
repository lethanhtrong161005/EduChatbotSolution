using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class UserImportRow : NaturalEntity
{
    public Guid BatchId { get; set; }

    [ForeignKey(nameof(BatchId))]
    public virtual UserImportBatch? Batch { get; set; }

    public int RowNumber { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public ImportRowStatus Status { get; set; } = ImportRowStatus.Pending;

    public string? ErrorMessage { get; set; }

    public Guid? CreatedUserId { get; set; }

    [ForeignKey(nameof(CreatedUserId))]
    public virtual ApplicationUser? CreatedUser { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
}

public enum ImportRowStatus
{
    Pending,
    Success,
    Failed,
    Skipped
}
