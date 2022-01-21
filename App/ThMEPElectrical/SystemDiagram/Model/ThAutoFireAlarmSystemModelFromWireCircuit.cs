using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPElectrical.SystemDiagram.Extension;
using ThMEPEngineCore.CAD;
using ThMEPElectrical.BlockConvert;
using DotNetARX;
using Autodesk.AutoCAD.ApplicationServices;

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
        private List<Entity> GlobleNotInAlarmControlWireCircuitData;//剔除非火灾自动报警总线的模块
        private ThCADCoreNTSSpatialIndex GlobalBlockInfoSpatialIndex;
        private ThCADCoreNTSSpatialIndex FloorNotInAlarmControlWireCircuitIndex;
        private List<Entity> FloorEntityData;
        private List<Entity> FloorNotInAlarmControlWireCircuitData;
        private List<string> FireCompartmentNameList;
        private List<string> WireCircuitNameList;

        public ThAutoFireAlarmSystemModelFromWireCircuit()
        {
            floors = new List<ThFloorModel>();
            FireCompartmentNameList = new List<string>();
            WireCircuitNameList = new List<string>();
        }

        /// <summary>
        /// 设置全局空间索引
        /// </summary>
        public override void SetGlobalData(Database database, Dictionary<Entity, List<KeyValuePair<string, string>>> elements, List<Entity> Entitydata)
        {
            this._database = database;
            GlobleBlockAttInfoDic = elements;
            GlobleEntityData = Entitydata;
            GlobleNotInAlarmControlWireCircuitData = Entitydata.Where(o => o is BlockReference br && ThAutoFireAlarmSystemCommon.NotInAlarmControlWireCircuitBlockNames.Contains(br.Name)).ToList();
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
            FloorNotInAlarmControlWireCircuitData = GlobleNotInAlarmControlWireCircuitData.Where(o => dbObjs.Contains(o)).ToList();
            FloorBlockAttInfoDic = GlobleBlockAttInfoDic.Where(o => dbObjs.Contains(o.Key)).ToDictionary(x => x.Key, y => y.Value);
            FloorNotInAlarmControlWireCircuitIndex = new ThCADCoreNTSSpatialIndex(FloorNotInAlarmControlWireCircuitData.ToCollection());
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
            InsertBlockService.ImportFireDistrictLayerAndStyle(adb.Database);
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
                                        FloorNumber = sobj.Storeys[0].GetFloorNumber(),
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

            var doc = adb.Database.GetDocument();
            //统计楼层内回路计数
            Floors.ForEach(floor =>
            {
                FireCompartmentNameList.AddRange(floor.FireDistricts.Where(o => !o.DrawFireDistrictNameText).Select(o => o.FireDistrictName));
                //定位楼层数据
                GetFloorBlockInfo(floor.FloorBoundary);
                //初始化寻路引擎
                ThAFASGraphEngine GraphEngine = new ThAFASGraphEngine(adb.Database, FloorEntityData, FloorBlockAttInfoDic, floor.FireDistricts.Select(o => o.FireDistrictBoundary), floor.FloorName == "JF");
                GraphEngine.InitGraph();
                GraphEngine.DrawCrossAlarms();
                //GraphEngine.DrawGraphs();
                if (GraphEngine.CrossAlarms.Count > 0)
                {
                    Active.Editor.WriteLine($"\n违反强条！{floor.FloorName}层共{GraphEngine.CrossAlarms.Count}个穿越防火分区处总线未设置短路隔离器，见标注×处");
                    GraphEngine.CrossAlarms.ForEach(o =>
                    {
                        FireCompartmentParameter.WarningCache.Last().AlarmList.Add((floor.FloorName+"_穿防火分区", o).ToTuple());
                    });
                }
                var The_MaxNo_FireDistrict = floor.FireDistricts.OrderByDescending(f => f.FireDistrictNo).FirstOrDefault();
                int Max_FireDistrictNo = The_MaxNo_FireDistrict.FireDistrictNo;
                string FloorName = Max_FireDistrictNo > 1 ? The_MaxNo_FireDistrict.FireDistrictName.Split('-')[0] : floor.FloorName;
                floor.FireDistricts.Where(f => f.DrawFireDistrictNameText).ToList().ForEach(o =>
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
                floor.FireDistricts.ForEach(fireDistrict =>
                {
                    FillingFireCompartmentData(ref fireDistrict, GraphEngine.GraphsDic, adb.Database);
                    fireDistrict.WireCircuits.ForEach(wireCircuit =>
                    {
                        wireCircuit.doc = doc;
                    });
                });
            });

            return Floors;
        }

        /// <summary>
        /// 虚拟初始化一栋楼,V2.0版本不支持此操作
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
                List<DBText> DrawFireDistrictEntitys = new List<DBText>();
                List<DBText> DrawWireCircuitEntitys = new List<DBText>();
                addFloorss.ForEach(f =>
                {
                    f.FireDistricts.ForEach(fireDistrict =>
                    {
                        //画防火分区名字
                        if (fireDistrict.DrawFireDistrictNameText && fireDistrict.DrawFireDistrict)
                        {
                            var newDBText = new DBText() { Height = 2000, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = fireDistrict.FireDistrictName, Position = fireDistrict.TextPoint, AlignmentPoint = fireDistrict.TextPoint, Layer = ThAutoFireAlarmSystemCommon.FireDistrictByLayer};
                            newDBText.Rotation = Rotation;
                            DrawFireDistrictEntitys.Add(newDBText);
                        }
                        //画电回路编号
                        fireDistrict.WireCircuits.ForEach(cw =>
                        {
                            if (cw.DrawWireCircuit && cw.DrawWireCircuitText)
                            {
                                var newDBText = new DBText() { Height = 300, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = cw.WireCircuitName, Position = cw.TextPoint, AlignmentPoint = cw.TextPoint, Layer = ThAutoFireAlarmSystemCommon.WireCircuitByLayer };
                                newDBText.Rotation = Rotation;
                                DrawWireCircuitEntitys.Add(newDBText);
                            }
                        });
                    });
                });
                DrawFireDistrictEntitys.ForEach(e => acadDatabase.ModelSpace.Add(e));            
                var textStyle = acadDatabase.TextStyles.Element("TH-STYLE1");
                DrawFireDistrictEntitys.ForEach(e => e.TextStyleId = textStyle.Id);

                DrawWireCircuitEntitys.ForEach(e => acadDatabase.ModelSpace.Add(e));
                var WireCircuittextStyle = acadDatabase.TextStyles.Element("TH-STYLE3");
                DrawWireCircuitEntitys.ForEach(e => e.TextStyleId = WireCircuittextStyle.Id);
            }
        }

        /// <summary>
        /// 准备数据
        /// </summary>
        protected override void PrepareData()
        {
            var Alarms = new List<Tuple<Document,string, Point3d>>() ;
            List<string> warningMsg = DataProcessing(out Alarms);
            foreach (var tuple in Alarms)
            {
                FireCompartmentParameter.WarningCache.First(o => o.Doc == tuple.Item1).AlarmList.Add((tuple.Item2, tuple.Item3).ToTuple());
            }
            //FireCompartmentParameter.WarningCache
            warningMsg.ForEach(msg => Active.Editor.WriteLine($"\n{msg}"));
            var AllData = GetDrawModelInfo();
            this.DrawData = AllData;
        }

        /// <summary>
        /// 填充防火分区数据
        /// </summary>
        /// <param name="fireDistrictBoundary"></param>
        /// <returns></returns>
        public void FillingFireCompartmentData(ref ThFireDistrictModel fireDistrict, Dictionary<Point3d, List<ThAlarmControlWireCircuitModel>> graphsDic, Database database)
        {
            string fireDistrictName = fireDistrict.FireDistrictName;
            var polygon = fireDistrict.FireDistrictBoundary;
            if (polygon is Polyline || polygon is MPolygon)
            {
                fireDistrict.NotInAlarmControlWireCircuitData = FloorNotInAlarmControlWireCircuitIndex.SelectCrossingPolygon(polygon).Cast<BlockReference>().ToList();
                int count = fireDistrict.NotInAlarmControlWireCircuitData.Count;
                for (int i = 0; i < count; i++)
                {
                    var quantity = ThQuantityMarkExtension.GetQuantity(database.GetBlockReferenceOBB(fireDistrict.NotInAlarmControlWireCircuitData[i]));
                    while(quantity>1)
                    {
                        fireDistrict.NotInAlarmControlWireCircuitData.Add(fireDistrict.NotInAlarmControlWireCircuitData[i]);
                        quantity--;
                    }
                }

                var DataSpatialIndex = new ThCADCoreNTSSpatialIndex(graphsDic.Keys.Select(o => new DBPoint(o)).ToCollection());
                var dbObjs = DataSpatialIndex.SelectCrossingPolygon(polygon).Cast<DBPoint>().Select(o => o.Position);
                var GraphData = graphsDic.Where(o => dbObjs.Contains(o.Key));
                //第一遍遍历，检索出所有不属于本防火分区或已经拥有名称的电路
                GraphData.ForEach(graphInfo => graphInfo.Value.ForEach(wc =>
                {
                    if (!string.IsNullOrWhiteSpace(wc.WireCircuitName))
                    {
                        wc.DrawWireCircuitText = false;
                        if (!WireCircuitNameList.Contains(wc.WireCircuitName))
                        {
                            WireCircuitNameList.Add(wc.WireCircuitName);
                        }
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
                        while (WireCircuitNameList.Contains(fireDistrictName + "-WFA" + (max_No + 1).ToString("00")))
                        {
                            max_No++;
                        }
                        o.WireCircuitNo = ++max_No;
                        o.WireCircuitName = fireDistrictName + "-WFA" + o.WireCircuitNo.ToString("00");
                        if (!WireCircuitNameList.Contains(o.WireCircuitName))
                        {
                            WireCircuitNameList.Add(o.WireCircuitName);
                        }
                    });
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 数据处理，按业务需求处理数据
        /// </summary>
        /// <param name="allData"></param>
        /// <returns></returns>
        private List<string> DataProcessing(out List<Tuple<Document, string,Point3d>> Alarms)
        {
            var alarms = new List<Tuple<Document, string, Point3d>>();
            var warningMsg = new List<string>();
            //过滤未延申到火灾自动报警总线包括的回路
            this.floors.ForEach(floor => floor.FireDistricts.ForEach(o =>
            {
                o.WireCircuits = o.WireCircuits.Where(x => x.DrawWireCircuit).ToList();
            }));
            //全部防火分区
            var AllFireZones = this.floors.SelectMany(o => o.FireDistricts).ToList();
            //合并同名称楼层
            var NormalFireDistricts = new List<ThFireDistrictModel>();
            AllFireZones.ForEach(o =>
            {
                int index = NormalFireDistricts.FindIndex(x => x.FireDistrictName == o.FireDistrictName);
                if (index >= 0)
                {
                    o.WireCircuits.ForEach(x =>
                    {
                        if(NormalFireDistricts[index].WireCircuits.Count(y => y.WireCircuitName == x.WireCircuitName) > 0)
                        {
                            for (int j = 0; j < NormalFireDistricts[index].WireCircuits.Count; j++)
                            {
                                var wirecircuit = NormalFireDistricts[index].WireCircuits[j];
                                if (wirecircuit.WireCircuitName == x.WireCircuitName)
                                {
                                    wirecircuit += x;
                                    NormalFireDistricts[index].WireCircuits[j] = wirecircuit;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            NormalFireDistricts[index].WireCircuits.Add(x);
                        }
                    });
                    NormalFireDistricts[index].NotInAlarmControlWireCircuitData.AddRange(o.NotInAlarmControlWireCircuitData);
                }
                else
                {
                    List<ThAlarmControlWireCircuitModel> wireCircuitModels = new List<ThAlarmControlWireCircuitModel>();
                    o.WireCircuits.ForEach(x =>
                    {
                        if (wireCircuitModels.Count(y => y.WireCircuitName == x.WireCircuitName) > 0)
                        {
                            for (int j = 0; j < wireCircuitModels.Count; j++)
                            {
                                var wirecircuit = wireCircuitModels[j];
                                if (wirecircuit.WireCircuitName == x.WireCircuitName)
                                {
                                    wirecircuit += x;
                                    wireCircuitModels[j] = wirecircuit;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            wireCircuitModels.Add(x);
                        }
                    });
                    o.WireCircuits = wireCircuitModels;
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
                    else if (o.FireDistrictNo != -1)
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
            this.floors.ForEach(x => x.FireDistricts.ForEach(y =>
            {
                var FirstWireCircuit = y.WireCircuits.Where(o => o.WireCircuitNo > 0).OrderBy(o => o.WireCircuitNo).FirstOrDefault();
                if (!FirstWireCircuit.IsNull())
                {
                    FirstWireCircuit.Data.BlockData.BlockStatistics["区域显示器/火灾显示盘"] = y.NotInAlarmControlWireCircuitData.Count(br => br.Name == "E-BFAS030");
                    FirstWireCircuit.Data.BlockData.BlockStatistics["楼层或回路重复显示屏"] = y.NotInAlarmControlWireCircuitData.Count(br => br.Name == "E-BFAS031");
                    FirstWireCircuit.Data.BlockData.BlockStatistics["火灾报警电话"] = y.NotInAlarmControlWireCircuitData.Count(br => br.Name == "E-BFAS220");
                    FirstWireCircuit.Data.BlockData.BlockStatistics["火灾应急广播扬声器-2"] = y.NotInAlarmControlWireCircuitData.Count(br => br.Name == "E-BFAS410-2");
                    FirstWireCircuit.Data.BlockData.BlockStatistics["火灾应急广播扬声器-3"] = y.NotInAlarmControlWireCircuitData.Count(br => br.Name == "E-BFAS410-3");
                    FirstWireCircuit.Data.BlockData.BlockStatistics["火灾应急广播扬声器-4"] = y.NotInAlarmControlWireCircuitData.Count(br => br.Name == "E-BFAS410-4");
                }
                y.WireCircuits.ForEach(o =>
                {
                    if (o.BlockCount > FireCompartmentParameter.ShortCircuitIsolatorCount)
                    {
                        warningMsg.Add($"违反强条！检测到回路{o.WireCircuitName}的消防设备总数超过了{FireCompartmentParameter.ShortCircuitIsolatorCount}个点,现有{o.BlockCount}个点，请复核。");
                        alarms.Add((o.doc, o.WireCircuitName + "回路设备超点", o.TextPoint).ToTuple());

                    }
                    if (o.Data.BlockData.BlockStatistics["楼层或回路重复显示屏"] > 0)
                        o.Data.BlockData.BlockStatistics["区域显示器/火灾显示盘"] = 0;
                });
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
                            string OldName = names[0];
                            names[0] = newfloor.FloorName;
                            var newFireDistrict = new ThFireDistrictModel()
                            {
                                FireDistrictName = string.Join("-", names),
                                DrawFireDistrict = x.DrawFireDistrict,
                                DrawFireDistrictNameText = newfloor.FloorName == floor.MulitFloors[0] ? x.DrawFireDistrictNameText : false,
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
                                    DrawWireCircuitText = newfloor.FloorName == floor.MulitFloors[0] ? cw.DrawWireCircuitText : false,
                                    TextPoint = cw.TextPoint,
                                    WireCircuitName = cw.WireCircuitName.Replace(OldName, newfloor.FloorName),
                                    WireCircuitNo = cw.WireCircuitNo,
                                    Data = cw.Data,
                                    Graph = cw.Graph,
                                    BlockCount = cw.BlockCount,
                                });
                            });
                            newfloor.FireDistricts.Add(newFireDistrict);
                        });
                        this.floors.Add(newfloor);
                    });
                    this.floors = this.floors.Where(o => !o.IsMultiFloor).ToList();
                });
            }
            else
            {
                //合并同防火分区的所有回路
                this.floors.ForEach(o =>
                {
                    for (int i = 0; i < o.FireDistricts.Count; i++)
                    {
                        ThFireDistrictModel fireDistrictModel = o.FireDistricts[i];
                        if (fireDistrictModel.WireCircuits.Count > 0)
                        {
                            ThAlarmControlWireCircuitModel wireCircuitModel = fireDistrictModel.WireCircuits[0];
                            for (int j = 1; j < fireDistrictModel.WireCircuits.Count; j++)
                            {
                                wireCircuitModel += fireDistrictModel.WireCircuits[j];
                            }
                            wireCircuitModel.MulitWireCircuitName = MergeInts(fireDistrictModel.WireCircuits.Select(x => x.WireCircuitNo));
                            fireDistrictModel.WireCircuits = new List<ThAlarmControlWireCircuitModel>() { wireCircuitModel };
                        }
                    }
                });
            }
            Alarms = alarms;
            return warningMsg;
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
                o.FireDistricts.OrderBy(x => x.FireDistrictNo).ForEach(x => x.WireCircuits.ForEach(y =>
                {
                    string fireDistrictName = o.IsMultiFloor ? $"{o.MulitFloorName}:{o.MulitStoreyNumber}F-{x.FireDistrictNo}" : x.FireDistrictName;
                    drawModels.Add(new ThDrawModel()
                    {
                        FireDistrictName = fireDistrictName,
                        Data = y.Data,
                        DrawCircuitName = true,
                        WireCircuitName = FireCompartmentParameter.DiagramDisplayEffect == 1 ? y.WireCircuitName : "WFA" + y.MulitWireCircuitName,
                        FloorCount = o.IsMultiFloor ? o.MulitFloors.Count : 1,
                    });
                }));
                return drawModels;
            }).ToList();
        }

        /// <summary>
        /// 合并int集合
        /// </summary>
        public string MergeInts(IEnumerable<int> ints)
        {
            if (ints.Count() == 1)
                return ints.First().ToString("00");

            string CollectionNumberStr = string.Empty;

            List<string> CollectionNumberStrArr = new List<string>();
            if (ints != null && ints.Count() > 0)
            {
                var query = ints.OrderBy(p => p).Aggregate<int, List<List<int>>>(null, (m, n) =>
                {
                    if (m == null)
                        return new List<List<int>>() { new List<int>() { n } };
                    if (m.Last().Last() != n - 1)
                        m.Add(new List<int>() { n });
                    else
                        m.Last().Add(n);
                    return m;
                });
                query.ForEach(p =>
                {
                    int First = p.First();
                    int Last = p.Last();

                    if (First == Last)
                        CollectionNumberStrArr.Add(First.ToString("00"));
                    else
                        CollectionNumberStrArr.Add(First.ToString("00") + "-" + Last.ToString("00"));
                });
                CollectionNumberStr = CollectionNumberStr.Trim();
            }
            return CollectionNumberStr = string.Join(",", CollectionNumberStrArr);
        }
    }
}
