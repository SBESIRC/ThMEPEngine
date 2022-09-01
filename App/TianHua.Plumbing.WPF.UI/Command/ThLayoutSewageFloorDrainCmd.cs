using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;

namespace TianHua.Plumbing.WPF.UI.Command
{
    class ThLayoutSewageFloorDrainCmd : ThMEPBaseCommand, IDisposable
    {
        private string layerName = "";
        private string blockName = "";
        public ThLayoutSewageFloorDrainCmd(string _layerName, string _blockName)
        {
            ActionName = "放置污水地漏";
            CommandName = "THFZWSDL";
            layerName = _layerName;
            blockName = _blockName;
        }
        public void Dispose()
        {
            //
        }
        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            {
                ImportLayers();
                ImportBlocks();
                OpenLayer();
                UserInteract();
            }
        }
        private void ImportBlocks()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(blockName), true);
            }
        }
        private void ImportLayers()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(layerName), true);
            }
        }
        private void OpenLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.OpenAILayer("0");
                acadDb.Database.OpenAILayer(layerName);
            }
        }
        private void UserInteract()
        {
            FocusToCAD();
            while (true)
            {
                var ppo = Active.Editor.GetPoint("\n选择插入点");
                if (ppo.Status == PromptStatus.OK)
                {
                    var wcsPt = ppo.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem);
                    InsertBlock(wcsPt);
                }
                else
                {
                    break;
                }
            }
        }

        private void InsertBlock(Point3d position)
        {
            var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
            var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var blkId = db.ModelSpace.ObjectId.InsertBlockReference(layerName, blockName, position, new Scale3d(), angle, null);
            }
        }

        private void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
