using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPElectrical.SystemDiagram.Extension;
using ThMEPElectrical.SystemDiagram.Model.WireCircuit;

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

        public ThAutoFireAlarmSystemModelFromFireCompartment()
        {
            floors = new List<ThFloorModel>();
        }

        /// <summary>
        /// 设置全局空间索引
        /// </summary>
        public override void SetGlobalData(Database database, Dictionary<Entity, List<KeyValuePair<string, string>>> elements, List<Entity> Entitydata)
        {
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
                                        FloorName = sobj.StoreyNumber.Contains("B") ? sobj.StoreyNumber : sobj.Storeys[0] + "F",
                                        FloorNumber = sobj.StoreyNumber.Contains("B") ? -sobj.Storeys[0] : sobj.Storeys[0]
                                    };
                                    NewFloor.InitFloors(adb.Database, blk, fireCompartments, spatialIndex);
                                    Floors.Add(NewFloor);
                                }
                                if (sobj.Storeys.Count > 1)
                                {
                                    ThFloorModel NewFloor = new ThFloorModel
                                    {
                                        FloorName = sobj.Storeys[0] + "F",
                                        FloorNumber = sobj.Storeys[0],
                                        IsMultiFloor = true,
                                        MulitFloorName = sobj.Storeys
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
                    o.FireDistrictNo = ++Max_FireDistrictNo;
                    o.FireDistrictName = FloorName + "-" + Max_FireDistrictNo;
                });
            });
            //分解复数楼层
            Floors.Where(o => o.IsMultiFloor).ToList().ForEach(floor =>
            {
                floor.MulitFloorName.ForEach(o =>
                {
                    var newfloor = new ThFloorModel();
                    newfloor.IsMultiFloor = false;
                    newfloor.FloorName = o + "F";
                    newfloor.FloorNumber = o;
                    floor.FireDistricts.ForEach(x =>
                    {
                        var names = x.FireDistrictName.Split('-');
                        names[0] = newfloor.FloorName;
                        newfloor.FireDistricts.Add(new ThFireDistrictModel()
                        {
                            FireDistrictName = string.Join("-", names),
                            DrawFireDistrict = x.DrawFireDistrict,
                            DrawFireDistrictNameText = newfloor.FloorNumber == floor.MulitFloorName[0] ? x.DrawFireDistrictNameText : false,
                            TextPoint = x.TextPoint,
                            Data = x.Data,
                            FireDistrictNo = x.FireDistrictNo
                        });
                    });
                    Floors.Add(newfloor);
                });
            });
            return Floors.Where(o => !o.IsMultiFloor).ToList();
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
                var textStyle = acadDatabase.TextStyles.Element("TH-STYLE1");
                List<Entity> DrawEntitys = new List<Entity>();
                addFloorss.ForEach(f =>
                {
                    f.FireDistricts.ForEach(fireDistrict =>
                    {
                        //画防火分区名字
                        if (fireDistrict.DrawFireDistrictNameText && fireDistrict.DrawFireDistrict)
                        {
                            var newDBText = new DBText() { Height = 2000, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = fireDistrict.FireDistrictName, Position = fireDistrict.TextPoint, AlignmentPoint = fireDistrict.TextPoint, Layer = ThAutoFireAlarmSystemCommon.FireDistrictByLayer, TextStyleId = textStyle.Id };
                            newDBText.Rotation = Rotation;
                            DrawEntitys.Add(newDBText);
                        }
                    });
                });
                //HostApplicationServices.WorkingDatabase = db;

                foreach (Entity item in DrawEntitys)
                {
                    acadDatabase.ModelSpace.Add(item);
                }
            }
        }

        /// <summary>
        /// 画系统图
        /// </summary>
        public override void DrawSystemDiagram(Vector3d Offset, Matrix3d ConversionMatrix)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                HostApplicationServices.WorkingDatabase = acadDatabase.Database;

                //设置全局偏移量
                InsertBlockService.SetOffset(Offset, ConversionMatrix);
                //初始化所有需要画的线并导入图层/线型等信息
                ThWireCircuitConfig.HorizontalWireCircuits.ForEach(o =>
                {
                    o.InitCircuitConnection();
                    InsertBlockService.InsertCircuitLayerAndLineType(o.CircuitLayer, o.CircuitLayerLinetype);
                });
                ThWireCircuitConfig.VerticalWireCircuits.ForEach(o =>
                {
                    o.InitCircuitConnection();
                    InsertBlockService.InsertCircuitLayerAndLineType(o.CircuitLayer, o.CircuitLayerLinetype);
                });
                //初始化系统图需要的图层/线型等信息
                InsertBlockService.InsertDiagramLayerAndStyle();
                //开启相关信号线绘画权限
                ThAutoFireAlarmSystemCommon.CanDrawFixedPartSmokeExhaust = true;
                ThAutoFireAlarmSystemCommon.CanDrawFireHydrantPump = true;
                ThAutoFireAlarmSystemCommon.CanDrawSprinklerPump = true;

                List<Entity> DrawEntitys = new List<Entity>();
                Dictionary<Point3d, ThBlockModel> dicBlockPoints = new Dictionary<Point3d, ThBlockModel>();
                int RowIndex = 1;//方格层数
                var AllFireDistrictsData = GetFireDistrictsInfo();
                AllFireDistrictsData = DataProcessing(AllFireDistrictsData);
                var AllData = DataConversion(AllFireDistrictsData);
                foreach (var fireDistrict in AllData)
                {
                    //初始化横线
                    ThWireCircuitConfig.HorizontalWireCircuits.ForEach(o =>
                    {
                        o.SetFloorIndex(RowIndex, fireDistrict);
                        DrawEntitys.AddRange(o.Draw());
                    });

                    //Draw Block
                    //业务更改，现在#10列，增加了多种报警器/探测器类型，需要特殊处理
                    ThBlockConfigModel.BlockConfig.Where(o => o.Index != 10).ForEach(o =>
                    {
                        if (!o.CanHidden || fireDistrict.Data.BlockData.BlockStatistics[o.UniqueName] > 0)
                        {

                            dicBlockPoints.Add(CalculateCoordinates(RowIndex, o), o);
                            if (o.HasMultipleBlocks)
                            {
                                o.AssociatedBlocks.ForEach(x =>
                                {
                                    dicBlockPoints.Add(CalculateCoordinates(RowIndex, x), x);
                                });
                            }
                        }
                    });
                    //处理#10 业务代码
                    {
                        //红外光束感烟火灾探测器发射器/接收器有任意一个，优先画，其余探测器隐藏不画
                        if (fireDistrict.Data.BlockData.BlockStatistics["红外光束感烟火灾探测器发射器"] > 0 || fireDistrict.Data.BlockData.BlockStatistics["红外光束感烟火灾探测器接收器"] > 0)
                        {
                            ThBlockConfigModel.BlockConfig.Where(o => (o.UniqueName == "红外光束感烟火灾探测器发射器") || (o.UniqueName == "红外光束感烟火灾探测器接收器")).ForEach(o =>
                              {
                                  if (!o.CanHidden || fireDistrict.Data.BlockData.BlockStatistics[o.UniqueName] > 0)
                                  {

                                      dicBlockPoints.Add(CalculateCoordinates(RowIndex, o), o);
                                      if (o.HasMultipleBlocks)
                                      {
                                          o.AssociatedBlocks.ForEach(x =>
                                          {
                                              dicBlockPoints.Add(CalculateCoordinates(RowIndex, x), x);
                                          });
                                      }
                                  }
                              });
                        }
                        //没有红外光束烟感探测器，画剩下的任意三个(至多三个)
                        else
                        {
                            var count = ThBlockConfigModel.BlockConfig.Where(o => o.Index == 10).Count(o => fireDistrict.Data.BlockData.BlockStatistics[o.UniqueName] > 0);
                            double spacing = 0;
                            //优先画[吸气式感烟火灾探测器],[家用感烟火灾探测报警器],[家用感温火灾探测报警器]因为这些块和其他的块长的都不一样
                            //没有其他探测器
                            if (count == 0)
                            {
                                //Do Not
                            }
                            //超过三个探测器
                            else if (count > 3)
                            {
                                spacing = 750.0;
                            }
                            //1-3个
                            else
                            {
                                spacing = 3000.0 / (count + 1);
                            }
                            double distance = spacing;
                            if (fireDistrict.Data.BlockData.BlockStatistics["吸气式感烟火灾探测器"] > 0)
                            {
                                ThBlockConfigModel.BlockConfig.Where(o => (o.UniqueName == "吸气式感烟火灾探测器")).ForEach(o =>
                                {
                                    if (!o.CanHidden || fireDistrict.Data.BlockData.BlockStatistics[o.UniqueName] > 0)
                                    {
                                        dicBlockPoints.Add(CalculateCoordinates(RowIndex, o).Add(new Vector3d(distance, 0, 0)), o);
                                        distance += spacing;
                                        if (o.HasMultipleBlocks)
                                        {
                                            o.AssociatedBlocks.ForEach(x =>
                                            {
                                                dicBlockPoints.Add(CalculateCoordinates(RowIndex, x), x);
                                            });
                                        }
                                    }
                                });
                            }
                            if (fireDistrict.Data.BlockData.BlockStatistics["家用感烟火灾探测报警器"] > 0 || fireDistrict.Data.BlockData.BlockStatistics["家用感温火灾探测报警器"] > 0)
                            {
                                ThBlockConfigModel.BlockConfig.Where(o => (o.UniqueName == "家用感烟火灾探测报警器") || (o.UniqueName == "家用感温火灾探测报警器")).ForEach(o =>
                                 {
                                     if (!o.CanHidden || fireDistrict.Data.BlockData.BlockStatistics[o.UniqueName] > 0)
                                     {
                                         dicBlockPoints.Add(CalculateCoordinates(RowIndex, o).Add(new Vector3d(distance, 0, 0)), o);
                                         distance += spacing;
                                         if (o.HasMultipleBlocks)
                                         {
                                             o.AssociatedBlocks.ForEach(x =>
                                             {
                                                 dicBlockPoints.Add(CalculateCoordinates(RowIndex, x), x);
                                             });
                                         }
                                     }
                                 });
                            }
                            ThBlockConfigModel.BlockConfig.Where(o => (o.Index == 10) && (o.UniqueName != "吸气式感烟火灾探测器") && (o.UniqueName != "家用感烟火灾探测报警器") && (o.UniqueName != "家用感温火灾探测报警器")).ForEach(o =>
                            {
                                if (distance < 3000)
                                {
                                    if (!o.CanHidden || fireDistrict.Data.BlockData.BlockStatistics[o.UniqueName] > 0)
                                    {
                                        dicBlockPoints.Add(CalculateCoordinates(RowIndex, o).Add(new Vector3d(distance, 0, 0)), o);
                                        distance += spacing;
                                        if (o.HasMultipleBlocks)
                                        {
                                            o.AssociatedBlocks.ForEach(x =>
                                            {
                                                dicBlockPoints.Add(CalculateCoordinates(RowIndex, x), x);
                                            });
                                        }
                                    }
                                }
                            });
                        }
                    }

                    //跳入下一层
                    RowIndex++;
                }
                {
                    //初始化竖线
                    ThWireCircuitConfig.VerticalWireCircuits.ForEach(o =>
                    {
                        o.SetFloorIndex(RowIndex, AllData);
                        DrawEntitys.AddRange(o.Draw());
                    });
                }

                foreach (Entity item in DrawEntitys)
                {
                    item.Move(Offset);
                    item.TransformBy(ConversionMatrix);
                    acadDatabase.ModelSpace.Add(item);
                }
                //画所有的外框线
                InsertBlockService.InsertOuterBorderBlock(RowIndex - 1, ThAutoFireAlarmSystemCommon.SystemColLeftNum + ThAutoFireAlarmSystemCommon.SystemColRightNum);
                //画所有的块
                InsertBlockService.InsertSpecifyBlock(dicBlockPoints);
                //画底部固定部分
                if (FireCompartmentParameter.FixedPartType != 3)
                {
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.FixedPartContainsFireRoom : ThAutoFireAlarmSystemCommon.FixedPartExcludingFireRoom);
                }
            }
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
                                BlockDataReturn.BlockStatistics[o.UniqueName] = Data.Count(x => (x.Outline as BlockReference).Name == o.BlockName);
                                if (o.HasAlias)
                                {
                                    BlockDataReturn.BlockStatistics[o.UniqueName] += Data.Count(x => o.AliasList.Contains((x.Outline as BlockReference).Name));
                                }
                                break;
                            }
                        case StatisticType.Attributes:
                            {
                                BlockDataReturn.BlockStatistics[o.UniqueName] = Data.Count(x => (GlobleBlockAttInfoDic.First(b => b.Key.Equals(x.Outline))).Value.Count(y => o.StatisticAttNameValues.ContainsKey(y.Key) && o.StatisticAttNameValues[y.Key].Contains(y.Value)) > 0);
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
                                    BlockDataReturn.BlockStatistics[o.UniqueName] = Data.Count(x => (x.Outline as BlockReference).Name == o.BlockName);
                                }
                                //其他正常楼层
                                else if (!IsJF && o.UniqueName == "消防水池")
                                {
                                    BlockDataReturn.BlockStatistics[o.UniqueName] = Data.Count(x => (x.Outline as BlockReference).Name == o.BlockName);
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
        /// 获取所有的防火分区信息
        /// </summary>
        /// <returns></returns>
        private List<ThFireDistrictModel> GetFireDistrictsInfo()
        {
            return this.floors.OrderBy(x => { x.FireDistricts = x.FireDistricts.OrderBy(y => y.FireDistrictNo).ToList(); return x.FloorNumber; }).SelectMany(o => o.FireDistricts).Where(f => f.DrawFireDistrict).ToList();
        }

        /// <summary>
        /// 数据处理，按业务需求处理数据
        /// </summary>
        /// <param name="allData"></param>
        /// <returns></returns>
        private List<ThFireDistrictModel> DataProcessing(List<ThFireDistrictModel> allData)
        {
            //正常的防火分区
            var NormalFireDistricts = allData.Where(f => f.FireDistrictNo != -1).ToList();
            allData.Where(f => f.FireDistrictNo == -1).ForEach(f =>
            {
                var FindData = NormalFireDistricts.FirstOrDefault(o => o.FireDistrictName == f.FireDistrictName);
                if (FindData.IsNull())
                {
                    NormalFireDistricts.Add(f);
                }
                else
                {
                    FindData.Data += f.Data;
                }
            });
            NormalFireDistricts.ForEach(o =>
            {
                if (o.Data.BlockData.BlockStatistics["楼层或回路重复显示屏"] > 0)
                    o.Data.BlockData.BlockStatistics["区域显示器/火灾显示盘"] = 0;
            });
            return NormalFireDistricts;
        }

        /// <summary>
        /// 数据转换，转成系统图能够识别的数据类型
        /// </summary>
        /// <param name="allData"></param>
        /// <returns></returns>
        private List<ThDrawModel> DataConversion(List<ThFireDistrictModel> allData)
        {
            return allData.Select(o => new ThDrawModel() { FireDistrictName = o.FireDistrictName, Data = o.Data }).ToList();
        }

        /// <summary>
        /// 计算块的实际坐标
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="BlockInfo"></param>
        /// <returns></returns>
        private Point3d CalculateCoordinates(int rowIndex, ThBlockModel BlockInfo)
        {
            return new Point3d(3000 * (BlockInfo.Index - 1) + BlockInfo.Position.X, 3000 * (rowIndex - 1) + BlockInfo.Position.Y, 0);
        }
    }
}
