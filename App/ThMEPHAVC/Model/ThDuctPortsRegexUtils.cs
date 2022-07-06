using System.Text.RegularExpressions;

namespace ThMEPHVAC.Model
{
    public static class ThDuctPortsRegexUtils
    {
        public static double GetAirVolume(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return 0.0;
            Match match = Regex.Match(s, @"^([+-]?[0-9]+(?:\.[0-9]*)?)(m3/h)$");
            if (match.Success)
            {
                return double.Parse(match.Groups[1].Value);
            }
            return 0.0;
        }
    }
}
