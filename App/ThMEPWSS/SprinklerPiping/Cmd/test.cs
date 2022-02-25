using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using NetTopologySuite.Operation.Relate;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Command;

using ThMEPWSS.DrainageSystemDiagram;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.SprinklerConnect.Model;

using ThMEPWSS.SprinklerPiping.Engine;
using ThMEPWSS.SprinklerPiping.Model;
using ThMEPWSS.SprinklerConnect;
using NetTopologySuite.Geometries;
using ThMEPWSS.SprinklerPiping.Service;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPWSS.SprinklerPiping.Cmd
{
    public partial class test
    {
        [CommandMethod("TIANHUACAD", "pipetesting", CommandFlags.Modal)]
        public void SprinklerPiping()
        {
            var cmd = new testCmd();
            cmd.Execute();
        }
    }

    class testCmd : ThMEPBaseCommand
    {
        public testCmd() { }
        public override void SubExecute()
        {
            SprinklerPipingExecute();
        }

        public void SprinklerPipingExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var regionPts = SelectRecPoints("\n请选择左上角点", "\n请再选择右下角点");
                if (regionPts.Item1 == regionPts.Item2)
                {
                    return;
                }

                var frame = toFrame(regionPts);
                if (frame == null || frame.NumberOfVertices == 0)
                {
                    return;
                }

                var lineList = GetLine(frame, "0");
                var t = lineList[0].Intersect(lineList[1], 0).Count;
            }
        }

        private static Polyline toFrame(Tuple<Point3d, Point3d> leftRight)
        {
            var pl = new Polyline();
            var ptRT = new Point2d(leftRight.Item2.X, leftRight.Item1.Y);
            var ptLB = new Point2d(leftRight.Item1.X, leftRight.Item2.Y);

            pl.AddVertexAt(pl.NumberOfVertices, leftRight.Item1.ToPoint2D(), 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, ptRT, 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, leftRight.Item2.ToPoint2D(), 0, 0, 0);
            pl.AddVertexAt(pl.NumberOfVertices, ptLB, 0, 0, 0);

            pl.Closed = true;

            return pl;
        }


        public static List<Line> GetLine(Polyline frame, string layer)
        {
            var lineList = new List<Line>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var extractService = new ThExtractLineService()
                {
                    ElementLayer = layer,

                };
                extractService.Extract(acadDatabase.Database, frame.Vertices());
                lineList.AddRange(extractService.Lines);
            }
            //lineList.ForEach(x => x.Closed = true);
            return lineList;

        }

        private Tuple<Point3d, Point3d> SelectRecPoints(string commandSuggestStrLeft, string commandSuggestStrRight)
        {
            var ptLeftRes = Active.Editor.GetPoint(commandSuggestStrLeft);
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Active.Editor.GetCorner(commandSuggestStrRight, leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                var rightTopPt = ptRightRes.Value;
                leftDownPt = leftDownPt.TransformBy(Active.Editor.UCS2WCS());
                rightTopPt = rightTopPt.TransformBy(Active.Editor.UCS2WCS());
                return Tuple.Create(leftDownPt, rightTopPt);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }


        private static Line selectLine()
        {
            var line = new Line();

            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "请选择布置区域框线",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                    RXClass.GetClass(typeof(Line)).DxfName,

            };
            var layers = new List<string> { "AI-防火分区" };
            var filter = ThSelectionFilterTool.Build(dxfNames, layers.ToArray());
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return line;
            }
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {   //获取外包框
                    var frame = acdb.Element<Line>(obj);
                    line = frame;
                }
            }

            return line;

        }

    }
}
