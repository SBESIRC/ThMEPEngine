using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThCADExtension;
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
                ID = CreateDistBoxID(marks, distBoxKey, distBoxData.EffectiveName),
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
        public ThPDSLoad LoadMarkAnalysis(List<string> marks, List<string> distBoxKey, ThPDSBlockReferenceData distBoxData)
        {
            var searchedString = new List<string>();
            var thPDSLoad = new ThPDSLoad
            {
                ID = CreateLoadID(marks, distBoxKey, distBoxData.EffectiveName, searchedString),
                InstalledCapacity = AnalysisPower(marks, searchedString),
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

            var StandbyRelationship = AnalysisStandbyRelationship(marks, searchedString);
            if (StandbyRelationship.Item1)
            {
                thPDSLoad.PrimaryAvail = StandbyRelationship.Item2;
                thPDSLoad.SpareAvail = StandbyRelationship.Item3;
            }

            var r = new Regex(@"[a-zA-Z]");
            foreach (var str in marks.Except(searchedString))
            {
                if(r.Match(str).Success)
                {
                    thPDSLoad.ID.LoadID = str;
                }
                else
                {
                    thPDSLoad.ID.Description.Add(str);
                }
            }

            return thPDSLoad;
        }

        public ThPDSLoad LoadMarkAnalysis(ThPDSBlockReferenceData distBoxData)
        {
            return new ThPDSLoad
            {
                ID = new ThPDSID
                {
                    BlockName = distBoxData.EffectiveName,
                    LoadID = distBoxData.Attributes.ContainsKey(ThPDSCommon.LOAD_ID)
                        ? distBoxData.Attributes[ThPDSCommon.LOAD_ID] : "",
                    Description = distBoxData.Attributes.ContainsKey(ThPDSCommon.DESCRIPTION)
                        ? new List<string> { distBoxData.Attributes[ThPDSCommon.DESCRIPTION] } : new List<string> { "" },
                },
                InstalledCapacity = AnalysisPower(new List<string> {distBoxData.Attributes.ContainsKey(ThPDSCommon.ELECTRICITY)
                        ? distBoxData.Attributes[ThPDSCommon.ELECTRICITY] : "", }, new List<string>())
            };
        }

        private ThPDSID CreateDistBoxID(List<string> infos, List<string> distBoxKey, string blockName)
        {
            var id = new ThPDSID
            {
                BlockName = blockName
            };
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
            if (circuitMarks.Distinct().Count() == 1)
            {
                id.CircuitNumber = circuitMarks[0];
            }
            return id;
        }

        private ThPDSID CreateLoadID(List<string> infos, List<string> distBoxKey, string blockName, List<string> searchedString)
        {
            var id = new ThPDSID
            {
                BlockName = blockName,
            };
            var circuitNumbers = new List<string>();
            var circuitIDs = new List<string>();
            infos.ForEach(info =>
            {
                foreach (var key in distBoxKey)
                {
                    if (info.Contains(key))
                    {
                        circuitNumbers.Add(info);
                        searchedString.Add(info);
                        var check = "";
                        if (info.Contains("-W"))
                        {
                            check = "-W.*";
                        }
                        else if (info.Contains("/W"))
                        {
                            check = "/W.*";
                        }

                        if (string.IsNullOrEmpty(check))
                        {
                            break;
                        }
                        var r = new Regex(@check);
                        var m = r.Match(info);
                        if (m.Success)
                        {
                            circuitIDs.Add(m.Value.Substring(1, m.Value.Length - 1));
                        }
                        break;
                    }
                }
            });

            if (circuitNumbers.Distinct().Count() == 1)
            {
                id.CircuitNumber = circuitNumbers[0];
            }
            if (circuitIDs.Distinct().Count() == 1)
            {
                id.CircuitID = circuitIDs[0];
            }
            return id;
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
            var circuitIDs = new List<string>();
            var doSearch = true;
            infos.ForEach(info =>
            {
                foreach (var key in distBoxKey)
                {
                    if (info.Contains(key))
                    {
                        if (info.Contains("-W") || info.Contains("/W"))
                        {
                            circuitMarks.Add(info);
                            var check = "";
                            if (info.Contains("-W"))
                            {
                                check = "-W.*";
                            }
                            else if (info.Contains("/W"))
                            {
                                check = "/W.*";
                            }
                            var r = new Regex(@check);
                            var m = r.Match(info);
                            if (m.Success)
                            {
                                circuitIDs.Add(m.Value.Substring(1, m.Value.Length - 1));
                            }
                            doSearch = false;
                        }
                    }
                }
            });

            if (doSearch)
            {
                var check = "W[a-zA-Z]{0,}[0-9]+.*";
                var r = new Regex(@check);
                infos.ForEach(info =>
                {
                    var m = r.Match(info);
                    if (m.Success)
                    {
                        circuitIDs.Add(m.Value);
                    }
                });
            }

            if (circuitMarks.Distinct().Count() == 1)
            {
                circuitID.CircuitNumber = circuitMarks[0];
            }
            if (circuitIDs.Distinct().Count() == 1)
            {
                circuitID.CircuitID = circuitIDs[0];
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
            foreach (var o in table)
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

        private ThInstalledCapacity AnalysisPower(List<string> infos, List<string> searchedString)
        {
            var results = new ThInstalledCapacity();
            var check = "[0-9]+[.]?[0-9]{0,}[kK]?[wW]{1}";
            var r = new Regex(@check);
            infos.ForEach(info =>
            {
                var m = r.Match(info);
                while (m.Success)
                {
                    searchedString.Add(info);
                    var result = m.Value.Replace("k", "");
                    result = result.Replace("K", "");
                    result = result.Replace("w", "");
                    result = result.Replace("W", "");
                    if (m.Value.Contains("k") || m.Value.Contains("K"))
                    {
                        results.UsualPower.Add(double.Parse(result) * 1000);
                    }
                    else
                    {
                        results.UsualPower.Add(double.Parse(result));
                    }
                    m = m.NextMatch();
                }
            });
            return results;
        }

        private Tuple<bool, int, int> AnalysisStandbyRelationship(List<string> infos, List<string> searchedString)
        {
            var exist = false;
            var primaryAvail = 0;
            var spareAvail = 0;
            var check = "[一二三四五六七八九两]{1}用.*";
            var r = new Regex(@check);
            infos.ForEach(info =>
            {
                if (r.Match(info).Success)
                {
                    searchedString.Add(info);
                    exist = true;
                    var numberCheck = "[一二三四五六七八九两]{1}";
                    var numberRegex = new Regex(numberCheck);
                    var numberMatch = numberRegex.Match(info);
                    if (numberMatch.Success)
                    {
                        if (numberMatch.Value == "两")
                        {
                            primaryAvail = 2;
                        }
                        else
                        {
                            primaryAvail = ThStringTools.ChineseToNumber(numberMatch.Value);
                        }
                    }

                    numberMatch = numberMatch.NextMatch();
                    if (numberMatch.Success)
                    {
                        if (numberMatch.Value == "两")
                        {
                            spareAvail = 2;
                        }
                        else
                        {
                            spareAvail = ThStringTools.ChineseToNumber(numberMatch.Value);
                        }
                    }
                }
            });
            return Tuple.Create(exist, primaryAvail, spareAvail);
        }
    }
}
