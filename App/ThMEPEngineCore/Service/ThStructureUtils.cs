﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ThMEPEngineCore.Service
{
    public class ThStructureUtils
    {
        public static bool IsColumnXref(string pathName)
        {
            return Path.GetFileName(pathName).ToUpper().Contains("COLU");
        }

        public static bool IsBeamXref(string pathName)
        {
            return Path.GetFileName(pathName).ToUpper().Contains("BEAM");
        }

        public static string OriginalFromXref(string xrefLayer)
        {
            int index = xrefLayer.LastIndexOf('|');
            return (index >= 0) ? xrefLayer.Substring(index + 1) : xrefLayer;
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
