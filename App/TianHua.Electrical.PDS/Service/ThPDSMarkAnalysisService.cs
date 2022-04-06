using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
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
                InstalledCapacity = AnalysisPower(marks, out _, out _),
                LoadTypeCat_1 = distBoxData.Cat_1,
                LoadTypeCat_2 = distBoxData.Cat_2,
                CircuitType = distBoxData.DefaultCircuitType,
                Phase = distBoxData.Phase,
                DemandFactor = distBoxData.DemandFactor,
                PowerFactor = distBoxData.PowerFactor,
                FireLoad = false,
                Location = new ThPDSLocation
                {
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
        public ThPDSLoad LoadMarkAnalysis(List<string> marks, List<string> distBoxKey,
            ThPDSBlockReferenceData distBoxData, ref string attributesCopy)
        {
            var searchedString = new List<string>();
            var thPDSLoad = new ThPDSLoad
            {
                ID = CreateLoadID(marks, distBoxKey, distBoxData.EffectiveName, searchedString),
                InstalledCapacity = AnalysisPower(marks, out var needCopy, out var frequencyConversion),
                LoadTypeCat_1 = distBoxData.Cat_1,
                LoadTypeCat_2 = distBoxData.Cat_2,
                CircuitType = distBoxData.DefaultCircuitType,
                Phase = distBoxData.Phase,
                DemandFactor = distBoxData.DemandFactor,
                PowerFactor = distBoxData.PowerFactor,
                Location = new ThPDSLocation
                {
                    ReferenceDWG = distBoxData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                    BasePoint = distBoxData.Position,
                },
                FrequencyConversion = frequencyConversion,
            };

            var fireLoad = false;
            marks.ForEach(str =>
            {
                if (str.Contains(ThPDSCommon.FIRE_POWER_SUPPLY))
                {
                    fireLoad = true;
                }
            });
            thPDSLoad.FireLoad = fireLoad;

            if (needCopy)
            {
                attributesCopy = distBoxData.EffectiveName;
            }

            var markStrings = new List<string>();
            marks.ForEach(o => markStrings.Add(o));
            var standbyRelationship = AnalysisStandbyRelationship(markStrings, searchedString);
            if (standbyRelationship.Item1)
            {
                thPDSLoad.PrimaryAvail = standbyRelationship.Item2;
                thPDSLoad.SpareAvail = standbyRelationship.Item3;
            }

            var r = new Regex(@"[a-zA-Z]");
            foreach (var str in markStrings.Except(searchedString))
            {
                if (r.Match(str).Success)
                {
                    if (distBoxData.EffectiveName.Equals("E-BL001-1"))
                    {
                        thPDSLoad.ID.CircuitID.Add(str);
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
                    // 电动机及负载标注 直接存块Id
                },
                InstalledCapacity = AnalysisPower(new List<string> {distBoxData.Attributes.ContainsKey(ThPDSCommon.ELECTRICITY)
                        ? distBoxData.Attributes[ThPDSCommon.ELECTRICITY] : "", }),
                FireLoad = distBoxData.CustomProperties.Contains(ThPDSCommon.POWER_CATEGORY)
                    ? distBoxData.CustomProperties.GetValue(ThPDSCommon.POWER_CATEGORY).Equals(ThPDSCommon.FIRE_POWER_SUPPLY) : false,
                LoadTypeCat_1 = distBoxData.Cat_1,
                LoadTypeCat_2 = distBoxData.Cat_2,
                CircuitType = distBoxData.DefaultCircuitType,
                Phase = distBoxData.Phase,
                DemandFactor = distBoxData.DemandFactor,
                PowerFactor = distBoxData.PowerFactor,
                Location = new ThPDSLocation
                {
                    ReferenceDWG = distBoxData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                    BasePoint = distBoxData.Position,
                },
                FrequencyConversion = distBoxData.Attributes.ContainsKey(ThPDSCommon.ELECTRICITY)
                    && distBoxData.Attributes[ThPDSCommon.ELECTRICITY].Contains(ThPDSCommon.FREQUENCY_CONVERSION),
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
                            if (m.Value.Contains("-W") || m.Value.Contains("/W"))
                            {
                                circuitMarks.Add(m.Value);
                            }
                            else
                            {
                                idMarks.Add(m.Value);
                            }
                            infos[i] = infos[i].Replace(m.Value, "");
                        }
                        break;
                    }
                }
            }

            if (idMarks.Distinct().Count() == 1)
            {
                id.LoadID = idMarks[0];
            }
            circuitMarks.Distinct().ForEach(o =>
            {
                var value = o.Replace("/", "-");
                var checkID = "-W[a-zA-Z]+[0-9]+";
                var regexID = new Regex(@checkID);
                var matchID = regexID.Match(value);
                if (matchID.Success)
                {
                    id.SourcePanelID.Add(o.Replace(matchID.Value, ""));
                    id.CircuitID.Add(matchID.Value.Replace("-", ""));
                }
            });

            return id;
        }

        private ThPDSID CreateLoadID(List<string> infos, List<string> distBoxKey,
            string blockName, List<string> searchedString)
        {
            var id = new ThPDSID
            {
                BlockName = blockName,
            };
            var panelIDs = new List<string>();
            var circuitIDs = new List<string>();
            infos.ForEach(str =>
            {
                foreach (var key in distBoxKey)
                {
                    if (str.Contains(key))
                    {
                        searchedString.Add(str);
                        var check = "";
                        if (str.Contains("-W"))
                        {
                            check = "-W.+";
                        }
                        else if (str.Contains("/W"))
                        {
                            check = "/W.+";
                        }

                        if (string.IsNullOrEmpty(check))
                        {
                            break;
                        }
                        var r = new Regex(@check);
                        var m = r.Match(str);
                        if (m.Success)
                        {
                            panelIDs.Add(str.Replace(m.Value, ""));
                            circuitIDs.Add(m.Value.Substring(1, m.Value.Length - 1));
                        }
                        break;
                    }
                }
            });

            panelIDs.Distinct().ForEach(o => id.SourcePanelID.Add(o));
            circuitIDs.Distinct().ForEach(o => id.CircuitID.Add(o));
            return id;
        }

        /// <summary>
        /// 回路标注解析
        /// </summary>
        /// <param name="infos"></param>
        /// <param name="distBoxKey"></param>
        /// <returns></returns>
        public ThPDSCircuit CircuitMarkAnalysis(string srcPanelID, List<string> infos,
            List<string> distBoxKey)
        {
            var id = CreateCircuitID(srcPanelID, infos, distBoxKey);
            var circuit = new ThPDSCircuit
            {
                ID = id,
            };
            return circuit;
        }

        public ThPDSCircuit CircuitMarkAnalysis(List<string> srcPanelID, List<string> circuitID)
        {
            var id = CreateCircuitID(srcPanelID, circuitID);
            var circuit = new ThPDSCircuit
            {
                ID = id,
            };
            return circuit;
        }

        private ThPDSID CreateCircuitID(string srcPanelID, List<string> infos, List<string> distBoxKey)
        {
            var circuitID = new ThPDSID();
            var panelIDs = new List<string>();
            var circuitIDs = new List<string>();
            var doSearch = true;
            infos.ForEach(str =>
            {
                foreach (var key in distBoxKey)
                {
                    if (str.Contains(key))
                    {
                        if (str.Contains("-W") || str.Contains("/W"))
                        {
                            var check = "";
                            if (str.Contains("-W"))
                            {
                                check = "-W[a-zA-Z]+[0-9]+";
                            }
                            else if (str.Contains("/W"))
                            {
                                check = "/W[a-zA-Z]+[0-9]+";
                            }
                            var r = new Regex(@check);
                            var m = r.Match(str);
                            if (m.Success)
                            {
                                panelIDs.Add(str.Replace(m.Value, ""));
                                circuitIDs.Add(m.Value.Substring(1, m.Value.Length - 1));
                            }
                            doSearch = false;
                        }
                    }
                }
            });

            if (doSearch)
            {
                var check = "W[a-zA-Z]+[0-9]+";
                var r = new Regex(@check);
                infos.ForEach(str =>
                {
                    var m = r.Match(str);
                    if (m.Success)
                    {
                        circuitIDs.Add(m.Value);
                    }
                });
            }

            circuitIDs = circuitIDs.Distinct().ToList();
            panelIDs = panelIDs.Distinct().ToList();
            if (circuitIDs.Count == 1)
            {
                if (circuitIDs.Count == panelIDs.Count
                    && (panelIDs[0].Equals(srcPanelID) || string.IsNullOrEmpty(srcPanelID)))
                {
                    circuitID.SourcePanelID.Add(panelIDs[0]);
                    circuitID.CircuitID.Add(circuitIDs[0]);
                }
                else if (!string.IsNullOrEmpty(srcPanelID))
                {
                    circuitID.SourcePanelID.Add(srcPanelID);
                    circuitID.CircuitID.Add(circuitIDs[0]);
                }
            }

            return circuitID;
        }

        private ThPDSID CreateCircuitID(List<string> srcPanelID, List<string> circuitID)
        {
            var thisCircuitID = new ThPDSID();
            thisCircuitID.SourcePanelID = srcPanelID;
            thisCircuitID.CircuitID = circuitID;
            return thisCircuitID;
        }

        private ThInstalledCapacity AnalysisPower(List<string> infos, out bool needCopy,
            out bool frequencyConversion)
        {
            var results = new ThInstalledCapacity();
            var check = "[0-9]+[.]?[0-9]{0,}[kK]?[wW]{1}";
            var r = new Regex(@check);
            needCopy = false;
            frequencyConversion = false;
            for (var i = 0; i < infos.Count; i++)
            {
                var m = r.Match(infos[i]);
                while (m.Success)
                {
                    infos[i] = infos[i].Replace(m.Value, "");
                    frequencyConversion = infos[i].Contains(ThPDSCommon.FREQUENCY_CONVERSION);
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
                        infos[i] = infos[i].Replace(numMatch.Value, "");
                        needCopy = true;
                    }

                    m = m.NextMatch();
                }
            }

            return results;
        }

        private ThInstalledCapacity AnalysisPower(List<string> infos)
        {
            var results = new ThInstalledCapacity();
            var check = "[0-9]+[.]?[0-9]{0,}[kK]?[wW]{1}";
            var r = new Regex(@check);
            for (var i = 0; i < infos.Count; i++)
            {
                var m = r.Match(infos[i]);
                while (m.Success)
                {
                    infos[i] = infos[i].Replace(m.Value, "");
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
                        infos[i] = infos[i].Replace(numMatch.Value, "");
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
