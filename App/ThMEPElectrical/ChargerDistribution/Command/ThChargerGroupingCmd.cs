using System.Data;
using System.Linq;
using System.Collections.Generic;

using Dbscan;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.ChargerDistribution.Group;
using ThMEPElectrical.ChargerDistribution.Common;
using ThMEPElectrical.ChargerDistribution.Service;

namespace ThMEPElectrical.ChargerDistribution.Command
{
    public class ThChargerGroupingCmd : ThMEPBaseCommand
    {
        private int MaxPoint = 9;

        public ThChargerGroupingCmd()
        {
            ActionName = "分组";
            CommandName = "THCDZPD";
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var currentDb = AcadDatabase.Active())
            {
                // 获取框线
                // 若选取多个框线，则计算所有框线内目标块数量
                var frames = ThChargerSelector.GetFrames(currentDb);
                if (frames.Count == 0)
                {
                    return;
                }

                var chargerBlocks = ThParkingStallUtils.ChargerRecognize(currentDb).Select(o => new DBPoint(o.Position)).ToList();
                if (chargerBlocks.Count == 0)
                {
                    return;
                }

                // 清理
                var layerId = currentDb.Database.CreateAILayer(ThChargerDistributionCommon.Grouping_Layer, 0);
                frames.ForEach(frame =>
                {
                    ThParkingStallUtils.CleanPolyline(currentDb, frame, layerId);
                });

                // 移动到原点附近
                //var transformer = new ThMEPOriginTransformer(Point3d.Origin);
                var transformer = new ThMEPOriginTransformer(frames[0].GeometricExtents.MinPoint);
                ThParkingStallUtils.Transform(transformer, frames.ToCollection());
                ThParkingStallUtils.Transform(transformer, chargerBlocks.ToCollection());

                frames.ForEach(frame =>
                {
                    var points = ThParkingStallUtils.SelectCrossingPolygon(frame, chargerBlocks).Select(o => o.Position).ToList();
                    var groupingService = new ThChargerGroupingService();
                    var results = groupingService.Grouping(points, Point3d.Origin, MaxPoint);

                    var k = 0;
                    results.ForEach(result =>
                    {
                        if (result.Count == 0 || result.Count > 9)
                        {
                            return;
                        }

                        var pline = ThMinimumPolylineService.CreatePolyline(result, layerId, k);

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
