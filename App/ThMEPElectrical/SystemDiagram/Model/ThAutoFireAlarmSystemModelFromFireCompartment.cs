using System;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.SystemDiagram.Extension;
using ThMEPElectrical.SystemDiagram.Service;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// THAFAS V1.0
    /// 自动火灾报警系统Model(按防火分区区分)
    /// 一个系统图里首先分楼层
    /// 其次每个楼层分0/1/N个防火分区
    /// </summary>
    public class ThAutoFireAlarmSystemModelFromFireCompartment : ThAutoFireAlarmSystemModel
    {
        private Dictionary<Entity, List<KeyValuePair<string, string>>> GlobleBlockAttInfoDic;
        private Dictionary<Entity, DBPoint> GlobleCenterPointDic;
        private List<ThIfcDistributionFlowElement> GlobalBlockInfo;
        private ThCADCoreNTSSpatialIndex GlobalBlockInfoSpatialIndex;
        private List<ThIfcDistributionFlowElement> FloorBlockInfo;
        private ThCADCoreNTSSpatialIndex FloorBlockInfoSpatialIndex;
        private List<string> FireCompartmentNameList;

        public ThAutoFireAlarmSystemModelFromFireCompartment()
        {
            floors = new List<ThFloorModel>();
            FireCompartmentNameList = new List<string>();
        }

        /// <summary>
        /// 设置全局空间索引
        /// </summary>
        public override void SetGlobalData(Database database, Dictionary<Entity, List<KeyValuePair<string, string>>> elements, List<Entity> Entitydata)
        {
            this._database = database;
            GlobleBlockAttInfoDic = elements;
            GlobalBlockInfo = new List<ThIfcDistributionFlowElement>();
            GlobleCenterPointDic = new Dictionary<Entity, DBPoint>();
            elements.ForEach(o =>
            {
                GlobalBlockInfo.Add(new ThIfcDistributionFlowElement() { Outline = o.Key });
                GlobleCenterPointDic.Add(o.Key, new DBPoint(database.GetBlockReferenceOBBCenter((o.Key as BlockReference))));
            });
            var dbObjs = GlobleCenterPointDic.Select(o => o.Value).ToCollection();
            GlobalBlockInfoSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
        }

        /// <summary>
        /// 获取楼层所有块信息
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public List<ThIfcDistributionFlowElement> GetFloorBlockInfo(Polyline polygon)
        {
            var dbObjs = GlobalBlockInfoSpatialIndex.SelectCrossingPolygon(polygon);
            var value = GlobalBlockInfo.Where(o => dbObjs.Contains(GlobleCenterPointDic[o.Outline])).ToList();
            SetFloorBlockInfo(value);
            return value;
        }

        /// <summary>
        /// 设置楼层块空间索引
        /// </summary>
        /// <param name="elements"></param>
        private void SetFloorBlockInfo(List<ThIfcDistributionFlowElement> elements)
        {
            FloorBlockInfo = elements;
            var dbObjs = elements.Select(o => GlobleCenterPointDic[o.Outline]).ToCollection();
            FloorBlockInfoSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
        }

        /// <summary>
        /// 初始化楼层
        /// </summary>
        /// <param name="adb"></param>
        /// <param name="storeys"></param>
        /// <param name="fireCompartments"></param>
        /// <returns></returns>
        public override List<ThFloorModel> InitStoreys(AcadDatabase adb, List<ThIfcSpatialElement> storeys, List<ThFireCompartment> fireCompartments)
        {
            List<ThFloorModel> Floors = new List<ThFloorModel>();
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(fireCompartments.Select(e => e.Boundary).ToCollection());
            //初始化楼层
            foreach (var s in storeys)
            {
                if (s is ThEStoreys sobj)
                {
                    var blk = adb.Element<BlockReference>(sobj.ObjectId);
                    switch (sobj.StoreyType)
                    {
                        case EStoreyType.LargeRoof:
                            {
                                //大屋面 如果没有小屋面 大屋面就是顶楼
                                ThFloorModel NewFloor = new ThFloorModel
                                {
                                    FloorName = "JF",
                                    FloorNumber = int.MaxValue - 1
                                };
                                NewFloor.InitFloors(adb.Database, blk, fireCompartments, spatialIndex);
                                Floors.Add(NewFloor);
                                break;
                            }
                        case EStoreyType.SmallRoof:
                            {
                                //小屋面，一般意味着顶楼
                                ThFloorModel NewFloor = new ThFloorModel
                                {
                                    FloorName = "RF",
                                    FloorNumber = int.MaxValue
                                };
                                NewFloor.InitFloors(adb.Database, blk, fireCompartments, spatialIndex);
                                Floors.Add(NewFloor);
                                break;
                            }
                        case EStoreyType.StandardStorey:
                        case EStoreyType.NonStandardStorey:
                        case EStoreyType.RefugeStorey:
                        case EStoreyType.PodiumRoof:
                        case EStoreyType.EvenStorey:
                        case EStoreyType.OddStorey:
                            {
                                if (sobj.Storeys.Count == 1)
                                {
                                    ThFloorModel NewFloor = new ThFloorModel
                                    {
                                        FloorName = sobj.Storeys[0],
                                        FloorNumber = sobj.Storeys[0].GetFloorNumber()
                                    };
                                    NewFloor.InitFloors(adb.Database, blk, fireCompartments, spatialIndex);
                                    Floors.Add(NewFloor);
                                }
                                if (sobj.Storeys.Count > 1)
                                {
                                    ThFloorModel NewFloor = new ThFloorModel
                                    {
                                        FloorName = sobj.Storeys[0],
                                        FloorNumber = sobj.Storeys[0].GetFloorNumber(),
                                        IsMultiFloor = true,
                                        MulitFloors = sobj.Storeys,
                                        MulitFloorName = sobj.StoreyTypeString,
                                        MulitStoreyNumber = sobj.StoreyNumber
                                    };
                                    NewFloor.InitFloors(adb.Database, blk, fireCompartments, spatialIndex);
                                    Floors.Add(NewFloor);
                                }
                            }
                            break;
                        case EStoreyType.Unknown:
                        default:
                            break;
                    }
                }
            }
            //统计楼层内防火分区计数
            Floors.ForEach(floor =>
            {
                FireCompartmentNameList.AddRange(floor.FireDistricts.Where(o => !o.DrawFireDistrictNameText).Select(o => o.FireDistrictName));
                var FloorBlockInfo = GetFloorBlockInfo(floor.FloorBoundary);
                floor.FireDistricts.ForEach(fireDistrict =>
                {
                    fireDistrict.Data = new DataSummary()
                    {
                        BlockData = FillingBlockNameConfigModel(fireDistrict.FireDistrictBoundary, floor.FloorName == "JF")
                    };
                    fireDistrict.DrawFireDistrict = fireDistrict.Data.BlockData.BlockStatistics.Values.Count(v => v > 0) > 1;
                });
                int Max_FireDistrictNo = 1;
                var The_MaxNo_FireDistrict = floor.FireDistricts.OrderByDescending(f => f.FireDistrictNo).FirstOrDefault();
                Max_FireDistrictNo = The_MaxNo_FireDistrict.FireDistrictNo;
                string FloorName = Max_FireDistrictNo > 1 ? The_MaxNo_FireDistrict.FireDistrictName.Split('-')[0] : floor.FloorName;
                floor.FireDistricts.Where(f => f.DrawFireDistrict && f.DrawFireDistrictNameText).ToList().ForEach(o =>
                {
                    Max_FireDistrictNo++;
                    while (FireCompartmentNameList.Contains(FloorName + "-" + Max_FireDistrictNo))
                    {
                        Max_FireDistrictNo++;
                    }
                    o.FireDistrictNo = Max_FireDistrictNo;
                    o.FireDistrictName = FloorName + "-" + Max_FireDistrictNo;
                    FireCompartmentNameList.Add(o.FireDistrictName);
                });
            });
            return Floors;
        }

        /// <summary>
        /// 虚拟初始化一栋楼
        /// </summary>
        /// <param name="storeys"></param>
        public override List<ThFloorModel> InitVirtualStoreys(Database db, Polyline storyBoundary, List<ThFireCompartment> fireCompartments)
        {
            List<ThFloorModel> AddFloorss = new List<ThFloorModel>();
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(fireCompartments.Select(e => e.Boundary).ToCollection());
            ThFloorModel NewFloor = new ThFloorModel
            {
                FloorName = "*",
                FloorNumber = 0
            };
            NewFloor.InitFloors(storyBoundary, fireCompartments, spatialIndex);
            AddFloorss.Add(NewFloor);

            AddFloorss.ForEach(floor =>
            {
                var FloorBlockInfo = GetFloorBlockInfo(floor.FloorBoundary);
                floor.FireDistricts.ForEach(fireDistrict =>
                {
                    fireDistrict.FireDistrictNo = 0;//该楼层属于虚拟楼层，需手动调整防火分区归属权
                    fireDistrict.Data = new DataSummary()
                    {
                        BlockData = FillingBlockNameConfigModel(fireDistrict.FireDistrictBoundary)
                    };
                });
                int Max_FireDistrictNo = 0;
                var choise = floor.FireDistricts.Where(f => f.FireDistrictNo == -2);
                if (choise.Count() > 0)
                {
                    var The_MaxNo_FireDistrict = choise.Max(o => int.Parse(o.FireDistrictName.Split('-')[1]));
                    Max_FireDistrictNo = The_MaxNo_FireDistrict;
                }
                string FloorName = "*";
                floor.FireDistricts.Where(f => f.DrawFireDistrict && f.DrawFireDistrictNameText).ToList().ForEach(o =>
                {
                    o.FireDistrictNo = ++Max_FireDistrictNo;
                    o.FireDistrictName = FloorName + "-" + Max_FireDistrictNo;
                });
            });
            return AddFloorss;
        }

        /// <summary>
        /// 画防火分区和回路编号
        /// </summary>
        /// <param name="db"></param>
        /// <param name="addFloorss"></param>
        public override void DrawFloorsNum(Database db, List<ThFloorModel> addFloorss)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(db))
            {
                HostApplicationServices.WorkingDatabase = db;
                double Rotation = 0.00;
                var blkrefs = acadDatabase.ModelSpace
               .OfType<BlockReference>()
               .FirstOrDefault(b => !b.BlockTableRecord.IsNull && b.GetEffectiveName() == "AI-楼层框定E");
                if (!blkrefs.IsNull())
                {
                    Rotation = blkrefs.Rotation;
                }
                InsertBlockService.ImportFireDistrictLayerAndStyle(db);
                List<DBText> DrawEntitys = new List<DBText>();
                addFloorss.ForEach(f =>
                {
                    f.FireDistricts.ForEach(fireDistrict =>
                    {
                        //画防火分区名字
                        if (fireDistrict.DrawFireDistrictNameText && fireDistrict.DrawFireDistrict)
                        {
                            var newDBText = new DBText() { Height = 2000, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = fireDistrict.FireDistrictName, Position = fireDistrict.TextPoint, AlignmentPoint = fireDistrict.TextPoint, Layer = ThAutoFireAlarmSystemCommon.FireDistrictByLayer};
                            newDBText.Rotation = Rotation;
                            DrawEntitys.Add(newDBText);
                        }
                    });
                });
                DrawEntitys.ForEach(e => acadDatabase.ModelSpace.Add(e));
                var textStyle = acadDatabase.TextStyles.Element("TH-STYLE1");
                DrawEntitys.ForEach(e => e.TextStyleId = textStyle.Id);
            }
        }

        /// <summary>
        /// 准备数据
        /// </summary>
        protected override void PrepareData()
        {
            DataProcessing();
            var AllData = GetDrawModelInfo();
            this.DrawData = AllData;
        }


        /// <summary>
        /// 填充防火分区数据
        /// </summary>
        /// <param name="fireDistrictBoundary"></param>
        /// <returns></returns>
        public ThBlockNumStatistics FillingBlockNameConfigModel(Entity polygon, bool IsJF = false)
        {
            ThBlockNumStatistics BlockDataReturn = new ThBlockNumStatistics();
            if (polygon is Polyline || polygon is MPolygon)
            {
                var dbObjs = FloorBlockInfoSpatialIndex.SelectCrossingPolygon(polygon);
                var Data = FloorBlockInfo.Where(o => dbObjs.Contains(GlobleCenterPointDic[o.Outline])).ToList();

                ThBlockConfigModel.BlockConfig.ForEach(o =>
                {
                switch (o.StatisticMode)
                {
                    case StatisticType.BlockName:
                        {
                                BlockDataReturn.BlockStatistics[o.UniqueName] = Data.Where(x => (x.Outline as BlockReference).Name == o.BlockName).Sum(x => ThQuantityMarkExtension.GetQuantity(_database.GetBlockReferenceOBB(x.Outline as BlockReference)));
                                if (o.HasAlias)
                                {
                                    BlockDataReturn.BlockStatistics[o.UniqueName] += Data.Where(x => o.AliasList.Contains((x.Outline as BlockReference).Name)).Sum(x => ThQuantityMarkExtension.GetQuantity(_database.GetBlockReferenceOBB(x.Outline as BlockReference)));
                                }
                                break;
                            }
                        case StatisticType.Attributes:
                            {
                                BlockDataReturn.BlockStatistics[o.UniqueName] = Data.Where(x =>
                                {
                                    var atts = GlobleBlockAttInfoDic.First(b => b.Key.Equals(x.Outline)).Value;
                                    return !atts.Any(y => o.StatisticAttNameValues.ContainsKey(y.Key) && o.StatisticAttNameValues[y.Key].Any(att => att[0] == '~' && att.Substring(1) == y.Value)) && 
                                    atts.Any(y => o.StatisticAttNameValues.ContainsKey(y.Key) && o.StatisticAttNameValues[y.Key].Contains(y.Value));
                                }).Sum(x => ThQuantityMarkExtension.GetQuantity(_database.GetBlockReferenceOBB(x.Outline as BlockReference)));
                                if (o.HasAlias)
                                {
                                    BlockDataReturn.BlockStatistics[o.UniqueName] += Data.Where(x => o.AliasList.Contains((x.Outline as BlockReference).Name)).Sum(x => ThQuantityMarkExtension.GetQuantity(_database.GetBlockReferenceOBB(x.Outline as BlockReference)));
                                }
                                break;
                            }
                        case StatisticType.RelyOthers:
                            {
                                break;
                            }
                        case StatisticType.NoStatisticsRequired:
                            {
                                break;
                            }
                        case StatisticType.Room:
                            {
                                break;
                            }
                        case StatisticType.NeedSpecialTreatment:
                            {
                                //如果是大屋面
                                if (IsJF && o.UniqueName == "消防水箱")
                                {
                                    BlockDataReturn.BlockStatistics[o.UniqueName] = Data.Where(x => (x.Outline as BlockReference).Name == o.BlockName).Sum(x => ThQuantityMarkExtension.GetQuantity(_database.GetBlockReferenceOBB(x.Outline as BlockReference)));
                                }
                                //其他正常楼层
                                else if (!IsJF && o.UniqueName == "消防水池")
                                {
                                    BlockDataReturn.BlockStatistics[o.UniqueName] = Data.Where(x => (x.Outline as BlockReference).Name == o.BlockName).Sum(x => ThQuantityMarkExtension.GetQuantity(_database.GetBlockReferenceOBB(x.Outline as BlockReference)));
                                }
                                break;
                            }
                        default:
                            break;
                    }
                });
                #region 最后统计需依赖其他模块的计数规则
                //#5
                ThBlockConfigModel.BlockConfig.Where(b => b.StatisticMode == StatisticType.RelyOthers).ForEach(o =>
                {
                    int FindCount = 0;
                    o.RelyBlockUniqueNames.ForEach(name =>
                    {
                        FindCount += BlockDataReturn.BlockStatistics[name] * ThBlockConfigModel.BlockConfig.First(x => x.UniqueName == name).CoefficientOfExpansion;//计数*权重
                    });
                    BlockDataReturn.BlockStatistics[o.UniqueName] = (int)Math.Ceiling((double)FindCount / o.DependentStatisticalRule);//向上缺省
                });
                BlockDataReturn.BlockStatistics["低压压力开关"] = 0;
                //与读取到的[消防水箱],[灭火系统压力开关]同一分区，默认数量为2
                if (BlockDataReturn.BlockStatistics["消防水池"] > 0 && BlockDataReturn.BlockStatistics["灭火系统压力开关"] > 0)
                {
                    BlockDataReturn.BlockStatistics["消火栓泵"] = Math.Max(BlockDataReturn.BlockStatistics["消火栓泵"], 2);
                    BlockDataReturn.BlockStatistics["喷淋泵"] = Math.Max(BlockDataReturn.BlockStatistics["喷淋泵"], 2);
                }
                //有[消火栓泵],[喷淋泵]，默认一定至少有一个[灭火系统压力开关]
                if (BlockDataReturn.BlockStatistics["消火栓泵"] > 0 || BlockDataReturn.BlockStatistics["喷淋泵"] > 0)
                {
                    BlockDataReturn.BlockStatistics["灭火系统压力开关"] = Math.Max(BlockDataReturn.BlockStatistics["灭火系统压力开关"], 1);
                    //新增需求：新增加了一个[低压压力开关]块，他的数量不通过统计来计算，数量与[消火栓泵]数量保持一致，且与[灭火系统流量开关]互斥，不会同时存在，假如同时存在，按[灭火系统流量开关]为0处理。
                    if (BlockDataReturn.BlockStatistics["消火栓泵"] > 0)
                    {
                        BlockDataReturn.BlockStatistics["灭火系统流量开关"] = 0;
                        BlockDataReturn.BlockStatistics["低压压力开关"] = BlockDataReturn.BlockStatistics["消火栓泵"];
                    }
                }
                #endregion
            }
            else
            {
                // throw new exception
            }
            return BlockDataReturn;
        }

        /// <summary>
        /// 数据处理，按业务需求处理数据
        /// </summary>
        /// <param name="allData"></param>
        /// <returns></returns>
        private void DataProcessing()
        {
            //全部防火分区
            var AllFireZones = this.floors.SelectMany(o => o.FireDistricts).ToList();
            //合并同名称楼层
            var NormalFireDistricts = new List<ThFireDistrictModel>();
            AllFireZones.ForEach(o =>
            {
                int index = NormalFireDistricts.FindIndex(x => x.FireDistrictName == o.FireDistrictName);
                if (index >= 0)
                {
                    NormalFireDistricts[index].Data += o.Data;
                }
                else
                {
                    NormalFireDistricts.Add(o);
                }
            });
            //计算楼层合并关系
            for (int i = 0; i < this.floors.Count; i++)
            {
                ThFloorModel floor = this.floors[i];
                List<ThFireDistrictModel> fireCompartments = new List<ThFireDistrictModel>();
                floor.FireDistricts.ForEach(o =>
                {
                    var fireDistrict = NormalFireDistricts.FirstOrDefault(f => f.FireDistrictName == o.FireDistrictName);
                    if (fireDistrict.IsNull())
                    {
                        //已被别的楼层合并完成，本楼层直接忽略
                        fireCompartments.Add(o);
                    }
                    else if(o.FireDistrictNo != -1)
                    {
                        //正常防火分区，合并所有重名防火分区数据
                        o.Data = fireDistrict.Data;
                        NormalFireDistricts.Remove(fireDistrict);
                    }
                    else
                    {
                        //不属于本楼层的防火分区，不执行合并操作，忽略掉
                        fireCompartments.Add(o);
                    }
                });
                floor.FireDistricts.RemoveAll(o => fireCompartments.Contains(o));
            }
            this.floors.ForEach(x => x.FireDistricts.ForEach(o =>
            {
                if (o.Data.BlockData.BlockStatistics["楼层或回路重复显示屏"] > 0)
                    o.Data.BlockData.BlockStatistics["区域显示器/火灾显示盘"] = 0;
            }));
            if (FireCompartmentParameter.DiagramDisplayEffect == 1)
            {
                //分解复数楼层
                this.floors.Where(o => o.IsMultiFloor).ToList().ForEach(floor =>
                {
                    floor.MulitFloors.ForEach(o =>
                    {
                        var newfloor = new ThFloorModel();
                        newfloor.IsMultiFloor = false;
                        newfloor.FloorName = o;
                        newfloor.FloorNumber = o.GetFloorNumber();
                        floor.FireDistricts.ForEach(x =>
                        {
                            var names = x.FireDistrictName.Split('-');
                            names[0] = newfloor.FloorName;
                            newfloor.FireDistricts.Add(new ThFireDistrictModel()
                            {
                                FireDistrictName = string.Join("-", names),
                                DrawFireDistrict = x.DrawFireDistrict,
                                DrawFireDistrictNameText = newfloor.FloorName == floor.MulitFloors[0] ? x.DrawFireDistrictNameText : false,
                                TextPoint = x.TextPoint,
                                Data = x.Data,
                                FireDistrictNo = x.FireDistrictNo
                            });
                        });
                        this.floors.Add(newfloor);
                    });
                    this.floors = this.floors.Where(o => !o.IsMultiFloor).ToList();
                });
            }
            return;
        }

        /// <summary>
        /// 获取DrawModelList
        /// </summary>
        /// <returns></returns>
        private List<ThDrawModel> GetDrawModelInfo()
        {
            return this.floors.OrderBy(o => o.FloorNumber).SelectMany(o =>
            {
                List<ThDrawModel> drawModels = new List<ThDrawModel>();
                o.FireDistricts.Where(x => x.DrawFireDistrict).OrderBy(x => x.FireDistrictNo).ForEach(x =>
                {
                    drawModels.Add(new ThDrawModel()
                    {
                        FireDistrictName = o.IsMultiFloor ? $"{o.MulitFloorName}:{o.MulitStoreyNumber}F-{x.FireDistrictNo}" : x.FireDistrictName,
                        Data = x.Data,
                        FloorCount = o.IsMultiFloor ? o.MulitFloors.Count : 1,
                    });
                });
                return drawModels;
            }).ToList();
        }
    }
}
