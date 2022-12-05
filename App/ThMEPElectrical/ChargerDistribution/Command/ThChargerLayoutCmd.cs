using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.ChargerDistribution.Model;
using ThMEPElectrical.ChargerDistribution.Common;
using ThMEPElectrical.ChargerDistribution.Service;

namespace ThMEPElectrical.ChargerDistribution.Command
{
    public class ThChargerLayoutCmd : ThMEPBaseCommand
    {
        private readonly double LaneHalfWidth = 4000.0;

        private readonly double ExtendWidth = 800.0;

        private readonly Scale3d Scale = new Scale3d(100);

        private readonly string BlockName = ThChargerDistributionCommon.Block_Name_Charging_Equipment;

        private readonly string BlockLayer = ThChargerDistributionCommon.Block_Layer_Charging_Equipment;

        public ThChargerLayoutCmd()
        {
            ActionName = "布置";
            CommandName = "THCDZPD";
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var blockDb = AcadDatabase.Open(ThParkingStallUtils.BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            using (var currentDb = AcadDatabase.Active())
            {
                // 获取框线
                var frames = ThChargerSelector.GetFrames(currentDb);
                if (frames.Count == 0)
                {
                    return;
                }

                var allLaneLines = ThParkingStallUtils.LaneLineRecognize(currentDb);
                if (allLaneLines.Count == 0)
                {
                    return;
                }

                var chargerBlocks = ThParkingStallUtils.ChargerRecognize(currentDb).Select(o => new ThChargerData(o)).ToList();
                var geometries = chargerBlocks.Select(o => o.Geometry).ToList();

                var engine = new ThParkingStallRecognization(new Point3dCollection());
                engine.Recognize();
                if (engine.ParkingStallPolys.Count == 0)
                {
                    return;
                }

                // 移动到原点附近
                //var transformer = new ThMEPOriginTransformer(Point3d.Origin);
                var transformer = new ThMEPOriginTransformer(frames[0].GeometricExtents.MinPoint);
                ThParkingStallUtils.Transform(transformer, frames.ToCollection());
                ThParkingStallUtils.Transform(transformer, allLaneLines.ToCollection());
                ThParkingStallUtils.Transform(transformer, engine.ParkingStallPolys.ToCollection());
                ThParkingStallUtils.Transform(transformer, geometries.ToCollection());

                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(BlockName), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(BlockLayer), false);

                frames.ForEach(frame =>
                {
                    // 车道线
                    var laneLines = ThParkingStallUtils.SelectCrossingPolygon(frame, allLaneLines);
                    var trimLines = laneLines.Trim(frame);

                    // 车位
                    var parkingStalls = ThParkingStallUtils.SelectCrossingPolygon(frame, engine.ParkingStallPolys);
                    var infos = new List<ThParkingStallInfo>();
                    var service = new ThChargerCalculateService();
                    parkingStalls.ForEach(o =>
                    {
                        var info = new ThParkingStallInfo
                        {
                            Outline = o,
                            Centroid = o.GetCentroidPoint(),
                            LaneLines = new List<Line>(),
                            Searched = false,
                            SetValue = false,
                        };
                        var objs = new DBObjectCollection();
                        o.Explode(objs);
                        info.Lines = objs.OfType<Line>().OrderByDescending(e => e.Length).ToList();
                        info.Direction = info.Lines[0].LineDirection();
                        infos.Add(info);
                    });

                    trimLines.ForEach(o =>
                    {
                        var laneFrame = o.Buffer(LaneHalfWidth);
                        var stalls = ThParkingStallUtils.SelectCrossingPolygon(laneFrame, parkingStalls);
                        stalls.ForEach(stall =>
                        {
                            var info = infos.Where(e => e.Outline.Equals(stall)).FirstOrDefault();
                            info.LaneLines.Add(o);
                        });
                    });

                    var searchedStalls = new List<Polyline>();
                    // 确定车位方向
                    infos.ForEach(info =>
                    {
                        if (info.LaneLines.Count == 1)
                        {
                            var laneLineDirection = info.LaneLines[0].LineDirection();
                            // 垂直式
                            if (Math.Abs(laneLineDirection.DotProduct(info.Direction)) < Math.Sin(1.0 / 180.0 * Math.PI))
                            {
                                info.Searched = true;
                                if (info.LaneLines[0].GetDistToPoint(info.Lines[0].EndPoint) < info.LaneLines[0].GetDistToPoint(info.Lines[0].StartPoint))
                                {
                                    info.Direction = -info.Direction;
                                }
                            }
                            // 平行式
                            else if (Math.Abs(laneLineDirection.DotProduct(info.Direction)) > Math.Cos(1.0 / 180.0 * Math.PI))
                            {

                            }
                            searchedStalls.Add(info.Outline);
                        }
                        else if (info.LaneLines.Count >= 2)
                        {
                            foreach (var laneLine in info.LaneLines)
                            {
                                var laneLineDirection = laneLine.LineDirection();
                                // 垂直式
                                if (Math.Abs(laneLineDirection.DotProduct(info.Direction)) < Math.Sin(1.0 / 180.0 * Math.PI))
                                {
                                    info.Searched = true;
                                    if (laneLine.GetDistToPoint(info.Lines[0].EndPoint) < laneLine.GetDistToPoint(info.Lines[0].StartPoint))
                                    {
                                        info.Direction = -info.Direction;
                                    }
                                    break;
                                }
                                // 平行式
                                else if (Math.Abs(laneLineDirection.DotProduct(info.Direction)) > Math.Cos(1.0 / 180.0 * Math.PI))
                                {

                                }
                            }
                            searchedStalls.Add(info.Outline);
                        }
                    });

                    // 根据临近车位确定车位方向
                    service.BufferSearch(infos, searchedStalls, ExtendWidth);
                    service.BufferSearch(infos, searchedStalls, 2 * ExtendWidth);

                    // 计算布置位置
                    infos.ForEach(info =>
                    {
                        if (!info.Searched)
                        {
                            return;
                        }

                        for (var i = 2; i < 4; i++)
                        {
                            var lineCenter = info.Lines[i].GetLineCenter();
                            var vector = (lineCenter - info.Centroid).GetNormal();
                            // 同向
                            if (info.Direction.DotProduct(vector) > Math.Cos(1.0 / 180.0 * Math.PI))
                            {
                                info.LayOutPosition = lineCenter;
                                info.Rotation = info.Lines[i].Angle;
                                info.SetValue = true;
                                break;
                            }
                        }
                    });

                    // insert
                    infos.ForEach(info =>
                    {
                        if (!info.SetValue)
                        {
                            return;
                        }

                        var position = transformer.Reset(info.LayOutPosition);
                        position -= 1.25 * Scale.X * info.Direction;
                        ThChargerInsertService.Insert(BlockLayer, BlockName, position, Scale, info.Rotation);
                    });

                    // 清空充电桩
                    var chargers = ThParkingStallUtils.SelectCrossingPolygon(frame, geometries);
                    ThParkingStallUtils.BlocksClean(currentDb, frame, chargerBlocks, chargers);
                });

                ThParkingStallUtils.Reset(transformer, frames.ToCollection());
                ThParkingStallUtils.Reset(transformer, geometries.ToCollection());
                ThParkingStallUtils.Reset(transformer, allLaneLines.ToCollection());
                ThParkingStallUtils.Reset(transformer, engine.ParkingStallPolys.ToCollection());
            }
        }
    }
}
