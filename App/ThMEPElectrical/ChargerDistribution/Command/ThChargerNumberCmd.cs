using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.Geom;
using ThMEPEngineCore.Command;
using ThMEPElectrical.ChargerDistribution.Model;
using ThMEPElectrical.ChargerDistribution.Common;
using ThMEPElectrical.ChargerDistribution.Service;
using NFox.Cad;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.ChargerDistribution.Command
{
    public class ThChargerNumberCmd : ThMEPBaseCommand
    {
        private readonly Vector3d XAxis = new Vector3d(1, 0, 0);

        private readonly double AngleTolerance = Math.PI / 180.0;

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var blockDb = AcadDatabase.Open(ThParkingStallUtils.BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            using (var currentDb = AcadDatabase.Active())
            {
                // 获取框线
                // 若选取多个框线，则计算所有框线内目标块数量
                var frames = ThParkingStallUtils.GetFrames(currentDb);
                if (frames.Count == 0)
                {
                    return;
                }

                // 车位
                var engine = new ThParkingStallRecognization(new Point3dCollection());
                engine.Recognize();
                if (engine.ParkingStallPolys.Count == 0)
                {
                    return;
                }

                // 充电桩
                var chargerBlocks = ThParkingStallUtils.ChargerRecognize(currentDb).Select(o => new ThChargerData(o)).ToList();
                if (chargerBlocks.Count == 0)
                {
                    return;
                }

                // 分组线
                var groupingPolyline = ThParkingStallUtils.GroupingPolylineRecognize(currentDb);
                if (groupingPolyline.Count == 0)
                {
                    return;
                }

                // 标注块
                var dimensions = ThParkingStallUtils.DimensionRecognize(currentDb).Select(o => new ThChargerData(o)).ToList();
                var dimensionGeometries = dimensions.Select(o => o.Geometry).ToList();

                // 移动到原点附近
                //var transformer = new ThMEPOriginTransformer(Point3d.Origin);
                var transformer = new ThMEPOriginTransformer(frames[0].StartPoint);
                ThParkingStallUtils.Transform(transformer, chargerBlocks);
                ThParkingStallUtils.Transform(transformer, frames.ToCollection());
                ThParkingStallUtils.Transform(transformer, groupingPolyline.ToCollection());
                ThParkingStallUtils.Transform(transformer, dimensionGeometries.ToCollection());
                ThParkingStallUtils.Transform(transformer, engine.ParkingStallPolys.ToCollection());

                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThChargerDistributionCommon.Block_Name_Dimension), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThChargerDistributionCommon.Block_Layer_Dimension), false);

                frames.ForEach(frame =>
                {
                    var blockData = ThParkingStallUtils.SelectCrossingPolygon(frame, chargerBlocks);
                    var groups = ThParkingStallUtils.SelectCrossingPolygon(frame, groupingPolyline);
                    var groupInfos = groups.Select(o => new ThGroupPolylineInfo(o)).ToList();
                    var parkingStalls = ThParkingStallUtils.SelectCrossingPolygon(frame, engine.ParkingStallPolys);

                    // 清理上一次的结果
                    ThParkingStallUtils.BlocksClean(currentDb, frame, dimensions, dimensionGeometries);

                    var dictionary = new Dictionary<double, int>();
                    blockData.ForEach(o =>
                    {
                        var contains = false;
                        dictionary.Select(pair => pair.Key).ToList().ForEach(key =>
                        {
                            if (Math.Abs(key - o.Rotation) < AngleTolerance)
                            {
                                dictionary[key]++;
                                contains = true;
                                return;
                            }
                        });
                        if (!contains)
                        {
                            dictionary.Add(o.Rotation, 1);
                        }
                    });
                    var mainPair = dictionary.OrderByDescending(o => o.Value).FirstOrDefault();
                    var mainDirection = XAxis.TransformBy(Matrix3d.Rotation(mainPair.Key % (Math.PI / 2), Vector3d.ZAxis, Point3d.Origin));
                    var normal = new Vector3d(-mainDirection.Y, mainDirection.X, 0).GetNormal();
                    var ucsRotation = mainDirection.GetAngleTo(Vector3d.XAxis);
                    groupInfos = groupInfos.OrderBy(o => o.Centroid.ToVector3d().DotProduct(mainDirection)).ThenByDescending(o => o.Centroid.ToVector3d().DotProduct(normal)).ToList();

                    var searchedList = new List<ThChargerData>();
                    var groupNumber = 0;
                    groupInfos.ForEach(info =>
                    {
                        groupNumber++;
                        var thisGroup = blockData.Except(searchedList).Where(block => info.Polyline.DistanceTo(block.Position, false) < 10.0).ToList();
                        searchedList.AddRange(thisGroup);

                        if (thisGroup.Count == 0)
                        {
                            return;
                        }

                        // 主方向方差
                        var mainVar = thisGroup.Select(o => o.Position.ToVector3d().DotProduct(mainDirection)).Range();
                        var normalVar = thisGroup.Select(o => o.Position.ToVector3d().DotProduct(normal)).Range();
                        if (mainVar > normalVar)
                        {
                            thisGroup = thisGroup.OrderByDescending(o => o.Position.Calculate(normal)).ThenBy(o => o.Position.Calculate(mainDirection)).ToList();
                        }
                        else
                        {
                            thisGroup = thisGroup.OrderBy(o => o.Position.Calculate(mainDirection)).ThenByDescending(o => o.Position.Calculate(normal)).ToList();
                        }

                        var id = 1;
                        thisGroup.ForEach(o =>
                        {
                            o.CircuitNumber = "AP" + groupNumber.NumberChange() + "-WP" + id.NumberChange();
                            id++;
                        });
                    });

                    blockData.ForEach(o =>
                    {
                        var dimensionRotation = o.Rotation + Math.PI / 2;
                        var direction = XAxis.TransformBy(Matrix3d.Rotation(dimensionRotation, Vector3d.ZAxis, Point3d.Origin));
                        var insertPoint = o.Position + 1.25 * o.ScaleFactors.X * direction;
                        // 挪回原位置
                        insertPoint = transformer.Reset(insertPoint);
                        var rotation = o.Rotation - ucsRotation;
                        if (rotation > AngleTolerance && rotation < Math.PI + AngleTolerance)
                        {
                            var dimension = ThChargerInsertService.InsertDimension(ThChargerDistributionCommon.Block_Layer_Dimension, ThChargerDistributionCommon.Block_Name_Dimension, insertPoint, new Scale3d(100.0), 0.0, o.CircuitNumber);
                            dimension.TransformBy(Matrix3d.Rotation(o.Rotation - Math.PI / 2, Vector3d.ZAxis, dimension.Position));
                            dimension.Id.SetDynamicProperty();
                        }
                        else
                        {
                            var dimension = ThChargerInsertService.InsertDimension(ThChargerDistributionCommon.Block_Layer_Dimension, ThChargerDistributionCommon.Block_Name_Dimension, insertPoint, new Scale3d(100.0), 0.0, o.CircuitNumber);
                            dimension.TransformBy(Matrix3d.Rotation(o.Rotation + Math.PI / 2, Vector3d.ZAxis, dimension.Position));
                        }
                    });
                });

                ThParkingStallUtils.Reset(transformer, chargerBlocks);
                ThParkingStallUtils.Reset(transformer, frames.ToCollection());
                ThParkingStallUtils.Reset(transformer, groupingPolyline.ToCollection());
                ThParkingStallUtils.Reset(transformer, dimensionGeometries.ToCollection());
                ThParkingStallUtils.Reset(transformer, engine.ParkingStallPolys.ToCollection());
            }
        }
    }
}
