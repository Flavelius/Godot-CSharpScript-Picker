using System.Text.RegularExpressions;

namespace CSharpScriptPicker
{
    public static class StringExtensions
    {
        public static bool MatchesWildcardedExpression(this string input, string valueWithWildcards) //because MatchN is bugged/doesn't work as advertised/expected
        {
            valueWithWildcards = Regex.Escape(valueWithWildcards).Replace("\\?", ".").Replace("\\*", ".*");
            return Regex.IsMatch(input, valueWithWildcards);
        }
    }
}