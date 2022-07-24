using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;

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
                ID = CreateDistBoxID(marks, distBoxKey, distBoxData),
                InstalledCapacity = AnalysePower(marks, out _, out _),
                LoadTypeCat_1 = distBoxData.Cat_1,
                LoadTypeCat_2 = distBoxData.Cat_2,
                CircuitType = distBoxData.DefaultCircuitType,
                Phase = distBoxData.Phase,
                DemandFactor = distBoxData.DemandFactor,
                PowerFactor = distBoxData.PowerFactor,
                CableLayingMethod1 = distBoxData.CableLayingMethod1,
                CableLayingMethod2 = distBoxData.CableLayingMethod2,
            };
            thPDSDistBox.SetLocation(new ThPDSLocation
            {
                BasePoint = ThPDSPoint3dService.ToPDSPoint3d(distBoxData.Position.TransformBy(distBoxData.OwnerSpace2WCS)),
            });

            var descriptionAssign = true;
            var numRegex = new Regex(@"[0-9]+");
            var powerTransformerCircuitRegex = new Regex(@"[0-9]?W[0-9]{1}[0-9]{2}-[0-9]{1,2}");
            var powerTransformerRegex = new Regex(@"[0-9]?W[0-9]{1}[0-9]{2}");
            foreach (var str in marks)
            {
                if (thPDSDistBox.LoadTypeCat_2.Equals(ThPDSLoadTypeCat_2.ResidentialDistributionPanel)
                    && str.Contains("AR"))
                {
                    var idRegex = new Regex(@"AR-[0-9]+");
                    var idMatch = idRegex.Match(str);
                    if (idMatch.Success)
                    {
                        thPDSDistBox.ID.LoadID = idMatch.Value;
                    }
                }
                else
                {
                    var strClean = StringClean(str);
                    if (strClean.Contains("n=") || strClean.Contains("N="))
                    {
                        if (strClean.Contains("~"))
                        {
                            var match = numRegex.Match(strClean);
                            if (match.Success)
                            {
                                var start = Convert.ToInt16(match.Value);
                                match = match.NextMatch();
                                if (match.Success)
                                {
                                    var end = Convert.ToInt16(match.Value);
                                    for (var k = start; k <= end; k++)
                                    {
                                        thPDSDistBox.ID.Storeys.Add(k);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var match = numRegex.Match(strClean);
                            while (match.Success)
                            {
                                thPDSDistBox.ID.Storeys.Add(Convert.ToInt16(match.Value));
                                match = match.NextMatch();
                            }
                        }
                    }
                    else
                    {
                        var match = powerTransformerCircuitRegex.Match(strClean);
                        if (match.Success)
                        {
                            while (match.Success)
                            {
                                var powerTransformer = powerTransformerRegex.Match(match.Value).Value;
                                var circuitId = match.Value.Replace(powerTransformer, "").Replace("-", "");
                                thPDSDistBox.ID.PowerTransformerCircuitList.Add(Tuple.Create(powerTransformer, circuitId));
                                match = match.NextMatch();
                            }
                        }
                        else if (!string.IsNullOrEmpty(strClean) && Regex.IsMatch(strClean, @"[\u4e00-\u9fa5]") && descriptionAssign)
                        {
                            thPDSDistBox.ID.Description = strClean;
                            descriptionAssign = false;
                        }
                    }
                }
            }

            thPDSDistBox.SetFireLoad(distBoxData.FireLoad);

            if (thPDSDistBox.LoadTypeCat_2.Equals(ThPDSLoadTypeCat_2.ResidentialDistributionPanel)
                && thPDSDistBox.InstalledCapacity.HighPower == 0.0)
            {
                ResidentialPanelHandle(thPDSDistBox, marks);
            }

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
        /// 住户配电箱的部分特殊处理
        /// </summary>
        /// <param name="distBox"></param>
        /// <param name="marks"></param>
        private void ResidentialPanelHandle(ThPDSLoad distBox, List<string> marks)
        {
            distBox.InstalledCapacity.HighPower = AnalyseResidentialPower(marks);
            if (PDSProject.Instance.projectGlobalConfiguration.MeterBoxCircuitType.Equals(MeterBoxCircuitType.上海住宅)
             || PDSProject.Instance.projectGlobalConfiguration.MeterBoxCircuitType.Equals(MeterBoxCircuitType.国标_表在断路器前)
             || PDSProject.Instance.projectGlobalConfiguration.MeterBoxCircuitType.Equals(MeterBoxCircuitType.国标_表在断路器后))
            {
                if (distBox.InstalledCapacity.HighPower < 12)
                {
                    distBox.Phase = ThPDSPhase.一相;
                }
                else
                {
                    distBox.Phase = ThPDSPhase.三相;
                }
            }
            else if (PDSProject.Instance.projectGlobalConfiguration.MeterBoxCircuitType.Equals(MeterBoxCircuitType.江苏住宅))
            {
                if (distBox.InstalledCapacity.HighPower <= 20)
                {
                    distBox.Phase = ThPDSPhase.一相;
                }
                else
                {
                    distBox.Phase = ThPDSPhase.三相;
                }
            }
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
                ID = CreateLoadID(marks, distBoxKey, loadData, searchedString),
                InstalledCapacity = AnalysePower(marks, out var needCopy, out var frequencyConversion),
                LoadTypeCat_1 = loadData.Cat_1,
                LoadTypeCat_2 = loadData.Cat_2,
                CircuitType = loadData.DefaultCircuitType,
                Phase = loadData.Phase,
                DemandFactor = loadData.DemandFactor,
                PowerFactor = loadData.PowerFactor,
                FrequencyConversion = frequencyConversion,
                CableLayingMethod1 = loadData.CableLayingMethod1,
                CableLayingMethod2 = loadData.CableLayingMethod2,
            };
            thPDSLoad.SetLocation(new ThPDSLocation
            {
                ReferenceDWG = loadData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                BasePoint = ThPDSPoint3dService.ToPDSPoint3d(loadData.Position.TransformBy(loadData.OwnerSpace2WCS)),
            });

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

            if (thPDSLoad.LoadTypeCat_2 == ThPDSLoadTypeCat_2.ACCharger)
            {
                if (thPDSLoad.InstalledCapacity.HighPower == 0)
                {
                    var N = 0;
                    switch (thPDSLoad.ID.BlockName)
                    {
                        case "E-BDB111":
                        case "＄equip_U＄00000102":
                        case "＄equip_U＄00000109":
                            N = 1;
                            break;
                        case "E-BDB112":
                        case "＄equip_U＄00000103":
                            N = 2;
                            break;
                        case "E-BDB114":
                        case "＄equip_U＄00000104":
                            N = 4;
                            break;
                    }
                    thPDSLoad.InstalledCapacity.HighPower = N * PDSProject.Instance.projectGlobalConfiguration.ACChargerPower;
                }
            }
            else if (thPDSLoad.LoadTypeCat_2 == ThPDSLoadTypeCat_2.DCCharger)
            {
                if (thPDSLoad.InstalledCapacity.HighPower == 0)
                {
                    thPDSLoad.InstalledCapacity.HighPower = PDSProject.Instance.projectGlobalConfiguration.DCChargerPower;
                }
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

            var r = new Regex(@"[\u4e00-\u9fa5]");
            var descriptionAssign = true;
            foreach (var str in markStrings.Except(searchedString))
            {
                if (!r.Match(str).Success)
                {
                    // 过滤电压
                    if (new Regex(@"[0-9]+[kK]?[vV]{1}").Match(str).Success
                        && (str.IndexOf('v') == str.Count() - 1 || str.IndexOf('V') == str.Count() - 1))
                    {
                        continue;
                    }

                    var value = ThPDSReplaceStringService.ReplaceLastChar(str, "/", "-");
                    var check1 = "W[a-zA-Z]+[-0-9]+";
                    var regex1 = new Regex(@check1);
                    var match1 = regex1.Match(value);
                    var check2 = "[0-9]W[0-9]{3}[-][0-9]";
                    var regex2 = new Regex(@check2);
                    var match2 = regex2.Match(value);
                    if (match1.Success || match2.Success)
                    {
                        thPDSLoad.ID.CircuitIDList.Add(str);
                    }
                    else
                    {
                        var assign = true;
                        foreach (var key in distBoxKey)
                        {
                            if (str.Contains(key))
                            {
                                assign = false;
                            }
                        }
                        if (assign)
                        {
                            thPDSLoad.ID.LoadID = StringFilter(str);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(str) && descriptionAssign)
                {
                    thPDSLoad.ID.Description = str;
                    descriptionAssign = false;
                }
            }

            if (thPDSLoad.LoadTypeCat_1 == ThPDSLoadTypeCat_1.Motor)
            {
                var cat3 = MatchFanIDCat3(thPDSLoad.ID.LoadID, thPDSLoad.ID.Description);
                if (cat3.Item1 != ThPDSLoadTypeCat_3.None)
                {
                    thPDSLoad.LoadTypeCat_3 = cat3.Item1;
                    thPDSLoad.ID.DefaultDescription = cat3.Item1.GetDescription();
                    thPDSLoad.SetFireLoad(SetCat3FireLoad(thPDSLoad.GetFireLoad(), cat3));
                }
                if (thPDSLoad.LoadTypeCat_3 == ThPDSLoadTypeCat_3.None)
                {
                    cat3 = MatchPumpCat3(thPDSLoad.ID.Description);
                    if (cat3.Item1 != ThPDSLoadTypeCat_3.None)
                    {
                        thPDSLoad.LoadTypeCat_3 = cat3.Item1;
                        thPDSLoad.ID.DefaultDescription = cat3.Item1.GetDescription();
                        thPDSLoad.SetFireLoad(SetCat3FireLoad(thPDSLoad.GetFireLoad(), cat3));
                    }
                }
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
                    DefaultDescription = distBoxData.DefaultDescription,
                    // 电动机及负载标注 直接存块Id
                },
                LoadTypeCat_1 = distBoxData.Cat_1,
                LoadTypeCat_2 = distBoxData.Cat_2,
                CircuitType = distBoxData.DefaultCircuitType,
                Phase = distBoxData.Phase,
                DemandFactor = distBoxData.DemandFactor,
                PowerFactor = distBoxData.PowerFactor,
                FrequencyConversion = distBoxData.Attributes.ContainsKey(ThPDSCommon.ELECTRICITY)
                    && distBoxData.Attributes[ThPDSCommon.ELECTRICITY].Contains(ThPDSCommon.FREQUENCY_CONVERSION),
                CableLayingMethod1 = distBoxData.CableLayingMethod1,
                CableLayingMethod2 = distBoxData.CableLayingMethod2,
            };
            if (distBoxData.Attributes.ContainsKey(ThPDSCommon.ELECTRICITY))
            {
                thPDSLoad.InstalledCapacity = AnalysePower(new List<string> { CleanBlank(distBoxData.Attributes[ThPDSCommon.ELECTRICITY]) });
            }
            else if (distBoxData.Attributes.ContainsKey(ThPDSCommon.LOAD_ELECTRICITY))
            {
                thPDSLoad.InstalledCapacity = AnalysePower(new List<string> { CleanBlank(distBoxData.Attributes[ThPDSCommon.LOAD_ELECTRICITY]) });
            }

            thPDSLoad.SetLocation(new ThPDSLocation
            {
                ReferenceDWG = distBoxData.Database.OriginalFileName.Split("\\".ToCharArray()).Last(),
                BasePoint = ThPDSPoint3dService.ToPDSPoint3d(distBoxData.Position.TransformBy(distBoxData.OwnerSpace2WCS)),
            });

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
                using (var acadDatabase = AcadDatabase.Use(distBoxData.Database))
                {
                    var br = distBoxData.ObjId.GetObject(OpenMode.ForRead) as BlockReference;
                    //如果不是动态块，则返回
                    if (br == null || !br.IsDynamicBlock)
                    {
                        thPDSLoad.SetFireLoad(ThPDSFireLoad.Unknown);
                    }
                    else
                    {
                        //获得动态块的动态属性
                        var customProperties = br.DynamicBlockReferencePropertyCollection;
                        if (customProperties.Contains(ThPDSCommon.POWER_CATEGORY))
                        {
                            var fireLoad = customProperties.GetValue(ThPDSCommon.POWER_CATEGORY).Equals(ThPDSCommon.PROPERTY_VALUE_FIRE_POWER);
                            thPDSLoad.SetFireLoad(fireLoad);
                        }
                    }
                }
            }

            var cat3 = MatchFanIDCat3(thPDSLoad.ID.LoadID, thPDSLoad.ID.Description);
            if (cat3.Item1 != ThPDSLoadTypeCat_3.None)
            {
                thPDSLoad.LoadTypeCat_3 = cat3.Item1;
                thPDSLoad.ID.DefaultDescription = cat3.Item1.GetDescription();
                thPDSLoad.SetFireLoad(SetCat3FireLoad(thPDSLoad.GetFireLoad(), cat3));
            }
            else
            {
                cat3 = MatchPumpCat3(thPDSLoad.ID.Description);
                if (cat3.Item1 != ThPDSLoadTypeCat_3.None)
                {
                    thPDSLoad.LoadTypeCat_3 = cat3.Item1;
                    thPDSLoad.ID.DefaultDescription = cat3.Item1.GetDescription();
                    thPDSLoad.SetFireLoad(SetCat3FireLoad(thPDSLoad.GetFireLoad(), cat3));
                }
            }

            return thPDSLoad;
        }

        private ThPDSID CreateDistBoxID(List<string> infos, List<string> distBoxKey, ThPDSBlockReferenceData blockData)
        {
            var id = new ThPDSID
            {
                BlockName = blockData.EffectiveName,
                DefaultDescription = blockData.DefaultDescription,
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
                        if (new Regex(@"[(][aAbBzZ][)]").Match(infos[i]).Success)
                        {
                            check = "[a-zA-Z0-9-/]+[(][aAbBzZ][)][a-zA-Z0-9-/]*";
                        }
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
                                infos[i] = infos[i].Replace(m.Value, "");
                            }
                            else
                            {
                                if (blockData.Attributes.ContainsKey("BOX"))
                                {
                                    if (m.Value.Contains(blockData.Attributes["BOX"])
                                        || blockData.Attributes["BOX"].Contains("K")
                                        || blockData.Attributes["BOX"].Contains("INT"))
                                    {
                                        idMarks.Add(m.Value);
                                    }
                                    infos[i] = infos[i].Replace(m.Value, "");
                                }
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
            circuitMarks.Distinct().ForEach(o =>
            {
                // 过滤无效回路信息
                if (!string.IsNullOrEmpty(id.LoadID) && o.Contains(id.LoadID))
                {
                    return;
                }

                var value = o;
                var count = o.Count(c => c.Equals('/'));
                if (count > 1)
                {
                    value = ThPDSReplaceStringService.ReplaceLastChar(o, "/", "-");
                }
                else if (count == 1)
                {
                    var charRegex = new Regex(@"[0-9]+/[0-9]+-");
                    if (!charRegex.Match(o).Success)
                    {
                        value = ThPDSReplaceStringService.ReplaceLastChar(o, "/", "-");
                    }
                }
                var check1 = "-W[a-zA-Z]+[-0-9]+";
                var regex1 = new Regex(@check1);
                var match1 = regex1.Match(value);
                var check2 = "-[0-9]W[0-9]{3}[-][0-9]";
                var regex2 = new Regex(@check2);
                var match2 = regex2.Match(value);
                if (match1.Success)
                {
                    id.SourcePanelIDList.Add(value.Replace(match1.Value, ""));
                    id.CircuitIDList.Add(match1.Value.Replace("-", ""));
                }
                else if (match2.Success)
                {
                    id.SourcePanelIDList.Add(value.Replace(match2.Value, ""));
                    id.CircuitIDList.Add(match2.Value.Remove(0, 1));
                }
            });

            return id;
        }

        private ThPDSID CreateLoadID(List<string> infos, List<string> distBoxKey,
            ThPDSBlockReferenceData blockData, List<string> searchedString)
        {
            var id = new ThPDSID
            {
                BlockName = blockData.EffectiveName,
                DefaultDescription = blockData.DefaultDescription,
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

            panelIDs.Distinct().ForEach(o => id.SourcePanelIDList.Add(o));
            circuitIDs.Distinct().ForEach(o => id.CircuitIDList.Add(o));
            return id;
        }

        /// <summary>
        /// 回路标注解析
        /// </summary>
        /// <param name="infos"></param>
        /// <param name="distBoxKey"></param>
        /// <returns></returns>
        public ThPDSCircuit CircuitMarkAnalysis(string srcPanelID, string tarPanelID, List<string> infos,
            List<string> distBoxKey)
        {
            var id = CreateCircuitID(srcPanelID, tarPanelID, infos, distBoxKey);
            if (string.IsNullOrEmpty(id.SourcePanelID) && !string.IsNullOrEmpty(srcPanelID))
            {
                id.SourcePanelIDList.Add(srcPanelID);
            }
            var circuit = new ThPDSCircuit
            {
                ID = id,
            };
            return circuit;
        }

        public ThPDSCircuit CircuitMarkAnalysis(Tuple<string, string> powerTransformerNumber)
        {
            var circuit = new ThPDSCircuit();
            circuit.ID.SourcePanelIDList.Add(powerTransformerNumber.Item1);
            circuit.ID.CircuitIDList.Add(powerTransformerNumber.Item2);

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

        private ThPDSID CreateCircuitID(string srcPanelID, string tarPanelID, List<string> infos, List<string> distBoxKey)
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
                        var value = ThPDSReplaceStringService.ReplaceLastChar(str, "/", "-");
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
                var regex1 = new Regex(@"W[a-zA-Z]+[-0-9]+");
                var regex2 = new Regex(@"[0-9]W[0-9]{3}[-][0-9]");
                infos.ForEach(str =>
                {
                    if (!string.IsNullOrEmpty(tarPanelID) && str.Contains(tarPanelID))
                    {
                        return;
                    }
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
                    && (panelIDs[0].Equals(srcPanelID) || string.IsNullOrEmpty(srcPanelID))
                    && !(panelIDs[0].Equals(tarPanelID)))
                {
                    circuitID.SourcePanelIDList.Add(panelIDs[0]);
                    circuitID.CircuitIDList.Add(circuitIDs[0]);
                }
                else if (panelIDs.Count == 0 && !string.IsNullOrEmpty(srcPanelID))
                {
                    circuitID.SourcePanelIDList.Add(srcPanelID);
                    circuitID.CircuitIDList.Add(circuitIDs[0]);
                }
            }

            return circuitID;
        }

        private ThPDSID CreateCircuitID(List<string> srcPanelID, List<string> circuitID)
        {
            var thisCircuitID = new ThPDSID
            {
                SourcePanelIDList = srcPanelID,
                CircuitIDList = circuitID
            };
            return thisCircuitID;
        }

        private ThInstalledCapacity AnalysePower(List<string> infos, out bool needCopy,
            out bool frequencyConversion)
        {
            var powers = new List<double>();
            var check = "[0-9]*[.]?[0-9]*[/]?[0-9]+[.]?[0-9]*[kK]?[wW]{1}";
            var r = new Regex(@check);
            needCopy = false;
            frequencyConversion = false;
            for (var i = 0; i < infos.Count; i++)
            {
                var m = r.Match(infos[i]);
                while (m.Success && (infos[i].IndexOf(m.Value) + m.Value.Count() + 1 > infos[i].Count()
                    || (infos[i][infos[i].IndexOf(m.Value) + m.Value.Count()] < '0' || infos[i][infos[i].IndexOf(m.Value) + m.Value.Count()] > '9')))
                {
                    infos[i] = infos[i].Replace(m.Value, "");
                    infos[i] = infos[i].Replace("/", "");
                    frequencyConversion = infos[i].Contains(ThPDSCommon.FREQUENCY_CONVERSION);
                    var result = m.Value.Replace("k", "");
                    result = result.Replace("K", "");
                    result = result.Replace("w", "");
                    result = result.Replace("W", "");
                    var value = result.Split("/".ToCharArray()).ToList();
                    value.ForEach(x =>
                    {
                        if (string.IsNullOrEmpty(x))
                        {
                            return;
                        }

                        if (!m.Value.Contains("k") && !m.Value.Contains("K"))
                        {
                            if (double.TryParse(x, out var xValue))
                            {
                                powers.Add(xValue / 1000.0);
                            }
                        }
                        else
                        {
                            if (double.TryParse(x, out var xValue))
                            {
                                powers.Add(xValue);
                            }
                        }
                    });

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

        private ThInstalledCapacity AnalysePower(List<string> infos)
        {
            var powers = new List<double>();
            var check = "[0-9]*[.]?[0-9]*[/]?[0-9]+[.]?[0-9]*[kK]?[wW]{1}";
            var r = new Regex(@check);
            for (var i = 0; i < infos.Count; i++)
            {
                infos[i] = infos[i].Replace("\\", "/");
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
                            if (double.TryParse(x, out var xValue))
                            {
                                powers.Add(xValue / 1000.0);
                            }
                        }
                        else
                        {
                            if (double.TryParse(x, out var xValue))
                            {
                                powers.Add(xValue);
                            }
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

        private double AnalyseResidentialPower(List<string> infos)
        {
            var highPower = 0.0;
            var regex = new Regex(@"AR-[0-9]+");
            foreach (var info in infos)
            {
                var match = regex.Match(info);
                if (match.Success)
                {
                    var numberRegex = new Regex(@"[0-9]+");
                    var numberMatch = numberRegex.Match(match.Value);
                    if (numberMatch.Success)
                    {
                        highPower = Convert.ToDouble(numberMatch.Value);
                        break;
                    }
                }
            }
            return highPower;
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

        private static Tuple<ThPDSLoadTypeCat_3, bool> MatchFanIDCat3(string loadID, string description)
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
                if (description.Contains("事故风机") || description.Contains("事故排风") || description.Contains("事故送风"))
                {
                    return Tuple.Create(ThPDSLoadTypeCat_3.EmergencyFan, false);
                }
                else if (description.Contains("事故后风机") || description.Contains("事故后排风") || description.Contains("事故后送风"))
                {
                    return Tuple.Create(ThPDSLoadTypeCat_3.PostEmergencyFan, false);
                }
                else
                {
                    return Tuple.Create(ThPDSLoadTypeCat_3.ExhaustFan, false);
                }
            }
            else if (loadID.Contains("SF"))
            {
                if (description.Contains("事故风机") || description.Contains("事故排风") || description.Contains("事故送风"))
                {
                    return Tuple.Create(ThPDSLoadTypeCat_3.EmergencyFan, false);
                }
                else if (description.Contains("事故后风机") || description.Contains("事故后排风") || description.Contains("事故后送风"))
                {
                    return Tuple.Create(ThPDSLoadTypeCat_3.PostEmergencyFan, false);
                }
                else
                {
                    return Tuple.Create(ThPDSLoadTypeCat_3.SupplyFan, false);
                }
            }
            else if (loadID.Contains("EKF"))
            {
                return Tuple.Create(ThPDSLoadTypeCat_3.KitchenExhaustFan, false);
            }
            return Tuple.Create(ThPDSLoadTypeCat_3.None, false);
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
            str = str.Replace("(", "");
            str = str.Replace("（", "");
            str = str.Replace(")", "");
            str = str.Replace("）", "");
            // 严格清理
            //if (str.IndexOf("（") == 0 && str.LastIndexOf("）") == str.Count() - 1)
            //{
            //    str = str.Remove(str.Count() - 1);
            //    str = str.Remove(0, 1);
            //}
            //else if (str.IndexOf("(") == 0 && str.LastIndexOf(")") == str.Count() - 1)
            //{
            //    str = str.Remove(str.Count() - 1);
            //    str = str.Remove(0, 1);
            //}
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

        private string StringFilter(string str)
        {
            str = str.Replace(" ", "");
            str = str.Replace("(", "");
            str = str.Replace("（", "");
            str = str.Replace(")", "");
            str = str.Replace("）", "");
            if (str.Equals("E"))
            {
                str = "";
            }
            return str;
        }

        /// <summary>
        /// 清除字符串中的所有空格
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string CleanBlank(string str)
        {
            str = str.Replace(" ", "");
            return str;
        }
    }
}
