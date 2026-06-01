using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Primitives;

namespace Presentation.Routing;

public class KebabCaseQueryParameterRule : IRule
{
    public void ApplyRule(RewriteContext context)
    {
        Dictionary<string, StringValues> newQueryCollection = context.HttpContext.Request.Query.ToDictionary(
            kv => ToCamelCase(kv.Key),
            kv => kv.Value
        );

        context.HttpContext.Request.Query = new QueryCollection(newQueryCollection);
    }

    private static string ToPascalCase(string kebabCase)
    {
        return string.Join(string.Empty, kebabCase.Split('-').Select(str => Capitalize(str)));
    }

    private static string ToCamelCase(string kebabCase)
    {
        var tokens = kebabCase.Split('-').Select(str => Capitalize(str)).ToArray();
        tokens[0] = Uncapitalize(tokens[0]);
        return string.Join(string.Empty, tokens);
    }

    public static string Capitalize(string str)
    {
        return !string.IsNullOrEmpty(str)
            ? char.ToUpper(str[0]) + str[1..]
            : str;
    }

    public static string Uncapitalize(string str)
    {
        return !string.IsNullOrEmpty(str)
            ? char.ToLower(str[0]) + str[1..]
            : str;
    }
}
