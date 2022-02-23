using System.Collections.Generic;
using System.Linq;
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
        public ThPDSLoad DistBoxMarkAnalysis(List<string> marks, List<string> distBoxKey)
        {
            var thPDSDistBox = new ThPDSLoad
            {
                ID = CreatePDSID(marks, distBoxKey),
                LoadType = ThPDSLoadType.DistributionPanel
            };
            return thPDSDistBox;
        }

        /// <summary>
        /// 负载标注解析
        /// </summary>
        /// <param name="marks"></param>
        /// <param name="distBoxKey"></param>
        /// <returns></returns>
        public ThPDSLoad LoadMarkAnalysis(List<string> marks)
        {
            var thPDSDistBox = new ThPDSLoad
            {
                ID = CreatePDSID(marks),
                LoadType = ThPDSLoadType.None,
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
            var circuit = new ThPDSCircuit
            {
                ID = CreateCircuitID(infos, distBoxKey)
            };
            return circuit;
        }

        public ThPDSCircuit CircuitMarkAnalysis(List<string> infos)
        {
            var circuit = new ThPDSCircuit
            {
                ID = CreateCircuitID(infos)
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
    }
}
