using AcHelper;
using AcHelper.Commands;
using System;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThPlatform3D.Command
{
    /// <summary>
    /// 定位点插入
    /// </summary>
    public class ThInsertBasePointCmd : IAcadCommand, IDisposable
    {
        private const string BasePointLayerName = "DEFPOINTS";
        private const string BasePointBlockName = "BASEPOINT";
        public ThInsertBasePointCmd()
        {
            //
        }
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            ImportTemplate();

            OpenLayer();

            UserInteract();
        }

        private void UserInteract()
        {
            Active.Document.Window.Focus();
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

        private ObjectId InsertBlock(Point3d pos)
        {
            using (var acadDb= AcadDatabase.Active())
            {
                var vec = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
                return acadDb.ModelSpace.ObjectId.InsertBlockReference(
                    BasePointLayerName, BasePointBlockName, pos, new Scale3d(), angle);
            }
        }

        private void OpenLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.OpenAILayer("0");
                acadDb.Database.OpenAILayer(BasePointLayerName);
            }
        }

        private void ImportTemplate()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThBIMCommon.CadTemplatePath(), DwgOpenMode.ReadOnly, false))
            {
                // 导入图层
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(BasePointLayerName), true);

                // 导入块
                acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(BasePointBlockName), true);
            }
        }
    }
}
