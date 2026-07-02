using AutoMapper;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Utils;
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
            .ForMember(d => d.Chapters,
                o => o.MapFrom(s => s.Chapters));

        CreateMap<Chapter, ChapterInfoDto>()
            .ForMember(d => d.ChapterName,
                o => o.MapFrom(s => s.Name));

        // ===========================
        // Details
        // ===========================

        CreateMap<DocumentDetails, DocumentDetailsVm>()
            .ForMember(dest => dest.Extension, opts => opts.MapFrom(src => Path.GetExtension(src.FileName)))
            .ForMember(dest => dest.ExtractedText, opts => opts.MapFrom(src => string.Join("\n\n", src.ParsedSections.OrderBy(s => s.SectionIndex).Select(x => x.Text))));

        CreateMap<ParsedSectionDetails, ParsedSectionVm>();

        CreateMap<DocumentCommentDetails, DocumentCommentVm>();

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
