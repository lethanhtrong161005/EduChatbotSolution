using AutoMapper;
using Domain.Entities;
using Presentation.ViewModels;

namespace Presentation.Mappings;

public class SubscriptionMappingProfile : Profile
{
    public SubscriptionMappingProfile()
    {
        CreateMap<Plan, PlanCardVm>()
            .ForMember(dest => dest.Options, opts => opts.MapFrom<PlanOptionsResolver>());

        CreateMap<PlanOption, PlanOptionCardVm>();

        CreateMap<Subscription, CurrentSubscriptionVm>()
            .ForMember(dest => dest.PlanName, opts => opts.MapFrom(src => src.Plan.Name));
    }
}

public class PlanOptionsResolver : IValueResolver<Plan, PlanCardVm, ICollection<PlanOptionCardVm>>
{
    public ICollection<PlanOptionCardVm> Resolve(Plan source, PlanCardVm destination, ICollection<PlanOptionCardVm> destMember, ResolutionContext context)
    {
        return context.Mapper.Map<ICollection<PlanOptionCardVm>>(source.PlanOptions);
    }
}
