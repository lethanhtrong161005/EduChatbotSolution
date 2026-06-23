using Domain.Entities;

namespace Presentation.DTOs;

public class ChatSessionDto
{
    public Guid Id { get; set; }

    public int? SubjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime LastMessageAt { get; set; }

    public List<ChatMessageDto> Messages { get; set; } = [];
}

public class ChatMessageDto
{
    public Guid Id { get; set; }

    public ChatRole ChatRole { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }

    public MessageStatus Status { get; set; }

    public string? GenerationErrors { get; set; }

    public List<ChatCitationDto> Citations { get; set; } = [];
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
