using System.Linq;
using System.Collections.Generic;
using TianHua.FanSelection.Function;
using ThMEPHVAC.IO;

namespace ThMEPHVAC.CAD
{
    public class ThDuctSelectionEngine
    {
        public List<DuctSizeParameter> DefaultCandidateDucts { get; set; }
        public ThDbModelFan FanModel { get; set; }
        public DuctSizeParameter DefaultRecommendDuct { get; set; }
        public DuctSizeParameter RecommendOuterDuct { get; set; }
        public string RecommendOuterDuctSize { get; set; }
        public DuctSizeParameter RecommendInnerDuct { get; set; }
        public string RecommendInnerDuctSize { get; set; }
        public ThDuctSelectionEngine(ThDbModelFan fanmodel)
        {
            FanModel = fanmodel;
            DefaultCandidateDucts = GetDefaultCandidateDucts();
            RecommendOuterDuct = DefaultCandidateDucts.First(d => d.AspectRatio == DefaultCandidateDucts.Max(f => f.AspectRatio));
            RecommendOuterDuctSize = RecommendOuterDuct.DuctWidth + "x" + RecommendOuterDuct.DuctHeight;
            RecommendInnerDuct = DefaultCandidateDucts.First(d => d.AspectRatio == DefaultCandidateDucts.Min(f => f.AspectRatio));
            RecommendInnerDuctSize = RecommendInnerDuct.DuctWidth + "x" + RecommendInnerDuct.DuctHeight;
        }
        private List<DuctSizeParameter> GetDefaultCandidateDucts()
        {
            double calculateDuctArea = FanModel.FanVolume / 3600.0 / ThFanSelectionUtils.GetDefaultAirSpeed(FanModel.FanScenario);
            var jsonReader = new ThDuctParameterJsonReader();
            var biggerDucts = jsonReader.Parameters.Where(d => d.SectionArea > calculateDuctArea).OrderBy(d => d.SectionArea);
            var satisfiedDucts = biggerDucts.Where(d=> d.SectionArea - calculateDuctArea < 0.15 * calculateDuctArea).ToList();

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
        public List<string> GetDefaultDuctsSizeString()
        {
            List<string> DuctsSizeString = new List<string>();
            DefaultCandidateDucts.ForEach(d=> DuctsSizeString.Add($"{d.DuctWidth}x{d.DuctHeight}"));
            return DuctsSizeString;
        }
    }
}
