using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using TianHua.FanSelection;
using TianHua.Publics.BaseCode;

namespace ThMEPHVAC.CAD
{
    public class DuctSizeParameter
    {
        public double DuctWidth { get; set; }
        public double DuctHeight { get; set; }
        public double SectionArea { get; set; }
        public double AspectRatio { get; set; }
    }
    public class ThDuctSelectionEngine
    {
        public List<DuctSizeParameter> DefaultCandidateDucts { get; set; }
        public ThDbModelFan FanModel { get; set; }
        public double DefaultAirSpeed { get; set; }
        public DuctSizeParameter DefaultRecommendDuct { get; set; }
        public DuctSizeParameter RecommendOuterDuct { get; set; }
        public DuctSizeParameter RecommendInnerDuct { get; set; }
        public ThDuctSelectionEngine(ThDbModelFan fanmodel)
        {
            FanModel = fanmodel;
            switch (FanModel.FanScenario)
            {
                case "消防排烟":
                case "消防补风":
                    DefaultAirSpeed = 15;
                    break;
                default:
                    DefaultAirSpeed = 8;
                    break;
            }
            DefaultCandidateDucts = GetDefaultCandidateDucts();
            RecommendOuterDuct = DefaultCandidateDucts.First(d => d.AspectRatio == DefaultCandidateDucts.Max(f => f.AspectRatio));
            RecommendInnerDuct = DefaultCandidateDucts.First(d => d.AspectRatio == DefaultCandidateDucts.Min(f => f.AspectRatio));
        }

        private List<DuctSizeParameter> GetDefaultCandidateDucts()
        {
            double calculateDuctArea = FanModel.FanVolume / 3600 / DefaultAirSpeed;
            var ductParameterString = ReadWord(ThCADCommon.DuctSizeParametersPath());
            var ductParameterObjs = FuncJson.Deserialize<List<DuctSizeParameter>>(ductParameterString);

            var biggerDucts = ductParameterObjs.Where(d => d.SectionArea > calculateDuctArea).OrderBy(d => d.SectionArea);
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

        public string ReadWord(string _Path)
        {
            try
            {
                using (StreamReader _StreamReader = new StreamReader(_Path, Encoding.Default))
                {
                    return _StreamReader.ReadToEnd();
                }
            }
            catch
            {
                return string.Empty;

            }
        }

    }
}
