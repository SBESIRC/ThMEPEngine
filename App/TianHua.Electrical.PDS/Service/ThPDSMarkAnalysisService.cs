using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSMarkAnalysisService
    {
        /// <summary>
        /// 配电箱标注解析
        /// </summary>
        /// <param name="marks"></param>
        /// <param name="distBoxKey"></param>
        /// <returns></returns>
        public ThPDSLoad DistBoxMarkAnalysis(List<string> marks, List<string> distBoxKey, ThPDSBlockReferenceData distBoxData)
        {
            var thPDSDistBox = new ThPDSLoad
            {
                ID = CreatePDSID(marks, distBoxKey),
                LoadTypeCat_1 = distBoxData.Cat_1,
                LoadTypeCat_2 = distBoxData.Cat_2,
                DefaultCircuitType = distBoxData.DefaultCircuitType,
                FireLoad = false,
                Location = new ThPDSLocation
                {
                    ReferenceDWG = distBoxData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                    BasePoint = distBoxData.Position,
                }
            };
            thPDSDistBox.ID.BlockName = distBoxData.EffectiveName;

            return thPDSDistBox;
        }

        /// <summary>
        /// 负载标注解析
        /// </summary>
        /// <param name="marks"></param>
        /// <param name="distBoxKey"></param>
        /// <returns></returns>
        public ThPDSLoad LoadMarkAnalysis(List<string> marks, ThPDSBlockReferenceData distBoxData)
        {
            var thPDSDistBox = new ThPDSLoad
            {
                ID = CreatePDSID(marks),
                LoadTypeCat_1 = ThPDSLoadTypeCat_1.None,
                LoadTypeCat_2 = distBoxData.Cat_2,
                DefaultCircuitType = distBoxData.DefaultCircuitType,
                FireLoad = false,
                Location = new ThPDSLocation
                {
                    ReferenceDWG = distBoxData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                    BasePoint = distBoxData.Position,
                }
            };
            return thPDSDistBox;
        }

        private ThPDSID CreatePDSID(List<string> infos, List<string> distBoxKey)
        {
            var id = new ThPDSID();
            var idMarks = new List<string>();
            var circuitMarks = new List<string>();
            infos.ForEach(info =>
            {
                var toAdd = true;
                foreach (var key in distBoxKey)
                {
                    if (info.Contains(key))
                    {
                        if (info.Contains("-W") || info.Contains("/W"))
                        {
                            circuitMarks.Add(info);
                        }
                        else
                        {
                            idMarks.Add(info);
                        }
                        toAdd = false;
                        break;
                    }
                }

                if (toAdd)
                {
                    id.Description.Add(info);
                }
            });

            if (idMarks.Distinct().Count() == 1)
            {
                id.LoadID = idMarks[0];
            }
            return id;
        }

        private ThPDSID CreatePDSID(List<string> infos)
        {
            return new ThPDSID
            {
                Description = infos,
            };
        }

        /// <summary>
        /// 回路标注解析
        /// </summary>
        /// <param name="infos"></param>
        /// <param name="distBoxKey"></param>
        /// <returns></returns>
        public ThPDSCircuit CircuitMarkAnalysis(List<string> infos, List<string> distBoxKey)
        {
            var id = CreateCircuitID(infos, distBoxKey);
            var circuitModel = SelectModel(id.CircuitNumber, ThPDSCircuitConfigModel.BlockConfig);
            var circuit = new ThPDSCircuit
            {
                ID = id,
                Type = circuitModel.CircuitType,
                KV = circuitModel.KV,
                Phase = circuitModel.Phase,
                DemandFactor = circuitModel.DemandFactor,
                PowerFactor = circuitModel.PowerFactor,
                FireLoad = circuitModel.FireLoad,
            };
            return circuit;
        }

        public ThPDSCircuit CircuitMarkAnalysis(List<string> infos)
        {
            var id = CreateCircuitID(infos);
            var circuitModel = SelectModel(id.CircuitNumber, ThPDSCircuitConfigModel.BlockConfig);
            var circuit = new ThPDSCircuit
            {
                ID = id,
                Type = circuitModel.CircuitType,
                KV = circuitModel.KV,
                Phase = circuitModel.Phase,
                DemandFactor = circuitModel.DemandFactor,
                PowerFactor = circuitModel.PowerFactor,
                FireLoad = circuitModel.FireLoad,
            };
            return circuit;
        }

        private ThPDSID CreateCircuitID(List<string> infos, List<string> distBoxKey)
        {
            var circuitID = new ThPDSID();
            var circuitMarks = new List<string>();
            infos.ForEach(info =>
            {
                foreach (var key in distBoxKey)
                {
                    if (info.Contains(key))
                    {
                        if (info.Contains("-W") || info.Contains("/W"))
                        {
                            circuitMarks.Add(info);
                        }
                    }
                }
            });

            if (circuitMarks.Distinct().Count() == 1)
            {
                circuitID.CircuitNumber = circuitMarks[0];
            }
            return circuitID;
        }

        private ThPDSID CreateCircuitID(List<string> infos)
        {
            var circuitID = new ThPDSID();
            if (infos.Distinct().Count() == 1)
            {
                circuitID.CircuitNumber = infos[0];
            }
            return circuitID;
        }

        private ThPDSCircuitModel SelectModel(string circuitNumber, List<ThPDSCircuitModel> table)
        {
            var result = new ThPDSCircuitModel();
            foreach(var o in table)
            {
                var check = o.TextKey.Replace("*", ".*");
                var r = new Regex(@check);
                var m = r.Match(circuitNumber);
                if (m.Success)
                {
                    result = o;
                    break;
                }
            }
            return result;
        }
    }
}
