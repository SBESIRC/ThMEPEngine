using System;
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

using ThMEPLighting.IlluminationLighting.Data;
using ThMEPEngineCore.Extension;

namespace ThMEPLighting.IlluminationLighting.Common
{
    public static class ThIlluminationUtils
    {
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
        
        public static List<ThGeometry> getIlluminationData(Point3dCollection pts, List<string> extractBlkList, bool referBeam)
        {
            var bReadJson = false;

            var geos = new List<ThGeometry>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                if (bReadJson == false)
                {
                    var datasetFactory = new ThIlluminationLayoutDataSetFactory()
                    {
                        ReferBeam = referBeam
                    };
                    var dataset = datasetFactory.Create(acadDatabase.Database, pts);
                    geos.AddRange(dataset.Container);

                    var businessDataFactory = new ThIlluminationLayoutBusinessDataSetFactory()
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
                    var businessDataFactory = new ThIlluminationLayoutBusinessDataSetFactory()
                    {
                        BlkNameList = extractBlkList,
                    };
                    var businessDataSet = businessDataFactory.Create(acadDatabase.Database, pts);
                    geos.AddRange(businessDataSet.Container);
                }
            }

            return geos;
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

            geos.ProjectOntoXYPlane();

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
            var table = dataSet.Tables[ThIlluminationCommon.RoomNameControl];
            if (table != null)
            {
                roomTableConfig = RoomConfigTreeService.CreateRoomTree(table);
            }

            return roomTableConfig;
        }

    }
}
