using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPElectrical.Command;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical
{
    public class ThAutoFireAlarmSystemCmd
    {

        [CommandMethod("TIANHUACAD", "THHZXTP", CommandFlags.Modal)]
        public void ThAFASB()
        {
            using (var cmd = new ThPolylineAutoFireAlarmSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THHZXTF", CommandFlags.Modal)]
        public void ThAFASF()
        {
            using (var cmd = new ThFrameFireSystemDiagramCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THHZXTA", CommandFlags.Modal)]
        public void ThAFASA()
        {
            using (var cmd = new ThAllDrawingsFireSystemDiagramCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThAFAST", CommandFlags.Modal)]
        public void ThAFAST()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var BlockReferenceEngine = new ThAutoFireAlarmSystemRecognitionEngine())//防火分区块引擎
                using (var ControlCircuitEngine = new ThControlCircuitRecognitionEngine() { LayerFilter =new List<string>() { "E-FAS-WIRE"} })//线引擎
                {

                    #region 选择区域
                    var input = SelectPoints();
                    var points = new Point3dCollection();
                    points.Add(input.Item1);
                    points.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
                    points.Add(input.Item2);
                    points.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
                    #endregion

                    //拿到全图所有线
                    ControlCircuitEngine.RecognizeMS(acadDatabase.Database, points);

                    //获取选择区域的所有所需块
                    BlockReferenceEngine.Recognize(acadDatabase.Database, points);
                    BlockReferenceEngine.RecognizeMS(acadDatabase.Database, points);

                    //获取块引擎附加信息
                    var datas = BlockReferenceEngine.QueryAllOriginDatas();

                    List<Entity> data = new List<Entity>();
                    data.AddRange(ControlCircuitEngine.Elements.Select(o => o.Geometry));
                    data.AddRange(BlockReferenceEngine.Elements.Select(o => o.Outline));

                    ThAFASGraphEngine GraphEngine = new ThAFASGraphEngine(data);
                    GraphEngine.SetDataBase(acadDatabase);
                    GraphEngine.InitGraph();
                }
            }
        }

        private Tuple<Point3d, Point3d> SelectPoints()
        {
            var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                return Tuple.Create(leftDownPt, ptRightRes.Value);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }
    }
}
