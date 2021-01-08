using System;
using System.Linq;
using Linq2Acad;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;


using Autodesk.AutoCAD.Geometry;


namespace ThMEPLighting.EmgLight.Service
{
    class StructureServiceLight
    {
        /// <summary>
        /// 获取停车线周边构建信息
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static List<Polyline> GetStruct(List<Line> lines, List<Polyline> polys, double tol)
        {

            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, tol, 0, tol, 0);
                //InsertLightService.ShowGeometry(linePoly, 44);
                return polys.Where(y =>
                {
                    var polyCollection = new DBObjectCollection() { y };
                    return linePoly.Contains(y) || linePoly.Intersects(y);
                }).ToList();
            }).ToList();

            return resPolys;
        }


        /// <summary>
        /// 沿着线将柱分隔成上下两部分
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static List<List<Polyline>> SeparateColumnsByLine(List<Polyline> polyline, List<Line> lines, double tol)
        {
            //改成上下 都filter一遍 

            //var linePolys = StructUtils.createRecBuffer(lines, tol);

            //InsertLightService.ShowGeometry(linePolys, 142);
            //List<Polyline> upPolyline = new List<Polyline>();
            //List<Polyline> downPolyline = new List<Polyline>();
            //foreach (var poly in polyline)
            //{
            //    var intersecRes = linePolys.Where(x => x.Contains(poly) || x.Intersects(poly)).ToList();
            //    intersecRes = linePolys.Where(x => x.Contains(poly.StartPoint) || x.Intersects(poly)).ToList();
            //    if (intersecRes.Count > 0)
            //    {
            //        upPolyline.Add(poly);
            //    }
            //    else
            //    {
            //        downPolyline.Add(poly);
            //    }
            //}

            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, tol, 0, 0, 0);
                //InsertLightService.ShowGeometry(linePoly, 142);
                return polyline.Where(y =>
                {
                    var polyCollection = new DBObjectCollection() { y };
                    return linePoly.Contains(y) || linePoly.Intersects(y);
                }).ToList();
            }).ToList();

         var   upPolyline = resPolys;

             resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, 0, 0, tol , 0);
                //InsertLightService.ShowGeometry(linePoly, 11);
                return polyline.Where(y =>
                {
                    var polyCollection = new DBObjectCollection() { y };
                    return linePoly.Contains(y) || linePoly.Intersects(y);
                }).ToList();
            }).ToList();
           var  downPolyline = resPolys;

            return new List<List<Polyline>>() { upPolyline, downPolyline };
        }

        /// <summary>
        /// 查找柱或墙平行于车道线且与防火墙不相交的边
        /// </summary>
        /// <param name="structrues"></param>
        /// <param name="line"></param>
        /// <param name="frame"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Polyline> FilterStructure(List<Polyline> structrues, Line line, Polyline frame, string type)
        {
            if (structrues.Count <= 0)
            {
                return null;
            }

            List<Polyline> layoutColumns = new List<Polyline>();

            var LineDir = (line.EndPoint - line.StartPoint).GetNormal();

            foreach (Polyline structure in structrues)
            {
                //平行于车道线的边
                List<Polyline> layoutInfo = null;
                if (type == "c")
                {
                    layoutInfo = StructureLayoutServiceLight.GetColumnParallelPart(structure, line.StartPoint, LineDir, out Point3d closetPt);
                }
                else if (type == "w")
                {
                    layoutInfo = StructureLayoutServiceLight.GetWallParallelPart(structure, line.StartPoint, LineDir, out Point3d closetPt);
                }



                //选与防火框不相交且在防火框内
                if (layoutInfo != null)
                {

                    layoutInfo = layoutInfo.Where(x =>
                    {
                        Point3dCollection pts = new Point3dCollection();
                        x.IntersectWith(frame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                        return pts.Count <= 0 && frame.Contains(x.StartPoint);
                    }).ToList();

                    layoutColumns.AddRange(layoutInfo);

                }

            }
            return layoutColumns;
        }
    }
}
