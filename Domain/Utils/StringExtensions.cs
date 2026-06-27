using System.Text.RegularExpressions;

namespace Domain.Utils;

public static class StringExtensions
{
    public static string ToKebabCaseLower(this string input)
    {
        return Regex.Replace(input.ToString(),
                             "([a-z])([A-Z])",
                             "$1-$2",
                             RegexOptions.CultureInvariant,
                             TimeSpan.FromMilliseconds(100))
                    .ToLowerInvariant();
    }
}
