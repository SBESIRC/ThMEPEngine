using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model;
using DotNetARX;

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

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
        }
    }

    public class ThDB3StairRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public List<Polyline> Rooms { get; set; }

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
                Elements.Add(CreatIfcStair(block, Rooms));
            }
        }

        private ThIfcStair CreatIfcStair(BlockReference stair, List<Polyline> rooms)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var ifcStair = new ThIfcStair();
                ifcStair.SrcBlock = stair;

                var spatialIndex = new ThCADCoreNTSSpatialIndex(rooms.ToCollection());
                var blockOBB = GetBlockOBB(stair);
                var frameTidal = spatialIndex.SelectCrossingPolygon(blockOBB.Vertices());
                if (frameTidal.Count == 0)
                {
                    return ifcStair;
                }
                var objs = new DBObjectCollection();
                stair.Explode(objs);
                var platforms = objs.OfType<Polyline>()
                    .Where(e => e.Layer.Contains("DEFPOINTS-2"))
                    .Where(e => e.Area > 0.8e6)
                    .Select(e => e.OBB())
                    .ToCollection();
                var rungs = objs.OfType<Curve>()
                    .Where(e => e.Layer.Contains("AE-STAR"))
                    .Where(e => e.Area > 0.1)
                    .ToCollection();
                var beams = objs.OfType<Curve>()
                    .Where(e => e.Layer.Contains("S_BEAM"))
                    .ToCollection();
                var down = objs.OfType<DBText>()
                    .Where(e => e.TextString == "下")
                    .Where(e => e.Layer.Contains("DEFPOINTS-1"));
                var up = objs.OfType<DBText>()
                    .Where(e => e.TextString == "上")
                    .Where(e => e.Layer.Contains("DEFPOINTS-1"));
                var coverage = objs.OfType<Wipeout>()
                    .Where(e => e.Layer == "0")
                    .ToCollection();

                var blockCenter = blockOBB.GeometricExtents.CenterPoint();
                var platform = new Polyline();
                var halfPlatform = new Polyline();

                if (platforms.Count == 0)
                {
                    var frame = new Polyline();
                    if (frameTidal.Count > 0)
                    {
                        frame = frameTidal.GeometricExtents().ToRectangle();
                    }
                    else
                    {
                        return ifcStair;
                    }

                    ifcStair.Storey = "首层";
                    if (rungs.Count == 1)
                    {
                        ifcStair.StairType = "双跑楼梯";
                        ifcStair.PlatForLayout.Add(GetLayoutList(frame, beams));
                    }
                    else if (rungs.Count == 2)
                    {
                        ifcStair.StairType = "剪刀楼梯";
                        ifcStair.PlatForLayout = GetLayoutList(frame, beams, up.ToCollection());
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
                        var scrPlat = GetLayoutList(platform, blockCenter, down.ToCollection(), true, beams);
                        ifcStair.PlatForLayout.Add(scrPlat);
                    }
                    else if (up.Any())
                    {
                        var scrPlat = GetLayoutList(platform, blockCenter, up.ToCollection(), false, beams);
                        ifcStair.PlatForLayout.Add(scrPlat);
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
                            ifcStair.Rungs = rungs;

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

                            ifcStair.PlatForLayout.Add(GetLayoutList(platform, blockCenter, down.ToCollection(), true, beams));
                            ifcStair.HalfPlatForLayout.Add(GetLayoutList(halfPlatform, blockCenter, down.ToCollection(), true, beams));
                        }
                        else if (down.ToCollection().Count == 2)
                        {
                            ifcStair.Storey = "顶层";
                            ifcStair.StairType = "剪刀楼梯";
                            ifcStair.Rungs = rungs;
                            ifcStair.PlatForLayout.Add(GetLayoutList(platforms[0] as Polyline,
                                blockCenter, down.ToCollection(), true, beams));
                            ifcStair.PlatForLayout.Add(GetLayoutList(platforms[1] as Polyline,
                                blockCenter, down.ToCollection(), true, beams));
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

                        var platformList = IntersectArea(platform, frameTidal);
                        var halfPlatformList = IntersectArea(halfPlatform, frameTidal);
                        platformList.ForEach(x =>
                        {
                            ifcStair.PlatForLayout.Add(GetLayoutList(x, blockCenter, down.ToCollection(), true, beams));
                        });
                        halfPlatformList.ForEach(x =>
                        {
                            ifcStair.HalfPlatForLayout.Add(GetLayoutList(x, blockCenter, down.ToCollection(), true, beams));
                        });
                    }
                    else if (coverage.Count == 2)
                    {
                        ifcStair.Storey = "中间层";
                        ifcStair.StairType = "剪刀楼梯";

                        var platformList = IntersectArea(platforms, frameTidal);
                        platformList.ForEach(x =>
                        {
                            var plat = GetLayoutList(x, blockCenter, down.ToCollection(), true, beams);
                            ifcStair.PlatForLayout.Add(plat);
                        });
                    }
                }
                return ifcStair;
            }
        }

        private List<Point3d> GetLayoutList(Polyline platform, Point3d blockCenter, DBObjectCollection labels, bool labelTag,
            DBObjectCollection beams)
        {
            var vertices = new List<Point3d>();
            var downPoint = new Point3d();
            var upPoint = new Point3d();
            var ptDict = new List<Tuple<Point3d, double>>();
            foreach (Point3d vertice in platform.Vertices())
            {
                ptDict.Add(Tuple.Create(vertice, vertice.DistanceTo(GetClosePoint(vertice, beams)) + vertice.DistanceTo(blockCenter)));
            }
            ptDict = ptDict.OrderByDescending(x => x.Item2).ToList();
            upPoint = ptDict[0].Item1;
            for (int i = 1; i < ptDict.Count; i++)
            {
                if (ptDict[i].Item1.DistanceTo(ptDict[0].Item1) > 1.0)
                {
                    downPoint = ptDict[i].Item1;
                    break;
                }
            }

            var closeLabelPt = labels.OfType<Entity>().First().GeometricExtents.CenterPoint();
            var closeLabelDist = platform.Distance(closeLabelPt);
            for(int i =1;i< labels.Count;i++)
            {
                var labelPt = labels.OfType<Entity>().ToList()[i].GeometricExtents.CenterPoint();
                var closeDist = platform.Distance(labelPt);
                if (closeDist < closeLabelDist)
                {
                    closeLabelDist = closeDist;
                    closeLabelPt = labelPt;
                }
            }

            if (labelTag)
            {
                if (downPoint.DistanceTo(closeLabelPt) > upPoint.DistanceTo(closeLabelPt))
                {
                    ChangeOrder(ref downPoint, ref upPoint);
                }
            }
            else
            {
                if (downPoint.DistanceTo(closeLabelPt) < upPoint.DistanceTo(closeLabelPt))
                {
                    ChangeOrder(ref downPoint, ref upPoint);
                }
            }

            vertices.Add(downPoint);
            vertices.Add(upPoint);
            vertices.Add(GetClosePoint(upPoint, beams));
            vertices.Add(GetClosePoint(downPoint, beams));
            return vertices;
        }

        private List<Point3d> GetLayoutList(Polyline frame, DBObjectCollection beams)
        {
            var center = frame.GetCentroidPoint();
            var maxDistance = 0.0;
            var maxPoint = new Point3d();
            foreach (Polyline e in beams)
            {
                foreach (Point3d vertice in e.Vertices())
                {
                    var distance = vertice.DistanceTo(center);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        maxPoint = vertice;
                    }
                }
            }

            var thirdPoint = maxPoint;
            var secondPoint = GetClosePoint(thirdPoint, frame.Vertices());
            var firstPoint = GetClosePoint(secondPoint, frame.Vertices());
            var forthPoint = ToPoint3d(GetVector(Point3d.Origin, thirdPoint) + GetVector(secondPoint, firstPoint));

            return new List<Point3d> { firstPoint, secondPoint, thirdPoint, forthPoint };
        }

        private List<List<Point3d>> GetLayoutList(Polyline frame, DBObjectCollection beams, DBObjectCollection labels)
        {
            var platForLayout = new List<List<Point3d>>();
            foreach (Entity e in labels)
            {
                if (e.Bounds.HasValue)
                {
                    var labelPoint = e.GeometricExtents.CenterPoint();
                    var downPoint = new Point3d();
                    var upPoint = new Point3d();
                    var maxDistance = 0.0;
                    Point3d maxPoint;
                    foreach (Point3d vertice in frame.Vertices())
                    {
                        var distance = vertice.DistanceTo(labelPoint);
                        if (distance - maxDistance > -10.0)
                        {
                            maxDistance = distance;
                            maxPoint = vertice;
                            downPoint = upPoint;
                            upPoint = maxPoint;
                        }
                    }
                    var platform = new List<Point3d>
                    {
                        upPoint,
                        downPoint,
                        GetClosePoint(downPoint, beams),
                        GetClosePoint(upPoint, beams)
                    };

                    platForLayout.Add(platform);
                }
            }
            return platForLayout;
        }

        private Point3d GetClosePoint(Point3d point, DBObjectCollection dBObject)
        {
            var minDistance = double.MaxValue;
            var minPoint = new Point3d();
            foreach (Polyline e in dBObject)
            {
                var closePt = e.GetClosePoint(point);
                var distance = closePt.DistanceTo(point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minPoint = closePt;
                }
            }
            return minPoint;
        }

        private Point3d GetClosePoint(Point3d point, Point3dCollection vertices)
        {
            var minDistance = double.MaxValue;
            var minPoint = new Point3d();
            foreach (Point3d vertice in vertices)
            {
                var distance = vertice.DistanceTo(point);
                if (distance < minDistance && distance > 1.0)
                {
                    minDistance = distance;
                    minPoint = vertice;
                }
            }

            return minPoint;
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

        private Vector3d GetVector(Point3d starPoint, Point3d endPoint)
        {
            return endPoint.GetAsVector() - starPoint.GetAsVector();
        }

        private Point3d ToPoint3d(Vector3d vector)
        {
            return new Point3d(vector.X, vector.Y, vector.Z);
        }

        private Point3d GetCenter(Point3d first, Point3d second)
        {
            return new Point3d((first.X + second.X) / 2, (first.Y + second.Y) / 2, 0);
        }

        private Polyline GetBlockOBB(BlockReference targetBlock)
        {
            var entities = new DBObjectCollection();
            ThBlockReferenceExtensions.Burst(targetBlock, entities);
            var filters = new DBObjectCollection();
            entities.OfType<Curve>()
                .Where(e => e.Visible && e.Bounds.HasValue)
                .ForEach(e =>
                {
                    filters.Add(e);
                });
            if (filters.Count > 0)
            {
                return filters.GetMinimumRectangle();
            }
            return new Polyline();
        }

        private List<Polyline> IntersectArea(Polyline platform, DBObjectCollection frameTidal)
        {
            var platformList = new List<Polyline>();
            frameTidal.OfType<Polyline>().ForEach(p =>
            {
                var intersectArea = platform.Intersection(new DBObjectCollection { p })
                    .OfType<Polyline>()
                    .Where(pline => pline.Area > 0.8e6)
                    .OrderBy(pline => pline.Area)
                    .FirstOrDefault();
                if (intersectArea != null)
                {
                    platformList.Add(intersectArea);
                }
            });
            return platformList;
        }

        private List<Polyline> IntersectArea(DBObjectCollection platforms, DBObjectCollection frameTidal)
        {
            var platformList = new List<Polyline>();
            platforms.OfType<Polyline>().ForEach(platform =>
            {
                frameTidal.OfType<Polyline>().ForEach(p =>
                {
                    var intersectArea = platform.Intersection(new DBObjectCollection { p })
                        .OfType<Polyline>()
                        .Where(pline => pline.Area > 0.8e6)
                        .OrderBy(pline => pline.Area)
                        .FirstOrDefault();
                    if (intersectArea != null)
                    {
                        platformList.Add(intersectArea.OBB());
                    }
                });
            });
            return platformList;
        }

        public override void RecognizeEditor(Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
