using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPHVAC.IO;

namespace ThMEPHVAC.CAD
{
    public class ThDuctSizeInfor
    {
        //出口管段推荐尺寸
        public string RecommendOuterDuctSize { get; set; }
        //入口管段推荐尺寸
        public string RecommendInnerDuctSize { get; set; }
        //管道尺寸信息
        public List<string> DefaultDuctsSizeString { get; set; }
        public double WHRatio { get; set; }
    }
    public class ThDuctParameter
    {
        public ThDuctSizeInfor DuctSizeInfor { get; set; }
        // 标准管径大小
        public static HashSet<double> ductMods = new HashSet<double>() { 120, 160, 200, 250, 320, 400, 500, 630, 800, 1000, 1250, 1600, 2000, 2500, 3000, 3500, 4000 };
        // 需要新增的风管高度
        public static HashSet<double> ductHeights = new HashSet<double>() { 120, 160, 200, 250, 320, 400, 500 };
        public ThDuctParameter(double airVolume, string scenario, double inW, double inH)// 添加非标的风管参数
        {
            GetAirSpeedRange(airVolume, scenario, out double ceiling, out double floor);
            var ratio = GetWHRatio(airVolume, scenario);
            var candidate = GetCandidateDucts(airVolume, ceiling, floor, inW, inH);
            DoSelectSize(candidate, ratio);
        }
        public ThDuctParameter(double airVolume, string scenario)
        {
            GetAirSpeedRange(airVolume, scenario, out double ceiling, out double floor);
            var ratio = GetWHRatio(airVolume, scenario);
            var candidate = GetCandidateDucts(airVolume, ceiling, floor);
            DoSelectSize(candidate, ratio);
        }
        private void AddNotNormalDuctSize(List<DuctSizeParameter> candidate, double inW, double inH)
        {
            candidate.Add(new DuctSizeParameter() { DuctWidth = inW, DuctHeight = inH, SectionArea = (inW * inH) / 1e6, AspectRatio = inW / inH });
            if (!ductMods.Contains(inW))
            {
                foreach (var h in ductHeights)
                {
                    candidate.Add(new DuctSizeParameter() { DuctWidth = inW, DuctHeight = h, SectionArea = (inW * h) / 1e6, AspectRatio = inW / h  });
                }
            }
            if (!ductHeights.Contains(inH))
            {
                foreach (var w in ductMods)
                {
                    candidate.Add(new DuctSizeParameter() { DuctWidth = w, DuctHeight = inH, SectionArea = (w * inH) / 1e6, AspectRatio = w / inH });
                }
            }
        }
        public void DoSelectSize(List<DuctSizeParameter> candidate, double ratio)
        {
            if (candidate.Count > 0)
            {
                var recommendOuterDuct = candidate.First(d => d.AspectRatio == candidate.Max(f => f.AspectRatio));
                var recommendInnerDuct = candidate.First(d => d.AspectRatio == candidate.Min(f => f.AspectRatio));
                var defaultDuctsSizeString = GetDefaultDuctsSizeString(candidate);
                string selectSize = String.Empty;
                double minRatio = Double.MaxValue;
                foreach (var size in defaultDuctsSizeString)
                {
                    string[] s = size.Split('x');
                    if (s.Length != 2)
                        throw new NotImplementedException("Duct size info doesn't contain width or height");
                    double width = Double.Parse(s[0]);
                    double height = Double.Parse(s[1]);
                    var r = width / height;
                    var t = Math.Abs(r - ratio);
                    if (t < minRatio && height <= 500)
                    {
                        minRatio = t;
                        selectSize = size;
                    }
                }
                var filter = new List<string>();
                foreach (var s in defaultDuctsSizeString)
                {
                    var h = GetHeight(s);
                    if (h <= 500)
                        filter.Add(s);
                }
                DuctSizeInfor = new ThDuctSizeInfor()
                {
                    DefaultDuctsSizeString = filter,
                    RecommendOuterDuctSize = selectSize,
                    RecommendInnerDuctSize = selectSize,
                    WHRatio = ratio
                };
            }
            else
                DuctSizeInfor = new ThDuctSizeInfor();
        }
        public static double GetHeight(string size)
        {
            if (size == null)
                return 0;
            string[] width = size.Split('x');
            if (width.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            return Double.Parse(width[1]);
        }
        public double GetWHRatio(double airVolume, string scenario)
        {
            if ((scenario.Contains("排烟") && !scenario.Contains("兼")) || scenario == "消防加压送风" || scenario == "消防补风")
            {
                if (airVolume >= 100000) { return 6; }
                else if (airVolume >= 80000) { return 5; }
                else if (airVolume >= 50000) { return 3; }
                else if (airVolume >= 8000) { return 2.5; }
                else { return 2; }
            }
            else if (scenario == "厨房排油烟")
            {
                if (airVolume >= 60000) { return 6; }
                else if (airVolume >= 50000) { return 5; }
                else if (airVolume >= 40000) { return 4; }
                else if (airVolume >= 30000) { return 3; }
                else if (airVolume >= 5000) { return 2.5; }
                else { return 2; }
            }
            else
            {
                if (airVolume >= 40000) { return 6; }
                else if (airVolume >= 35000) { return 5; }
                else if (airVolume >= 30000) { return 4; }
                else if (airVolume >= 12000) { return 3; }
                else if (airVolume >= 2000) { return 2.5; }
                else { return 2; }
            }
        }
        private List<DuctSizeParameter> GetCandidateDucts(double airVolume, double airSpeedCeiling, double airSpeedFloor, double inW, double inH)
        {
            double ductAreaFloor = airVolume / 3600.0 / airSpeedFloor;
            double ductAreaCeiling = airVolume / 3600.0 / airSpeedCeiling;
            Round2float(ref ductAreaFloor);
            Round2float(ref ductAreaCeiling);
            var jsonReader = new ThDuctParameterJsonReader();
            AddNotNormalDuctSize(jsonReader.Parameters, inW, inH);
            var sizeFloor = jsonReader.Parameters.Where(d => d.SectionArea >= ductAreaCeiling).OrderBy(d => d.SectionArea);
            var satisfiedDucts = sizeFloor.Where(d => d.SectionArea <= ductAreaFloor).ToList();

            if (satisfiedDucts.Count == 0)
            {
                if (sizeFloor.Count() == 0)
                {
                    return new List<DuctSizeParameter>();
                }
                return new List<DuctSizeParameter>() { sizeFloor.First() };
            }
            return satisfiedDucts.OrderByDescending(d => d.DuctWidth).ThenByDescending(d => d.DuctHeight).ToList();
        }
        
        private List<DuctSizeParameter> GetCandidateDucts(double airVolume, double airSpeedCeiling, double airSpeedFloor)
        {
            double ductAreaFloor = airVolume / 3600.0 / airSpeedFloor;
            double ductAreaCeiling = airVolume / 3600.0 / airSpeedCeiling;
            Round2float(ref ductAreaFloor);
            Round2float(ref ductAreaCeiling);
            var jsonReader = new ThDuctParameterJsonReader();
            var sizeFloor = jsonReader.Parameters.Where(d => d.SectionArea >= ductAreaCeiling).OrderBy(d => d.SectionArea);
            var satisfiedDucts = sizeFloor.Where(d => d.SectionArea <= ductAreaFloor).ToList();

            if (satisfiedDucts.Count == 0)
            {
                if (sizeFloor.Count() == 0)
                {
                    return new List<DuctSizeParameter>();
                }
                return new List<DuctSizeParameter>() { sizeFloor.First() };
            }
            return satisfiedDucts.OrderByDescending(d => d.DuctWidth).ThenByDescending(d => d.DuctHeight).ToList();
        }
        //获取管道尺寸信息列表
        private List<string> GetDefaultDuctsSizeString(List<DuctSizeParameter> defaultcandidateducts)
        {
            List<string> DuctsSizeString = new List<string>();
            defaultcandidateducts.ForEach(d=> DuctsSizeString.Add($"{d.DuctWidth}x{d.DuctHeight}"));
            return DuctsSizeString;
        }
        private static void GetAirSpeedRange(double airVolume, string scenario, out double ceiling, out double floor)
        {
            if ((scenario.Contains("排烟") && !scenario.Contains("兼")) || scenario == "消防加压送风" || scenario == "消防补风")
            {
                if (airVolume >= 15000) { ceiling = 20; floor = 12; }
                else if (airVolume >= 10000) { ceiling = 20; floor = 10; }
                else if (airVolume >= 3000) { ceiling = 20; floor = 9; }
                else { ceiling = 20; floor = 9; }
            }
            else if (scenario == "厨房排油烟")
            {
                if (airVolume >= 15000) { ceiling = 12; floor = 8; }
                else if (airVolume >= 10000) { ceiling = 12; floor = 7; }
                else if (airVolume >= 3000) { ceiling = 12; floor = 6; }
                else { ceiling = 12; floor = 6; }
            }
            else
            {
                if (airVolume >= 26000) { ceiling = 8.9; floor = 5; }
                else if (airVolume >= 12000) { ceiling = 8.9; floor = 4.5; }
                else if (airVolume >= 8000) { ceiling = 7; floor = 4; }
                else if (airVolume >= 4000) { ceiling = 5.5; floor = 3; }
                else if (airVolume >= 3000) { ceiling = 5.5; floor = 3; }
                else if (airVolume >= 2800) { ceiling = 4.5; floor = 2.5; }
                else { ceiling = 4; floor = 2; }//throw new NotImplementedException();
            }
        }
        private void Round2float(ref double f)
        {
            string s = f.ToString("#0.00");
            f = Double.Parse(s);
        }
    }
}