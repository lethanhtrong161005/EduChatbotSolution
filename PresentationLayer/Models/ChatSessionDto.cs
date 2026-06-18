using Domain.Entities;

namespace Presentation.Models;

public class ChatSessionDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<ChatMessageDto> Messages { get; set; } = [];

    public DateTime LastMessageAt { get; set; }
}

public class ChatMessageDto
{
    public Guid Id { get; set; }

    public ChatRole ChatRole { get; set; }

    public string Content { get; set; } = string.Empty;

    public List<ChatCitationDto> Citations { get; set; } = [];

    public DateTime SentAt { get; set; }
}

public class ChatCitationDto
{
    public Guid ChunkId { get; set; }

    public int CitationIndex { get; set; }

    public string QuotedText { get; set; } = string.Empty;

    public string? LocationInDocument { get; set; }

    public int ChunkIndex { get; set; }

    public string ChunkText { get; set; } = string.Empty;

    public string DocumentTitle { get; set; } = string.Empty;

    public Guid DocumentId { get; set; }
}
