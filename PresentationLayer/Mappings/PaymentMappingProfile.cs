using AutoMapper;
using Domain.Entities;
using Presentation.Models;

namespace Presentation.Mappings;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        CreateMap<Order, OrderCheckoutVm>()
            .ForMember(dest => dest.PlanName, opts => opts.MapFrom(src => src.Plan.Name))
            .ForMember(dest => dest.OptionName, opts => opts.MapFrom(src => src.PlanOption.Name))
            .ForMember(dest => dest.StartDate, opts => opts.MapFrom(src => TimeZoneInfo.ConvertTimeFromUtc(src.Subscription.StartDate, timezone)))
            .ForMember(dest => dest.EndDate, opts => opts.MapFrom(src => TimeZoneInfo.ConvertTimeFromUtc(src.Subscription.EndDate, timezone)))
            .ForMember(dest => dest.Total, opts => opts.MapFrom(src => src.ChargedAmount));

        CreateMap<Payment, PaymentProcessingVm>()
            .ForMember(dest => dest.PlanName, opts => opts.MapFrom(src => src.Order.Plan.Name))
            .ForMember(dest => dest.OptionName, opts => opts.MapFrom(src => src.Order.PlanOption.Name))
            .ForMember(dest => dest.Amount, opts => opts.MapFrom(src => src.Order.ChargedAmount));
    }
}
