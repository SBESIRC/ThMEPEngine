﻿using System.Linq;
using System.Collections.Generic;
using TianHua.FanSelection.Function;
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
        public ThDuctParameter(double fanvolume, double airspeed)
        {
            DuctSizeInfor = GetUpdateDuctSizeInfor(fanvolume, airspeed);
        }
        public ThDuctSizeInfor GetUpdateDuctSizeInfor(double fanvolume, double airspeed)
        {
            var defaultCandidateDucts = GetDefaultCandidateDucts(fanvolume, airspeed);
            var recommendOuterDuct = defaultCandidateDucts.First(d => d.AspectRatio == defaultCandidateDucts.Max(f => f.AspectRatio));
            var recommendInnerDuct = defaultCandidateDucts.First(d => d.AspectRatio == defaultCandidateDucts.Min(f => f.AspectRatio));

            return new ThDuctSizeInfor()
            {
                DefaultDuctsSizeString = GetDefaultDuctsSizeString(defaultCandidateDucts),
                RecommendOuterDuctSize = recommendOuterDuct.DuctWidth + "x" + recommendOuterDuct.DuctHeight,
                RecommendInnerDuctSize = recommendInnerDuct.DuctWidth + "x" + recommendInnerDuct.DuctHeight
            };
        }
        private List<DuctSizeParameter> GetDefaultCandidateDucts(double fanvolume, double airspeed)
        {
            double calculateDuctArea = fanvolume / 3600.0 / airspeed;
            var jsonReader = new ThDuctParameterJsonReader();
            var biggerDucts = jsonReader.Parameters.Where(d => d.SectionArea > calculateDuctArea).OrderBy(d => d.SectionArea);
            var satisfiedDucts = biggerDucts.Where(d=> d.SectionArea< 1.3 * calculateDuctArea).ToList();

            if (satisfiedDucts.Count == 0)
            {
                if (biggerDucts.Count() == 0)
                {
                    return new List<DuctSizeParameter>();
                }
                return new List<DuctSizeParameter>() { biggerDucts.First() };
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
    }
}
