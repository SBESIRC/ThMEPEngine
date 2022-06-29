using System;
using System.Text.RegularExpressions;

namespace ThMEPStructure.StructPlane.Service
{
    internal static class ThBeamMarkParseTool
    {
        public static double? GetElevation(this string elevation)
        {
            string pattern1 = @"[+-]{1}\s{0,}\d+[.]?\d+";
            var mt1 = Regex.Matches(elevation, pattern1);
            if (mt1.Count == 1)
            {
                var value = mt1[0].Value;
                string pattern2 = @"\d+[.]?\d+";
                var mt2 = Regex.Matches(value, pattern2);
                double plus = 1.0;
                var firstChar = value[0];
                if (firstChar == '-')
                {
                    plus *= -1.0;
                }
                if (mt2.Count == 1)
                {
                    var dValue = double.Parse(mt2[0].Value);
                    dValue *= plus;
                    dValue *= 1000.0; // m To mm
                    return dValue;
                }
            }
            return null;
        }
        public static string GetBeamSpec(this string beamElevation)
        {
            // 200x530(BG+5.670) 
            var newElevation = beamElevation.Replace("（", "(");
            var firstIndex = newElevation.IndexOf('(');
            if (firstIndex > 0)
            {
                return newElevation.Substring(0, firstIndex);
            }
            else
            {
                return newElevation;
            }
        }
        public static string UpdateBGElevation(this string elevation,double flrHeight)
        {
            // 200x530(BG+5.670) 
            var spec = elevation.GetBeamSpec();
            var bg = elevation.GetElevation();
            if (bg.HasValue)
            {
                var minus = (bg.Value - flrHeight) / 1000.0; //mm to m
                if (minus >= 0)
                {
                    return spec + "(BG+" + minus.ToString("0.000") + ")";
                }
                else
                {
                    return spec + "(BG" + minus.ToString("0.000") + ")";
                }
            }
            else
            {
                return elevation;
            }
        }
        public static string GetObliqueBeamBGElevation(this string description)
        {
            // 更新斜梁标注
            // description="'(228,0), (467,0)'"
            var newDescription = description.Replace('（', '(');
            newDescription = description.Replace('）', ')');
            newDescription = description.Replace('，', ',');

            var pattern = @"[(]\S+[)]";
            var mc = Regex.Matches(newDescription, pattern);
            if(mc.Count==2)
            {
                var m1 = mc[0].Value;
                var m2 = mc[1].Value;
                var m1Values = m1.GetDoubles();
                var m2Values = m2.GetDoubles();
                if(m1Values.Count==2 && m2Values.Count==2 && m1.IndexOf(',')>0 && m2.IndexOf(',') >0)
                {
                    var height1 = (m1Values[0] + m1Values[1] ) / 1000.0;
                    var height2 = (m2Values[0] + m2Values[1]) / 1000.0;
                    return "(BG" + "-" + height1.ToString("0.000") + "~" + "-" + height2.ToString("0.000")+ ")";
                }
            }
            return "";
        }
        public static bool IsEqualElevation(this string beamBGMark,double flrHeight)
        {
            var elevation = beamBGMark.GetElevation();
            if (elevation.HasValue)
            {
                return Math.Abs(elevation.Value - flrHeight) <= 1.0;
            }
            else
            {
                return false;
            }
        }
    }
}
