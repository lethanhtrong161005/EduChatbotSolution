using AutoMapper;
using Domain.DTOs;
using Domain.Entities;
using Presentation.Models;

namespace Presentation.Mappings;

public class ChatMappingProfile : Profile
{
    public ChatMappingProfile()
    {
        CreateMap<Subject, SubjectSelectionVm>();

        CreateMap<ChatSessionHeader, ChatSidebarSessionVm>()
            .ForMember(dest => dest.Title, opts => opts.MapFrom(src => !string.IsNullOrWhiteSpace(src.Title) ? src.Title : $"Conversation {src.Id}"));

        CreateMap<Citation, ChatCitationDto>()
                .ForMember(dest => dest.ChunkIndex, opts => opts.MapFrom(src => src.Chunk.ChunkIndex))
                .ForMember(dest => dest.ChunkText, opts => opts.MapFrom(src => src.Chunk.ChunkText))
                .ForMember(dest => dest.DocumentTitle, opts => opts.MapFrom(src => src.Chunk.Document.Title))
                .ForMember(dest => dest.DocumentId, opts => opts.MapFrom(src => src.Chunk.DocumentId));

        CreateMap<ChatMessage, ChatMessageDto>();

        CreateMap<ChatSession, ChatSessionDto>()
            .ForMember(dest => dest.LastMessageAt, opts => opts.MapFrom<LastMessageAtResolver>());

        CreateMap<CreatedChatMessage, ChatMessageDto>();

        CreateMap<ResolvedCitation, ChatCitationDto>();
    }
}

public sealed class LastMessageAtResolver
    : IValueResolver<ChatSession, ChatSidebarSessionVm, DateTime>,
      IValueResolver<ChatSession, ChatSessionDto, DateTime>
{
    public DateTime Resolve(
        ChatSession source,
        ChatSidebarSessionVm destination,
        DateTime destMember,
        ResolutionContext context)
    {
        return source.Messages.Max(e => (DateTime?)e.SentAt) ?? source.CreatedAt;
    }

    public DateTime Resolve(
        ChatSession source,
        ChatSessionDto destination,
        DateTime destMember,
        ResolutionContext context)
    {
        return source.Messages.Max(e => (DateTime?)e.SentAt) ?? source.CreatedAt;
    }
}