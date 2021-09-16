using AcHelper.Commands;
using System;
using System.Linq;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPElectrical.Broadcast.Service;
using System.Collections.Generic;
using System.Collections;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.Runtime;
using NFox.Collections;//树
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay.Snap;
using NetTopologySuite.Operation.Overlay;
using Dreambuild.AutoCAD;
using ThMEPElectrical.AlarmSensorLayout.Method;
using NetTopologySuite.Operation.OverlayNG;

namespace ThMEPElectrical.AlarmLayout.Utils
{
    public static class AreaCaculator
    {
        /// <summary>
        /// 获取设备覆盖区域集合
        /// </summary>
        /// <param name="pts">设备布置点集</param>
        /// <param name="radius">设备覆盖半径</param>
        /// <returns>返回设备覆盖区域集合</returns>
        public static NetTopologySuite.Geometries.Geometry GetUnion(List<Point3d> pts, double radius)
        {
            List<Circle> carryAreaUnion = new List<Circle>();
            foreach (Point3d pt in pts)
            {
                carryAreaUnion.Add(new Circle(pt, Vector3d.ZAxis, radius));
            }
            NetTopologySuite.Geometries.Geometry coveredRegion = SnapIfNeededOverlayOp.Union(carryAreaUnion.First().ToNTSPolygon(), carryAreaUnion.Last().ToNTSPolygon());
            foreach (Circle a in carryAreaUnion)
            {
                coveredRegion = SnapIfNeededOverlayOp.Union(coveredRegion, a.ToNTSPolygon());
            }
            //ShowGeometry(coveredRegion, acdb, 1);

            //var objs = new DBObjectCollection();
            //foreach (var a in carryAreaUnion)
            //{
            //    objs.Add(a.Tessellate(100.0));
            //}
            //var results = mPolygon.ToNTSPolygon().Difference(objs, true);
            return coveredRegion;
        }
    }
}
