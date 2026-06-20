using Domain.Entities;

namespace Domain.Contracts.DTOs;

public record ParsedDocument
{
    public required ICollection<ParsedSection> Sections { get; init; }
}
