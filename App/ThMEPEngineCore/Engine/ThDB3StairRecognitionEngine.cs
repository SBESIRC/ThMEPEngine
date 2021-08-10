using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
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
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            var engine = new ThDB3StairExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }

        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            var stairBlocks = new DBObjectCollection();
            var objs = datas.Select(o => o.Geometry).ToCollection();
            if (polygon.Count > 0)
            {
                var blocksSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                foreach (var filterObj in blocksSpatialIndex.SelectCrossingPolygon(polygon))
                {
                    stairBlocks.Add(filterObj as BlockReference);
                }
            }
            else
            {
                stairBlocks = objs;
            }

            foreach(BlockReference block in stairBlocks)
            {
                var ifcStair = new ThIfcStair
                {
                    PlatForLayout = GetPlatForLayout(block, true),
                    HalfPlatForLayout = GetHalfPlatForLayout(GetPlatForLayout(block, false))
                };
                Elements.Add(ifcStair);
            }
        }

        private List<Point3d> GetPlatForLayout(BlockReference stair, bool isPlat)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objs = new DBObjectCollection();
                stair.Explode(objs);
                var platforms = objs.Cast<Entity>()
                    .OfType<Curve>()
                    .Where(e => e.Layer.Contains("DEFPOINTS-2"))
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

                var beamCenter = beams.GeometricExtents().CenterPoint();
                var platform = new Polyline();
                var halfPlatform = new Polyline();
                var listPlatform = new List<Point3d>();
                if (platforms.Count == 0)
                {
                    return new List<Point3d>();
                }
                else if (platforms.Count == 1)
                {
                    platform = platforms[0] as Polyline;
                    if (down.Any())
                    {
                        listPlatform = GetLatoutList(platform, beamCenter, down.ToCollection(), true, beams);
                    }
                    else if (up.Any())
                    {
                        listPlatform = GetLatoutList(platform, beamCenter, up.ToCollection(), false, beams);
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else if (platforms.Count == 2)
                {
                    platform = platforms[0] as Polyline;
                    halfPlatform = platforms[1] as Polyline;
                    if (down.Any())
                    {
                        var downPoint = down.ToCollection().GeometricExtents().CenterPoint();
                        if (platform.Distance(downPoint) > halfPlatform.Distance(downPoint))
                        {
                            changeOrder(platform, halfPlatform);
                        }
                        if (isPlat)
                        {
                            listPlatform = GetLatoutList(platform, beamCenter, down.ToCollection(), true, beams);
                        }
                        else
                        {
                            listPlatform = GetLatoutList(halfPlatform, beamCenter, down.ToCollection(), true, beams);
                        }
                    }
                    else if (up.Any())
                    {
                        var upPoint = up.ToCollection().GeometricExtents().CenterPoint();
                        if (platform.Distance(upPoint) < halfPlatform.Distance(upPoint))
                        {
                            changeOrder(platform, halfPlatform);
                        }
                        if (isPlat)
                        {
                            listPlatform = GetLatoutList(platform, beamCenter, up.ToCollection(), false, beams);
                        }
                        else
                        {
                            listPlatform = GetLatoutList(halfPlatform, beamCenter, up.ToCollection(), false, beams);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
                return listPlatform;
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
                    changeOrder(downPoint, upPoint);
                }
            }
            else
            {
                if (downPoint.DistanceTo(labelPoint) < upPoint.DistanceTo(labelPoint))
                {
                    changeOrder(downPoint, upPoint);
                }
            }

            vertices.Add(downPoint);
            vertices.Add(upPoint);
            vertices.Add(GetMinPoint(upPoint, beams));
            vertices.Add(GetMinPoint(downPoint, beams));
            return vertices;
        }

        private List<Point3d> GetHalfPlatForLayout(List<Point3d> vertices)
        {
            if (vertices.Count == 0)
            {
                return vertices;
            }
            var newVertices = new List<Point3d>
            {
                vertices[1],
                vertices[0],
                vertices[3],
                vertices[2]
            };
            return newVertices;
        }

        private void changeOrder(Point3d downPoint, Point3d upPoint)
        {
            var temp = downPoint;
            downPoint = upPoint;
            upPoint = temp;
        }

        private void changeOrder(Polyline platform, Polyline halfPlatform)
        {
            var temp = platform;
            platform = halfPlatform;
            halfPlatform = temp;
        }
    }
}
