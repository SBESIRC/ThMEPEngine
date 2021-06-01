using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Service
{
    public class ThStructureUtils
    {
        public static string OriginalFromXref(string xrefLayer)
        {
            return ThMEPXRefService.OriginalFromXref(xrefLayer);
        }

        /// <summary>
        /// 验证规格
        /// </summary>
        /// <returns></returns>
        public static bool ValidateSpec(string spec)
        {
            if (string.IsNullOrEmpty(spec))
            {
                return false;
            }
            string pattern = @"^[\s]{0,}\d+[\s]{0,}[xX×]{1}[\s]{0,}\d+[\s]{0,}$";
            if (Regex.IsMatch(spec, pattern))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static List<double> GetDoubleValues(string str)
        {
            List<double> values = new List<double>();
            string pattern = "[-]?\\d+([.]{1}\\d+)?";
            MatchCollection matches = Regex.Matches(str, pattern);
            foreach (var match in matches)
            {
                if (!string.IsNullOrEmpty(match.ToString()))
                {
                    values.Add(Convert.ToDouble(match.ToString()));
                }
            }
            return values;
        }
    }
}
