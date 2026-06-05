using AutoMapper;
using Domain.Entities;
using Presentation.Models;

namespace Presentation.Mappings;

public class DocumentMappingProfile : Profile
{
    public DocumentMappingProfile()
    {
        CreateMap<Subject, SubjectLookupVm>();

        CreateMap<Chapter, ChapterLookupVm>();

        CreateMap<Document, DocumentFileVm>()
            .ForMember(dest => dest.Extension, opts => opts.MapFrom(src => Path.GetExtension(src.FileName)))
            .ForMember(dest => dest.Status, opts => opts.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.UploadedBy, opts => opts.MapFrom(src => src.Uploader.FullName));

        CreateMap<Chunk, ChunkPreviewVm>()
            .ForMember(dest => dest.VectorPreview, opts => opts.MapFrom(src => src.Embedding != null ? src.Embedding.ToArray().Take(16).ToArray() : Array.Empty<float>()));
    }
}
