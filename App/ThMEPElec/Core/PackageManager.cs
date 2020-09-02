using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.CAD;
using ThMEPElectrical.Business;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPElectrical.Model;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.Colors;
using ThMEPElectrical.Geometry;
using AcHelper;
using GeometryExtensions;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPElectrical.Core
{
    public class PackageManager
    {
        public PackageManager()
        {
            ;
        }

        public List<Polyline> DoMainBeamProfiles()
        {
            var dataExtract = new DBExtract();
            dataExtract.GetCurves();

            var calculateDect = new CalculateDetection(dataExtract.Walls.First(), dataExtract.SubtractCurves);

            var mainBeamProfiles = calculateDect.CalculateMainBeamProfiles();
            return mainBeamProfiles;
        }

        /// <summary>
        /// 主梁布置
        /// </summary>
        /// <returns></returns>
        public List<Point3d> DoMainBeamPlace()
        {
            var ptLst = new List<Point3d>();
            var polylines = DoMainBeamProfiles();
            if (polylines.Count == 0)
            {
                return ptLst;
            }

            DrawUtils.DrawProfile(polylines.Polylines2Curves(), "polylines");

            // 转到UCS
            var wcs2Ucs = Active.Editor.WCS2UCS();
            var ucs2Wcs = Active.Editor.UCS2WCS();

            polylines.ForEach(o => o.TransformBy(wcs2Ucs));
            DrawUtils.DrawProfile(polylines.Polylines2Curves(), "ucsTransPolylines");

            // 插入点的计算
            PlaceParameter placePara = new PlaceParameter();
            var placeInputDatas = new List<PlaceInputProfileData>();
            polylines.ForEach(e => { placeInputDatas.Add(new PlaceInputProfileData(e)); });
            var tempPts = PlacePointCalculator.MakeCalculatePlacePoints(placeInputDatas, placePara);
            tempPts.ForEach(pt => ptLst.Add(pt.TransformBy(ucs2Wcs)));

            // 转到WCS
            if (ptLst.Count > 0)
            {
                var circles = GeometryTrans.Points2Circles(ptLst, placePara.ProtectRadius, Vector3d.ZAxis);
                var centerCircles = GeometryTrans.Points2Circles(ptLst, 100, Vector3d.ZAxis);
                var curves = GeometryTrans.Circles2Curves(circles);
                var centerCurves = GeometryTrans.Circles2Curves(centerCircles);
                DrawUtils.DrawProfile(centerCurves, "centerCurves", Color.FromRgb(0, 255, 0));
                DrawUtils.DrawProfile(curves, "placePoints");
            }

            return ptLst;
        }

        public void DoMainBeamRect()
        {
            var polylines = DoMainBeamProfiles();
            if (polylines == null || polylines.Count == 0)
                return;

            var rectPolylines = new List<Polyline>();
            foreach (var poly in polylines)
            {
                rectPolylines.Add(MinRectangle.Calculate(poly));
            }

            DrawUtils.DrawProfile(rectPolylines.Polylines2Curves(), "rectPolyline");
        }

        public void DoMainBeamABBRect()
        {
            var polylines = DoMainBeamProfiles();
            if (polylines == null || polylines.Count == 0)
                return;

            var rectPolylines = new List<Polyline>();
            foreach (var poly in polylines)
            {
                rectPolylines.Add(ABBRectangle.MakeABBPolyline(poly));
            }

            DrawUtils.DrawProfile(rectPolylines.Polylines2Curves(), "rectPolyline");
        }
    }
}
