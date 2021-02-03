using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLight.Model;


using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;

using Autodesk.AutoCAD.DatabaseServices;

using Autodesk.AutoCAD.Colors;


namespace ThMEPLighting.EmgLight.Service
{
    class StructFilterService
    {
        private ThLane m_thLane;
        private List<Polyline> m_columns;
        private List<Polyline> m_walls;

        public StructFilterService(ThLane thLane, List<Polyline> columns, List<Polyline> walls)
        {
            m_thLane = thLane;
            m_columns = columns;
            m_walls = walls;
        }

        public void filterStruct(out List<List<ThStruct>> columnsStructs, out List<List<ThStruct>> wallStructs)
        {
            //获取该车道线上的构建
            var closeColumn = GetStruct(m_columns, EmgLightCommon.TolLane);
            var closeWall = GetStruct(m_walls, EmgLightCommon.TolLane);

            DrawUtils.ShowGeometry(closeColumn, EmgLightCommon.LayerGetStruct, Color.FromColorIndex(ColorMethod.ByColor, 1), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(closeWall, EmgLightCommon.LayerGetStruct, Color.FromColorIndex(ColorMethod.ByColor, 92), LineWeight.LineWeight035);

            foreach (Line l in m_thLane.geom)
            {
                var linePoly = GeomUtils.ExpandLine(l, EmgLightCommon.TolLane, 0, EmgLightCommon.TolLane, 0);
                DrawUtils.ShowGeometry(linePoly, EmgLightCommon.LayerSeparatePoly, Color.FromColorIndex(ColorMethod.ByColor, 44));
            }

            //打散构建并生成数据结构
            var columnSegment = StructureService.BrakeStructToStructSeg(closeColumn);
            var wallSegment = StructureService.BrakeStructToStructSeg(closeWall);

            DrawUtils.ShowGeometry(columnSegment.Select(x => x.geom).ToList(), EmgLightCommon.LayerStructSeg, Color.FromColorIndex(ColorMethod.ByColor, 1), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(wallSegment.Select(x => x.geom).ToList(), EmgLightCommon.LayerStructSeg, Color.FromColorIndex(ColorMethod.ByColor, 92), LineWeight.LineWeight035);

            //选取构建平行车道线的边
            var parallelColmuns = getStructureParallelPart(columnSegment);
            var parallelWalls = getStructureParallelPart(wallSegment);

            //破墙
            var brokeWall = StructureService.breakWall(parallelWalls, EmgLightCommon.TolBrakeWall);

            DrawUtils.ShowGeometry(parallelColmuns.Select(x => x.geom).ToList(), EmgLightCommon.LayerParallelStruct, Color.FromColorIndex(ColorMethod.ByColor, 5), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(brokeWall.Select(x => x.geom).ToList(), EmgLightCommon.LayerParallelStruct, Color.FromColorIndex(ColorMethod.ByColor, 5), LineWeight.LineWeight035);

            //过滤柱与墙交叉的部分
            var filterColumns = FilterStructIntersect(parallelColmuns, m_walls, EmgLightCommon.TolIntersect);
            var filterWalls = FilterStructIntersect(brokeWall, m_columns, EmgLightCommon.TolIntersect);

            DrawUtils.ShowGeometry(filterColumns.Select(x => x.geom).ToList(), EmgLightCommon.LayerNotIntersectStruct, Color.FromColorIndex(ColorMethod.ByColor, 140), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(filterWalls.Select(x => x.geom).ToList(), EmgLightCommon.LayerNotIntersectStruct, Color.FromColorIndex(ColorMethod.ByColor, 140), LineWeight.LineWeight035);

            //将构建按车道线方向分成左(0)右(1)两边
            var usefulColumns = StructureService.SeparateStructByLine(filterColumns, m_thLane.geom, EmgLightCommon.TolLane);
            var usefulWalls = StructureService.SeparateStructByLine(filterWalls, m_thLane.geom, EmgLightCommon.TolLane);

            StructureService.removeDuplicateStruct(ref usefulColumns);
            StructureService.removeDuplicateStruct(ref usefulWalls);

            columnsStructs = usefulColumns;
            wallStructs = usefulWalls;
        }

        /// <summary>
        /// 查找柱或墙平行于车道线
        /// </summary>
        /// <param name="structrues"></param>
        /// <param name="line"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<ThStruct> getStructureParallelPart(List<ThStruct> structureSegment)
        {
            //平行于车道线的边
            var structureLayoutSegment = structureSegment.Where(x =>
            {
                bool bAngle = Math.Abs(m_thLane.dir.DotProduct(x.dir)) / (m_thLane.dir.Length * x.dir.Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
                return bAngle;
            }).ToList();


            return structureLayoutSegment;
        }

        /// <summary>
        /// 查找车道线附近的构建
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private List<Polyline> GetStruct(List<Polyline> structs, double tol)
        {
            var resPolys = m_thLane.geom.SelectMany(x =>
            {
                var linePoly = GeomUtils.ExpandLine(x, tol, 0, tol, 0);
                return structs.Where(y =>
                {
                    return linePoly.Contains(y) || linePoly.Intersects(y);
                }).ToList();
            }).ToList();

            return resPolys;
        }

        /// <summary>
        /// 过滤墙柱相交的构建
        /// </summary>
        /// <param name="structSeg"></param>
        /// <param name="structure"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private static List<ThStruct> FilterStructIntersect(List<ThStruct> structSeg, List<Polyline> structure, double tol)
        {
            List<ThStruct> notIntersectSeg = new List<ThStruct>();

            foreach (var seg in structSeg)
            {
                var bInter = false;
                var bContain = false;

                foreach (var poly in structure)
                {
                    bContain = bContain || poly.Contains(seg.geom);
                    bInter = poly.Intersects(seg.geom);
                    if (bInter == true)
                    {
                        Point3dCollection pts = new Point3dCollection();
                        seg.geom.IntersectWith(poly, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);

                        if (pts.Count > 1)
                        {
                            bInter = true;
                            break;
                        }
                        else if (pts.Count == 1)
                        {
                            Point3d ptIn = new Point3d();
                            Point3d ptOut = new Point3d();
                            if (poly.Contains(seg.geom.StartPoint) == true)
                            {
                                ptIn = seg.geom.StartPoint;
                                ptOut = seg.geom.EndPoint;
                            }
                            if (poly.Contains(seg.geom.EndPoint) == true)
                            {
                                ptIn = seg.geom.EndPoint;
                                ptOut = seg.geom.StartPoint;
                            }

                            var lIn = new Line(pts[0], ptIn);
                            var lOut = new Line(pts[0], ptOut);

                            if (ptIn.X == 0)
                            {
                                bInter = false;
                            }

                            else if (lIn.Length < EmgLightCommon.TolIntersect || lOut.Length >= EmgLightCommon.TolInterFilter)
                            {
                                bInter = false;
                            }
                            else
                            {
                                bInter = true;
                                break;
                            }
                        }
                        else
                        {
                            bInter = false;
                        }
                    }
                }
                if (bInter == false && bContain == false)
                {
                    notIntersectSeg.Add(seg);
                }
            }
            return notIntersectSeg;
        }

        public LayoutService moreFilter(List<List<ThStruct>> usefulColumns, List<List<ThStruct>> usefulWalls, Polyline frame)
        {
            LayoutService layoutServer = new LayoutService(usefulColumns, usefulWalls, m_thLane);

            DrawUtils.ShowGeometry(layoutServer.UsefulStruct[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerSeparate, Color.FromColorIndex(ColorMethod.ByColor, 161), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(layoutServer.UsefulStruct[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerSeparate, Color.FromColorIndex(ColorMethod.ByColor, 11), LineWeight.LineWeight035);

            ////滤掉重合部分
            layoutServer.filterOverlapStruc();

            ////滤掉框外边的部分
            layoutServer.getInsideFramePart(frame);

            ////滤掉框后边的部分
            layoutServer.filterStrucBehindFrame(frame);

            DrawUtils.ShowGeometry(layoutServer.UsefulColumns[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, Color.FromColorIndex(ColorMethod.ByColor, 161), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(layoutServer.UsefulColumns[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, Color.FromColorIndex(ColorMethod.ByColor, 161), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(layoutServer.UsefulWalls[0].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, Color.FromColorIndex(ColorMethod.ByColor, 11), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(layoutServer.UsefulWalls[1].Select(x => x.geom).ToList(), EmgLightCommon.LayerStruct, Color.FromColorIndex(ColorMethod.ByColor, 11), LineWeight.LineWeight035);

            return layoutServer;

        }

    }


}
