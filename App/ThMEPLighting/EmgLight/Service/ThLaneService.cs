using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLight.Model;
using ThCADCore.NTS;

namespace ThMEPLighting.EmgLight.Service
{
    class ThLaneService
    {
        private ThLane m_lane;

        public ThLaneService(ThLane lane)
        {
            this.m_lane = lane;
        }

        public ThLane thLane
        {
            get
            {
                return m_lane;
            }
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
            Vector3d otherDir = Vector3d.ZAxis.CrossProduct(m_lane.dir);

            var structureLayoutSegment = structureSegment.Where(x =>
            {
                var xDir = (x.geom.EndPoint - x.geom.StartPoint).GetNormal();
                bool bAngle = Math.Abs(m_lane.dir.DotProduct(xDir)) / (m_lane.dir.Length * xDir.Length) > Math.Abs(Math.Cos(30 * Math.PI / 180));
                return bAngle;
            }).ToList();


            return structureLayoutSegment;
        }

        public List<Polyline> GetStruct(List<Polyline> structs, double tol)
        {
            var resPolys = m_lane.geom.SelectMany(x =>
            {
                var linePoly = StructUtils.ExpandLine(x, tol, 0, tol, 0);
                return structs.Where(y =>
                {
                    return linePoly.Contains(y) || linePoly.Intersects(y);
                }).ToList();
            }).ToList();

            return resPolys;
        }

    }
}
