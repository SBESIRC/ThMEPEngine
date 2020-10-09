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
using Linq2Acad;
using ThMEPEngineCore.Engine;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Operation;

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

            var calculateDect = new BeamDetectionCalculator(dataExtract.Walls.First(), dataExtract.SubtractCurves);

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
                var dectCalculator = new BeamDetectionCalculator(poly, dataExtract.SubtractCurves);
                resPolylines.AddRange(dectCalculator.CalculateMainBeamProfiles());
            }
            return resPolylines;
        }

        /// <summary>
        /// 计算主次梁的梁跨信息
        /// </summary>
        /// <returns></returns>
        public List<PlaceInputProfileData> DoMainSecondBeamProfiles()
        {
            // 前置数据读取器
            var infoReader = new InfoReader();
            infoReader.Do();

            // 用户选择
            var wallPolylines = EntityPicker.MakeUserPickEntities();
            var inputProfileDatas = new List<PlaceInputProfileData>();

            // 外墙轮廓数据
            foreach (var poly in wallPolylines)
            {
                var wallPtCollection = poly.Vertices();
                var innerHoles = GetValidProfiles(infoReader.RecognizeMainBeamColumnWalls, wallPtCollection);
                var secondBeams = GetValidProfileInfos(infoReader.RecognizeSecondBeams, wallPtCollection);

                // 外墙，内洞，次梁
                var profileDatas = BeamDetectionCalculator.MakeDetectionData(poly, innerHoles, secondBeams);
                // 主次梁信息
                inputProfileDatas.AddRange(profileDatas);
            }

            return inputProfileDatas;
        }

        private List<Polyline> GetValidProfiles(List<Polyline> srcPolylines, Point3dCollection window)
        {
            var polylines = new List<Polyline>();
            DBObjectCollection dbObjs = new DBObjectCollection();
            srcPolylines.ForEach(o => dbObjs.Add(o));
            ThCADCoreNTSSpatialIndex SpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            foreach (var filterObj in SpatialIndex.SelectCrossingPolygon(window))
            {
                if (filterObj is Polyline poly)
                    polylines.Add(poly);
            }

            return polylines;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="beamProfileInfos"></param>
        /// <param name="window"></param>
        /// <returns></returns>
        private List<SecondBeamProfileInfo> GetValidProfileInfos(List<SecondBeamProfileInfo> beamProfileInfos, Point3dCollection window)
        {
            var secondBeamInfos = new List<SecondBeamProfileInfo>();
            var polyWindow = window.ToPolyline();
            foreach (var singleSecondBeamInfo in beamProfileInfos)
            {
                if (RelatedPolyline(polyWindow, singleSecondBeamInfo.Profile))
                    secondBeamInfos.Add(singleSecondBeamInfo);
            }

            return secondBeamInfos;
        }

        private bool RelatedPolyline(Polyline polyFir, Polyline PolySec)
        {
            var ptCollection = PolySec.Vertices();
            foreach (Point3d pt in ptCollection)
            {
                if (GeomUtils.PtInLoop(polyFir, pt.ToPoint2D()))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 无梁楼盖布置
        /// </summary>
        /// <returns></returns>
        public List<Point3d> DoNoBeamPlacePoints()
        {
            var ptLst = new List<Point3d>();

            // 计算轴网和梁结构关系
            var inputProfileDatas = DoNoBeamStoreyProfiles();
            if (inputProfileDatas.Count == 0)
                return ptLst;

            // 转到UCS
            var wcs2Ucs = Active.Editor.WCS2UCS();
            var ucs2Wcs = Active.Editor.UCS2WCS();

            // 插入点的计算
            PlaceParameter placePara = new PlaceParameter();
            var transformPlaceInputDatas = TransformProfileDatas(inputProfileDatas, wcs2Ucs);

            var tempPts = PlacePointCalculator.MakeCalculatePlacePoints(transformPlaceInputDatas, placePara);
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
        /// 有梁吊顶布置
        /// </summary>
        /// <returns></returns>
        public List<Point3d> DoGridBeamPlacePoints()
        {
            var ptLst = new List<Point3d>();

            // 计算轴网和梁结构关系
            var inputProfileDatas = DoGridBeamProfiles();
            if (inputProfileDatas.Count == 0)
                return ptLst;

            // 转到UCS
            var wcs2Ucs = Active.Editor.WCS2UCS();
            var ucs2Wcs = Active.Editor.UCS2WCS();

            // 插入点的计算
            PlaceParameter placePara = new PlaceParameter();
            var transformPlaceInputDatas = TransformProfileDatas(inputProfileDatas, wcs2Ucs);

            var tempPts = PlacePointCalculator.MakeCalculatePlacePoints(transformPlaceInputDatas, placePara);
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
        /// 计算无梁楼盖的信息
        /// </summary>
        /// <returns></returns>
        public List<PlaceInputProfileData> DoNoBeamStoreyProfiles()
        {
            // 前置数据读取器
            var infoReader = new InfoReader();
            infoReader.PickColumns(); // 提取柱子

            // 用户选择
            var wallPolylines = EntityPicker.MakeUserPickEntities();
            var inputProfileDatas = new List<PlaceInputProfileData>();
            var gridPolys = new List<Polyline>();

            // 外墙轮廓数据
            foreach (var poly in wallPolylines)
            {
                var wallPtCollection = poly.Vertices();

                var validColumns = GetValidProfiles(infoReader.Columns, wallPtCollection);
                var gridCalculator = new GridService();

                //轴网线
                var gridInfo = gridCalculator.CreateGrid(poly, validColumns, ThMEPCommon.spacingValue);
                gridPolys.Clear();
                gridInfo.ForEach(e => gridPolys.AddRange(e.Value));
                //DrawUtils.DrawProfile(gridPolys.Polylines2Curves(), "gridPolys");
                //return inputProfileDatas;
                // 外墙，内洞，轴网
                var profileDatas = NoBeamStoreyDetectionCalculator.MakeNoBeamStoreyDetectionCalculator(gridPolys, validColumns, poly);
                // 轴网 + 相关次梁信息
                inputProfileDatas.AddRange(profileDatas);
            }

            return inputProfileDatas;
        }

        /// <summary>
        /// 轴网测试
        /// </summary>
        public void DoGridTestProfiles()
        {
            // 前置数据读取器
            var infoReader = new InfoReader();
            infoReader.PickColumns(); // 提取柱子

            // 用户选择
            var wallPolylines = EntityPicker.MakeUserPickEntities();
            var gridPolys = new List<Polyline>();

            // 外墙轮廓数据
            foreach (var poly in wallPolylines)
            {
                var wallPtCollection = poly.Vertices();

                var validColumns = GetValidProfiles(infoReader.Columns, wallPtCollection);
                var gridCalculator = new GridService();

                //轴网线
                var gridInfo = gridCalculator.CreateGrid(poly, validColumns, ThMEPCommon.spacingValue);
                gridPolys.Clear();
                gridInfo.ForEach(e => gridPolys.AddRange(e.Value));
                DrawUtils.DrawProfile(gridPolys.Polylines2Curves(), "gridPolys");
            }
        }


        /// <summary>
        /// 计算梁吊顶的信息
        /// </summary>
        /// <returns></returns>
        public List<PlaceInputProfileData> DoGridBeamProfiles()
        {
            // 前置数据读取器
            var infoReader = new InfoReader();
            infoReader.Do();

            // 用户选择
            var wallPolylines = EntityPicker.MakeUserPickEntities();
            var inputProfileDatas = new List<PlaceInputProfileData>();
            var gridPolys = new List<Polyline>();

            var innerHoles = new List<Polyline>();
            // 外墙轮廓数据
            foreach (var poly in wallPolylines)
            {
                var wallPtCollection = poly.Vertices();
                innerHoles.Clear();
                // 所有的内部洞数据
                innerHoles.AddRange(infoReader.RecognizeMainBeamColumnWalls);
                infoReader.RecognizeSecondBeams.ForEach(e => innerHoles.Add(e.Profile));

                var validHoles = GetValidProfiles(innerHoles, wallPtCollection);

                var validColumns = GetValidProfiles(infoReader.Columns, wallPtCollection);
                var gridCalculator = new GridService();

                //轴网线
                var gridInfo = gridCalculator.CreateGrid(poly, validColumns, ThMEPCommon.spacingValue);
                gridPolys.Clear();
                gridInfo.ForEach(e => gridPolys.AddRange(e.Value));
                //DrawUtils.DrawProfile(gridPolys.Polylines2Curves(), "tet");
                //return inputProfileDatas;
                // 外墙，内洞，轴网
                var profileDatas = GridDetectionCalculator.MakeGridDetectionCalculator(poly, gridPolys, validHoles);
                // 轴网 + 相关次梁信息
                inputProfileDatas.AddRange(profileDatas);
            }

            return inputProfileDatas;
        }

        /// <summary>
        /// 计算主次梁的布置点集
        /// </summary>
        /// <returns></returns>
        public List<Point3d> DoMainSecondBeamPlacePoints()
        {
            var ptLst = new List<Point3d>();

            // 计算主次梁结构关系
            var inputProfileDatas = DoMainSecondBeamProfiles();
            if (inputProfileDatas.Count == 0)
                return ptLst;

            // 转到UCS
            var wcs2Ucs = Active.Editor.WCS2UCS();
            var ucs2Wcs = Active.Editor.UCS2WCS();

            // 插入点的计算
            PlaceParameter placePara = new PlaceParameter();
            var transformPlaceInputDatas = TransformProfileDatas(inputProfileDatas, wcs2Ucs);

            var tempPts = PlacePointCalculator.MakeCalculatePlacePoints(transformPlaceInputDatas, placePara);
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
        /// 主次梁信息坐标转换
        /// </summary>
        /// <param name="inputProfileDatas"></param>
        /// <returns></returns>
        private List<PlaceInputProfileData> TransformProfileDatas(List<PlaceInputProfileData> inputProfileDatas, Matrix3d matrix)
        {
            var resProfileDatas = new List<PlaceInputProfileData>();

            foreach (var singleProfileData in inputProfileDatas)
            {
                resProfileDatas.Add(TransformProfileData(singleProfileData, matrix));
            }

            return resProfileDatas;
        }

        private PlaceInputProfileData TransformProfileData(PlaceInputProfileData profileData, Matrix3d matrix)
        {
            var mainBeam = profileData.MainBeamOuterProfile;
            var secondBeams = profileData.SecondBeamProfiles;

            secondBeams.ForEach(e =>
            {
                e.TransformBy(matrix);
            });

            mainBeam.TransformBy(matrix);
            return new PlaceInputProfileData(mainBeam, secondBeams);
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
            polylines.ForEach(e => { placeInputDatas.Add(new PlaceInputProfileData(e, null)); });
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
            polylines.ForEach(e => { placeInputDatas.Add(new PlaceInputProfileData(e, null)); });
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
