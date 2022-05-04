﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
                Location = new ThPDSLocation
                {
                    BasePoint = ThPDSPoint3dService.ToPDSPoint3d(distBoxData.Position),
                }
            };
            thPDSDistBox.ID.BlockName = distBoxData.EffectiveName;
            foreach (var str in marks)
            {
                thPDSDistBox.ID.Description += StringClean(str);
            }

            thPDSDistBox.SetFireLoad(distBoxData.FireLoad);

            // 处理无标注时识别ACa不准确的情况
            if (thPDSDistBox.ID.LoadID.Contains("ACa")
                && thPDSDistBox.LoadTypeCat_2 != ThPDSLoadTypeCat_2.EmergencyPowerDistributionPanel)
            {
                thPDSDistBox.LoadTypeCat_2 = ThPDSLoadTypeCat_2.EmergencyPowerDistributionPanel;
                thPDSDistBox.CircuitType = ThPDSCircuitType.EmergencyPowerEquipment;
                thPDSDistBox.SetFireLoad(true);
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
            ThPDSBlockReferenceData loadData, ref string attributesCopy)
        {
            var searchedString = new List<string>();
            var thPDSLoad = new ThPDSLoad
            {
                ID = CreateLoadID(marks, distBoxKey, loadData.EffectiveName, searchedString),
                InstalledCapacity = AnalysisPower(marks, out var needCopy, out var frequencyConversion),
                LoadTypeCat_1 = loadData.Cat_1,
                LoadTypeCat_2 = loadData.Cat_2,
                CircuitType = loadData.DefaultCircuitType,
                Phase = loadData.Phase,
                DemandFactor = loadData.DemandFactor,
                PowerFactor = loadData.PowerFactor,
                Location = new ThPDSLocation
                {
                    ReferenceDWG = loadData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                    BasePoint = ThPDSPoint3dService.ToPDSPoint3d(loadData.Position),
                },
                FrequencyConversion = frequencyConversion,
            };

            if (loadData.FireLoad == ThPDSFireLoad.FireLoad)
            {
                thPDSLoad.SetFireLoad(true);
            }
            else if (loadData.FireLoad == ThPDSFireLoad.NonFireLoad)
            {
                thPDSLoad.SetFireLoad(false);
            }
            else if (loadData.FireLoad == ThPDSFireLoad.Unknown)
            {
                marks.ForEach(str =>
                {
                    if (str.Contains(ThPDSCommon.PROPERTY_VALUE_FIRE_POWER))
                    {
                        thPDSLoad.SetFireLoad(true);
                        return;
                    }
                    else if (str.Contains(ThPDSCommon.NON_PROPERTY_VALUE_FIRE_POWER))
                    {
                        thPDSLoad.SetFireLoad(false);
                        return;
                    }
                });
            }

            if (needCopy)
            {
                attributesCopy = loadData.EffectiveName;
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
                    if (loadData.EffectiveName.IndexOf(ThPDSCommon.LIGHTING_LOAD) == 0)
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
                var cat3 = MatchFanIDCat3(thPDSLoad.ID.LoadID);
                thPDSLoad.LoadTypeCat_3 = cat3.Item1;
                thPDSLoad.SetFireLoad(SetCat3FireLoad(thPDSLoad.GetFireLoad(), cat3));
                if (thPDSLoad.LoadTypeCat_3 == ThPDSLoadTypeCat_3.None)
                {
                    cat3 = MatchFanDescriptionCat3(thPDSLoad.ID.Description);
                    thPDSLoad.LoadTypeCat_3 = cat3.Item1;
                    thPDSLoad.SetFireLoad(SetCat3FireLoad(thPDSLoad.GetFireLoad(), cat3));
                }
            }
            else if (thPDSLoad.LoadTypeCat_2 == ThPDSLoadTypeCat_2.Pump)
            {
                var cat3 = MatchPumpCat3(thPDSLoad.ID.Description);
                thPDSLoad.LoadTypeCat_3 = cat3.Item1;
                thPDSLoad.SetFireLoad(SetCat3FireLoad(thPDSLoad.GetFireLoad(), cat3));
            }

            return thPDSLoad;
        }

        public ThPDSLoad LoadMarkAnalysis(ThPDSBlockReferenceData distBoxData)
        {
            var thPDSLoad = new ThPDSLoad
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
                LoadTypeCat_1 = distBoxData.Cat_1,
                LoadTypeCat_2 = distBoxData.Cat_2,
                CircuitType = distBoxData.DefaultCircuitType,
                Phase = distBoxData.Phase,
                DemandFactor = distBoxData.DemandFactor,
                PowerFactor = distBoxData.PowerFactor,
                Location = new ThPDSLocation
                {
                    ReferenceDWG = distBoxData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                    BasePoint = ThPDSPoint3dService.ToPDSPoint3d(distBoxData.Position),
                },
                FrequencyConversion = distBoxData.Attributes.ContainsKey(ThPDSCommon.ELECTRICITY)
                    && distBoxData.Attributes[ThPDSCommon.ELECTRICITY].Contains(ThPDSCommon.FREQUENCY_CONVERSION),
            };

            if (distBoxData.FireLoad == ThPDSFireLoad.FireLoad)
            {
                thPDSLoad.SetFireLoad(true);
            }
            else if (distBoxData.FireLoad == ThPDSFireLoad.NonFireLoad)
            {
                thPDSLoad.SetFireLoad(false);
            }
            else if (distBoxData.FireLoad == ThPDSFireLoad.Unknown)
            {
                if (distBoxData.CustomProperties.Contains(ThPDSCommon.POWER_CATEGORY))
                {
                    var fireLoad = distBoxData.CustomProperties.GetValue(ThPDSCommon.POWER_CATEGORY).Equals(ThPDSCommon.PROPERTY_VALUE_FIRE_POWER);
                    thPDSLoad.SetFireLoad(fireLoad);
                }
                else
                {
                    thPDSLoad.SetFireLoad(ThPDSFireLoad.Unknown);
                }
            }

            var cat3 = MatchFanIDCat3(thPDSLoad.ID.LoadID);
            thPDSLoad.LoadTypeCat_3 = cat3.Item1;
            thPDSLoad.SetFireLoad(SetCat3FireLoad(thPDSLoad.GetFireLoad(), cat3));
            if (thPDSLoad.LoadTypeCat_3 == ThPDSLoadTypeCat_3.None)
            {
                cat3 = MatchFanDescriptionCat3(thPDSLoad.ID.Description);
                thPDSLoad.LoadTypeCat_3 = cat3.Item1;
                thPDSLoad.SetFireLoad(SetCat3FireLoad(thPDSLoad.GetFireLoad(), cat3));
            }
            if (thPDSLoad.LoadTypeCat_3 == ThPDSLoadTypeCat_3.None)
            {
                cat3 = MatchPumpCat3(thPDSLoad.ID.Description);
                thPDSLoad.LoadTypeCat_3 = cat3.Item1;
                thPDSLoad.SetFireLoad(SetCat3FireLoad(thPDSLoad.GetFireLoad(), cat3));
            }

            return thPDSLoad;
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
                            var value = m.Value.Replace("/", "-");
                            var check1 = "-W[a-zA-Z]+[-0-9]+";
                            var regex1 = new Regex(@check1);
                            var match1 = regex1.Match(value);
                            var check2 = "-[0-9]W[0-9]{3}[-][0-9]";
                            var regex2 = new Regex(@check2);
                            var match2 = regex2.Match(value);
                            if (match1.Success || match2.Success)
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
                // 过滤无效回路信息
                if (!string.IsNullOrEmpty(id.LoadID) && o.Contains(id.LoadID))
                {
                    return;
                }

                var check1 = "-W[a-zA-Z]+[-0-9]+";
                var regex1 = new Regex(@check1);
                var match1 = regex1.Match(o);
                var check2 = "-[0-9]W[0-9]{3}[-][0-9]";
                var regex2 = new Regex(@check2);
                var match2 = regex2.Match(o);
                if (match1.Success)
                {
                    id.SourcePanelID.Add(o.Replace(match1.Value, ""));
                    id.CircuitID.Add(match1.Value.Replace("-", ""));
                }
                else if (match2.Success)
                {
                    id.SourcePanelID.Add(o.Replace(match2.Value, ""));
                    id.CircuitID.Add(match2.Value.Remove(0, 1));
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
                        var value = str.Replace("/", "-");
                        var check1 = "-W[a-zA-Z]+[-0-9]+";
                        var regex1 = new Regex(@check1);
                        var match1 = regex1.Match(value);
                        var check2 = "-[0-9]W[0-9]{3}[-][0-9]";
                        var regex2 = new Regex(@check2);
                        var match2 = regex2.Match(value);

                        if (match1.Success)
                        {
                            searchedString.Add(value);
                            panelIDs.Add(value.Replace(match1.Value, ""));
                            circuitIDs.Add(match1.Value.Substring(1, match1.Value.Length - 1));
                        }
                        else if (match2.Success)
                        {
                            searchedString.Add(value);
                            panelIDs.Add(value.Replace(match2.Value, ""));
                            circuitIDs.Add(match2.Value.Substring(1, match2.Value.Length - 1));
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
                        var value = str.Replace("/", "-");
                        var check1 = "-W[a-zA-Z]+[-0-9]+";
                        var regex1 = new Regex(@check1);
                        var match1 = regex1.Match(value);
                        var check2 = "-[0-9]W[0-9]{3}[-][0-9]";
                        var regex2 = new Regex(@check2);
                        var match2 = regex2.Match(value);

                        if (match1.Success)
                        {
                            panelIDs.Add(value.Replace(match1.Value, ""));
                            circuitIDs.Add(match1.Value.Substring(1, match1.Value.Length - 1));
                            doSearch = false;
                        }
                        else if (match2.Success)
                        {
                            panelIDs.Add(value.Replace(match2.Value, ""));
                            circuitIDs.Add(match2.Value.Substring(1, match2.Value.Length - 1));
                            doSearch = false;
                        }
                    }
                }
            });

            if (doSearch)
            {
                var check1 = "W[a-zA-Z]+[-0-9]+";
                var regex1 = new Regex(@check1);
                var check2 = "[0-9]W[0-9]{3}[-][0-9]";
                var regex2 = new Regex(@check2);
                infos.ForEach(str =>
                {
                    var match1 = regex1.Match(str);
                    var match2 = regex2.Match(str);
                    if (match1.Success)
                    {
                        circuitIDs.Add(match1.Value);
                    }
                    else if (match2.Success)
                    {
                        circuitIDs.Add(match2.Value);
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
                else if (panelIDs.Count == 0 && !string.IsNullOrEmpty(srcPanelID))
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
            var powers = new List<double>();
            var check = "[0-9]+[.]?[0-9]{0,}[kK]?[wW]{1}";
            var r = new Regex(@check);
            needCopy = false;
            frequencyConversion = false;
            for (var i = 0; i < infos.Count; i++)
            {
                var m = r.Match(infos[i]);
                while (m.Success)
                {
                    infos[i] = infos[i].Replace("/", "");
                    infos[i] = infos[i].Replace(m.Value, "");
                    frequencyConversion = infos[i].Contains(ThPDSCommon.FREQUENCY_CONVERSION);
                    var result = m.Value.Replace("k", "");
                    result = result.Replace("K", "");
                    result = result.Replace("w", "");
                    result = result.Replace("W", "");
                    if (!m.Value.Contains("k") && !m.Value.Contains("K"))
                    {
                        powers.Add(double.Parse(result) / 1000.0);
                    }
                    else
                    {
                        powers.Add(double.Parse(result));
                    }
                    var numRegex = new Regex(@"[1-9][xX]");
                    var numMatch = numRegex.Match(infos[i]);
                    if (numMatch.Success)
                    {
                        infos[i] = infos[i].Replace(numMatch.Value, "");
                        if (Convert.ToInt16(numMatch.Value[0]) > 1)
                        {
                            needCopy = true;
                        }
                    }

                    m = m.NextMatch();
                }
            }

            powers = powers.OrderBy(o => o).ToList();
            var results = new ThInstalledCapacity();
            if (powers.Count == 2)
            {
                results.IsDualPower = true;
                results.LowPower = powers[0];
                results.HighPower = powers[1];
            }
            else if (powers.Count == 1)
            {
                results.HighPower = powers[0];
            }

            return results;
        }

        private ThInstalledCapacity AnalysisPower(List<string> infos)
        {
            var powers = new List<double>();
            var check = "[0-9]*[.]?[0-9]*[/]?[0-9]+[.]?[0-9]*[kK]?[wW]{1}";
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
                    var value = result.Split("/".ToCharArray()).ToList();
                    value.ForEach(x =>
                    {
                        if (!m.Value.Contains("k") && !m.Value.Contains("K"))
                        {
                            powers.Add(double.Parse(x) / 1000.0);
                        }
                        else
                        {
                            powers.Add(double.Parse(x));
                        }
                    });

                    m = m.NextMatch();
                }
            }

            powers = powers.OrderBy(o => o).ToList();
            var results = new ThInstalledCapacity();
            if (powers.Count == 2)
            {
                results.IsDualPower = true;
                results.LowPower = powers[0];
                results.HighPower = powers[1];
            }
            else if (powers.Count == 1)
            {
                results.HighPower = powers[0];
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

        private static Tuple<ThPDSLoadTypeCat_3, bool> MatchFanIDCat3(string loadID)
        {
            if (loadID.Contains("ESF"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.SmokeExhaustFan, true);
            }
            else if (loadID.Contains("SSF"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.MakeupAirFan, true);
            }
            else if (loadID.Contains("SPF"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.StaircasePressurizationFan, true);
            }
            else if (loadID.Contains("E(S)F"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.ExhaustFan_Smoke, true);
            }
            else if (loadID.Contains("S(S)F"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.SupplyFan_Smoke, true);
            }
            else if (loadID.Contains("EF"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.ExhaustFan, false);
            }
            else if (loadID.Contains("SF"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.SupplyFan, false);
            }
            else if (loadID.Contains("EKF"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.KitchenExhaustFan, false);
            }
            return Tuple.Create(ThPDSLoadTypeCat_3.None, false);
        }

        public static Tuple<ThPDSLoadTypeCat_3, bool> MatchFanDescriptionCat3(string description)
        {
            if (description.Contains("事故风机") || description.Contains("事故排风") || description.Contains("事故送风"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.EmergencyFan, false);
            }
            else
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.None, false);
            }
        }

        public static Tuple<ThPDSLoadTypeCat_3, bool> MatchPumpCat3(string description)
        {
            if (description.Contains("生活水泵"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.DomesticWaterPump, false);
            }
            else if (description.Contains("消防泵") || description.Contains("喷淋泵") || description.Contains("消火栓泵"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.FirePump, true);
            }
            else if (description.Contains("稳压泵"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.RegulatorsPump, false);
            }
            else if (description.Contains("潜水泵"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.SubmersiblePump, false);
            }
            else
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.None, false);
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

        private ThPDSFireLoad SetCat3FireLoad(ThPDSFireLoad fireLoad, Tuple<ThPDSLoadTypeCat_3, bool> cat3)
        {
            if (fireLoad != ThPDSFireLoad.Unknown)
            {
                return fireLoad;
            }
            else
            {
                if (cat3.Item1 != ThPDSLoadTypeCat_3.None)
                {
                    return cat3.Item2 ? ThPDSFireLoad.FireLoad : ThPDSFireLoad.NonFireLoad;
                }
                else
                {
                    return ThPDSFireLoad.Unknown;
                }
            }
        }
    }
}
