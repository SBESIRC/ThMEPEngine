using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Business.Procedure;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Business.BlindAreaReminder
{
    /// <summary>
    /// 盲区提醒
    /// </summary>
    public class BlindReminderCalculator
    {
        public List<PolygonInfo> BlindPolygons
        {
            get;
            private set;
        } = new List<PolygonInfo>();

        private double m_protectRadius;

        private BlindReminderCalculator(double protectRadius)
        {
            m_protectRadius = protectRadius;
        }

        public static List<PolygonInfo> MakeBlindAreaReminderCalculator(double protectRadius = 5800)
        {
            var blindReminderCalculator = new BlindReminderCalculator(protectRadius);
            blindReminderCalculator.DoBlindCalculator();
            return blindReminderCalculator.BlindPolygons;
        }

        public void DoBlindCalculator()
        {
            var polygons = CalculatePolygonInfos();

            if (polygons.Count == 0)
                return;

            var protectAreas = CalculateProtectArea();

            //DrawUtils.DrawProfileDebug(protectAreas.Polylines2Curves(), "protectArea");
            CalculateBlindPolylines(polygons, protectAreas);
        }

        private void CalculateBlindPolylines(List<PolygonInfo> polygonInfos, List<Polyline> polylines)
        {
            foreach (var polygon in polygonInfos)
            {
                var external = polygon.ExternalProfile;
                var dbLst = new DBObjectCollection();
                polylines.ForEach(p => dbLst.Add(p));
                polygon.InnerProfiles.ForEach(p => dbLst.Add(p));

                BlindPolygons.AddRange(SplitRegions(external, dbLst));
            }
        }

        protected List<PolygonInfo> SplitRegions(Polyline externalPolyline, DBObjectCollection dbLst)
        {
            var polygons = new List<PolygonInfo>();
            var drawCurves = new List<Entity>();

            foreach (Entity item in externalPolyline.DifferenceMP(dbLst))
            {
                if (item is Polyline polyline)
                {
                    polygons.Add(new PolygonInfo(polyline));
                    drawCurves.Add(polyline);
                }
                else if (item is MPolygon mPolygon)
                {
                    polygons.Add(GeomUtils.MPolygon2Polygon(mPolygon));
                    drawCurves.Add(mPolygon);
                }
            }

            DrawUtils.DrawEntitiesDebug(drawCurves, "entities");
            return polygons;
        }

        private List<Polyline> CalculateProtectArea()
        {
            var polys = new List<Polyline>();

            using (var db = AcadDatabase.Active())
            {
                var blockRefs = db.ModelSpace.OfType<BlockReference>().Where(p => p.Name.Equals(ThMEPCommon.SMOKE_SENSOR_BLOCK_NAME)).ToList();
                if (blockRefs.Count == 0)
                {
                    return polys;
                }

                foreach (var blockRef in blockRefs)
                {
                    var polyline = new Polyline()
                    {
                        Closed = true
                    };
                    var arc1 = new Arc(blockRef.Position, m_protectRadius, 0, Math.PI);
                    var arc2 = new Arc(blockRef.Position, m_protectRadius, Math.PI, Math.PI * 2);
                    polyline.AddVertexAt(0, arc1.StartPoint.ToPoint2D(), 1, 0, 0);
                    polyline.AddVertexAt(1, arc2.StartPoint.ToPoint2D(), 1, 0, 0);
                    polyline.AddVertexAt(2, arc2.EndPoint.ToPoint2D(), 0, 0, 0);
                    polys.Add(polyline.TessellatePolylineWithChord(ThMEPCommon.ProtectAreaScatterLength));
                }
            }

            return polys;
        }

        private List<PolygonInfo> CalculatePolygonInfos()
        {
            var polygons = new List<PolygonInfo>();
            // 用户选择Curves
            var wallCurves = EntityPicker.MakeUserPickCurves();
            if (wallCurves.Count == 0)
                return polygons;

            var wallPolys = SplitWallWorker.MakeSplitWallProfiles(wallCurves);

            return CalculateMaps(wallPolys);
        }

        private List<PolygonInfo> CalculateMaps(List<Polyline> srcPolys)
        {
            var tempPolygonInfos = new List<PolygonInfo>();
            foreach (var poly in srcPolys)
            {
                tempPolygonInfos.Add(new PolygonInfo(poly));
            }

            // 被包含则不是有效的轮廓区域
            for (int i = 0; i < tempPolygonInfos.Count; i++)
            {
                if (tempPolygonInfos[i].IsUsed)
                    continue;

                var curPoly = tempPolygonInfos[i].ExternalProfile;
                for (int j = 0; j < tempPolygonInfos.Count; j++)
                {
                    if (i == j)
                        continue;

                    var otherPoly = tempPolygonInfos[j].ExternalProfile;
                    var startPt = otherPoly.StartPoint;
                    if (PolylineContainsPoly(curPoly, otherPoly))
                    {
                        tempPolygonInfos[j].IsUsed = true;
                        tempPolygonInfos[i].InnerProfiles.Add(otherPoly);
                    }
                }
            }

            return tempPolygonInfos.Where(p =>
                !p.IsUsed).ToList();
        }

        private bool PolylineContainsPoly(Polyline polyFir, Polyline polySec)
        {
            var secPts = polySec.Vertices();
            foreach (Point3d pt in secPts)
            {
                if (!GeomUtils.PtInLoop(polyFir, pt))
                    return false;
            }

            return true;
        }
    }
}
