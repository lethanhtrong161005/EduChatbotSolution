using AutoMapper;
using Domain.Entities;
using Presentation.Models;

namespace Presentation.Mappings;

public class SubscriptionMappingProfile : Profile
{
    public SubscriptionMappingProfile()
    {
        CreateMap<Plan, PlanCardVm>()
            .ConvertUsing<SelectPlanConverter>();

        CreateMap<PlanOption, PlanOptionCardVm>();

        CreateMap<Subscription, CurrentSubscriptionVm>()
            .ForMember(dest => dest.PlanName, opts => opts.MapFrom(src => src.Plan.Name));
    }
}

public class SelectPlanConverter : ITypeConverter<Plan, PlanCardVm>
{
    public PlanCardVm Convert(Plan source, PlanCardVm destination, ResolutionContext context)
    {
        destination ??= new PlanCardVm();

        destination.Name = source.Name;
        destination.Tier = source.Tier;
        destination.Description = source.Description;
        destination.DailyMessageQuota = source.DailyMessageQuota;
        destination.ChatSessionLimit = source.ChatSessionLimit;
        destination.DailyFileUploadQuota = source.DailyFileUploadQuota;
        destination.FileLibraryLimit = source.FileLibraryLimit;
        destination.AllowAdvancedModels = source.AllowAdvancedModels;
        destination.IsFeatured = source.IsFeatured;

        destination.Options = context.Mapper.Map<ICollection<PlanOptionCardVm>>(source.PlanOptions);

        return destination;
    }
}
