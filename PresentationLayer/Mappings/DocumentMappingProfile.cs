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

        CreateMap<Document, DocumentDetailsVm>()
            .ForMember(dest => dest.ChapterName, opts => opts.MapFrom(src => src.Chapter.Name))
            .ForMember(dest => dest.Extension, opts => opts.MapFrom(src => Path.GetExtension(src.FileName)))
            .ForMember(dest => dest.Status, opts => opts.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.EmbeddingModel, opts => opts.MapFrom(src => src.Chunks.Count > 0 ? src.Chunks.First().EmbeddingModel : null))
            .ForMember(dest => dest.ChunkCount, opts => opts.MapFrom(src => src.Chunks.Count))
            .ForMember(dest => dest.Status, opts => opts.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.UploadedBy, opts => opts.MapFrom(src => src.Uploader.FullName));

        CreateMap<Chunk, ChunkPreviewVm>()
            .ForMember(dest => dest.VectorPreview, opts => opts.MapFrom(src => src.Embedding != null
                                                                        ? src.Embedding.ToArray().Take(15).ToArray()
                                                                        : Array.Empty<float>()));
    }
}
