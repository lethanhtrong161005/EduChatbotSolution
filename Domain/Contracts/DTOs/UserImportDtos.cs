using Domain.Entities;

namespace Domain.Contracts.DTOs;

public record UserImportValidationResult(
    bool IsValid,
    List<string> Errors,
    List<UserImportRow> ValidRows
);

public record UserImportBatchSummaryDto(
    Guid Id,
    string FileName,
    int TotalRows,
    int ProcessedRows,
    int SuccessRows,
    int FailedRows,
    ImportBatchStatus Status,
    string ImportedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);

public record UserImportBatchDetailDto(
    UserImportBatchSummaryDto Summary,
    List<UserImportRowDetailDto> Rows
);

public record UserImportRowDetailDto(
    int RowNumber,
    string FullName,
    string Email,
    string Role,
    ImportRowStatus Status,
    string? ErrorMessage,
    DateTimeOffset? ProcessedAt
);
