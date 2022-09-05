using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThInsertBeamHandler
    {
        private double PointTolerance = 1.0;
        private List<ThGeometry> BeamGeos { get; set; }
        private ThCADCoreNTSSpatialIndex BeamMarkSpatialIndex { get; set; }
        public ThInsertBeamHandler(List<ThGeometry> beamGeos,DBObjectCollection beamMarks)
        {
            BeamGeos = beamGeos;
            PointTolerance = ThStructurePlaneCommon.PointTolerance;
            BeamMarkSpatialIndex = new ThCADCoreNTSSpatialIndex(beamMarks);
        }
        public void Handle()
        {
            var lines = GetLines(BeamGeos);
            var groups = FindGroups(lines);
            groups.Where(g=> HasBeamMark(g))
                  .ForEach(g => Repair(g));
        }

        private bool HasBeamMark(InsertBeamInfo info)
        {
            return HasBeamMark(info.Main, info.Branch1Side) || HasBeamMark(info.Main, info.Branch2Side);
        }

        private bool HasBeamMark(Line main, Line branchSide)
        {
            var pt1 = branchSide.StartPoint.GetProjectPtOnLine(main.StartPoint, main.EndPoint);
            var pt2 = branchSide.EndPoint.GetProjectPtOnLine(main.StartPoint, main.EndPoint);
            var pts = new Point3dCollection { branchSide.StartPoint,pt1,pt2, branchSide.EndPoint };
            var outline = pts.CreatePolyline();
            var texts = QueryBeamMark(outline);
            int count = texts.OfType<DBText>().Where(o => outline.EntityContains(o.Position)).Count();
            outline.Dispose();
            return count > 0;
        }

        private void Repair(InsertBeamInfo info)
        {
            /*
             * |   |                       |   |
             * |   |                       |   |                   
             * |------------------         |   |------------------
             * |                      =>   |   |
             * |------------------         |   |------------------
             * |   |                       |   |
             * |   |                       |   |
            */
            var branch1SideGeo = FindGeometry(info.Branch1Side);
            var branch2SideGeo = FindGeometry(info.Branch2Side);
            if (GetLineType(branch1SideGeo) != GetLineType(branch2SideGeo))
            {
                return;
            }
            var branch1Geo = FindGeometry(info.Branch1);
            var branch2Geo = FindGeometry(info.Branch2);
            if(branch1SideGeo == null || branch2SideGeo==null || branch1Geo==null || branch2Geo==null)
            {
                return;
            }            
            if (info.Branch1.StartPoint.IsPointOnLine(info.Main,PointTolerance))
            {
                info.Branch1.StartPoint = info.IntersPt1;
            }
            else
            {
                info.Branch1.EndPoint = info.IntersPt1;
            }
            if (info.Branch2.StartPoint.IsPointOnLine(info.Main, PointTolerance))
            {
                info.Branch2.StartPoint = info.IntersPt2;
            }
            else
            {
                info.Branch2.EndPoint = info.IntersPt2;
            }
            var midPt = info.IntersPt1.GetMidPt(info.IntersPt2);
            if (info.Branch1Side.StartPoint.IsPointOnLine(info.Branch1, PointTolerance))
            {
                info.Branch1Side.StartPoint = midPt;
            }
            else
            {
                info.Branch1Side.EndPoint = midPt;
            }
            if (info.Branch2Side.StartPoint.IsPointOnLine(info.Branch2, PointTolerance))
            {
                info.Branch2Side.StartPoint = midPt;
            }
            else
            {
                info.Branch2Side.EndPoint = midPt;
            }

            // 更新
            branch1Geo.Boundary = info.Branch1;
            branch2Geo.Boundary = info.Branch2;
            branch1SideGeo.Boundary = info.Branch1Side;
            branch2SideGeo.Boundary = info.Branch2Side;
        }

        private DBObjectCollection QueryBeamMark(Polyline outline)
        {
            return BeamMarkSpatialIndex.SelectCrossingPolygon(outline);
        }

        private ThGeometry FindGeometry(Curve curve)
        {
            var index = BeamGeos.Select(o => o.Boundary).ToCollection().IndexOf(curve);
            return index >= 0 ? BeamGeos[index] : null;
        }

        private string GetLineType(ThGeometry geo)
        {
            return geo == null ? "" : geo.Properties.GetLineType();
        }

        private List<InsertBeamInfo> FindGroups(DBObjectCollection lines)
        {
            var grouper = new ThInsertBeamInfoGrouper(lines);
            return grouper.Group();
        }

        private DBObjectCollection GetLines(List<ThGeometry> beamGeos)
        {
            return beamGeos
                .Select(o => o.Boundary)
                .OfType<Line>()
                .ToCollection();
        }
    }
    internal class ThInsertBeamInfoGrouper
    {
        private double PointTolerance = 1.0;
        private const double MinimumBeamWidth = 20.0;
        private const double MaximumBeamWidth = 1000.0;
        private DBObjectCollection Lines { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThInsertBeamInfoGrouper(DBObjectCollection lines)
        {
            Lines = lines;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(lines);
            PointTolerance = ThStructurePlaneCommon.PointTolerance;
        }
        public List<InsertBeamInfo> Group()
        {
            var results = new List<InsertBeamInfo>();
            Lines.OfType<Line>().ForEach(l =>
            {
                var outline = CreateOutline(l, PointTolerance * 2.0);
                var spEnvelop = CreateOutline(l.StartPoint, PointTolerance * 2.0);
                var epEnvelop = CreateOutline(l.EndPoint, PointTolerance * 2.0);

                // 找l的分支线，过滤掉与l平行的线，过滤掉l端点处的线
                var lineObjs = Query(outline).OfType<Line>().Where(o => !o.IsParallelToEx(l)).ToCollection();
                lineObjs = lineObjs.Difference(Query(spEnvelop));
                lineObjs = lineObjs.Difference(Query(epEnvelop));
                // 过滤分支线的端点在l上的线
                lineObjs = lineObjs.OfType<Line>().Where(o => o.StartPoint.IsPointOnLine(l,PointTolerance) ||
                o.EndPoint.IsPointOnLine(l, PointTolerance)).ToCollection();

                // 查找分支上平行的线，
                var canLayoutPairs = FindCanLayoutParallelPair(l, lineObjs);

                // 查找符合规则的结构
                canLayoutPairs.ForEach(pair =>
                {
                    var info = GetBranchSides(l, pair.Item1, pair.Item2);
                    if (info != null)
                    {
                        results.Add(info);
                    }
                });

                outline.Dispose();
                spEnvelop.Dispose();
                epEnvelop.Dispose();
            });

            return results;
        }

        private InsertBeamInfo GetBranchSides(Line main, Line branch1, Line branch2)
        {
            var branch1Side = FindCloseParallelLine(main, branch1);
            if (branch1Side == null)
            {
                return null;
            }
            var branch2Side = FindCloseParallelLine(main, branch2);
            if (branch2Side == null)
            {
                return null;
            }
            if (!ThGeometryTool.IsCollinearEx(branch1Side.StartPoint, branch1Side.EndPoint,
                branch1Side.StartPoint, branch1Side.EndPoint))
            {
                return null;
            }
            var interPt1 = branch1Side.StartPoint.IsPointOnLine(branch1, PointTolerance) ? branch1Side.StartPoint : branch1Side.EndPoint;
            var interPt2 = branch2Side.StartPoint.IsPointOnLine(branch2, PointTolerance) ? branch2Side.StartPoint : branch2Side.EndPoint;

            var intervalOutline = CreateOutline(interPt1, interPt2, PointTolerance * 2);
            var intervalObjs = Query(intervalOutline);
            intervalOutline.Dispose();
            if (intervalObjs.Count == 4 && intervalObjs.Contains(branch1) && intervalObjs.Contains(branch1Side)
                && intervalObjs.Contains(branch2) && intervalObjs.Contains(branch2Side))
            {
                return new InsertBeamInfo()
                {
                    Main = main,
                    Branch1 = branch1,
                    Branch2 = branch2,
                    Branch1Side = branch1Side,
                    Branch2Side = branch2Side,
                    IntersPt1 = interPt1,
                    IntersPt2 = interPt2,
                };
            }
            else
            {
                return null;
            }
        }
        private Line FindCloseParallelLine(Line main, Line branch)
        {
            /*
             *  -----------------------------(main)
             *               |
             *               |---------------(target)
             *               |
             *               |
             *              (branch)
             */
            // target 满足条件: 1、平行于main,且平行距离在一定范围内(<=) 2、target的一个端点在branch内
            var outline = CreateOutline(branch, PointTolerance * 2);
            var objs = Query(outline);
            objs.Remove(main);
            objs.Remove(branch);
            objs = objs.OfType<Line>().Where(o => o.IsParallelToEx(main)).ToCollection();
            objs = objs.OfType<Line>().Where(o => IsBeamIntervalValid(o.ParallelDistanceTo(main))).ToCollection();
            objs = objs.OfType<Line>().Where(o => IsPortOnLine(o,branch)).ToCollection();
            var isStart = branch.StartPoint.IsPointOnLine(main, PointTolerance);
            objs = Sort(branch, objs, isStart);
            outline.Dispose();
            if (objs.Count > 0)
            {
                return objs.OfType<Line>().First();
            }
            else
            {
                return null;
            }
        }
        private List<Tuple<Line, Line>> FindCanLayoutParallelPair(Line main, DBObjectCollection branches)
        {
            var results = new List<Tuple<Line, Line>>();
            branches = Sort(main, branches);
            for (int i = 0; i < branches.Count - 1; i++)
            {
                var first = branches[i] as Line;
                var second = branches[i + 1] as Line;
                if (first.IsParallelToEx(second) && IsBeamIntervalValid(first.ParallelDistanceTo(second)))
                {
                    results.Add(Tuple.Create(first, second));
                }
            }
            return results;
        }
        private DBObjectCollection Sort(Line main, DBObjectCollection branches, bool isCloseStart = true)
        {
            // 对分支上的线进行排序
            var comparePt = isCloseStart ? main.StartPoint : main.EndPoint;
            return branches.OfType<Line>().OrderBy(l =>
            {
                if (l.StartPoint.IsPointOnLine(main,PointTolerance))
                {
                    return comparePt.DistanceTo(l.StartPoint);
                }
                else
                {
                    return comparePt.DistanceTo(l.EndPoint);
                }
            }).ToCollection();
        }
        private bool IsPortOnLine(Line first, Line second)
        {
            return first.StartPoint.IsPointOnLine(second, PointTolerance) ||
                first.EndPoint.IsPointOnLine(second, PointTolerance);
        }
        private DBObjectCollection Query(Polyline polyline)
        {
            return SpatialIndex.SelectCrossingPolygon(polyline);
        }
        private Polyline CreateOutline(Line line, double width)
        {
            return CreateOutline(line.StartPoint, line.EndPoint, width);
        }
        private Polyline CreateOutline(Point3d sp, Point3d ep, double width)
        {
            return ThDrawTool.ToOutline(sp, ep, width);
        }
        private Polyline CreateOutline(Point3d pt, double width)
        {
            return pt.CreateSquare(width);
        }
        private bool IsBeamIntervalValid(double dis)
        {
            return dis >= MinimumBeamWidth && dis <= MaximumBeamWidth;
        }
    }
    class InsertBeamInfo
    {
        /* Main
             * |     |Branch1Side                    
             * |     |                                       
             * |------------------ Branch1    
             * |                       
             * |------------------ Branch2      
             * |     |                      
             * |     |Branch2Side                  
             */
        public Line Main { get; set; }
        public Line Branch1 { get; set; }
        public Line Branch2 { get; set; }
        public Line Branch1Side { get; set; }
        public Line Branch2Side { get; set; }
        /// <summary>
        /// Branch1Side搭接在Branch1的点
        /// </summary>
        public Point3d IntersPt1 { get; set; }
        /// <summary>
        /// Branch2Side搭接在Branch2的点
        /// </summary>
        public Point3d IntersPt2 { get; set; }
    }
}
