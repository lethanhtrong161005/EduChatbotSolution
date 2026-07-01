using AutoMapper;
using Domain.Entities;
using Presentation.DTOs;
using Presentation.ViewModels;

namespace Presentation.Mappings;

public class DocumentMappingProfile : Profile
{
    public DocumentMappingProfile()
    {
        // ===========================
        // Library
        // ===========================

        CreateMap<Subject, SubjectSidebarDto>()
            .ForMember(d => d.ChapterCount,
                o => o.MapFrom(s => s.Chapters.Count))
            .ForMember(d => d.DocumentCount,
                o => o.MapFrom(s => s.Chapters.SelectMany(c => c.Documents).Count()));

        CreateMap<Chapter, ChapterSidebarDto>()
            .ForMember(d => d.DocumentCount,
                o => o.MapFrom(s => s.Documents.Count));

        CreateMap<Subject, SubjectDetailsDto>()
            .ForMember(d => d.ChapterCount,
                o => o.MapFrom(s => s.Chapters.Count))
            .ForMember(d => d.DocumentCount,
                o => o.MapFrom(s => s.Chapters.SelectMany(c => c.Documents).Count()))
            .ForMember(d => d.MemberCount,
                o => o.MapFrom(s => s.Memberships.Count));

        CreateMap<Chapter, ChapterDetailsDto>()
            .ForMember(d => d.SubjectCode,
                o => o.MapFrom(s => s.Subject.Code))
            .ForMember(d => d.SubjectName,
                o => o.MapFrom(s => s.Subject.Name))
            .ForMember(d => d.DocumentCount,
                o => o.MapFrom(s => s.Documents.Count));

        CreateMap<Document, DocumentFileDto>()
            .ForMember(d => d.Extension,
                o => o.MapFrom(s => Path.GetExtension(s.FileName)))
            .ForMember(d => d.Status,
                o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.UploadedBy,
                o => o.MapFrom(s => s.Uploader.FullName))
            .ForMember(d => d.ChapterName,
                o => o.MapFrom(s => s.Chapter.Name))
            .ForMember(d => d.ChapterNumber,
                o => o.MapFrom(s => s.Chapter.ChapterNumber));

        // ===========================
        // Details
        // ===========================

        CreateMap<Document, DocumentDetailsVm>()
            .ForMember(dest => dest.ChapterName, opts => opts.MapFrom(src => src.Chapter.Name))
            .ForMember(dest => dest.SubjectId, opts => opts.MapFrom(src => src.Chapter.SubjectId))
            .ForMember(dest => dest.Extension, opts => opts.MapFrom(src => Path.GetExtension(src.FileName)))
            .ForMember(dest => dest.Status, opts => opts.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.EmbeddingModel, opts => opts.MapFrom(src => src.Chunks.Count > 0 ? src.Chunks.First().EmbeddingModel : null))
            .ForMember(dest => dest.ChunkCount, opts => opts.MapFrom(src => src.Chunks.Count))
            .ForMember(dest => dest.UploadedBy, opts => opts.MapFrom(src => src.Uploader.FullName));

        CreateMap<ParsedSection, ParsedSectionVm>();

        CreateMap<Chunk, ChunkPreviewDto>()
            .ForMember(dest => dest.VectorPreview, opts => opts.MapFrom(src => src.Embedding != null
                                                                        ? src.Embedding.ToArray().Take(15).ToArray()
                                                                        : Array.Empty<float>()));

        // ===========================
        // Edit
        // ===========================

        CreateMap<Document, DocumentEditVm>();
    }
}
