using Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.DTOs;

[NotMapped]
public class ParsedDocument
{
    public List<ParsedSection> Sections { get; set; } = [];
}
