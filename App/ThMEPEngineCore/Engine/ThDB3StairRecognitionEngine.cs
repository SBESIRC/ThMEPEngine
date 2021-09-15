using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDB3StairExtractionEngine : ThBuildingElementExtractionEngine
    {
        public override void Extract(Database database)
        {
            var stairVisitor = new ThDB3StairExtractionVisitor();
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(stairVisitor);
            extractor.Extract(database);
            Results = new List<ThRawIfcBuildingElementData>();
            Results.AddRange(stairVisitor.Results);
        }
    }

    public class ThDB3StairRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection frame)
        {
            var engine = new ThDB3StairExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, frame);
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection frame)
        {
            var stairBlocks = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (frame.Count > 0)
            {
                var blocksSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                foreach (var filterObj in blocksSpatialIndex.SelectCrossingPolygon(frame))
                {
                    stairBlocks.Add(filterObj as BlockReference);
                }
            }
            else
            {
                stairBlocks = objs;
            }

            foreach (BlockReference block in stairBlocks)
            {
                Elements.Add(CreatIfcStair(block));
            }
        }

        private ThIfcStair CreatIfcStair(BlockReference stair)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var ifcStair = new ThIfcStair();
                ifcStair.SrcBlock = stair;

                var objs = new DBObjectCollection();
                stair.Explode(objs);
                var platforms = objs.Cast<Entity>()
                    .OfType<Curve>()
                    .Where(e => e.Layer.Contains("DEFPOINTS-2"))
                    .ToCollection();
                var rungs = objs.Cast<Entity>()
                    .OfType<Curve>()
                    .Where(e => e.Layer.Contains("AE-STAR"))
                    .Where(e => e.Area > 0.1)
                    .ToCollection();
                var beams = objs.Cast<Entity>()
                    .OfType<Curve>()
                    .Where(e => e.Layer.Contains("S_BEAM"))
                    .ToCollection();
                var down = objs.Cast<Entity>()
                    .OfType<DBText>()
                    .Where(e => e.TextString == "下")
                    .Where(e => e.Layer.Contains("DEFPOINTS-1"));
                var up = objs.Cast<Entity>()
                    .OfType<DBText>()
                    .Where(e => e.TextString == "上")
                    .Where(e => e.Layer.Contains("DEFPOINTS-1"));
                var coverage = objs.Cast<Entity>()
                    .OfType<Wipeout>()
                    .Where(e => e.Layer == "0")
                    .ToCollection();

                var beamCenter = beams.GeometricExtents().CenterPoint();
                var platform = new Polyline();
                var halfPlatform = new Polyline();
                if (platforms.Count == 0)
                {
                    ifcStair.Storey = "首层";
                    if (rungs.Count == 1)
                    {
                        ifcStair.StairType = "双跑楼梯";
                    }
                    else if (rungs.Count == 2)
                    {
                        ifcStair.StairType = "剪刀楼梯";
                    }

                    return ifcStair;
                }
                else if (platforms.Count == 1)
                {
                    ifcStair.Storey = "中间层";
                    ifcStair.StairType = "剪刀楼梯";
                    platform = platforms[0] as Polyline;
                    if (down.Any())
                    {
                        ifcStair.PlatForLayout.Add(GetLatoutList(platform, beamCenter, down.ToCollection(), true, beams));
                    }
                    else if (up.Any())
                    {
                        ifcStair.PlatForLayout.Add(GetLatoutList(platform, beamCenter, up.ToCollection(), false, beams));
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else if (platforms.Count == 2)
                {
                    if (coverage.Count == 0)
                    {
                        if (down.ToCollection().Count == 1)
                        {
                            ifcStair.Storey = "顶层";
                            ifcStair.StairType = "双跑楼梯";

                            platform = platforms[0] as Polyline;
                            halfPlatform = platforms[1] as Polyline;
                            if (down.Any())
                            {
                                var downPoint = down.ToCollection().GeometricExtents().CenterPoint();
                                if (platform.Distance(downPoint) > halfPlatform.Distance(downPoint))
                                {
                                    ChangeOrder(ref platform, ref halfPlatform);
                                }
                            }
                            else if (up.Any())
                            {
                                var upPoint = up.ToCollection().GeometricExtents().CenterPoint();
                                if (platform.Distance(upPoint) < halfPlatform.Distance(upPoint))
                                {
                                    ChangeOrder(ref platform, ref halfPlatform);
                                }
                            }
                            ifcStair.PlatForLayout.Add(GetLatoutList(platform, beamCenter, down.ToCollection(), true, beams));
                            ifcStair.HalfPlatForLayout.Add(GetLatoutList(halfPlatform, beamCenter, down.ToCollection(), true, beams));
                        }
                        else if (down.ToCollection().Count == 2)
                        {
                            ifcStair.Storey = "顶层";
                            ifcStair.StairType = "剪刀楼梯";
                            ifcStair.PlatForLayout.Add(GetLatoutList(platforms[0] as Polyline,
                                                       beamCenter, down.ToCollection(), true, beams));
                            ifcStair.PlatForLayout.Add(GetLatoutList(platforms[1] as Polyline,
                                                       beamCenter, down.ToCollection(), true, beams));
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else if (coverage.Count == 1)
                    {
                        ifcStair.Storey = "中间层";
                        ifcStair.StairType = "双跑楼梯";
                        platform = platforms[0] as Polyline;
                        halfPlatform = platforms[1] as Polyline;
                        if (down.Any())
                        {
                            var downPoint = down.ToCollection().GeometricExtents().CenterPoint();
                            if (platform.Distance(downPoint) > halfPlatform.Distance(downPoint))
                            {
                                ChangeOrder(ref platform, ref halfPlatform);
                            }
                        }
                        else if (up.Any())
                        {
                            var upPoint = up.ToCollection().GeometricExtents().CenterPoint();
                            if (platform.Distance(upPoint) < halfPlatform.Distance(upPoint))
                            {
                                ChangeOrder(ref platform, ref halfPlatform);
                            }
                        }
                        var temp = down.ToCollection();
                        ifcStair.PlatForLayout.Add(GetLatoutList(platform, beamCenter, down.ToCollection(), true, beams));
                        ifcStair.HalfPlatForLayout.Add(GetLatoutList(halfPlatform, beamCenter, down.ToCollection(), true, beams));
                    }
                    else if (coverage.Count == 2)
                    {
                        ifcStair.Storey = "中间层";
                        ifcStair.StairType = "剪刀楼梯";
                        ifcStair.PlatForLayout.Add(GetLatoutList(platforms[0] as Polyline,
                                                       beamCenter, down.ToCollection(), true, beams));
                        ifcStair.PlatForLayout.Add(GetLatoutList(platforms[1] as Polyline,
                                                   beamCenter, down.ToCollection(), true, beams));
                    }
                }
                return ifcStair;
            }
        }

        private Point3d GetMinPoint(Point3d point, DBObjectCollection dBObject)
        {
            var minDistance = 0.0;
            var minPoint = new Point3d();
            foreach (Polyline e in dBObject)
            {
                foreach (Point3d vertice in e.Vertices())
                {
                    var distance = vertice.DistanceTo(point);
                    if (minDistance < 0.01)
                    {
                        minDistance = distance;
                        minPoint = vertice;
                    }
                    else
                    {
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            minPoint = vertice;
                        }
                    }
                }
            }
            return minPoint;
        }

        private List<Point3d> GetLatoutList(Polyline platform, Point3d beamCenter, DBObjectCollection labels, bool labelTag, DBObjectCollection beams)
        {
            var vertices = new List<Point3d>();
            var downPoint = new Point3d();
            var upPoint = new Point3d();
            var maxDistance = 0.0;
            Point3d maxPoint;
            foreach (Point3d vertice in platform.Vertices())
            {
                var distance = vertice.DistanceTo(beamCenter);
                if (distance - maxDistance > -10.0)
                {
                    maxDistance = distance;
                    maxPoint = vertice;
                    upPoint = downPoint;
                    downPoint = maxPoint;
                }
            }

            var labelPoint = labels.GeometricExtents().CenterPoint();
            if (labelTag)
            {
                if (downPoint.DistanceTo(labelPoint) > upPoint.DistanceTo(labelPoint))
                {
                    ChangeOrder(ref downPoint, ref upPoint);
                }
            }
            else
            {
                if (downPoint.DistanceTo(labelPoint) < upPoint.DistanceTo(labelPoint))
                {
                    ChangeOrder(ref downPoint, ref upPoint);
                }
            }

            vertices.Add(downPoint);
            vertices.Add(upPoint);
            vertices.Add(GetMinPoint(upPoint, beams));
            vertices.Add(GetMinPoint(downPoint, beams));
            return vertices;
        }

        private void ChangeOrder(ref Point3d downPoint, ref Point3d upPoint)
        {
            var temp = downPoint;
            downPoint = upPoint;
            upPoint = temp;
        }

        private void ChangeOrder(ref Polyline platform, ref Polyline halfPlatform)
        {
            var temp = platform;
            platform = halfPlatform;
            halfPlatform = temp;
        }
    }
}
