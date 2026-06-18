using Domain.Entities;

namespace Domain.DTOs;

public record ParsedDocument
{
    public required ICollection<ParsedSection> Sections { get; init; }
}
