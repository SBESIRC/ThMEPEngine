namespace ThMEPWSS.Assistant
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using ThMEPWSS.JsonExtensionsNs;
    using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
    using DU = ThMEPWSS.Assistant.DrawUtils;
    using Autodesk.AutoCAD.EditorInput;
    using AcHelper;
    using Autodesk.AutoCAD.Geometry;
    using Linq2Acad;
    using ThMEPWSS.Pipe.Model;
    using ThMEPWSS.Pipe.Engine;
    using Autodesk.AutoCAD.DatabaseServices;
    using System.Diagnostics;
    using Autodesk.AutoCAD.ApplicationServices;
    using Dreambuild.AutoCAD;
    using DotNetARX;
    using Autodesk.AutoCAD.Internal;
    using static ThMEPWSS.DebugNs.ThPublicMethods;
    using ThMEPWSS.CADExtensionsNs;
    using ThMEPWSS.Uitl;
    using ThMEPWSS.Uitl.DebugNs;
    using ThMEPWSS.Uitl.ExtensionsNs;
    using ThMEPWSS.Assistant;
    using ThMEPWSS.Pipe.Service;
    using NFox.Cad;
    using ThCADCore.NTS;
    using Autodesk.AutoCAD.Colors;
    using System.Runtime.Remoting;
    using PolylineTools = Pipe.Service.PolylineTools;
    using CircleTools = Pipe.Service.CircleTools;
    using System.IO;
    using Autodesk.AutoCAD.Runtime;
    using static ThMEPWSS.DebugNs.StaticMethods;
    using ThCADExtension;

    public class ThWRainSystemDiagramTest
    {
        public static void Test2()
        {
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                Dbg.BuildAndSetCurrentLayer(adb.Database);
                var pt1 = "{x:-84185.9559129075,y:1871639.15102121,z:0}".JsonToPoint3d();
                var pt2 = "{x:282170.133176226,y:335611.579893751,z:0}".JsonToPoint3d();

                var points = new Point3dCollection();
                points.Add(pt1);
                points.Add(new Point3d(pt1.X, pt2.Y, 0));
                points.Add(pt2);
                points.Add(new Point3d(pt2.X, pt1.Y, 0));
                Dbg.FocusMainWindow();
                var basePtOptions = new PromptPointOptions("\n选择图纸基点");


                //const string KEY = "qrjq0v";
                //const string KEY = "qrjq0w";
                const string KEY = "qrjq0x";


                //var rst = Active.Editor.GetPoint(basePtOptions);
                //if (rst.Status != PromptStatus.OK) return;
                //var basePt = rst.Value;
                var basePt = default(Point3d);
                CollectDataAndSave(adb, points, KEY, basePt);
                //DrawFromJson(adb, points, KEY, basePt);

                DrLazy.Default.DrawLazy();
                DrawUtils.Draw();
                Dbg.FocusMainWindow();
            }
        }
        public static void Test1()
        {
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var adb = AcadDatabase.Active())
            {
                var pt1 = "{x:-84185.9559129075,y:1871639.15102121,z:0}".JsonToPoint3d();
                var pt2 = "{x:282170.133176226,y:335611.579893751,z:0}".JsonToPoint3d();

                var points = new Point3dCollection();
                points.Add(pt1);
                points.Add(new Point3d(pt1.X, pt2.Y, 0));
                points.Add(pt2);
                points.Add(new Point3d(pt2.X, pt1.Y, 0));
                Dbg.FocusMainWindow();
                var basePtOptions = new PromptPointOptions("\n选择图纸基点");


                //const string KEY = "qrjq0v";
                //const string KEY = "qrjq0w";
                const string KEY = "qrjq0x";


                var rst = Active.Editor.GetPoint(basePtOptions);
                if (rst.Status != PromptStatus.OK) return;
                var basePt = rst.Value;

                //CollectDataAndSave(adb, points, KEY, basePt);
                DrawFromJson(adb, points, KEY, basePt);

                DrLazy.Default.DrawLazy();
                DrawUtils.Draw();
                Dbg.FocusMainWindow();
            }
        }

        private static void CollectDataAndSave(AcadDatabase adb, Point3dCollection points, string KEY, Point3d basePt)
        {
            var diagram = new ThWRainSystemDiagram();
            InitDiagram(adb, diagram, basePt, points);
            SaveData(KEY, diagram);
        }

        private static void DrawFromJson(AcadDatabase adb, Point3dCollection points, string KEY, Point3d basePt)
        {
            var diagram = LoadData<ThWRainSystemDiagram>(KEY);

            //{
            //    var sys = diagram.BalconyVerticalRainPipes.First();
            //    var r = sys.PipeRuns.First();
            //    r.CheckPoint.HasCheckPoint = true;
            //    r.CondensePipes.Add(new ThWSDCondensePipe() { DN = "DN77" });
            //    r.CondensePipes.Add(new ThWSDCondensePipe() { DN = "DN77" });
            //    r.FloorDrains.Add(new ThWSDFloorDrain() { HasDrivePipe = true, DN = "DN666" });
            //    r.FloorDrains.Add(new ThWSDFloorDrain() { HasDrivePipe = true, DN = "DN666" });
            //}


            diagram.Draw(basePt);
        }
        public static void InitDiagram(AcadDatabase adb, ThWRainSystemDiagram diagram, Point3d basePt, Point3dCollection range)
        {
            var storeysRecEngine = new ThWStoreysRecognitionEngine();
            storeysRecEngine.Recognize(adb.Database, range);
            diagram.InitServices(adb, range);
            diagram.InitStoreys(storeysRecEngine.Elements);
            diagram.InitVerticalPipeSystems(range);
        }
    }
}
