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
            var polylines = DoMainBeamProfiles();
            if (polylines == null || polylines.Count == 0)
                return null;

            // 
            PlaceParameter placePara = new PlaceParameter();
            var placeInputDatas = new List<PlaceInputProfileData>();
            polylines.ForEach(e => { placeInputDatas.Add(new PlaceInputProfileData(e)); });

            // 插入点的计算
            var ptLst = CalculatePlacePoint.MakeCalculatePlacePoints(placeInputDatas, placePara);

            if (ptLst != null && ptLst.Count != 0)
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
    }
}
