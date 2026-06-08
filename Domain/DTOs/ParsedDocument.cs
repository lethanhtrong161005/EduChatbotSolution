using Domain.Entities;

namespace Domain.DTOs;

public record ParsedDocument
{
    public List<ParsedSection> Sections { get; set; } = [];
}
