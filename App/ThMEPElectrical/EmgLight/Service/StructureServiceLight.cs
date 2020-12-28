using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.EmgLight.Service
{
    class StructureServiceLight
    {
        /// <summary>
        /// 获取停车线周边构建信息
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<Polyline> GetStruct(List<Line> lines, List<Polyline> polys, double tol)
        {
            //var resPolys = lines.SelectMany(x =>
            //{
            //    var linePoly = StructUtils.ExpandLine(x, tol);
            //    InsertLightService.ShowGeometry(linePoly, 44);
            //    var polyCollection = new DBObjectCollection() { linePoly };
            //    return polys.Where(y => y.Intersection(polyCollection).Count > 0).ToList();
            //}).ToList();

            //var resPolys = lines.SelectMany(x =>
            //{
            //    var linePoly = StructUtils.ExpandLine(x, tol);
            //    InsertLightService.ShowGeometry(linePoly, 44);
            //    return polys.Where(y =>
            //    {
            //        var polyCollection = new DBObjectCollection() { y };
            //        return linePoly.Intersection(polyCollection).Count > 0;
            //    }).ToList();
            //}).ToList();

            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, tol);
               
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
        public List<List<Polyline>> SeparateColumnsByLine(List<Polyline> polyline, List<Line> lines, double length)
        {
            
         var   linePolys= StructUtils.createRecBuffer(lines, length);
            
            List<Polyline> upPolyline = new List<Polyline>();
            List<Polyline> downPolyline = new List<Polyline>();
            foreach (var poly in polyline)
            {
                var intersecRes = linePolys.Where(x => x.Contains(poly) || x.Intersects(poly)).ToList();
                if (intersecRes.Count > 0)
                {
                    upPolyline.Add(poly);
                }
                else
                {
                    downPolyline.Add(poly);
                }
            }

            return new List<List<Polyline>>() { upPolyline, downPolyline };
        }

       

    }
}
