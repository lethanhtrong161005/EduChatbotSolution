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
        CreateMap<ChatSession, ChatSessionDto>()
            .ForMember(dest => dest.LastMessageAt, opts => opts.MapFrom<LastMessageAtResolver>());

        CreateMap<ChatMessage, ChatMessageDto>()
            .ForMember(dest => dest.VariantNavigation, opts => opts.MapFrom<ChatVariantNavigationResolver>());

        CreateMap<Citation, ChatCitationDto>()
            .ForMember(dest => dest.ChunkIndex, opts => opts.MapFrom(src => src.Chunk.ChunkIndex))
            .ForMember(dest => dest.ChunkText, opts => opts.MapFrom(src => src.Chunk.ChunkText))
            .ForMember(dest => dest.DocumentTitle, opts => opts.MapFrom(src => src.Chunk.Document.Title))
            .ForMember(dest => dest.DocumentId, opts => opts.MapFrom(src => src.Chunk.DocumentId));

        CreateMap<Citation, ResolvedCitation>()
            .ForMember(dest => dest.ChunkIndex, opts => opts.MapFrom(src => src.Chunk.ChunkIndex))
            .ForMember(dest => dest.ChunkText, opts => opts.MapFrom(src => src.Chunk.ChunkText))
            .ForMember(dest => dest.DocumentTitle, opts => opts.MapFrom(src => src.Chunk.Document.Title))
            .ForMember(dest => dest.DocumentId, opts => opts.MapFrom(src => src.Chunk.DocumentId));

        /* Send message */
        CreateMap<ChatMessage, ResolvedChatMessage>()
            .ForMember(dest => dest.Citations, opts => opts.Ignore())
            .ForMember(dest => dest.VariantNavigation, opts => opts.Ignore());

        CreateMap<ResolvedChatMessage, ChatMessageDto>();

        CreateMap<ResolvedChatVariantOption, ChatVariantOptionDto>();
        CreateMap<ResolvedChatVariantNavigation, ChatVariantNavigationDto>();

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

public sealed class ChatVariantNavigationResolver : IValueResolver<ChatMessage, ChatMessageDto, ChatVariantNavigationDto?>
{
    public ChatVariantNavigationDto? Resolve(
        ChatMessage source,
        ChatMessageDto destination,
        ChatVariantNavigationDto? destMember,
        ResolutionContext context)
    {
        if (source.ChatRole != ChatRole.Assistant || source.InReplyToMessageId is null || source.VariantIndex is null)
            return null;

        var variants = source.InReplyToMessage?.AssistantVariants
            .OrderBy(message => message.VariantIndex)
            .ToList() ?? [source];

        return new ChatVariantNavigationDto
        {
            UserMessageId = source.InReplyToMessageId.Value,
            CurrentVariantIndex = source.VariantIndex.Value,
            TotalVariantCount = variants.Count,
            Variants = [.. variants.Select(message => new ChatVariantOptionDto
            {
                MessageId = message.Id,
                VariantIndex = message.VariantIndex!.Value,
                IsSelected = message.IsSelectedVariant,
                Status = message.Status,
            })],
        };
    }
}
