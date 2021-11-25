using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.AFASRegion.Model;
using ThMEPEngineCore.AFASRegion.Model.DetectionRegionGraphModel;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.AFASRegion.Service
{
    public class AFASBeamExtendFactory
    {
        private List<Polyline> ObstacleBoundarys { get; set; }
        private List<AFASBeamContour> Beams { get; set; }
        public AFASDetector detectorType { get; set; }
        private ThCADCoreNTSSpatialIndex thobstacleSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex thbeamsSpatialIndex { get; set; }

        public void Initialize(List<ThIfcBuildingElement> columns, List<ThIfcBuildingElement> beams, List<ThIfcBuildingElement> walls, List<Polyline> holes)
        {
            //获取柱
            var thcolumns = columns.Select(o => o.Outline).Cast<Polyline>();

            //获取梁
            var thBeams = beams.Cast<ThIfcLineBeam>().ToList();

            //获取墙
            var thwalls = walls.Select(o => o.Outline).Cast<Polyline>();


            List<Polyline> obstacle = new List<Polyline>();
            obstacle.AddRange(thcolumns);
            obstacle.AddRange(thwalls);
            obstacle.AddRange(holes);
            this.ObstacleBoundarys = obstacle;
            Beams = new List<AFASBeamContour>();
            Beams.AddRange(thBeams.Select(o => BeamConversion(o)));

            thobstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(obstacle.ToCollection());
        }

        public void ExtendBeamCenterLine()
        {
            DBObjectCollection objs = new DBObjectCollection();
            this.ObstacleBoundarys.ForEach(o => objs.Add(o));
            this.Beams.ForEach(o => objs.Add(o.BeamCenterline));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);

            Beams.ForEach(o =>
            {
                //梁的两端创建长探针，去分别探索自己的边界 
                Point3d NewStartPoint;
                Point3d NewEndPoint;

                //Start
                Vector3d direction = o.EndPoint.GetVectorTo(o.StartPoint).GetNormal();
                Point3d pt1 = o.StartPoint + direction.MultiplyBy(1500.0);
                Point3d pt2 = o.StartPoint - direction.MultiplyBy(600.0);
                Line LongProbe = new Line(pt1, pt2);
                var StartResult = spatialIndex.SelectFence(LongProbe);
                StartResult.Remove(o.BeamBoundary);
                var Intersection = new List<Point3d>();
                foreach (Entity OtherBounder in StartResult.Cast<Entity>())
                {
                    var pts = new Point3dCollection();
                    LongProbe.IntersectWith(OtherBounder, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    Intersection.AddRange(pts.Cast<Point3d>());
                }
                if (Intersection.Count > 0)
                {
                    var ReverseCompensation = Intersection.Where(x => x.GetVectorTo(o.StartPoint).GetAngleTo(direction) < 1.0);
                    if (ReverseCompensation.Count() > 0)
                    {
                        Point3d IntersectionPoint = ReverseCompensation.OrderBy(x => x.DistanceTo(o.StartPoint)).First();
                        NewStartPoint = o.StartPoint - direction * (IntersectionPoint.DistanceTo(o.StartPoint) - 5);
                    }
                    else
                    {
                        Point3d IntersectionPoint = Intersection.OrderBy(x => x.DistanceTo(o.StartPoint)).First();
                        NewStartPoint = o.StartPoint + direction * (IntersectionPoint.DistanceTo(o.StartPoint) + 5);
                    }
                }
                else
                {
                    NewStartPoint = o.StartPoint;
                }

                //End
                direction = direction.Negate();
                pt1 = o.EndPoint + direction.MultiplyBy(1500.0);
                pt2 = o.EndPoint - direction.MultiplyBy(600.0);
                LongProbe = new Line(pt1, pt2);
                var EndResult = spatialIndex.SelectFence(LongProbe);
                EndResult.Remove(o.BeamBoundary);
                Intersection = new List<Point3d>();
                foreach (Entity OtherBounder in EndResult.Cast<Entity>())
                {
                    if (OtherBounder is Line line && line.IsParallelToEx(LongProbe))
                        continue;
                    var pts = new Point3dCollection();
                    LongProbe.IntersectWith(OtherBounder, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    Intersection.AddRange(pts.Cast<Point3d>());
                }
                if (Intersection.Count > 0)
                {
                    var ReverseCompensation = Intersection.Where(x => x.GetVectorTo(o.EndPoint).GetAngleTo(direction) < 1.0);
                    if (ReverseCompensation.Count() > 0)
                    {
                        Point3d IntersectionPoint = ReverseCompensation.OrderBy(x => x.DistanceTo(o.EndPoint)).First();
                        NewEndPoint = o.EndPoint - direction * (IntersectionPoint.DistanceTo(o.EndPoint) - 5);
                    }
                    else
                    {
                        Point3d IntersectionPoint = Intersection.OrderBy(x => x.DistanceTo(o.EndPoint)).First();
                        NewEndPoint = o.EndPoint + direction * (IntersectionPoint.DistanceTo(o.EndPoint) + 5);
                    }
                }
                else
                {
                    NewEndPoint = o.EndPoint;
                }
                o.StartPoint = NewStartPoint;
                o.EndPoint = NewEndPoint;
                o.BeamCenterline = CreatBeamCenterLine(NewStartPoint, NewEndPoint);
                o.BeamBoundary = CreatBeamOutline(o.BeamCenterline, o.Width);
            });
            this.Beams = Beams;
            thbeamsSpatialIndex = new ThCADCoreNTSSpatialIndex(Beams.Select(o => o.BeamBoundary).ToCollection());
        }

        public List<AFASBeamContour> ExtendBeamCenterLineFromRoom(Entity room, List<Polyline> obstacles, List<AFASBeamContour> beamcenterLines)
        {
            List<AFASBeamContour> newBeamLines = new List<AFASBeamContour>();
            beamcenterLines.ForEach(o => newBeamLines.Add(o.Clone()));
            DBObjectCollection objs = new DBObjectCollection();
            objs.Add(room);
            obstacles.ForEach(o => objs.Add(o));

            newBeamLines.ForEach(o => objs.Add(o.BeamCenterline));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);

            newBeamLines.ForEach(o =>
            {
                //梁的两端创建长探针，去分别探索自己的边界
                Point3d NewStartPoint;
                Point3d NewEndPoint;

                //Start
                Vector3d direction = o.EndPoint.GetVectorTo(o.StartPoint).GetNormal();
                Point3d pt1 = o.StartPoint + direction.MultiplyBy(1500.0);
                Point3d pt2 = o.StartPoint - direction.MultiplyBy(10);
                Line LongProbe = new Line(pt1, pt2);
                var StartResult = spatialIndex.SelectFence(LongProbe);
                StartResult.Remove(o.BeamBoundary);
                var Intersection = new List<Point3d>();
                foreach (Entity OtherBounder in StartResult.Cast<Entity>())
                {
                    var pts = new Point3dCollection();
                    LongProbe.IntersectWith(OtherBounder, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    Intersection.AddRange(pts.Cast<Point3d>());
                }
                if (Intersection.Count > 0)
                {
                    var ReverseCompensation = Intersection.Where(x => x.GetVectorTo(o.StartPoint).GetAngleTo(direction) < 1.0);
                    if (ReverseCompensation.Count() > 0)
                    {
                        Point3d IntersectionPoint = ReverseCompensation.OrderBy(x => x.DistanceTo(o.StartPoint)).First();
                        NewStartPoint = o.StartPoint - direction * (IntersectionPoint.DistanceTo(o.StartPoint) - 5);
                    }
                    else
                    {
                        Point3d IntersectionPoint = Intersection.OrderBy(x => x.DistanceTo(o.StartPoint)).First();
                        NewStartPoint = o.StartPoint + direction * (IntersectionPoint.DistanceTo(o.StartPoint) + 5);
                    }
                }
                else
                {
                    NewStartPoint = o.StartPoint;
                }

                //End
                direction = direction.Negate();
                pt1 = o.EndPoint + direction.MultiplyBy(1500.0);
                pt2 = o.EndPoint - direction.MultiplyBy(10.0);
                LongProbe = new Line(pt1, pt2);
                var EndResult = spatialIndex.SelectFence(LongProbe);
                EndResult.Remove(o.BeamBoundary);
                Intersection = new List<Point3d>();
                foreach (Entity OtherBounder in EndResult.Cast<Entity>())
                {
                    if (OtherBounder is Line line && line.IsParallelToEx(LongProbe))
                        continue;
                    var pts = new Point3dCollection();
                    LongProbe.IntersectWith(OtherBounder, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    Intersection.AddRange(pts.Cast<Point3d>());
                }
                if (Intersection.Count > 0)
                {
                    var ReverseCompensation = Intersection.Where(x => x.GetVectorTo(o.EndPoint).GetAngleTo(direction) < 1.0);
                    if (ReverseCompensation.Count() > 0)
                    {
                        Point3d IntersectionPoint = ReverseCompensation.OrderBy(x => x.DistanceTo(o.EndPoint)).First();
                        NewEndPoint = o.EndPoint - direction * (IntersectionPoint.DistanceTo(o.EndPoint) - 5);
                    }
                    else
                    {
                        Point3d IntersectionPoint = Intersection.OrderBy(x => x.DistanceTo(o.EndPoint)).First();
                        NewEndPoint = o.EndPoint + direction * (IntersectionPoint.DistanceTo(o.EndPoint) + 5);
                    }
                }
                else
                {
                    NewEndPoint = o.EndPoint;
                }

                o.BeamCenterline = CreatBeamCenterLine(NewStartPoint, NewEndPoint);
                o.BeamBoundary = CreatBeamOutline(o.BeamCenterline, o.Width);
            });
            return newBeamLines.ToList();
        }

        /// <summary>
        /// 可探测区域
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public DBObjectCollection DetectionRegions(Entity frame)
        {
            //获取障碍物
            var obstacles = thobstacleSpatialIndex.SelectCrossingPolygon(frame).Cast<Polyline>().ToList();

            //获取梁
            var allbeams = thbeamsSpatialIndex.SelectCrossingPolygon(frame).Cast<Polyline>();
            var internalbeams = thbeamsSpatialIndex.SelectWindowPolygon(frame).Cast<Polyline>();
            var intersectbeams = allbeams.Except(internalbeams);
            var selectedBeam = new List<AFASBeamContour>();
            //房间内梁过滤边界
            Beams.Where(o => internalbeams.Contains(o.BeamBoundary)).ForEach(o =>
            {
                var beamframe = GetBeamHorizontalFrame(o);
                if (!beamframe.IsIntersects(frame))
                {
                    selectedBeam.Add(o);
                }
            });
            
            //房间边梁过滤边界
            Beams.Where(o => intersectbeams.Contains(o.BeamBoundary)).ForEach(o =>
            {
                var beamframe = GetBeamVerticalFrame(o);
                if (IsNonWeltBeam(frame, beamframe))
                {
                    selectedBeam.Add(o);
                }
            });

            var extendBeam = ExtendBeamCenterLineFromRoom(frame, obstacles, selectedBeam);
            //探测区域Lines
            DBObjectCollection DetectionAreaLines = new DBObjectCollection();
            frame.ToNTSPolygonalGeometry().ToDbPolylines().ForEach(o => DetectionAreaLines.Add(o));//添加房间框线
            obstacles.ForEach(o => DetectionAreaLines.Add(o));//添加障碍物
            extendBeam.Where(o=>o.BeamType==BeamType.HighBeam).ForEach(o => DetectionAreaLines.Add(o.BeamCenterline));//添加梁中心线
            var polygons = DetectionAreaLines.PolygonsEx();

            var obstacleBufferArea = obstacles.Cast<Polyline>().Select(o => o.Buffer(10)[0] as Polyline);//障碍物空间（柱，剪力墙,结构墙)
            var beamBufferArea = extendBeam.Select(o => o.BeamBoundary).Cast<Polyline>().Select(o => o.Buffer(10)[0] as Polyline);//梁
            var IsMPolygonRoom = false;
            var Holes = new List<Polyline>();
            var RoomBoundary = new Polyline();
            if(frame is Polyline plspace)
            {
                RoomBoundary = plspace.Buffer(10)[0] as Polyline;
            }
            else if(frame is MPolygon space)
            {
                IsMPolygonRoom = true;
                Holes = space.Holes().Select(o => o.Buffer(10)[0] as Polyline).ToList();
                RoomBoundary = space.Shell().Buffer(10)[0] as Polyline;
            }

            List<Entity> BuildSpace = new List<Entity>();
            List<Polyline> beaminternaSpace = new List<Polyline>();
            foreach (Entity item in polygons)
            {
                if (item is Polyline polyline && RoomBoundary.Contains(polyline))
                {
                    if (obstacleBufferArea.Any(o => o.Contains(polyline)))
                    {
                        //过滤掉墙，柱
                        continue;
                    }
                    else if (IsMPolygonRoom && Holes.Any(o => o.Contains(polyline)))
                    {
                        //过滤掉MPolygon的洞
                        continue;
                    }
                    else if (beamBufferArea.Any(o => o.Contains(polyline)))
                    {
                        //合并梁内区间
                        beaminternaSpace.Add(polyline);
                    }
                    else
                    {
                        BuildSpace.Add(item);
                    }
                }
                else if (item is MPolygon mPolygon)
                {
                    BuildSpace.Add(item);
                }
            }
            if (beaminternaSpace.Count > 0)
            {
                var BuildSpaceClone = BuildSpace.Clone().ToList();
                var BuildSpatialIndex = new ThCADCoreNTSSpatialIndex(BuildSpaceClone.ToCollection());
                beaminternaSpace.ForEach(o =>
                {
                    Polyline space = o.Buffer(1)[0] as Polyline;
                    var AdjacentObjs = BuildSpatialIndex.SelectCrossingPolygon(space);
                    if (AdjacentObjs.Count > 0)
                    {
                        var index = BuildSpaceClone.IndexOf(AdjacentObjs[0] as Entity);
                        DBObjectCollection objs = new DBObjectCollection();
                        objs.Add(BuildSpace[index]);
                        objs.Add(o);
                        BuildSpace[index] = objs.UnionPolygons().Cast<Polyline>().OrderByDescending(x => x.Area).First();
                    }
                });
            }
            return BuildSpace.ToCollection();
            //return MergeDetectionRegion(BuildSpace, extendBeam).ToCollection();
        }

        private List<Entity> MergeDetectionRegion(List<Entity> buildSpace, List<AFASBeamContour> beam)
        {
            DetectionRegionGraphModel graph = new DetectionRegionGraphModel(beam, detectorType);
            graph.BuildAFASGraph(buildSpace);
            return graph.GetMergeRegion();
            //graph.DrawGroup();
        }

        /// <summary>
        /// 判断不是贴边梁
        /// </summary>
        /// <param name="frame">房间Entity</param>
        /// <param name="beamframe"></param>
        /// <returns></returns>
        private bool IsNonWeltBeam(Entity frame, List<Line> beamframe)
        {
            if (frame is Polyline polyline)
            {
                return (beamframe[0].IsIntersects(polyline) || polyline.Contains(beamframe[0])) && (beamframe[1].IsIntersects(frame) || polyline.Contains(beamframe[1]));
            }
            else if (frame is MPolygon mPolygon)
            {
                var roomShell = mPolygon.Shell();
                var roomHoles = mPolygon.Holes();
                return (beamframe[0].IsIntersects(mPolygon) || ((roomShell.Contains(beamframe[0]) && !roomHoles.Any(o => o.Contains(beamframe[0])))))
                    && (beamframe[1].IsIntersects(mPolygon) || ((roomShell.Contains(beamframe[1]) && !roomHoles.Any(o => o.Contains(beamframe[1])))));
            }
            else
            {
                return false;
            }
        }

        private AFASBeamContour BeamConversion(ThIfcLineBeam beam)
        {
            Line beamCenterline = CreatBeamCenterLine(beam.StartPoint, beam.EndPoint);
            return new AFASBeamContour()
            {
                Width = beam.Width,
                BottomElevation = beam.BottomElevation(),
                Height = beam.Height,
                BeamCenterline = beamCenterline,
                StartPoint = beam.StartPoint,
                EndPoint = beam.EndPoint,
                BeamBoundary = beam.Outline as Polyline,
            };
        }

        private List<Line> GetBeamVerticalFrame(AFASBeamContour beamContour)
        {
            Vector3d direction = beamContour.StartPoint.GetVectorTo(beamContour.EndPoint);
            Vector3d perpendDir = direction.GetPerpendicularVector();
            Point3d pt1 = beamContour.StartPoint - perpendDir.GetNormal().MultiplyBy(beamContour.Width / 2.0 + 200.0);
            Point3d pt2 = beamContour.StartPoint + perpendDir.GetNormal().MultiplyBy(beamContour.Width / 2.0 + 200.0);
            Line line1 = new Line(pt1, pt1 + direction.GetNormal().MultiplyBy(beamContour.StartPoint.DistanceTo(beamContour.EndPoint)));
            Line line2 = new Line(pt2, pt2 + direction.GetNormal().MultiplyBy(beamContour.StartPoint.DistanceTo(beamContour.EndPoint)));
            return new List<Line>() { line1, line2 };
        }

        private Line GetBeamHorizontalFrame(AFASBeamContour beamContour)
        {
            Vector3d direction = beamContour.StartPoint.GetVectorTo(beamContour.EndPoint);
            Vector3d perpendDir = direction.GetPerpendicularVector();
            Point3d center = beamContour.BeamCenterline.GetCenter();
            Point3d pt1 = center - perpendDir.GetNormal().MultiplyBy(beamContour.Width / 2.0 + 200.0);
            Point3d pt2 = center + perpendDir.GetNormal().MultiplyBy(beamContour.Width / 2.0 + 200.0);
            return new Line(pt1, pt2);
        }

        private Line CreatBeamCenterLine(Point3d StartPoint, Point3d EndPoint)
        {
            return new Line(StartPoint, EndPoint);
        }

        private Polyline CreatBeamOutline(Line line, double width)
        {
            Vector3d direction = line.StartPoint.GetVectorTo(line.EndPoint);
            Vector3d perpendDir = direction.GetPerpendicularVector();
            Point3d pt1 = line.StartPoint - perpendDir.GetNormal().MultiplyBy(width / 2.0);
            Point3d pt2 = line.StartPoint + perpendDir.GetNormal().MultiplyBy(width / 2.0);
            Point3d pt3 = pt2 + direction.GetNormal().MultiplyBy(line.StartPoint.DistanceTo(line.EndPoint));
            Point3d pt4 = pt1 + direction.GetNormal().MultiplyBy(line.StartPoint.DistanceTo(line.EndPoint));
            Point3dCollection pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };
            return pts.CreatePolyline();
        }
    }
}
