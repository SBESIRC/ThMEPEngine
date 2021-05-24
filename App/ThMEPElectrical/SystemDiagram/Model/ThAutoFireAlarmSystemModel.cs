using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Model.WireCircuit;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
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

        /// <summary>
        /// 获取所有的防火分区信息
        /// </summary>
        /// <returns></returns>
        private List<ThFireDistrictModel> GetFireDistrictsInfo()
        {
            return this.floors.OrderBy(x => { x.FireDistricts.OrderBy(y => y.FireDistrictName); return x.FloorNumber; }).SelectMany(o => o.FireDistricts).ToList();
            //List<ThFireDistrict> result = new List<ThFireDistrict>();
            //foreach (var item in this.floors.OrderBy(o => o.FloorNumber))
            //{
            //    result.AddRange(item.FireDistricts);
            //}
            //return result;
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
        public void InitStoreys(List<ThIfcSpatialElement> storeys, List<ThFireCompartment> fireCompartments)
        {
            using (var adb = AcadDatabase.Active())
            {
                var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(fireCompartments.Select(e=>e.Boundary).ToCollection());
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
                                        FloorName = "RF",
                                        FloorNumber = int.MaxValue - 1
                                    };
                                    NewFloor.InitFloors(adb, blk, spatialIndex);
                                    this.floors.Add(NewFloor);
                                    break;
                                }
                            case StoreyType.SmallRoof:
                                {
                                    //小屋面，一般意味着顶楼
                                    ThFloorModel NewFloor = new ThFloorModel
                                    {
                                        FloorName = "RF+1",
                                        FloorNumber = int.MaxValue - 1
                                    };
                                    NewFloor.InitFloors(adb, blk, spatialIndex);
                                    this.floors.Add(NewFloor);
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
                                        NewFloor.InitFloors(adb, blk, spatialIndex);
                                        this.floors.Add(NewFloor);
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
                                            NewFloor.InitFloors(adb, blk, spatialIndex);
                                            this.floors.Add(NewFloor);
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
            }
        }

        public void Draw()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
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
                //初始化黄色外方块的图层信息
                InsertBlockService.InsertOuterBorderBlockLayer();

                List<Entity> DrawEntitys = new List<Entity>();
                Dictionary<Point3d, ThBlockModel> dicBlockPoints = new Dictionary<Point3d, ThBlockModel>();
                int RowIndex = 1;//方格层数
                var AllData = GetFireDistrictsInfo();
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
                        if (!o.CanHidden || fireDistrict.Data.BlockData.BlockStatistics[o.BlockName] > 0)
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

                    //画防火分区名字
                    if (fireDistrict.DrawFireDistrictNameText)
                    {
                        DrawEntitys.Add(new DBText() { Height = 2000, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextStyleId = DbHelper.GetTextStyleId("TH-STYLE1"), TextString = fireDistrict.FireDistrictName, Position = fireDistrict.TextPoint, AlignmentPoint = fireDistrict.TextPoint, ColorIndex = 2, Layer = ThAutoFireAlarmSystemCommon.FireDistrictByLayer });
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
                //此方法有BUG，无法正常显示
                //acadDatabase.ModelSpace.Add(DrawEntitys);
                foreach (Entity item in DrawEntitys)
                {
                    acadDatabase.ModelSpace.Add(item);
                }
                //画所有的外框线
                InsertBlockService.InsertOuterBorderBlock(RowIndex - 1, ThAutoFireAlarmSystemCommon.SystemColLeftNum + ThAutoFireAlarmSystemCommon.SystemColRightNum);
                //画所有的块
                InsertBlockService.InsertSpecifyBlock(dicBlockPoints);
            }
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

        private List<Polyline> GetFireDividePartition(AcadDatabase acadDatabase)
        {
            return acadDatabase.ModelSpace.OfType<Polyline>().Where(o => o.Layer.ToUpper() == ThAutoFireAlarmSystemCommon.FireDistrictByLayer && o.Closed && o.Length > ThAutoFireAlarmSystemCommon.FireDistrictShortestLength).ToList();
        }

    }
}
