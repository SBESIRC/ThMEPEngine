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

        /// <summary>
        /// 单个外墙轮廓
        /// </summary>
        /// <returns></returns>
        public List<Polyline> DoMainBeamProfiles()
        {
            var dataExtract = new DBExtract();
            dataExtract.GetCurves();

            var calculateDect = new CalculateDetection(dataExtract.Walls.First(), dataExtract.SubtractCurves);

            var mainBeamProfiles = calculateDect.CalculateMainBeamProfiles();
            return mainBeamProfiles;
        }

        /// <summary>
        /// 多个外墙轮廓处理
        /// </summary>
        /// <returns></returns>
        public List<Polyline> DoMultiMainBeamProfiles()
        {
            var wallPolylines = EntityPicker.MakeUserPickEntities();
            var dataExtract = new DBExtract();
            dataExtract.GetCurves();
            var resPolylines = new List<Polyline>();

            foreach (var poly in wallPolylines)
            {
                var dectCalculator = new CalculateDetection(poly, dataExtract.SubtractCurves);
                resPolylines.AddRange(dectCalculator.CalculateMainBeamProfiles());
            }
            return resPolylines;
        }

        /// <summary>
        /// 多个墙轮廓的主梁布置
        /// </summary>
        /// <returns></returns>
        public List<Point3d> DoMultiWallMainBeamPlace()
        {
            var ptLst = new List<Point3d>();
            var polylines = DoMultiMainBeamProfiles();
            if (polylines.Count == 0)
            {
                return ptLst;
            }

            // 转到UCS
            var wcs2Ucs = Active.Editor.WCS2UCS();
            var ucs2Wcs = Active.Editor.UCS2WCS();
            polylines.ForEach(o => o.TransformBy(wcs2Ucs));

            // 插入点的计算
            PlaceParameter placePara = new PlaceParameter();
            var placeInputDatas = new List<PlaceInputProfileData>();
            polylines.ForEach(e => { placeInputDatas.Add(new PlaceInputProfileData(e)); });
            var tempPts = PlacePointCalculator.MakeCalculatePlacePoints(placeInputDatas, placePara);
            tempPts.ForEach(pt => ptLst.Add(pt.TransformBy(ucs2Wcs)));

            // 转到WCS
            if (ptLst.Count > 0)
            {
                BlockInsertor.MakeBlockInsert(tempPts, placePara.sensorType);

                var circles = GeometryTrans.Points2Circles(ptLst, placePara.ProtectRadius, Vector3d.ZAxis);
                var curves = GeometryTrans.Circles2Curves(circles);
                DrawUtils.DrawProfile(curves, "placePoints");
            }

            return ptLst;
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

            // 转到UCS
            var wcs2Ucs = Active.Editor.WCS2UCS();
            var ucs2Wcs = Active.Editor.UCS2WCS();
            polylines.ForEach(o => o.TransformBy(wcs2Ucs));

            // 插入点的计算
            PlaceParameter placePara = new PlaceParameter();
            var placeInputDatas = new List<PlaceInputProfileData>();
            polylines.ForEach(e => { placeInputDatas.Add(new PlaceInputProfileData(e)); });
            var tempPts = PlacePointCalculator.MakeCalculatePlacePoints(placeInputDatas, placePara);
            tempPts.ForEach(pt => ptLst.Add(pt.TransformBy(ucs2Wcs)));

            // 转到WCS
            if (ptLst.Count > 0)
            {
                BlockInsertor.MakeBlockInsert(tempPts, placePara.sensorType);

                var circles = GeometryTrans.Points2Circles(ptLst, placePara.ProtectRadius, Vector3d.ZAxis);
                var curves = GeometryTrans.Circles2Curves(circles);
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
