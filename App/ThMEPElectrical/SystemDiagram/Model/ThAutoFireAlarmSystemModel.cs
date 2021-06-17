using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Model.WireCircuit;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// THAFAS V1.0
    /// 自动火灾报警系统Model
    /// 一个系统图里首先分楼层
    /// 其次每个楼层分0/1/N个防火分区
    /// </summary>
    public class ThAutoFireAlarmSystemModel
    {
        public List<ThFloorModel> floors { get; set; }
        public ThAutoFireAlarmSystemModel()
        {
            floors = new List<ThFloorModel>();
        }
        private Dictionary<Entity, List<KeyValuePair<string, string>>> GlobleBlockAttInfo;
        private List<ThIfcDistributionFlowElement> GlobalBlockInfo;
        private ThCADCoreNTSSpatialIndex GlobalBlockInfoSpatialIndex;
        private List<ThIfcDistributionFlowElement> FloorBlockInfo;
        private ThCADCoreNTSSpatialIndex FloorBlockInfoSpatialIndex;

        /// <summary>
        /// 设置全局块空间索引
        /// </summary>
        /// <param name="elements"></param>
        public void SetGlobalBlockInfo(Dictionary<Entity, List<KeyValuePair<string, string>>> elements)
        {
            GlobleBlockAttInfo = elements;
            GlobalBlockInfo = elements.Select(o => new ThIfcDistributionFlowElement() { Outline = o.Key }).ToList();
            var dbObjs = elements.Select(o => o.Key).ToCollection();
            GlobalBlockInfoSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
        }

        /// <summary>
        /// 设置楼层块空间索引
        /// </summary>
        /// <param name="elements"></param>
        private void SetFloorBlockInfo(List<ThIfcDistributionFlowElement> elements)
        {
            FloorBlockInfo = elements;
            var dbObjs = elements.Select(o => o.Outline).ToCollection();
            FloorBlockInfoSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
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
        /// 获取所有的楼层信息
        /// </summary>
        /// <returns></returns>
        public List<ThFloorModel> GetFloorInfo()
        {
            return this.floors;//.OrderBy(x => { x.FireDistricts.OrderBy(y => y.FireDistrictName); return x.FloorNumber; }).SelectMany(o => o.FireDistricts).ToList();
        }

        /// <summary>
        /// 初始化整栋楼
        /// </summary>
        /// <param name="storeys"></param>
        public List<ThFloorModel> InitStoreys(AcadDatabase adb, List<ThIfcSpatialElement> storeys, List<ThFireCompartment> fireCompartments)
        {
            List<ThFloorModel> Floors = new List<ThFloorModel>();
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(fireCompartments.Select(e => e.Boundary).ToCollection());
            foreach (var s in storeys)
            {
                if (s is ThStoreys sobj)
                {
                    var blk = adb.Element<BlockReference>(sobj.ObjectId);
                    //var pts = blk.GeometricExtents.ToRectangle().Vertices();

                    switch (sobj.StoreyType)
                    {
                        case StoreyType.LargeRoof:
                            {
                                //大屋面 如果没有小屋面 大屋面就是顶楼
                                ThFloorModel NewFloor = new ThFloorModel
                                {
                                    FloorName = "JF",
                                    FloorNumber = int.MaxValue - 1
                                };
                                NewFloor.InitFloors(adb, blk, fireCompartments, spatialIndex);
                                Floors.Add(NewFloor);
                                break;
                            }
                        case StoreyType.SmallRoof:
                            {
                                //小屋面，一般意味着顶楼
                                ThFloorModel NewFloor = new ThFloorModel
                                {
                                    FloorName = "RF",
                                    FloorNumber = int.MaxValue
                                };
                                NewFloor.InitFloors(adb, blk, fireCompartments, spatialIndex);
                                Floors.Add(NewFloor);
                                break;
                            }
                        case StoreyType.StandardStorey:
                        case StoreyType.NonStandardStorey:
                            //标准层或者非标层
                            {
                                if (sobj.Storeys.Count == 1)
                                {
                                    ThFloorModel NewFloor = new ThFloorModel
                                    {
                                        FloorName = sobj.StoreyNumber.Contains("B") ? sobj.StoreyNumber : sobj.Storeys[0] + "F",
                                        FloorNumber = sobj.StoreyNumber.Contains("B") ? -sobj.Storeys[0] : sobj.Storeys[0]
                                    };
                                    NewFloor.InitFloors(adb, blk, fireCompartments, spatialIndex);
                                    Floors.Add(NewFloor);
                                }
                                if (sobj.Storeys.Count > 1)
                                {
                                    for (int i = sobj.Storeys[0]; i <= sobj.Storeys[1]; i++)
                                    {
                                        ThFloorModel NewFloor = new ThFloorModel
                                        {
                                            FloorName = i + "F",
                                            FloorNumber = i
                                        };
                                        NewFloor.InitFloors(adb, blk, fireCompartments, spatialIndex);
                                        Floors.Add(NewFloor);
                                    }
                                }
                            }
                            break;
                        case StoreyType.Unknown:
                        default:
                            break;
                    }
                }
            }
            return Floors;
        }

        /// <summary>
        /// 画系统图
        /// </summary>
        public void DrawSystemDiagram(Vector3d Offset, Matrix3d ConversionMatrix)
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
                    InsertBlockService.InsertLineType(o.CircuitLayer, o.CircuitLayerLinetype);
                });
                ThWireCircuitConfig.VerticalWireCircuits.ForEach(o =>
                {
                    o.InitCircuitConnection();
                    InsertBlockService.InsertLineType(o.CircuitLayer, o.CircuitLayerLinetype);
                });
                //初始化黄色外方块的图层信息和文字图层信息
                InsertBlockService.InsertOuterBorderBlockLayer();
                //开启联动关闭排烟风机信号线绘画权限
                ThAutoFireAlarmSystemCommon.CanDrawFixedPartSmokeExhaust = true;

                List<Entity> DrawEntitys = new List<Entity>();
                Dictionary<Point3d, ThBlockModel> dicBlockPoints = new Dictionary<Point3d, ThBlockModel>();
                int RowIndex = 1;//方格层数
                var AllData = GetFireDistrictsInfo();
                AllData = DelDuplicateFireDistricts(AllData);
                foreach (var fireDistrict in AllData)
                {
                    //初始化横线
                    ThWireCircuitConfig.HorizontalWireCircuits.ForEach(o =>
                    {
                        o.SetFloorIndex(RowIndex, fireDistrict);
                        DrawEntitys.AddRange(o.Draw());
                    });

                    //Draw Block
                    ThBlockConfigModel.BlockConfig.ForEach(o =>
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
                InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.FixedPartContainsFireRoom : ThAutoFireAlarmSystemCommon.FixedPartExcludingFireRoom);
            }
        }

        /// <summary>
        /// 画防火分区编号
        /// </summary>
        /// <param name="db"></param>
        /// <param name="addFloorss"></param>
        public void DrawFireCompartmentNum(Database db, List<ThFloorModel> addFloorss)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(db))
            {
                HostApplicationServices.WorkingDatabase = db;
                InsertBlockService.InsertFireDistrictByLayer(acadDatabase);
                var textStyle = acadDatabase.TextStyles.Element("TH-STYLE1");
                List<Entity> DrawEntitys = new List<Entity>();
                addFloorss.ForEach(f =>
                {
                    f.FireDistricts.ForEach(fireDistrict =>
                    {
                        //画防火分区名字
                        if (fireDistrict.DrawFireDistrictNameText && fireDistrict.DrawFireDistrict)
                        {
                            DrawEntitys.Add(new DBText() { Height = 2000, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = fireDistrict.FireDistrictName, Position = fireDistrict.TextPoint, AlignmentPoint = fireDistrict.TextPoint, ColorIndex = 2, Layer = ThAutoFireAlarmSystemCommon.FireDistrictByLayer, TextStyleId = textStyle.Id });
                        }
                    });
                });
                foreach (Entity item in DrawEntitys)
                {
                    acadDatabase.ModelSpace.Add(item);
                }
            }
        }

        /// <summary>
        /// 合并不属于本楼层的防火分区
        /// </summary>
        /// <param name="allData"></param>
        /// <returns></returns>
        private List<ThFireDistrictModel> DelDuplicateFireDistricts(List<ThFireDistrictModel> allData)
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
            return NormalFireDistricts;
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

        /// <summary>
        /// 获取楼层所有块信息
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public List<ThIfcDistributionFlowElement> GetFloorBlockInfo(Polyline polygon)
        {
            var dbObjs = GlobalBlockInfoSpatialIndex.SelectCrossingPolygon(polygon);
            var value = GlobalBlockInfo.Where(o => dbObjs.Contains(o.Outline)).ToList();
            SetFloorBlockInfo(value);
            return value;
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
                var Data = FloorBlockInfo.Where(o => dbObjs.Contains(o.Outline)).ToList();

                //这个地方感觉可以优化速率，有时间在好好搞一下
                ThBlockConfigModel.BlockConfig.ForEach(o =>
                {
                    switch (o.StatisticMode)
                    {
                        case StatisticType.BlockName:
                            {
                                BlockDataReturn.BlockStatistics[o.UniqueName] = Data.Count(x => (x.Outline as BlockReference).Name == o.BlockName);
                                break;
                            }
                        case StatisticType.Attributes:
                            {
                                BlockDataReturn.BlockStatistics[o.UniqueName] = Data.Count(x => (GlobleBlockAttInfo.First(b => b.Key.Equals(x.Outline))).Value.Count(y => o.StatisticAttNameValues.ContainsKey(y.Key) && o.StatisticAttNameValues[y.Key].Contains(y.Value)) > 0);
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
                    BlockDataReturn.BlockStatistics[o.UniqueName] = FindCount / o.DependentStatisticalRule + 1;//向上缺省
                });
                //与读取到的[消防水箱],[灭火系统压力开关]同一分区，默认数量为2
                if (BlockDataReturn.BlockStatistics["消防水池"] > 0 && BlockDataReturn.BlockStatistics["灭火系统压力开关"] > 0)
                {
                    BlockDataReturn.BlockStatistics["消火栓泵"] = Math.Max(BlockDataReturn.BlockStatistics["消火栓泵"], 2);
                    BlockDataReturn.BlockStatistics["喷淋泵"] = Math.Max(BlockDataReturn.BlockStatistics["喷淋泵"], 2);
                }
                #endregion
            }
            else
            {
                // throw new exception
            }
            return BlockDataReturn;
        }      
    }
}
