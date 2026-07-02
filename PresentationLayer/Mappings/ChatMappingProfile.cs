using AutoMapper;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Presentation.DTOs;

namespace Presentation.Mappings;

public class ChatMappingProfile : Profile
{
    public ChatMappingProfile()
    {
        /* Page shell */
        CreateMap<Subject, SubjectHeaderDto>();

        CreateMap<ChatSessionInfo, SessionHeaderDto>();

        /* New session */
        CreateMap<ChatSession, CreateChatSessionResponse>()
            .ForMember(dest => dest.SessionId, opts => opts.MapFrom(src => src.Id));

        /* Load session */
        CreateMap<Citation, ChatCitationDto>()
            .ForMember(dest => dest.ChunkIndex, opts => opts.MapFrom(src => src.Chunk.ChunkIndex))
            .ForMember(dest => dest.ChunkText, opts => opts.MapFrom(src => src.Chunk.ChunkText))
            .ForMember(dest => dest.DocumentTitle, opts => opts.MapFrom(src => src.Chunk.Document.Title))
            .ForMember(dest => dest.DocumentId, opts => opts.MapFrom(src => src.Chunk.DocumentId));

        CreateMap<ChatMessage, ChatMessageDto>();

        CreateMap<ChatSession, ChatSessionDto>()
            .ForMember(dest => dest.LastMessageAt, opts => opts.MapFrom<LastMessageAtResolver>());

        /* Send message */
        CreateMap<ChatMessage, ResolvedChatMessage>()
            .ForMember(dest => dest.Citations, opts => opts.Ignore());

        CreateMap<ResolvedChatMessage, ChatMessageDto>();

        CreateMap<ResolvedCitation, ChatCitationDto>();
    }
}

public sealed class LastMessageAtResolver : IValueResolver<ChatSession, ChatSessionDto, DateTime>
{
    public DateTime Resolve(
        ChatSession source,
        ChatSessionDto destination,
        DateTime destMember,
        ResolutionContext context)
    {
        return source.Messages.Max(e => (DateTime?)e.SentAt) ?? source.CreatedAt;
    }
}