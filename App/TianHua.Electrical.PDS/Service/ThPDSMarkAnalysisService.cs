using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Linq2Acad;

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
                InstalledCapacity = AnalysisPower(marks, out _),
                LoadTypeCat_1 = distBoxData.Cat_1,
                LoadTypeCat_2 = distBoxData.Cat_2,
                DefaultCircuitType = distBoxData.DefaultCircuitType,
                Phase = distBoxData.Phase,
                DemandFactor = distBoxData.DemandFactor,
                PowerFactor = distBoxData.PowerFactor,
                FireLoad = false,
                Location = new ThPDSLocation
                {
                    ReferenceDWG = distBoxData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                    BasePoint = distBoxData.Position,
                }
            };
            thPDSDistBox.ID.BlockName = distBoxData.EffectiveName;
            foreach (var str in marks)
            {
                thPDSDistBox.ID.Description += StringClean(str);
            }

            return thPDSDistBox;
        }

        /// <summary>
        /// 负载标注解析
        /// </summary>
        /// <param name="marks"></param>
        /// <param name="distBoxKey"></param>
        /// <returns></returns>
        public ThPDSLoad LoadMarkAnalysis(List<string> marks, List<string> distBoxKey, ThPDSBlockReferenceData distBoxData,
            ref string attributesCopy)
        {
            var searchedString = new List<string>();
            var thPDSLoad = new ThPDSLoad
            {
                ID = CreateLoadID(marks, distBoxKey, distBoxData.EffectiveName, searchedString),
                InstalledCapacity = AnalysisPower(marks, out var needCopy),
                LoadTypeCat_1 = distBoxData.Cat_1,
                LoadTypeCat_2 = distBoxData.Cat_2,
                DefaultCircuitType = distBoxData.DefaultCircuitType,
                Phase = distBoxData.Phase,
                DemandFactor = distBoxData.DemandFactor,
                PowerFactor = distBoxData.PowerFactor,
                FireLoad = marks.Contains("消防电源"),
                Location = new ThPDSLocation
                {
                    ReferenceDWG = distBoxData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                    BasePoint = distBoxData.Position,
                }
            };

            if (needCopy)
            {
                attributesCopy = distBoxData.EffectiveName;
            }

            var standbyRelationship = AnalysisStandbyRelationship(marks, searchedString);
            if (standbyRelationship.Item1)
            {
                thPDSLoad.PrimaryAvail = standbyRelationship.Item2;
                thPDSLoad.SpareAvail = standbyRelationship.Item3;
            }

            var r = new Regex(@"[a-zA-Z]");
            foreach (var str in marks.Except(searchedString))
            {
                if (r.Match(str).Success)
                {
                    if (distBoxData.EffectiveName.Equals("E-BL001-1"))
                    {
                        thPDSLoad.ID.CircuitID = str;
                    }
                    else
                    {
                        thPDSLoad.ID.LoadID = str;
                    }
                }
                else
                {
                    thPDSLoad.ID.Description += str;
                }
            }

            if (thPDSLoad.LoadTypeCat_2 == ThPDSLoadTypeCat_2.Fan)
            {
                thPDSLoad.LoadTypeCat_3 = MatchFanIDCat3(thPDSLoad.ID.LoadID);
                if (thPDSLoad.LoadTypeCat_3 == ThPDSLoadTypeCat_3.None)
                {
                    thPDSLoad.LoadTypeCat_3 = MatchFanDescriptionCat3(thPDSLoad.ID.Description);
                }
            }
            else if (thPDSLoad.LoadTypeCat_2 == ThPDSLoadTypeCat_2.Pump)
            {
                thPDSLoad.LoadTypeCat_3 = MatchPumpCat3(thPDSLoad.ID.Description);
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
                        ? distBoxData.Attributes[ThPDSCommon.DESCRIPTION] : "",
                },
                InstalledCapacity = AnalysisPower(new List<string> {distBoxData.Attributes.ContainsKey(ThPDSCommon.ELECTRICITY)
                        ? distBoxData.Attributes[ThPDSCommon.ELECTRICITY] : "", }, out var needCopy),
                FireLoad = distBoxData.CustomProperties.Contains(ThPDSCommon.POWER_CATEGORY)
                    ? distBoxData.CustomProperties.GetValue(ThPDSCommon.POWER_CATEGORY).Equals("消防电源") : false,
                LoadTypeCat_1 = distBoxData.Cat_1,
                LoadTypeCat_2 = distBoxData.Cat_2,
                DefaultCircuitType = distBoxData.DefaultCircuitType,
                Phase = distBoxData.Phase,
                DemandFactor = distBoxData.DemandFactor,
                PowerFactor = distBoxData.PowerFactor,
                Location = new ThPDSLocation
                {
                    ReferenceDWG = distBoxData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                    BasePoint = distBoxData.Position,
                }
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
            for (var i = 0; i < infos.Count; i++)
            {
                foreach (var key in distBoxKey)
                {
                    if (infos[i].Contains(key))
                    {
                        var check = "[a-zA-Z0-9-/]+";
                        var r = new Regex(@check);
                        var m = r.Match(infos[i]);
                        if (m.Success)
                        {
                            infos[i] = infos[i].Replace(m.Value, "");
                            if (m.Value.Contains("-W") || m.Value.Contains("/W"))
                            {
                                circuitMarks.Add(m.Value);
                            }
                            else
                            {
                                idMarks.Add(m.Value);
                            }
                        }
                        break;
                    }
                }
            }

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
                var check = "W[a-zA-Z]{0,}[0-9]+.*";
                var r = new Regex(@check);
                infos.ForEach(info =>
                {
                    var m = r.Match(info);
                    if (m.Success)
                    {
                        circuitID.CircuitID = m.Value;
                    }
                });
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

        private ThInstalledCapacity AnalysisPower(List<string> infos, out bool needCopy)
        {
            var results = new ThInstalledCapacity();
            var check = "[0-9]+[.]?[0-9]{0,}[kK]?[wW]{1}";
            var r = new Regex(@check);
            needCopy = false;
            for (var i = 0; i < infos.Count; i++)
            {
                var m = r.Match(infos[i]);
                while (m.Success)
                {
                    var result = m.Value.Replace("k", "");
                    result = result.Replace("K", "");
                    result = result.Replace("w", "");
                    result = result.Replace("W", "");
                    if (!m.Value.Contains("k") && !m.Value.Contains("K"))
                    {
                        results.UsualPower.Add(double.Parse(result) / 1000.0);
                    }
                    else
                    {
                        results.UsualPower.Add(double.Parse(result));
                    }
                    var numRegex = new Regex(@"[2-9][xX]");
                    var numMatch = numRegex.Match(infos[i]);
                    if (numMatch.Success)
                    {
                        needCopy = true;
                    }

                    m = m.NextMatch();
                }
            }

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

        private static ThPDSLoadTypeCat_3 MatchFanIDCat3(string loadID)
        {
            if (loadID.Contains("ESF"))
            {
                return ThPDSLoadTypeCat_3.SmokeExhaustFan;
            }
            else if (loadID.Contains("SSF"))
            {
                return ThPDSLoadTypeCat_3.MakeupAirFan;
            }
            else if (loadID.Contains("SPF"))
            {
                return ThPDSLoadTypeCat_3.StaircasePressurizationFan;
            }
            else if (loadID.Contains("E(S)F"))
            {
                return ThPDSLoadTypeCat_3.ExhaustFan_Smoke;
            }
            else if (loadID.Contains("S(S)F"))
            {
                return ThPDSLoadTypeCat_3.SupplyFan_Smoke;
            }
            else if (loadID.Contains("EF"))
            {
                return ThPDSLoadTypeCat_3.ExhaustFan;
            }
            else if (loadID.Contains("SF"))
            {
                return ThPDSLoadTypeCat_3.SupplyFan;
            }
            else if (loadID.Contains("EKF"))
            {
                return ThPDSLoadTypeCat_3.KitchenExhaustFan;
            }
            return ThPDSLoadTypeCat_3.None;
        }

        public static ThPDSLoadTypeCat_3 MatchFanDescriptionCat3(string description)
        {
            if (description.Contains("事故风机"))
            {
                return ThPDSLoadTypeCat_3.EmergencyFan;
            }
            else
            {
                return ThPDSLoadTypeCat_3.None;
            }
        }

        public static ThPDSLoadTypeCat_3 MatchPumpCat3(string description)
        {
            if (description.Contains("生活水泵"))
            {
                return ThPDSLoadTypeCat_3.DomesticWaterPump;
            }
            else if (description.Contains("消防泵") || description.Contains("喷淋泵") || description.Contains("消火栓泵"))
            {
                return ThPDSLoadTypeCat_3.FirePump;
            }
            else if (description.Contains("潜水泵"))
            {
                return ThPDSLoadTypeCat_3.SubmersiblePump;
            }
            else
            {
                return ThPDSLoadTypeCat_3.None;
            }
        }

        private string StringClean(string str)
        {
            str = str.Replace(" ", "");
            if (str.IndexOf("（") == 0 && str.IndexOf("）") == str.Count() - 1)
            {
                str = str.Remove(str.Count() - 1);
                str = str.Remove(0, 1);
            }
            else if (str.IndexOf("(") == 0 && str.IndexOf(")") == str.Count() - 1)
            {
                str = str.Remove(str.Count() - 1);
                str = str.Remove(0, 1);
            }
            return str;
        }
    }
}
