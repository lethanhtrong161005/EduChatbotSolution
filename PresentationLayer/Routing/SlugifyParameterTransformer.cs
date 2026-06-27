using Domain.Utils;

namespace Presentation.Routing;

public class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value is not string str) return null;
        return str.ToKebabCaseLower();
    }
}
