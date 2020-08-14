﻿using System.IO;
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
    }
}
