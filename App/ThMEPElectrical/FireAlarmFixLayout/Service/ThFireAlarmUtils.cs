using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using AcHelper;
using Linq2Acad;
using NFox.Cad;
using ThCADCore.NTS;
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
using ThMEPElectrical.AFAS.Utils;

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

        public static Point3dCollection GetFrame()
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

        public static Point3dCollection GetFrameBlk()
        {
            Point3dCollection pts = new Point3dCollection();

            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
            };
            //var filter = ThSelectionFilterTool.Build(dxfNames, new string[] { ThMEPCommon.FRAME_LAYER_NAME });
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return pts;
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                List<BlockReference> frameLst = new List<BlockReference>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<BlockReference>(obj);
                    frameLst.Add(frame.Clone() as BlockReference);
                }

                List<Polyline> frames = new List<Polyline>();
                foreach (var frameBlock in frameLst)
                {
                    var frame = GetBlockInfo(frameBlock).Where(x => x is Polyline).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                    if (frame != null)
                    {
                        frames.Add(frame);
                    }
                }

                var frameL = frames.OrderByDescending(x => x.Area).FirstOrDefault();

                if (frameL != null && frameL.Area > 10)
                {
                    frameL = ProcessFrame(frameL);
                }
                if (frameL != null && frameL.Area > 10)
                {
                    pts = frameL.Vertices();
                }
            }

            return pts;
        }

        private static List<Entity> GetBlockInfo(BlockReference blockReference)
        {
            var matrix = blockReference.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var results = new List<Entity>();
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                foreach (var objId in blockTableRecord)
                {
                    var dbObj = acadDatabase.Element<Entity>(objId).Clone() as Entity;
                    dbObj.TransformBy(matrix);
                    results.Add(dbObj);
                }
                return results;
            }
        }

        public static List<ThGeometry> GetSmokeData(Point3dCollection pts, List<string> extractBlkList, bool referBeam, double wallThick,bool needDetective)
        {
            var bReadJson = false;

            var geos = new List<ThGeometry>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if (bReadJson == false)
                {
                    var datasetFactory = new ThFaAreaLayoutDataSetFactory()
                    {
                        ReferBeam = referBeam,
                        WallThick = wallThick,
                        NeedDetective= needDetective,
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

        public static List<ThGeometry> GetFixLayoutData(Point3dCollection pts, List<string> extractBlkList)
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
        public static ThMEPOriginTransformer TransformToOrig(Point3dCollection pts, List<ThGeometry> geos)
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

            geos.MoveToXYPlane();

            return transformer;
        }

        /// <summary>
        /// 读取房间配置表
        /// </summary>
        public static List<RoomTableTree> ReadRoomConfigTable(string roomConfigUrl)
        {
            var roomTableConfig = new List<RoomTableTree>();
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(roomConfigUrl, true);
            var roomNameControl = "房间名称处理";
            var table = dataSet.Tables[roomNameControl];
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
        public static List<ThGeometry> WriteSmokeData(Point3dCollection pts, List<string> extractBlkList, bool referBeam, double wallThick,bool needDetective)
        {
            var fileInfo = new FileInfo(Active.Document.Name);
            var path = fileInfo.Directory.FullName;

            var geos = new List<ThGeometry>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var datasetFactory = new ThFaAreaLayoutDataSetFactory()
                {
                    ReferBeam = referBeam,
                    WallThick = wallThick,
                    NeedDetective = needDetective,
                }; 
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
                    var nFrame = ProcessFrame(frameTemp);
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

        private static Polyline ProcessFrame(Polyline frame)
        {
            Polyline nFrame = null;
            var tol = 1000;

            Polyline nFrameNormal = ThMEPFrameService.Normalize(frame);
            // Polyline nFrameNormal = ThMEPFrameService.NormalizeEx(frame, tol);
            if (nFrameNormal.Area > 10)
            {
                nFrameNormal = nFrameNormal.DPSimplify(1);
                nFrame = nFrameNormal;
            }

            return nFrame;

        }

        #endregion
    }
}
