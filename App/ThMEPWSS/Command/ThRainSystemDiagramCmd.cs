using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper.Commands;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Service;
using ThMEPWSS.Bussiness;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Pipe.Engine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace ThMEPWSS.Command
{
    using ThMEPEngineCore.Engine;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.JsonExtensionsNs;
    using ThMEPWSS.Pipe.Service;

    //雨水排水系统图
    public class ThRainSystemDiagramCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
        }
        //test
        public static string CollectSelectionTest()
        {
            var rg = SelectPoints();
            var basePtOptions = new PromptPointOptions("\n选择图纸基点");
            var rst = Active.Editor.GetPoint(basePtOptions);
            return $"[{(rg.Item1.ToJson())},{(rg.Item2.ToJson())},{(rst.Value.ToJson())}]";
        }
        private static Tuple<Point3d, Point3d> SelectPoints()
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

        public void Execute()
        {
            //发布出去的时候做try catch，本地测试直接调用，不要catch，便于捕获异常！
            try
            {
                ThRainSystemService.DrawRainSystemDiagram1();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static void Execute1()
        {
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                DrawUtils.DrawingQueue.Clear();
                try
                {
                    //todo: process

                    //1. In the result of selected area, get storeys information, such as lable, bouding box of each storey

                    //2. In the result of selected area, get all rain pipes, condense pipes, floor drains, water buckets and their lables

                    //3. due to bounding box of each storey, put related rain pipes, condense pipes, floor drains, water bucket into certain storey

                    //4. build relationships in a certain storey

                    //5. create system diagram due to above data
                    var diagram = new ThWRainSystemDiagram();

                    //todo: extract storeys
                    var storeysRecEngine = new ThStoreysRecognitionEngine();


                    var input = SelectPoints();
                    var points = new Point3dCollection();
                    points.Add(input.Item1);
                    points.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
                    points.Add(input.Item2);
                    points.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));

                    storeysRecEngine.Recognize(adb.Database, points);

                    var basePtOptions = new PromptPointOptions("\n选择图纸基点");

                    var rst = Active.Editor.GetPoint(basePtOptions);
                    if (rst.Status != PromptStatus.OK)
                    {
                        return;
                    }

                    var bastPt = rst.Value;

                    diagram.InitServices(adb, points);
                    diagram.InitStoreys(storeysRecEngine.Elements);
                    diagram.InitVerticalPipeSystems(points);

                    diagram.Draw(bastPt);
                    //if (false) DrLazy.Default.DrawLazy();
                    DrawUtils.Draw(adb);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
