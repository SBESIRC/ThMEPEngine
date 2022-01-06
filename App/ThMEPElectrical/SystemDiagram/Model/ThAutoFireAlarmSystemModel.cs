using System;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPElectrical.SystemDiagram.Model.WireCircuit;
using Autodesk.AutoCAD.ApplicationServices;
using AcHelper;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// 自动火灾报警系统Model
    /// </summary>
    public abstract class ThAutoFireAlarmSystemModel
    {
        public Database _database;
        public List<ThFloorModel> floors { get; set; }
        public List<ThDrawModel> DrawData { get; set; }

        //设置全局数据
        public abstract void SetGlobalData(Database database, Dictionary<Entity, List<KeyValuePair<string, string>>> elements, List<Entity> Entitydata);
        //初始化楼层
        public abstract List<ThFloorModel> InitStoreys(AcadDatabase adb, List<ThIfcSpatialElement> storeys, List<ThFireCompartment> fireCompartments);
        //初始化虚拟楼层
        public abstract List<ThFloorModel> InitVirtualStoreys(Database db, Polyline storyBoundary, List<ThFireCompartment> fireCompartments);
        //画编号
        public abstract void DrawFloorsNum(Database db, List<ThFloorModel> addFloorss);
        //统计数据
        protected abstract void PrepareData();

        public void DrawSystemDiagram(Vector3d Offset, Matrix3d ConversionMatrix)
        {
            PrepareData();
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

                List<Entity> DrawEntitys;
                Dictionary<Point3d, ThBlockModel> dicBlockPoints;
                int RowIndex = 1;//方格层数
                List<ThDrawModel> AllData = this.DrawData;
                List<ObjectIdList> Groups = new List<ObjectIdList>();
                foreach (var fireDistrict in AllData)
                {
                    dicBlockPoints = new Dictionary<Point3d, ThBlockModel>();
                    DrawEntitys = new List<Entity>();
                    var groupIds = new ObjectIdList();
                    //初始化横线
                    ThWireCircuitConfig.HorizontalWireCircuits.ForEach(o =>
                    {
                        o.SetFloorIndex(RowIndex, fireDistrict);
                        var entitys = o.Draw();
                        DrawEntitys.AddRange(entitys);
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

                    //画所有的外框线
                    var OuterBorderBlockIds = InsertBlockService.InsertOuterBorderBlock(RowIndex, ThAutoFireAlarmSystemCommon.SystemColLeftNum + ThAutoFireAlarmSystemCommon.SystemColRightNum);
                    //画该层所有的块
                    var SpecifyBlockIds = InsertBlockService.InsertSpecifyBlock(dicBlockPoints);
                    //画所有的横线
                    var EntityIDs = InsertBlockService.InsertEntity(DrawEntitys);
                    groupIds.AddRange(OuterBorderBlockIds);
                    groupIds.AddRange(SpecifyBlockIds);
                    groupIds.AddRange(EntityIDs);
                    Groups.Add(groupIds);
                    //跳入下一层
                    RowIndex++;
                }
                {
                    DrawEntitys = new List<Entity>();
                    //初始化竖线
                    ThWireCircuitConfig.VerticalWireCircuits.ForEach(o =>
                    {
                        o.SetFloorIndex(RowIndex, AllData);
                        DrawEntitys.AddRange(o.Draw());
                        var DrawDic = o.DrawVertical();
                        DrawDic.ForEach(x =>
                        {
                            var EntityIDs = InsertBlockService.InsertEntity(x.Value);
                            Groups[x.Key - 1].AddRange(EntityIDs);
                        });
                    });
                }

                //公共部分，不需要加入到组中
                InsertBlockService.InsertEntity(DrawEntitys);

                //画底部固定部分
                if (FireCompartmentParameter.FixedPartType != 3)
                {
                    InsertBlockService.InsertSpecifyBlock(FireCompartmentParameter.FixedPartType == 1 ? ThAutoFireAlarmSystemCommon.FixedPartContainsFireRoom : ThAutoFireAlarmSystemCommon.FixedPartExcludingFireRoom);
                }

                //加入组
                if (FireCompartmentParameter.DiagramCreateGroup == 1)
                {
                    Groups.ForEach(g => GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), g));
                }
            }
        }

        public void DrawAlarm()
        {
            foreach (Document doc in Application.DocumentManager)
            {
                var alarm = FireCompartmentParameter.WarningCache.FirstOrDefault(o => o.Doc == doc);
                if (alarm.IsNull() || alarm.AlarmList.Count < 1)
                {
                    continue;
                }
                using (DocumentLock docLock = doc.LockDocument())
                using (new ThDbWorkingDatabaseSwitch(doc.Database))
                using (AcadDatabase db = AcadDatabase.Use(doc.Database))
                {
                    InsertBlockService.ImportCloudBlock(doc.Database, ThAutoFireAlarmSystemCommon.CloudBlockName);
                    alarm.AlarmList.ForEach(o =>
                    {
                        var objID = InsertBlockService.InsertCloudBlock(db.Database, ThAutoFireAlarmSystemCommon.CloudBlockName, o.Item2);
                        alarm.UiAlarmList.Add((o.Item1, objID).ToTuple());
                    });
                }
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
