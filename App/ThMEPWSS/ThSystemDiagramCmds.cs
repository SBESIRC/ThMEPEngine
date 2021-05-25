using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ThCADExtension;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using Linq2Acad;
using ThMEPWSS.Assistant;
using ThMEPWSS.Pipe.Service;
using ThCADCore.NTS;
using DotNetARX;
using System;
using System.ComponentModel;
using System.Linq;
using Autodesk.AutoCAD.EditorInput;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "THCRSD", CommandFlags.Modal)]
        public void ThCreateRainSystemDiagram()
        {
            using (var cmd = new ThRainSystemDiagramCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "TestExractWaterBucket", CommandFlags.Modal)]
        public void TestExractWaterBucket()
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            using (var engine = new ThWGravityWaterBucketRecognitionEngine())
            {
                var per = AcHelper.Active.Editor.GetEntity("\n选择一个框");
                if (per.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    var frame = db.Element<Polyline>(per.ObjectId);
                    engine.Recognize(db.Database, frame.Vertices());
                }
            }
        }
        [CommandMethod("TIANHUACAD", "DelTemps", CommandFlags.Modal)]
        public void DeleteTestGeometries()
        {
            Dbg.DeleteTestGeometries();
        }
        [CommandMethod("TIANHUACAD", "LocatePipe", CommandFlags.Modal)]
        public void ShowVerticalPipe()
        {
            //var rst = AcHelper.Active.Editor.GetString("\n输入立管编号");
            //if (rst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            //{
            //    return;
            //}

            //using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            //using (var adb = AcadDatabase.Active())
            //using (var tr = DrawUtils.DrawingTransaction)
            //{
            //    string targetLabel = rst.StringResult;
            //    var db = adb.Database;
            //    Dbg.BuildAndSetCurrentLayer(db);

            //    var sv = new ThRainSystemService() { adb = adb };
            //    sv.InitCache();
            //    sv.CollectVerticalPipesData();
            //    foreach (var e in sv.VerticalPipes)
            //    {
            //        if (sv.VerticalPipeToLabelDict.TryGetValue(e, out string lb))
            //        {
            //            if (lb == targetLabel)
            //            {
            //                Dbg.ShowWhere(e);
            //            }
            //        }
            //    }
            //}
            DebugNs.Util1.FindText();
        }
        [CommandMethod("TIANHUACAD", "TzTest", CommandFlags.Modal)]
        public void TzTest()
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var rst = AcHelper.Active.Editor.GetEntity("\nSelect a TianZheng entity");
                if(rst.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    var entity = db.Element<Entity>(rst.ObjectId);
                    //todo: extract property
                    var properties = TypeDescriptor.GetProperties(entity.AcadObject).Cast<PropertyDescriptor>().ToDictionary(prop => prop.Name);
                    var DNPropName = "DNDiameter";
                    if (properties.ContainsKey(DNPropName))
                    {
                        var DNPropObject = properties[DNPropName];
                        var DNValue = DNPropObject.GetValue(entity.AcadObject);
                        var DNString = DNValue.ToString();
                    }
                }
            }
        }

        
    }
}
