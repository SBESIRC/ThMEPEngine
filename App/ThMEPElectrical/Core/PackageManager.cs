using AcHelper;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Business;
using ThMEPElectrical.Business.BlindAreaReminder;
using ThMEPElectrical.Business.Operation;
using ThMEPElectrical.Business.Procedure;
using ThMEPElectrical.CAD;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Operation;

namespace ThMEPElectrical.Core
{
    public class PackageManager
    {
        private PlaceParameter Parameter { get; set; }
        public PackageManager(PlaceParameter parameter)
        {
            Parameter = parameter;
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
            var wallPolylines = EntityPicker.MakeUserPickPolys();
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
            var inputProfileDatas = new List<PlaceInputProfileData>();
            var preWindow = PreWindowSelector.GetSelectRectPoints();
            if (preWindow.Count == 0)
                return inputProfileDatas;

            // 用户选择
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return inputProfileDatas;

            // 前置数据读取器
            var infoReader = new InfoReader(preWindow, Parameter.RoofThickness);
            infoReader.Do();

            // 外墙轮廓数据
            foreach (var poly in wallPolylines)
            {
                var wallPtCollection = poly.Vertices();
                var innerHoles = GetValidProfiles(infoReader.RecognizeMainBeamColumnWalls, wallPtCollection);
                var secondBeams = GetValidProfileInfos(infoReader.RecognizeSecondBeams, wallPtCollection);

                //DrawUtils.DrawProfile(SecondBeamProfile2Polyline(infoReader.RecognizeSecondBeams).Polylines2Curves(), "RecognizeSecondBeams");
                //DrawUtils.DrawProfile(infoReader.RecognizeMainBeamColumnWalls.Polylines2Curves(), "RecognizeMainBeamColumnWalls");
                // 外墙，内洞，次梁
                var profileDatas = BeamDetectionCalculator.MakeDetectionData(poly, innerHoles, secondBeams);
                // 主次梁信息
                inputProfileDatas.AddRange(profileDatas);
            }

            return inputProfileDatas;
        }

        /// <summary>
        /// 计算主次梁的梁跨信息
        /// </summary>
        /// <returns></returns>
        public List<UcsPlaceInputProfileData> DoMainSecondBeamProfilesWithUcs(out ThMEPOriginTransformer originTransformer)
        {
            originTransformer = null;
            var ucsInputProfileDatas = new List<UcsPlaceInputProfileData>();
            var preWindow = PreWindowSelector.GetSelectRectPoints();
            if (preWindow.Count == 0)
                return ucsInputProfileDatas;

            // 用户选择Curves
            var wallCurves = EntityPicker.MakeUserPickCurves();
            if (wallCurves.Count == 0)
                return ucsInputProfileDatas;

            var pt = preWindow.Envelope().CenterPoint();
            originTransformer = new ThMEPOriginTransformer(pt);
            var points = new List<Point3d>();
            foreach (var preWPoint in preWindow.ToPointList())
            {
                var newPoint = new Point3d(preWPoint.X, preWPoint.Y, preWPoint.Z);
                originTransformer.Transform(newPoint);
                points.Add(newPoint);
            }
            //preWindow = new Point3dCollection(points.ToArray());
            var transOutLine = new List<Curve>();
            foreach (var curve in wallCurves)
            {
                var copyCurve = (Curve)curve.Clone();
                originTransformer.Transform(copyCurve);
                transOutLine.Add(copyCurve);
            }

            var wallPairInfos = UserCoordinateWorker.MakeUserCoordinateWorker(transOutLine, ThMEPCommon.UCS_COMPASS_LAYER_NAME, originTransformer);

            // 前置数据读取器
            var infoReader = new InfoReader(preWindow, Parameter.RoofThickness);
            infoReader.Do();

            // 建立映射关系对
            foreach (var pairInfo in wallPairInfos)
            {
                var tempOute = (Polyline)pairInfo.ExternalProfile.Clone();
                originTransformer.Reset(tempOute);
                DrawUtils.DrawProfileDebug(new List<Curve> { tempOute }, "drawCurves11");
                var wallPtCollection = pairInfo.ExternalProfile.Vertices();
                //var innerHoles = GetValidProfiles(infoReader.RecognizeMainBeamColumnWalls, wallPtCollection);
                var tempHoles = new List<Polyline>();
                foreach (var item in infoReader.RecognizeMainBeamColumnWalls)
                {
                    var copy = (Polyline)item.Clone();
                    originTransformer.Transform(copy);
                    tempHoles.Add(copy);
                }
                var innerHoles = GetValidProfiles(tempHoles, wallPtCollection);

                //var secondBeams = GetValidProfileInfos(infoReader.RecognizeSecondBeams, wallPtCollection);
                var transSecondBeams = TransSecondBeamProfileInfo(infoReader.RecognizeSecondBeams, originTransformer);
                var secondBeams = GetValidProfileInfos(transSecondBeams, wallPtCollection);
                //DrawUtils.DrawProfile(infoReader.RecognizeMainBeamColumnWalls.Polylines2Curves(), "innerHoles11");
                //var drawCurves = SecondBeamProfile2Polyline(transSecondBeams).Polylines2Curves();
                //DrawUtils.DrawProfileDebug(drawCurves, "drawCurves11");
                // 外墙，内洞，次梁
                var profileDatas = BeamDetectionCalculatorEx.MakeDetectionDataEx(pairInfo.ExternalProfile, innerHoles, secondBeams,originTransformer);

                var validProfileDatas = ValidInputPairInfoCalculator.MakeValidInputPairInfoCalculator(profileDatas, pairInfo);
                //DrawUtils.DrawGroup(validProfileDatas);
                // 主次梁 坐标系信息
                ucsInputProfileDatas.Add(new UcsPlaceInputProfileData(validProfileDatas, pairInfo.UserSys, pairInfo.rotateAngle));
            }

            return ucsInputProfileDatas;
        }

        /// <summary>
        /// 计算梁吊顶的信息
        /// </summary>
        /// <returns></returns>
        public List<UcsPlaceInputProfileData> DoGridBeamProfilesWithUcs(out ThMEPOriginTransformer originTransformer)
        {
            originTransformer = null;
            var ucsInputProfileDatas = new List<UcsPlaceInputProfileData>();
            var preWindow = PreWindowSelector.GetSelectRectPoints();
            if (preWindow.Count == 0)
                return ucsInputProfileDatas;
            // 用户选择
            var wallCurves = EntityPicker.MakeUserPickCurves();
            if (wallCurves.Count == 0)
                return ucsInputProfileDatas;

            var pt = preWindow.Envelope().CenterPoint();
            originTransformer = new ThMEPOriginTransformer(pt);
            var points = new List<Point3d>();
            foreach (var preWPoint in preWindow.ToPointList())
            {
                var newPoint = new Point3d(preWPoint.X, preWPoint.Y, preWPoint.Z);
                originTransformer.Transform(newPoint);
                points.Add(newPoint);
            }
            //preWindow = new Point3dCollection(points.ToArray());
            var transOutLine = new List<Curve>();
            foreach (var curve in wallCurves)
            {
                var copyCurve = (Curve)curve.Clone();
                originTransformer.Transform(copyCurve);
                transOutLine.Add(copyCurve);
            }

            var wallPairInfos = UserCoordinateWorker.MakeUserCoordinateWorker(transOutLine, ThMEPCommon.UCS_COMPASS_LAYER_NAME, originTransformer);
            // 前置数据读取器
            var infoReader = new InfoReader(preWindow, Parameter.RoofThickness);
            infoReader.Do();

            var gridPolys = new List<Polyline>();

            var innerHoles = new List<Polyline>();

            var swallColumnHoles = new List<Polyline>();
            // 建立映射关系对
            foreach (var pairInfo in wallPairInfos)
            {
                var wallPtCollection = pairInfo.ExternalProfile.Vertices();
                innerHoles.Clear();
                // 所有的内部洞数据, 收集主次梁-柱子剪力墙等数据
                var tempHoles = new List<Polyline>();
                foreach (var item in infoReader.RecognizeMainBeamColumnWalls)
                {
                    var copy = (Polyline)item.Clone();
                    originTransformer.Transform(copy);
                    tempHoles.Add(copy);
                }
                //innerHoles.AddRange(infoReader.RecognizeMainBeamColumnWalls);
                innerHoles.AddRange(tempHoles);
                var transSecondBeams = TransSecondBeamProfileInfo(infoReader.RecognizeSecondBeams, originTransformer);
                swallColumnHoles.AddRange(infoReader.RecognizeMainBeamColumnWalls);

                //infoReader.RecognizeSecondBeams.ForEach(e => innerHoles.Add(e.Profile));
                transSecondBeams.ForEach(c => innerHoles.Add(c.Profile));
                var validHoles = GetValidProfiles(innerHoles, wallPtCollection);
                var validSwallColumnHoles = GetValidProfiles(swallColumnHoles, wallPtCollection);
                //var validColumns = GetValidProfiles(infoReader.Columns, wallPtCollection);
                //DrawUtils.DrawProfile(GeometryTrans.MatrixSystemCurves(pairInfo.OriginMatrix, 100), "drawMatrix");
                //轴网线
                var gridCalculator = new GridService();
                var columns = new List<Polyline>();
                foreach (var item in infoReader.Columns) 
                {
                    var copy = (Polyline)item.Clone();
                    originTransformer.Transform(copy);
                    columns.Add(copy);
                }
                var columnTrans = TransformPolylines(columns, pairInfo.UserSys);
                //DrawUtils.DrawProfile(columnTrans.Polylines2Curves(), "columnTrans");
                //DrawUtils.DrawProfile(new List<Curve>() { pairInfo.ExternalProfile }, "ExternalProfile");

                var gridInfo = gridCalculator.CreateGrid(pairInfo.ExternalProfile.GetTransformedCopy(pairInfo.UserSys) as Polyline, columnTrans, pairInfo.UserSys.Inverse(), ThMEPCommon.spacingValue);
                //DrawGridInfos(gridInfo);
                gridInfo = BothExtendPolys(gridInfo);

                gridInfo = TransfromGridInfos(gridInfo, pairInfo.UserSys.Inverse());
                //var obbExternal = new DBObjectCollection() { pairInfo.ExternalProfile }.GetMinimumRectangle(100);
                //DrawGridInfos(gridInfo, obbExternal);
                gridPolys.Clear();
                gridInfo.ForEach(e => gridPolys.AddRange(e.Value));
                // 外墙，内洞，轴网
                var profileDatas = GridDetectionCalculator.MakeGridDetectionCalculator(pairInfo.ExternalProfile, gridPolys, validHoles, validSwallColumnHoles,originTransformer);
                var validProfileDatas = ValidInputPairInfoCalculator.MakeValidInputPairInfoCalculator(profileDatas, pairInfo);
                DrawUtils.DrawGroup(validProfileDatas,originTransformer);
                ucsInputProfileDatas.Add(new UcsPlaceInputProfileData(validProfileDatas, pairInfo.UserSys, pairInfo.rotateAngle));
            }

            return ucsInputProfileDatas;
        }

        /// <summary>
        /// 计算无梁楼盖的信息
        /// </summary>
        /// <returns></returns>
        public List<UcsPlaceInputProfileData> DoNoBeamStoreyProfilesWithUcs(out ThMEPOriginTransformer originTransformer)
        {
            originTransformer = null;
            var ucsInputProfileDatas = new List<UcsPlaceInputProfileData>();
            var preWindow = PreWindowSelector.GetSelectRectPoints();
            if (preWindow.Count == 0)
                return ucsInputProfileDatas;

            var wallCurves = EntityPicker.MakeUserPickCurves();
            if (wallCurves.Count == 0)
                return ucsInputProfileDatas;


            var pt = preWindow.Envelope().CenterPoint();
            originTransformer = new ThMEPOriginTransformer(pt);
            var transOutLine = new List<Curve>();
            foreach (var curve in wallCurves)
            {
                var copyCurve = (Curve)curve.Clone();
                originTransformer.Transform(copyCurve);
                transOutLine.Add(copyCurve);
            }

            var wallPairInfos = UserCoordinateWorker.MakeUserCoordinateWorker(transOutLine, ThMEPCommon.UCS_COMPASS_LAYER_NAME, originTransformer);
            // 前置数据读取器
            var infoReader = new InfoReader(preWindow, Parameter.RoofThickness);
            infoReader.PickColumnAndShearWall(); // 提取和剪力墙
            var gridPolys = new List<Polyline>();
            // 建立映射关系对
            foreach (var pairInfo in wallPairInfos)
            {
                var wallPtCollection = pairInfo.ExternalProfile.Vertices();
                //var validColumns = infoReader.Columns;
                var validColumns = new List<Polyline>();
                foreach (var item in infoReader.Columns)
                {
                    var copy = (Polyline)item.Clone();
                    originTransformer.Transform(copy);
                    validColumns.Add(copy);
                }
                var columnTrans = TransformPolylines(validColumns, pairInfo.UserSys);
                var tempWalls = new List<Polyline>();
                foreach (var item in infoReader.RecognizeShearWalls)
                {
                    var copy = (Polyline)item.Clone();
                    originTransformer.Transform(copy);
                    tempWalls.Add(copy);
                }
                var validShearWalls = GetValidProfiles(tempWalls, wallPtCollection);

                //轴网线
                var gridCalculator = new GridService();
                //DrawUtils.DrawProfile(columnTrans.Polylines2Curves(), "columnTrans");
                //DrawUtils.DrawProfile(new List<Curve>() { pairInfo.ExternalProfile }, "ExternalProfile");

                var gridInfo = gridCalculator.CreateGrid(pairInfo.ExternalProfile.GetTransformedCopy(pairInfo.UserSys) as Polyline, columnTrans, pairInfo.UserSys.Inverse(), ThMEPCommon.spacingValue);
                //DrawGridInfos(gridInfo);
                gridInfo = BothExtendPolys(gridInfo);

                gridInfo = TransfromGridInfos(gridInfo, pairInfo.UserSys.Inverse());
                //DrawGridInfos(gridInfo);
                gridPolys.Clear();
                gridInfo.ForEach(e => gridPolys.AddRange(e.Value));
                // 外墙，内洞，轴网
                validColumns.AddRange(validShearWalls);
                var profileDatas = NoBeamStoreyDetectionCalculator.MakeNoBeamStoreyDetectionCalculator(gridPolys, validColumns, pairInfo.ExternalProfile,originTransformer);
                var validProfileDatas = ValidInputPairInfoCalculator.MakeValidInputPairInfoCalculator(profileDatas, pairInfo);
                DrawUtils.DrawGroup(validProfileDatas,originTransformer);
                ucsInputProfileDatas.Add(new UcsPlaceInputProfileData(validProfileDatas, pairInfo.UserSys, pairInfo.rotateAngle));
            }

            return ucsInputProfileDatas;
        }

        private List<KeyValuePair<Vector3d, List<Polyline>>> TransfromGridInfos(List<KeyValuePair<Vector3d, List<Polyline>>> srcGridInfos, Matrix3d matrix)
        {
            var resGridInfos = new List<KeyValuePair<Vector3d, List<Polyline>>>();
            if (srcGridInfos.Count == 0)
                return resGridInfos;

            foreach (var pairValue in srcGridInfos)
            {
                resGridInfos.Add(new KeyValuePair<Vector3d, List<Polyline>>(pairValue.Key, TransformPolylines(pairValue.Value, matrix)));
            }

            return resGridInfos;
        }

        private void DrawGridInfos(List<KeyValuePair<Vector3d, List<Polyline>>> gridInfos, Polyline externalProfile)
        {
            var drawCurves = new List<Curve>();
            foreach (var pairValue in gridInfos)
            {
                drawCurves.AddRange(pairValue.Value);
            }

            var resExtendPolys = new List<Polyline>();
            foreach (Polyline offsetPoly in externalProfile.Buffer(ThMEPCommon.GridPolyExtendLength * 4))
            {
                resExtendPolys.Add(offsetPoly);
            }

            if (resExtendPolys.Count == 0)
                return;

            var validExternalProfile = resExtendPolys.First();

            var vadlidCurves = drawCurves.Where(p =>
            {
                return GeomUtils.PtInLoop(validExternalProfile, p.StartPoint);
            }).ToList();
            DrawUtils.DrawProfileDebug(vadlidCurves, "gridInfos");
        }

        private List<Polyline> TransformPolylines(List<Polyline> srcPolys, Matrix3d transMatrix)
        {
            var polys = new List<Polyline>();
            foreach (var poly in srcPolys)
            {
                polys.Add(poly.GetTransformedCopy(transMatrix) as Polyline);
            }

            return polys;
        }

        private List<Polyline> SecondBeamProfile2Polyline(List<SecondBeamProfileInfo> secondBeamProfiles)
        {
            var polys = new List<Polyline>();
            secondBeamProfiles.ForEach(e => polys.Add(e.Profile));
            return polys;
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
            if (IsIntersect(polyFir, PolySec))
                return true;

            var ptCollection = PolySec.Vertices();
            foreach (Point3d pt in ptCollection)
            {
                if (GeomUtils.PtInLoop(polyFir, pt))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 包含的不算
        /// </summary>
        /// <param name="firstPly"></param>
        /// <param name="secPly"></param>
        /// <returns></returns>
        private bool IsIntersect(Polyline firstPly, Polyline secPly)
        {
            if (GeomUtils.IsIntersectValid(firstPly, secPly))
            {
                var ptLst = new Point3dCollection();
                firstPly.IntersectWith(secPly, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
                if (ptLst.Count != 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 轴网测试
        /// </summary>
        public void DoGridTestProfiles()
        {
            // 用户选择
            var preWindow = PreWindowSelector.GetSelectRectPoints();
            if (preWindow.Count == 0)
                return;
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return;

            // 前置数据读取器
            var infoReader = new InfoReader(preWindow, Parameter.RoofThickness);
            infoReader.PickColumnAndShearWall(); // 提取柱子和剪力墙
            var gridPolys = new List<Polyline>();

            // 外墙轮廓数据
            foreach (var poly in wallPolylines)
            {
                var wallPtCollection = poly.Vertices();
                var validColumns = GetValidProfiles(infoReader.Columns, wallPtCollection);
                var gridCalculator = new GridService();

                //DrawUtils.DrawProfile(validColumns.Polylines2Curves(), "validColumns");
                //轴网线
                var gridInfo = gridCalculator.CreateGrid(poly, validColumns, new Matrix3d(), ThMEPCommon.spacingValue);
                gridInfo = BothExtendPolys(gridInfo);
                gridPolys.Clear();
                gridInfo.ForEach(e => gridPolys.AddRange(e.Value));
                //DrawUtils.DrawProfile(gridPolys.Polylines2Curves(), "gridPolys");
            }
        }

        /// <summary>
        /// 轴网测试
        /// </summary>
        public void DoGridTestProfilesWithUcs()
        {
            var preWindow = PreWindowSelector.GetSelectRectPoints();
            if (preWindow.Count == 0)
                return;
            // 用户选择
            var wallCurves = EntityPicker.MakeUserPickCurves();
            if (wallCurves.Count == 0)
                return;

            var wallPairInfos = UserCoordinateWorker.MakeUserCoordinateWorker(wallCurves, ThMEPCommon.UCS_COMPASS_LAYER_NAME,null);
            // 前置数据读取器
            var infoReader = new InfoReader(preWindow, Parameter.RoofThickness);
            infoReader.Do();

            // 建立映射关系对
            foreach (var pairInfo in wallPairInfos)
            {
                //轴网线
                var gridCalculator = new GridService();
                var columnTrans = TransformPolylines(infoReader.Columns, pairInfo.UserSys);
                var gridInfo = gridCalculator.CreateGrid(pairInfo.ExternalProfile.GetTransformedCopy(pairInfo.UserSys) as Polyline, columnTrans, pairInfo.UserSys.Inverse(), ThMEPCommon.spacingValue);
                gridInfo = BothExtendPolys(gridInfo);
                gridInfo = TransfromGridInfos(gridInfo, pairInfo.UserSys.Inverse());
                var obbExternal = new DBObjectCollection() { pairInfo.ExternalProfile }.GetMinimumRectangle();
                DrawGridInfos(gridInfo, obbExternal);
            }
        }

        public List<KeyValuePair<Vector3d, List<Polyline>>> BothExtendPolys(List<KeyValuePair<Vector3d, List<Polyline>>> gridInfo)
        {
            var resGridInfos = new List<KeyValuePair<Vector3d, List<Polyline>>>();
            if (gridInfo.Count == 0)
                return resGridInfos;

            for (int i = 0; i < gridInfo.Count; i++)
            {
                var pairData = gridInfo[i];
                var polys = pairData.Value;
                var resPolys = new List<Polyline>();
                foreach (var poly in polys)
                {
                    var firstPoint = poly.GetPoint2dAt(0);
                    var lastPoint = poly.GetPoint2dAt(poly.NumberOfVertices - 1);
                    var extend = (lastPoint - firstPoint).GetNormal() * ThMEPCommon.GridPolyExtendLength;
                    var resfirstPoint = firstPoint - extend;
                    
                    var reslastPoint = lastPoint + extend;

                    var resPoly = new Polyline();
                    for (int j = 0; j < poly.NumberOfVertices; j++)
                    {
                        if (j == 0)
                        {
                            resPoly.AddVertexAt(j, resfirstPoint, 0, 0, 0);
                        }
                        else if (j == poly.NumberOfVertices - 1)
                        {
                            resPoly.AddVertexAt(j, reslastPoint, 0, 0, 0);
                        }
                        else
                        {
                            resPoly.AddVertexAt(j, poly.GetPoint2dAt(j), 0, 0, 0);
                        }
                    }

                    resPolys.Add(resPoly);
                }

                resGridInfos.Add(new KeyValuePair<Vector3d, List<Polyline>>(pairData.Key, resPolys));
            }

            return resGridInfos;
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
                BlockInsertor.MakeBlockInsert(ptLst, placePara.sensorType,0,placePara.BlockScale);

                var circles = GeometryTrans.Points2Circles(ptLst, placePara.ProtectRadius, Vector3d.ZAxis);
                var curves = GeometryTrans.Circles2Curves(circles);
                DrawUtils.DrawProfileDebug(curves, ThMEPCommon.PROTECTAREA_LAYER_NAME, Color.FromRgb(0, 0, 255));
            }

            return ptLst;
        }

        public void DoBlindAreaReminder()
        {
            var polygons = BlindReminderCalculator.MakeBlindAreaReminderCalculator(Parameter.ProtectRadius);
            HatchCreater.MakeHatchCreater(polygons);
        }

        /// <summary>
        /// 计算主次梁 自定义坐标系的布置点集
        /// </summary>
        /// <returns></returns>
        public void DoMainSecondBeamPlacePointsWithUcs()
        {
            ThMEPOriginTransformer originTransformer;
            // 计算主次梁 和ucs 信息
            var ucsInputProfileDatas = DoMainSecondBeamProfilesWithUcs(out originTransformer);
            if (ucsInputProfileDatas.Count == 0)
                return;

            // 插入点的计算
            DoUcsInputDataPlace(ucsInputProfileDatas, originTransformer);
        }

        public void DoUcsInputDataPlace(List<UcsPlaceInputProfileData> ucsInputProfileDatas, ThMEPOriginTransformer originTransformer)
        {
            if (ucsInputProfileDatas.Count == 0)
                return;
            // 插入点的计算
            foreach (var ucsProfileData in ucsInputProfileDatas)
            {
                var wcs2Ucs = ucsProfileData.UcsMatrix;
                var ucs2Wcs = wcs2Ucs.Inverse();
                var ptLst = new List<Point3d>();
                var transformPlaceInputDatas = TransformProfileDatas(ucsProfileData.PlaceInputProfileDatas, wcs2Ucs);
                // ucs 插入点
                var tempPts = PlacePointCalculator.MakeCalculatePlacePoints(transformPlaceInputDatas, Parameter);
                tempPts.ForEach(pt => ptLst.Add(pt.TransformBy(ucs2Wcs)));
                if (ptLst.Count > 0)
                {
                    var transPoints = new List<Point3d>();
                    if (null != originTransformer)
                    {
                        foreach (var item in ptLst)
                        {
                            var transPoint = new Point3d(item.X, item.Y, item.Z);
                            originTransformer.Reset(ref transPoint);
                            transPoints.Add(transPoint);
                        }
                    }
                    else
                    {
                        transPoints.AddRange(ptLst);
                    }
                    BlockInsertor.MakeBlockInsert(transPoints, Parameter.sensorType, ucsProfileData.rotateAngle, Parameter.BlockScale);
                    var circles = GeometryTrans.Points2Circles(transPoints, Parameter.ProtectRadius, Vector3d.ZAxis);
                    var curves = GeometryTrans.Circles2Curves(circles);
                    DrawUtils.DrawProfileDebug(curves, ThMEPCommon.PROTECTAREA_LAYER_NAME, Color.FromRgb(0, 0, 255));
                }
            }
        }

        public void DoGridBeamPlacePointsWithUcs()
        {
            ThMEPOriginTransformer originTransformer;
            // 计算轴网和梁结构关系
            var ucsInputProfileDatas = DoGridBeamProfilesWithUcs(out originTransformer);
            if (ucsInputProfileDatas.Count == 0)
                return;

            // 插入点的计算
            DoUcsInputDataPlace(ucsInputProfileDatas, originTransformer);
        }

        /// <summary>
        /// 无梁楼盖布置
        /// </summary>
        /// <returns></returns>
        public void DoNoBeamPlacePointsWithUcs()
        {
            ThMEPOriginTransformer originTransformer;
            // 计算轴网和梁结构关系
            var ucsInputProfileDatas = DoNoBeamStoreyProfilesWithUcs(out originTransformer);
            if (ucsInputProfileDatas.Count == 0)
                return;

            // 插入点的计算
            DoUcsInputDataPlace(ucsInputProfileDatas, originTransformer);
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

            var holes = profileData.Holes;
            var holesTrans = new List<Polyline>();
            holes.ForEach(hole =>
            {
                holesTrans.Add(hole.GetTransformedCopy(matrix) as Polyline);
            });

            var secondBeamsTrans = new List<Polyline>();
            secondBeams.ForEach(e =>
            {
                secondBeamsTrans.Add(e.GetTransformedCopy(matrix) as Polyline);
            });

            var cloneMainBeamTrans = mainBeam.GetTransformedCopy(matrix) as Polyline;
            return new PlaceInputProfileData(cloneMainBeamTrans, secondBeamsTrans, holesTrans);
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
                BlockInsertor.MakeBlockInsert(tempPts, placePara.sensorType,0,placePara.BlockScale);

                var circles = GeometryTrans.Points2Circles(ptLst, placePara.ProtectRadius, Vector3d.ZAxis);
                var curves = GeometryTrans.Circles2Curves(circles);
                DrawUtils.DrawProfileDebug(curves, ThMEPCommon.PROTECTAREA_LAYER_NAME);
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
                BlockInsertor.MakeBlockInsert(tempPts, placePara.sensorType,0,placePara.BlockScale);

                var circles = GeometryTrans.Points2Circles(ptLst, placePara.ProtectRadius, Vector3d.ZAxis);
                var curves = GeometryTrans.Circles2Curves(circles);
                DrawUtils.DrawProfileDebug(curves, ThMEPCommon.PROTECTAREA_LAYER_NAME);
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

            //DrawUtils.DrawProfile(rectPolylines.Polylines2Curves(), "rectPolyline");
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

            //DrawUtils.DrawProfile(rectPolylines.Polylines2Curves(), "rectPolyline");
        }
        private List<SecondBeamProfileInfo> TransSecondBeamProfileInfo(List<SecondBeamProfileInfo> secondBeams, ThMEPOriginTransformer originTransformer)
        {
            var transBeams = new List<SecondBeamProfileInfo>();
            foreach (var beamProfileInfo in secondBeams)
            {
                var pline = (Polyline)beamProfileInfo.Profile.Clone();
                originTransformer.Transform(pline);
                var transBeam = new SecondBeamProfileInfo(pline, beamProfileInfo.Height);
                if (null != beamProfileInfo.RelatedSecondBeams && beamProfileInfo.RelatedSecondBeams.Count > 0)
                {
                    transBeam.RelatedSecondBeams.AddRange(TransSecondBeamProfileInfo(beamProfileInfo.RelatedSecondBeams, originTransformer));
                }
                transBeams.Add(transBeam);
            }
            return transBeams;
        }
    }
}
