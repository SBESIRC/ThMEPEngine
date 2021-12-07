﻿using System;
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
    }
    public class ThDuctParameter
    {
        public ThDuctSizeInfor DuctSizeInfor { get; set; }
        public ThDuctParameter(double airVolume, string scenario)
        {
            GetAirSpeedRange(airVolume, scenario, out double ceiling, out double floor);
            var candidate = GetCandidateDucts(airVolume, ceiling, floor);
            if (candidate.Count > 0)
            {
                var recommendOuterDuct = candidate.First(d => d.AspectRatio == candidate.Max(f => f.AspectRatio));
                var recommendInnerDuct = candidate.First(d => d.AspectRatio == candidate.Min(f => f.AspectRatio));

                DuctSizeInfor = new ThDuctSizeInfor()
                {
                    DefaultDuctsSizeString = GetDefaultDuctsSizeString(candidate),
                    RecommendOuterDuctSize = recommendOuterDuct.DuctWidth + "x" + recommendOuterDuct.DuctHeight,
                    RecommendInnerDuctSize = recommendInnerDuct.DuctWidth + "x" + recommendInnerDuct.DuctHeight
                };
            }
            else
                DuctSizeInfor = new ThDuctSizeInfor();
        }
        private List<DuctSizeParameter> GetCandidateDucts(double air_volume, double air_speed_ceiling, double air_speed_floor)
        {
            double duct_area_floor = air_volume / 3600.0 / air_speed_floor;
            double duct_area_ceiling = air_volume / 3600.0 / air_speed_ceiling;
            Round_2_float(ref duct_area_floor);
            Round_2_float(ref duct_area_ceiling);
            var json_reader = new ThDuctParameterJsonReader();
            var size_floor = json_reader.Parameters.Where(d => d.SectionArea >= duct_area_ceiling).OrderBy(d => d.SectionArea);
            var satisfied_ducts = size_floor.Where(d => d.SectionArea <= duct_area_floor).ToList();

            if (satisfied_ducts.Count == 0)
            {
                if (size_floor.Count() == 0)
                {
                    return new List<DuctSizeParameter>();
                }
                return new List<DuctSizeParameter>() { size_floor.First() };
            }
            return satisfied_ducts.OrderByDescending(d => d.DuctWidth).ThenByDescending(d => d.DuctHeight).ToList();
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
            if (scenario.Contains("排烟") || scenario == "消防加压送风" || scenario == "消防补风")
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
                if (airVolume >= 26000) { ceiling = 10; floor = 8; }
                else if (airVolume >= 12000) { ceiling = 8; floor = 6; }
                else if (airVolume >= 8000) { ceiling = 6; floor = 4.5; }
                else if (airVolume >= 4000) { ceiling = 4.5; floor = 3.5; }
                else if (airVolume >= 3000) { ceiling = 7.5; floor = 5.14; }
                else if (airVolume >= 2800) { ceiling = 7; floor = 4.8; }
                else { ceiling = 10; floor = 8; }//throw new NotImplementedException();
            }
        }
        private void Round_2_float(ref double f)
        {
            string s = f.ToString("#0.00");
            f = Double.Parse(s);
        }
    }
}