using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using ThMEPEngineCore.Algorithm;
using DotNetARX;
using AcHelper;
using System;

namespace ThMEPEngineCore.Engine
{
    public class ThArchitectureWallRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        private const double AbnormalBufferDis = 30.0;
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            using (var fixedPrecision = new ThCADCoreNTSFixedPrecision())
            using (var archWallDbExtension = new ThArchitectureWallDbExtension(database))
            {
                archWallDbExtension.BuildElementCurves();
                List<Curve> curves = new List<Curve>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    archWallDbExtension.WallCurves.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex shearwallCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in shearwallCurveSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        curves.Add(filterObj as Curve);
                    }
                }
                else
                {
                    curves = archWallDbExtension.WallCurves;
                }
                //var outlines = HandleExtractCurves(curves);
                curves.ForEach(o => acadDatabase.ModelSpace.Add(o));
                //var results = BuildArea(outlines);
                //results.ForEach(o => Elements.Add(ThIfcWall.Create(o)));
            }
        }
        private List<Polyline> HandleExtractCurves(List<Curve> curves)
        {
            List<Polyline> results = new List<Polyline>();
            curves.ForEach(o =>
            {
                if (o is Polyline polyline && polyline.Area > 0.0)
                {
                    var handleObjs = HandleAbnormalEdge(polyline);
                    handleObjs.ForEach(m =>
                    {
                        var bufferObjs = m.Buffer(ThMEPEngineCoreCommon.ShearWallBufferDistance);
                        if (bufferObjs.Count > 0)
                        {
                            var first = bufferObjs.Cast<Polyline>().OrderByDescending(n => n.Area).First();
                            results.Add(first);
                        }
                    });
                }
            });
            return results;
        }
        private List<Polyline> HandleAbnormalEdge(Polyline origin)
        {
            List<Polyline> results = new List<Polyline>();
            var polyline = origin.ToNTSLineString().ToDbPolyline();
            var innerObjs = polyline.Buffer(-AbnormalBufferDis);
            if (innerObjs.Count == 0)
            {
                return results;
            }
            var inner = innerObjs[0] as Polyline;
            var outerObjs = inner.Buffer(AbnormalBufferDis);
            if (outerObjs.Count == 0)
            {
                return results;
            }
            results.Add(outerObjs[0] as Polyline);
            return results;
        }
        private List<Polyline> BuildArea(List<Polyline> outlines)
        {
            var results = new List<Polyline>();
            var polygons = outlines.ToPolygons();
            using (AcadDatabase acadDatabase=AcadDatabase.Active())
            {
                polygons.ForEach(o =>
                {
                    ObjectIdList objIds = new ObjectIdList();
                    var shell = o.Shell.ToDbPolyline();
                    objIds.Add(acadDatabase.ModelSpace.Add(shell));
                    shell.ColorIndex = 1;
                    o.Holes.ForEach(m =>
                    {
                        var hole = m.ToDbPolyline();
                        hole.ColorIndex = 3;
                        objIds.Add(acadDatabase.ModelSpace.Add(hole));
                    });                   
                    GroupTools.CreateGroup(Active.Database, Guid.NewGuid().ToString(), objIds);
                });
            }
            foreach (var polygon in polygons)
            {
                // 把“甜甜圈”式的带洞的Polygon（有且只有洞）转成不带洞的Polygon
                // 在区域划分时，剪力墙是“不可以用”区域，剪力墙的外部和内部都是“可用区域”
                // 通过这种处理，将剪力墙的外部和内部区域联通起来，从而获取正确的可用区域
                List<Polyline> gapOutlines = ThPolygonToGapPolylineService.ToGapPolyline(polygon);
                gapOutlines.ForEach(o => results.Add(o));
            }
            return results.Where(o=>o.Area>0.0).ToList();
        }
    }
}
