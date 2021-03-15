using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Service;
using ThMEPWSS.Engine;

namespace ThMEPWSS.Command
{
    public class ThSprinklerLayoutAreaCmd : IAcadCommand, IDisposable
    {
        public bool DoValidate { get; set; }

        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线区域
                var frames = QueryFrames("选择区域");
                if (frames.Count == 0)
                {
                    return;
                }

                // 获取UCS朝向
                if (!ThSprinklerLayoutCmdUtils.CalWCSLayoutDirection(out Matrix3d matrix))
                {
                    return;
                }

                // 获取喷淋点位并建立索引
                var engine = new ThWSprinklerRecognitionEngine();
                engine.Recognize(acdb.Database, new Point3dCollection());
                var spatialIndex = new ThCADCoreNTSSpatialIndex(engine.Elements.Select(o => o.Outline).ToCollection());

                List<Polyline> polylines = new List<Polyline>();
                foreach (ObjectId frame in frames)
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);
                    polylines.Add(plFrame);
                }

                CalHolesService calHolesService = new CalHolesService();
                var holeDic = calHolesService.CalHoles(polylines);
                foreach (var holeInfo in holeDic)
                {
                    var plFrame = holeInfo.Key;
                    var holes = holeInfo.Value;

                    //清除原有构件
                    plFrame.ClearLayouArea();

                    //获取构建信息
                    var calStructPoly = (plFrame.Clone() as Polyline).Buffer(10000)[0] as Polyline;
                    ThSprinklerLayoutCmdUtils.GetStructureInfo(acdb, calStructPoly, plFrame, out List<Polyline> columns, out List<Polyline> beams, out List<Polyline> walls);

                    //转换usc
                    plFrame.TransformBy(matrix.Inverse());
                    holes.ForEach(x => x.TransformBy(matrix.Inverse()));
                    columns.ForEach(x => x.TransformBy(matrix.Inverse()));
                    beams.ForEach(x => x.TransformBy(matrix.Inverse()));
                    walls.ForEach(x => x.TransformBy(matrix.Inverse()));
                    holes.AddRange(walls);

                    //不考虑梁
                    if (!ThWSSUIService.Instance.Parameter.ConsiderBeam)
                    {
                        beams = new List<Polyline>();
                    }

                    //计算可布置区域
                    var layoutAreas = CreateLayoutAreaService.GetLayoutArea(plFrame, beams, columns, holes, 300);

                    //打印可布置区域
                    layoutAreas.ForEach(o => o.TransformBy(matrix));
                    MarkService.PrintLayoutArea(layoutAreas);

                    //打印喷淋点位是否在可布置区域外
                    if (DoValidate)
                    {
                        spatialIndex.SelectCrossingPolygon(plFrame)
                            .Cast<Polyline>()
                            .Where(o => OutOfArea(layoutAreas, o))
                            .ForEach(o => MarkService.PrintOutOfAreaSpray(o));
                    }
                }
            }
        }

        private bool OutOfArea(List<Polyline> areas, Polyline sprinkler)
        {
            var geometry = areas.ToCollection().UnionGeometries();
            return !geometry.Contains(sprinkler.GetCentroidPoint().ToNTSPoint());
        }

        public ObjectIdCollection QueryFrames(string text)
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = text,
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status == PromptStatus.OK)
            {
                return new ObjectIdCollection(result.Value.GetObjectIds());
            }
            return new ObjectIdCollection();
        }
    }
}
