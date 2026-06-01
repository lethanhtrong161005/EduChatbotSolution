using AutoMapper;
using Domain.Entities;
using Presentation.Models;

namespace Presentation.Mappings;

public class SubscriptionMappingProfile : Profile
{
    public SubscriptionMappingProfile()
    {
        CreateMap<SubscriptionPlan, SubscriptionPlanCardVm>()
            .ConvertUsing<SelectSubscriptionPlanConverter>();

        CreateMap<SubscriptionPlanOption, SubscriptionPlanOptionCardVm>()
            .ReverseMap();
    }
}

public class SelectSubscriptionPlanConverter : ITypeConverter<SubscriptionPlan, SubscriptionPlanCardVm>
{
    public SubscriptionPlanCardVm Convert(SubscriptionPlan source, SubscriptionPlanCardVm destination, ResolutionContext context)
    {
        destination ??= new SubscriptionPlanCardVm();

        destination.Name = source.Name;
        destination.Tier = source.Tier;
        destination.Description = source.Description;
        destination.DailyMessageQuota = source.DailyMessageQuota;
        destination.ChatSessionLimit = source.ChatSessionLimit;
        destination.DailyFileUploadQuota = source.DailyFileUploadQuota;
        destination.FileLibraryLimit = source.FileLibraryLimit;
        destination.AllowAdvancedModels = source.AllowAdvancedModels;
        destination.IsFeatured = source.IsFeatured;

        destination.Options = context.Mapper.Map<ICollection<SubscriptionPlanOptionCardVm>>(source.SubscriptionPlanOptions);

        return destination;
    }
}
