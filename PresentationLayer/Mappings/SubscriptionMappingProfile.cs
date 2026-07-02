using AutoMapper;
using Domain.Entities;
using Presentation.ViewModels;

namespace Presentation.Mappings;

public class SubscriptionMappingProfile : Profile
{
    public SubscriptionMappingProfile()
    {
        CreateMap<Plan, PlanCardVm>();

        CreateMap<PlanOption, PlanOptionCardVm>();

        CreateMap<Subscription, CurrentSubscriptionVm>()
            .ForMember(dest => dest.PlanName, opts => opts.MapFrom(src => src.Plan.Name));
    }
}
