using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

using Dbscan;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using Dbscan.RBush;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using ThMEPElectrical.ChargerDistribution.Service;
using ThMEPElectrical.ChargerDistribution.Common;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.ChargerDistribution.Command
{
    public class ThChargerGroupingCmd : ThMEPBaseCommand
    {
        private double MaxPoint = 9.0;

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var currentDb = AcadDatabase.Active())
            {
                // 获取框线
                // 若选取多个框线，则计算所有框线内目标块数量
                var frames = ThParkingStallUtils.GetFrames(currentDb);
                if (frames.Count == 0)
                {
                    return;
                }

                var chargerBlocks = ThParkingStallUtils.ChargerRecognize(currentDb).Select(o => new DBPoint(o.Position)).ToList();
                if (chargerBlocks.Count == 0)
                {
                    return;
                }

                // 移动到原点附近
                //var transformer = new ThMEPOriginTransformer(Point3d.Origin);
                var transformer = new ThMEPOriginTransformer(frames[0].StartPoint);
                ThParkingStallUtils.Transform(transformer, frames.ToCollection());
                ThParkingStallUtils.Transform(transformer, chargerBlocks.ToCollection());

                frames.ForEach(frame =>
                {
                    var points = ThParkingStallUtils.SelectCrossingPolygon(frame, chargerBlocks).Select(o => o.Position).ToList();
                    var iPoints = new List<PointInfo<SimplePoint>>();
                    points.ForEach(o =>
                    {
                        var pointInfo = new PointInfo<SimplePoint>(new SimplePoint(o.X, o.Y));
                        iPoints.Add(pointInfo);
                    });
                    var clusters = DbscanRBush.CalculateClusters(iPoints, epsilon: 10000.0, minimumPointsPerCluster: 1);

                    var results = new List<List<Point3d>>();
                    for (var i = 0; i < clusters.Clusters.Count; i++)
                    {
                        var cluster = clusters.Clusters[i];
                        var result = cluster.Objects.Select(o => o.Item.ToPoint3d()).ToList();

                        while (result.Count > MaxPoint)
                        {
                            var groupCount = Math.Ceiling(result.Count / MaxPoint);
                            var number = Math.Ceiling(result.Count / groupCount);

                            var centerX = result.Sum(o => o.X) / result.Count;
                            var centerY = result.Sum(o => o.Y) / result.Count;
                            var center = new Point3d(centerX, centerY, 0);

                            var borderPoint = result.OrderByDescending(o => o.DistanceTo(center)).FirstOrDefault();
                            var pointList = result.OrderBy(o => o.DistanceTo(borderPoint)).ToList();
                            var partList = new List<Point3d>();
                            for (var j = 0; j < pointList.Count && j < number; j++)
                            {
                                partList.Add(pointList[j]);
                            }

                            results.Add(Sort(partList));
                            result = result.Except(partList).ToList();
                        }
                        results.Add(Sort(result));
                    }

                    short k = 0;
                    var layerName = currentDb.Database.CreateAILayer(ThChargerDistributionCommon.Grouping_Layer, 0);
                    ThParkingStallUtils.CleanPolyline(currentDb, frame, layerName);
                    results.ForEach(result =>
                    {
                        var service = new ThMinimumPolylineService();
                        result = service.Calculate(result);

                        var pointCollection = result.ToCollection();
                        var pline = new Polyline();
                        pline.CreatePolyline(pointCollection);
                        pline.LayerId = layerName;
                        pline.ColorIndex = k;
                        // 变换回原位置
                        transformer.Reset(pline);
                        currentDb.ModelSpace.Add(pline);
                        k++;
                    });
                });

                ThParkingStallUtils.Reset(transformer, frames.ToCollection());
                ThParkingStallUtils.Reset(transformer, chargerBlocks.ToCollection());
            }
        }

        private List<Point3d> Sort(List<Point3d> partList)
        {
            var localCenterX = partList.Sum(o => o.X) / partList.Count;
            var localCenterY = partList.Sum(o => o.Y) / partList.Count;
            var localCenter = new Point3d(localCenterX, localCenterY, 0);
            var farPoint = partList.OrderByDescending(o => o.DistanceTo(localCenter)).FirstOrDefault();
            return partList.OrderBy(o => o.DistanceTo(farPoint)).ToList();
        }
    }

    public class SimplePoint : IPointData
    {
        public SimplePoint(double x, double y) => Point = new Point(x, y);

        public Point Point { get; }

        public Point3d ToPoint3d()
        {
            return new Point3d(Point.X, Point.Y, 0);
        }
    }
}
