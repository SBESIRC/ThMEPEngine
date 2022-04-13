using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using AcHelper;
using System.Collections.Generic;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.UCSDivisionService;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using System.Linq;
using ThMEPEngineCore.GridOperation;
using ThMEPEngineCore.GridOperation.Model;
using ThMEPEngineCore.UCSDivisionService.DivisionMethod;
using NFox.Cad;

namespace ThMEPElectrical
{
    public class ThEarthingGridCmds
    {
        /// <summary>
        /// ucs分区
        /// </summary>
        [CommandMethod("TIANHUACAD", "THUCSDIV", CommandFlags.Modal)]
        public void ThUcsDisivision()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                List<Polyline> frameLst = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(Point3d.Origin);
                foreach (var frame in frameLst)
                {
                    var simiplyFrame = frame.DPSimplify(5);
                    GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                    getPrimitivesService.GetStructureInfo(frame, out List<Polyline> columns, out List<Polyline> walls);

                    //区域分割
                    UCSService uCSService = new UCSService();
                    var ucsInfo = uCSService.UcsDivision(columns, frame);
                    foreach (var item in ucsInfo)
                    {
                        acadDatabase.ModelSpace.Add(item.Key);
                    }
                }
            }
        }

        /// <summary>
        /// ucs分区
        /// </summary>
        [CommandMethod("TIANHUACAD", "THGRIDDIV", CommandFlags.Modal)]
        public void ThGridUcsDisivision()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                // 从外参中提取房间
                var frame = acadDatabase.Element<Polyline>(result.ObjectId);

                var axisEngine = new ThAXISLineRecognitionEngine();
                axisEngine.Recognize(acadDatabase.Database, frame.Vertices());
                var retAxisCurves = new List<Curve>();
                foreach (var item in axisEngine.Elements)
                {
                    if (item == null || item.Outline == null)
                        continue;
                    if (item.Outline is Curve curve)
                    {
                        var copy = (Curve)curve.Clone();
                        retAxisCurves.Add(copy);
                    }
                }
                if (retAxisCurves.Count <= 0)
                {
                    retAxisCurves = GetAxis(frame);
                }

                GetStructureInfo(acadDatabase, frame, out List<Polyline> columns, out List<Polyline> walls);

                GridLineCleanService gridLineClean = new GridLineCleanService();
                gridLineClean.CleanGrid(retAxisCurves, columns, out List<LineGridModel> lineGirds, out List<ArcGridModel> arcGrids);

                var curves = new List<List<Curve>>(lineGirds.Select(x => { var lines = new List<Curve>(x.xLines); lines.AddRange(x.yLines); return lines; }));
                curves.Add(arcGrids.SelectMany(x => { var lines = new List<Curve>(x.arcLines); lines.AddRange(x.lines); return lines; }).ToList());
                curves = curves.Where(x => x.Count > 0).ToList();
               
                GridDivision gridDivision = new GridDivision();
                var ucsPolygons = gridDivision.DivisionGridRegions(curves);
                foreach (var item in ucsPolygons)
                {
                    var aa = item.GridPolygon;
                    aa.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(aa);
                    foreach (var s in item.regions)
                    {
                        acadDatabase.ModelSpace.Add(s);
                    }
                }
            }
        }

        /// <summary>
        /// 获取轴网线
        /// </summary>
        /// <param name="polyline"></param>
        public List<Curve> GetAxis(Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var axis = acadDatabase.ModelSpace
                    .OfType<Curve>()
                    .ToList();

                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(axis.ToCollection());
                var axisLines = thCADCoreNTSSpatialIndex.SelectWindowPolygon(bufferPoly).Cast<Curve>().ToList();

                return axisLines;
            }
        }

        /// <summary>
        /// 获取构建信息
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="beams"></param>
        /// <param name="walls"></param>
        private void GetStructureInfo(AcadDatabase acdb, Polyline polyline, out List<Polyline> columns, out List<Polyline> walls)
        {
            var ColumnExtractEngine = new ThColumnExtractionEngine();
            ColumnExtractEngine.Extract(acdb.Database);
            //ColumnExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
            var ColumnEngine = new ThColumnRecognitionEngine();
            ColumnEngine.Recognize(ColumnExtractEngine.Results, polyline.Vertices());

            ////获取柱
            columns = new List<Polyline>();
            columns = ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            // 启动墙识别引擎
            var ShearWallExtractEngine = new ThShearWallExtractionEngine();
            ShearWallExtractEngine.Extract(acdb.Database);
            //ShearWallExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
            var ShearWallEngine = new ThShearWallRecognitionEngine();
            ShearWallEngine.Recognize(ShearWallExtractEngine.Results, polyline.Vertices());

            var archWallExtractEngine = new ThDB3ArchWallExtractionEngine();
            archWallExtractEngine.Extract(acdb.Database);
            //archWallExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
            var archWallEngine = new ThDB3ArchWallRecognitionEngine();
            archWallEngine.Recognize(archWallExtractEngine.Results, polyline.Vertices());

            //获取剪力墙
            walls = new List<Polyline>();
            walls = ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            //获取建筑墙
            foreach (var o in archWallEngine.Elements)
            {
                if (o.Outline is Polyline wall)
                {
                    walls.Add(wall);
                }
            }
        }
    }
}
