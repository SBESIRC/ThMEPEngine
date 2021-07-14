using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
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
using ThMEPElectrical.SystemDiagram.Extension;
using ThMEPElectrical.SystemDiagram.Model.WireCircuit;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// THAFAS V2.0
    /// 自动火灾报警系统Model（按回路区分）
    /// 一个系统图里首先分楼层
    /// 其次每个楼层分0/1/N个防火分区
    /// 再来每个防火分区分0/1/N个回路
    /// </summary>
    public class ThAutoFireAlarmSystemModelFromWireCircuit : ThAutoFireAlarmSystemModel
    {
        private Dictionary<Entity, List<KeyValuePair<string, string>>> GlobleBlockAttInfoDic;
        private Dictionary<Entity, List<KeyValuePair<string, string>>> FloorBlockAttInfoDic;
        private List<Entity> GlobleEntityData;
        private ThCADCoreNTSSpatialIndex GlobalBlockInfoSpatialIndex;
        private List<Entity> FloorEntityData;

        public ThAutoFireAlarmSystemModelFromWireCircuit()
        {
            floors = new List<ThFloorModel>();
        }
        
        /// <summary>
        /// 设置全局空间索引
        /// </summary>
        public override void SetGlobalData(Database database, Dictionary<Entity, List<KeyValuePair<string, string>>> elements, List<Entity> Entitydata)
        {
            GlobleBlockAttInfoDic = elements;
            GlobleEntityData = Entitydata;
            var dbObjs = Entitydata.ToCollection();
            GlobalBlockInfoSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
        }

        /// <summary>
        /// 定位楼层
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public void GetFloorBlockInfo(Polyline polygon)
        {
            var dbObjs = GlobalBlockInfoSpatialIndex.SelectCrossingPolygon(polygon);
            FloorEntityData = GlobleEntityData.Where(o => dbObjs.Contains(o)).ToList();
            FloorBlockAttInfoDic = GlobleBlockAttInfoDic.Where(o => dbObjs.Contains(o.Key)).ToDictionary(x => x.Key, y => y.Value);
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
                //定位楼层数据
                GetFloorBlockInfo(floor.FloorBoundary);
                //初始化寻路引擎
                ThAFASGraphEngine GraphEngine = new ThAFASGraphEngine(adb.Database, FloorEntityData, FloorBlockAttInfoDic, floor.FloorName == "JF");
                GraphEngine.InitGraph();
                //GraphEngine.DrawGraphs();
                var The_MaxNo_FireDistrict = floor.FireDistricts.OrderByDescending(f => f.FireDistrictNo).FirstOrDefault();
                int Max_FireDistrictNo = The_MaxNo_FireDistrict.FireDistrictNo;
                string FloorName = Max_FireDistrictNo > 1 ? The_MaxNo_FireDistrict.FireDistrictName.Split('-')[0] : floor.FloorName;
                floor.FireDistricts.Where(f => f.FireDistrictNo == 0).ToList().ForEach(o =>
                {
                    o.DrawFireDistrictNameText = true;
                    o.FireDistrictNo = ++Max_FireDistrictNo;
                    o.FireDistrictName = FloorName + "-" + Max_FireDistrictNo;
                });
                floor.FireDistricts.ForEach(fireDistrict =>
                {
                    FillingFireCompartmentData(ref fireDistrict, GraphEngine.GraphsDic);
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
                        string OldName = names[0];
                        names[0] = newfloor.FloorName;
                        var newFireDistrict = new ThFireDistrictModel()
                        {
                            FireDistrictName = string.Join("-", names),
                            DrawFireDistrict = x.DrawFireDistrict,
                            DrawFireDistrictNameText = newfloor.FloorNumber == floor.MulitFloorName[0] ? x.DrawFireDistrictNameText : false,
                            TextPoint = x.TextPoint,
                            Data = x.Data,
                            FireDistrictNo = x.FireDistrictNo,
                            WireCircuits = new List<ThAlarmControlWireCircuitModel>()
                        };
                        x.WireCircuits.ForEach(cw =>
                        {
                            newFireDistrict.WireCircuits.Add(new ThAlarmControlWireCircuitModel()
                            {
                                DrawWireCircuit = cw.DrawWireCircuit,
                                DrawWireCircuitText = newfloor.FloorNumber == floor.MulitFloorName[0] ? cw.DrawWireCircuitText : false,
                                TextPoint=cw.TextPoint,
                                WireCircuitName=cw.WireCircuitName.Replace(OldName, newfloor.FloorName),
                                WireCircuitNo=cw.WireCircuitNo,
                                Data=cw.Data,
                                Graph=cw.Graph,
                                BlockCount=cw.BlockCount,
                            });
                        });
                        newfloor.FireDistricts.Add(newFireDistrict);
                        Floors.Add(newfloor);
                    });
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
            throw new NotSupportedException();
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
                var WireCircuittextStyle = acadDatabase.TextStyles.Element("TH-STYLE3");
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
                        //画电回路编号
                        fireDistrict.WireCircuits.ForEach(cw =>
                        {
                            if (cw.DrawWireCircuit && cw.DrawWireCircuitText)
                            {
                                var newDBText = new DBText() { Height = 300, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = cw.WireCircuitName, Position = cw.TextPoint, AlignmentPoint = cw.TextPoint, Layer = ThAutoFireAlarmSystemCommon.WireCircuitByLayer, TextStyleId = WireCircuittextStyle.Id };
                                newDBText.Rotation = Rotation;
                                DrawEntitys.Add(newDBText);
                            }
                        });
                    });
                });
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
                List<ThDrawModel> AllData = new List<ThDrawModel>();
                AllData = DataProcessingAndConversion(AllFireDistrictsData, out List<string> warningMsg);
                warningMsg.ForEach(msg => Active.Editor.WriteLine($"\n{msg}"));
                    
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
        /// 获取所有的防火分区信息
        /// </summary>
        /// <returns></returns>
        private List<ThFireDistrictModel> GetFireDistrictsInfo()
        {
            return this.floors.OrderBy(x => { x.FireDistricts = x.FireDistricts.Where(f=>f.WireCircuits.Count>0).OrderBy(y => y.FireDistrictNo).ToList(); return x.FloorNumber; }).SelectMany(o => o.FireDistricts).Where(f => f.DrawFireDistrict).ToList();
        }

        /// <summary>
        /// 数据处理，按业务需求处理数据并进行数据转换
        /// </summary>
        /// <param name="allData"></param>
        /// <returns></returns>
        private List<ThDrawModel> DataProcessingAndConversion(List<ThFireDistrictModel> allData,out List<string> Msg)
        {
            List<KeyValuePair<string, ThAlarmControlWireCircuitModel>> WireCircuitModels = new List<KeyValuePair<string, ThAlarmControlWireCircuitModel>>();
            Msg = new List<string>();
            List<string> warningMsg = new List<string>();
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
                    FindData.WireCircuits.AddRange(f.WireCircuits);
                }
            });
            NormalFireDistricts.ForEach(o =>
            {
                o.WireCircuits.OrderBy(x => x.WireCircuitNo).ForEach(x =>
                {
                    if (x.DrawWireCircuit)
                    {
                        int index = WireCircuitModels.FindLastIndex(y => y.Value.WireCircuitName == x.WireCircuitName);
                        if (index == -1)
                        {
                            if (x.BlockCount > 32)
                            {
                                warningMsg.Add($"违反强条！检测到回路{x.WireCircuitName}的消防设备总数超过了{FireCompartmentParameter.ShortCircuitIsolatorCount}个点,现有{x.BlockCount}个点，请复核。");
                            }
                            WireCircuitModels.Add(new KeyValuePair<string, ThAlarmControlWireCircuitModel>(o.FireDistrictName, x));
                        }
                        else
                        {
                            string Name = WireCircuitModels[index].Key;
                            var data = WireCircuitModels[index].Value + x;
                            if (data.BlockCount > 32)
                            {
                                warningMsg.Add($"违反强条！检测到回路{x.WireCircuitName}的消防设备总数超过了{FireCompartmentParameter.ShortCircuitIsolatorCount}个点,现有{data.BlockCount}个点，请复核。");
                            }
                            WireCircuitModels[index] = new KeyValuePair<string, ThAlarmControlWireCircuitModel>(Name, data);
                        }
                    }
                });
            });

            WireCircuitModels.ForEach(o =>
            {
                if (o.Value.Data.BlockData.BlockStatistics["楼层或回路重复显示屏"] > 0)
                    o.Value.Data.BlockData.BlockStatistics["区域显示器/火灾显示盘"] = 0;
            });
            Msg.AddRange(warningMsg);
            return WireCircuitModels.Select(o => new ThDrawModel() { 
                FireDistrictName = o.Key, 
                Data = o.Value.Data, 
                DrawCircuitName = true, 
                WireCircuitName = o.Value.WireCircuitName 
            }).ToList();
        }

        /// <summary>
        /// 填充防火分区数据
        /// </summary>
        /// <param name="fireDistrictBoundary"></param>
        /// <returns></returns>
        public void FillingFireCompartmentData(ref ThFireDistrictModel fireDistrict, Dictionary<Point3d, List<ThAlarmControlWireCircuitModel>> graphsDic)
        {
            string fireDistrictName = fireDistrict.FireDistrictName;
            var polygon = fireDistrict.FireDistrictBoundary;
            if (polygon is Polyline || polygon is MPolygon)
            {
                var DataSpatialIndex = new ThCADCoreNTSSpatialIndex(graphsDic.Keys.Select(o => new DBPoint(o)).ToCollection());
                var dbObjs = DataSpatialIndex.SelectCrossingPolygon(polygon).Cast<DBPoint>().Select(o => o.Position);
                var GraphData = graphsDic.Where(o => dbObjs.Contains(o.Key));
                //第一遍遍历，检索出所有不属于本防火分区或已经拥有名称的电路
                GraphData.ForEach(graphInfo => graphInfo.Value.ForEach(wc =>
                {
                    if(!string.IsNullOrWhiteSpace(wc.WireCircuitName))
                    {
                        wc.DrawWireCircuitText = false;
                        if (wc.WireCircuitName.Contains(fireDistrictName))
                        {
                            wc.WireCircuitNo = int.Parse(wc.WireCircuitName.Replace(fireDistrictName + "-WFA", ""));
                        }
                        else
                        {
                            wc.WireCircuitNo = -1;//该电路不属于本防火分区，打个标记
                        }
                    }
                    else
                    {
                        wc.DrawWireCircuitText = true;
                        if (wc.BlockCount == 0)
                            wc.DrawWireCircuit = false;
                    }
                }));
                foreach (var dic in GraphData)
                {
                    fireDistrict.WireCircuits.AddRange(dic.Value);
                }
                if (fireDistrict.WireCircuits.Count > 0)
                {
                    int max_No = fireDistrict.WireCircuits.Max(o => o.WireCircuitNo);
                    fireDistrict.WireCircuits.Where(o => o.DrawWireCircuit && o.DrawWireCircuitText).ForEach(o =>
                    {
                        o.WireCircuitNo = ++max_No;
                        o.WireCircuitName = fireDistrictName + "-WFA" + o.WireCircuitNo.ToString("00");
                    });
                }
            }
            else
            {
                throw new NotSupportedException();
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
    }
}
