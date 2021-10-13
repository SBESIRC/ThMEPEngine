﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;


using AcHelper;
using Linq2Acad;
using NFox.Cad;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.GeoJSON;
using ThMEPEngineCore.Config;
using ThMEPElectrical.Service;

using ThMEPElectrical.FireAlarmFixLayout.Data;
using ThMEPElectrical.FireAlarmSmokeHeat.Data;

namespace ThMEPElectrical.FireAlarm.Service
{
    public static class ThFireAlarmUtils
    {
        public static bool IsPositiveInfinity(this Point3d pt)
        {
            return double.IsPositiveInfinity(pt.X) ||
                double.IsPositiveInfinity(pt.Y) ||
                double.IsPositiveInfinity(pt.Z);
        }

        public static void MoveToOrigin(this ThBuildingElementVisitorManager vm, ThMEPOriginTransformer transformer)
        {
            vm.DB3ArchWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3WindowVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3RailingVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3CurtainWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3DoorMarkVisitor.Results.ForEach(o =>
            {
                if (o is ThRawDoorMark doorMark)
                {
                    transformer.Transform(doorMark.Data as Entity);
                }
                transformer.Transform(o.Geometry);
            });
            vm.DB3DoorStoneVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
        }

        public static Point3dCollection getFrame()
        {
            Point3dCollection pts = new Point3dCollection();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var frame = ThMEPEngineCore.CAD.ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });

                if (frame.Area > 1e-4)
                {
                    pts = frame.Vertices();
                }

                return pts;
            }
        }

        public static List<ThGeometry> getSmokeData(Point3dCollection pts, List<string> extractBlkList, bool referBeam)
        {
            var bReadJson = false;

            var geos = new List<ThGeometry>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if (bReadJson == false)
                {
                    var datasetFactory = new ThFaAreaLayoutDataSetFactory()
                    {
                        ReferBeam = referBeam
                    };
                    var dataset = datasetFactory.Create(acadDatabase.Database, pts);
                    geos.AddRange(dataset.Container);

                    var businessDataFactory = new ThFaAreaLayoutBusinessDataSetFactory()
                    {
                        BlkNameList = extractBlkList,
                    };
                    var businessDataSet = businessDataFactory.Create(acadDatabase.Database, pts);
                    geos.AddRange(businessDataSet.Container);
                }
                else
                {
                    var psr = Active.Editor.GetFileNameForOpen("\n选择要打开的Geojson文件");
                    if (psr.Status != PromptStatus.OK)
                    {
                        {
                            return geos;
                        }
                    }
                    var sName = psr.StringResult;
                    geos = ThGeometryJsonReader.ReadFromFile(sName);
                    var businessDataFactory = new ThFaAreaLayoutBusinessDataSetFactory()
                    {
                        BlkNameList = extractBlkList,
                    };
                    var businessDataSet = businessDataFactory.Create(acadDatabase.Database, pts);
                    geos.AddRange(businessDataSet.Container);
                }
            }

            return geos;
        }

        public static List<ThGeometry> getFixLayoutData(Point3dCollection pts, List<string> extractBlkList)
        {
            var geos = new List<ThGeometry>();

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var datasetFactory = new ThFaFixLayoutDataSetFactory();
                var dataset = datasetFactory.Create(acadDatabase.Database, pts);
                geos.AddRange(dataset.Container);
                var businessDataFactory = new ThFaFixLayoutBusinessDataSetFactory()
                {
                    BlkNameList = extractBlkList,
                };
                var businessDataSet = businessDataFactory.Create(acadDatabase.Database, pts);
                geos.AddRange(businessDataSet.Container);

                return geos;
            }
        }

        /// <summary>
        /// 将数据转回原点。同时返回transformer
        /// </summary>
        /// <param name="geos"></param>
        /// <returns></returns>
        public static ThMEPOriginTransformer transformToOrig(Point3dCollection pts, List<ThGeometry> geos)
        {
            ThMEPOriginTransformer transformer = null;

            if (pts.Count > 0)
            {
                var center = pts.Envelope().CenterPoint();
                transformer = new ThMEPOriginTransformer(center);
            }

            foreach (var o in geos)
            {
                if (o.Boundary != null)
                {
                    transformer.Transform(o.Boundary);
                }
            }

            ThFireAlarmUtils.MoveToXYPlane(geos);

            return transformer;
        }

        public static void MoveToXYPlane(List<ThGeometry> geos)
        {

            geos.ForEach(g =>
            {
                if (g.Boundary != null)
                {
                    if (g.Boundary is Polyline polyline)
                    {
                        if (polyline.NumberOfVertices == 0)
                        {
                            var a = 0;
                        }
                        else
                        {


                            var vec = new Vector3d(0, 0, -polyline.GetPoint3dAt(0).Z);
                            var mt = Matrix3d.Displacement(vec);
                            g.Boundary.TransformBy(mt);
                        }
                    }
                    else if (g.Boundary is MPolygon mPolygon)
                    {
                        if (mPolygon.Shell().NumberOfVertices == 0)
                        {
                            var a = 0;
                        }
                        else
                        {

                            var vec = new Vector3d(0, 0, -1.0 * mPolygon.Shell().GetPoint3dAt(0).Z);
                            var mt = Matrix3d.Displacement(vec);
                            g.Boundary.TransformBy(mt);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            });
        }

        /// <summary>
        /// 读取房间配置表
        /// </summary>
        public static List<RoomTableTree> ReadRoomConfigTable(string roomConfigUrl)
        {
            var roomTableConfig = new List<RoomTableTree>();
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(roomConfigUrl, true);
            var table = dataSet.Tables[ThElectricalUIService.Instance.Parameter.RoomNameControl];
            if (table != null)
            {
                roomTableConfig = RoomConfigTreeService.CreateRoomTree(table);
            }

            return roomTableConfig;
        }



        #region DebugFunction
        /// <summary>
        /// for debug
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="extractBlkList"></param>
        /// <returns></returns>
        public static List<ThGeometry> writeSmokeData(Point3dCollection pts, List<string> extractBlkList, bool referBeam)
        {
            var fileInfo = new FileInfo(Active.Document.Name);
            var path = fileInfo.Directory.FullName;

            var geos = new List<ThGeometry>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var datasetFactory = new ThFaAreaLayoutDataSetFactory()
                {
                    ReferBeam = referBeam
                }; ;
                var dataset = datasetFactory.Create(acadDatabase.Database, pts);
                geos.AddRange(dataset.Container);

                ThGeoOutput.Output(geos, path, fileInfo.Name);

            }

            return geos;
        }

        /// <summary>
        /// for debug
        /// </summary>
        public static Polyline SelectFrame()
        {
            var frame = new Polyline();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {

                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                Autodesk.AutoCAD.Runtime.RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return frame;
                }

                var frameList = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acdb.Element<Polyline>(obj);
                    var nFrame = processFrame(frameTemp);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    frameList.Add(nFrame);
                }
                frame = frameList.OrderByDescending(x => x.Area).First();

                return frame;
            }
        }

        private static Polyline processFrame(Polyline frame)
        {
            var tol = 1000;
            //获取外包框
            var frameClone = frame.WashClone() as Polyline;
            //处理外包框
            Polyline nFrame = ThMEPFrameService.NormalizeEx(frameClone, tol);

            return nFrame;

        }

        #endregion
    }
}
