using System;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using AcHelper.Commands;
using ThCADExtension;
using ThMEPEngineCore;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThPlatform3D.Command
{
    public class ThWallHoleDrawCmd : IAcadCommand, IDisposable
    {
        private const string WallHoleLayerName = "TH-墙洞";
        public ThWallHoleDrawCmd()
        {
        }

        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            ImportTemplate();

            OpenLayer();

            DrawWallHole();
        }

        private void DrawWallHole()
        {
            var oldLayer = GetCurrentLayer();
            SetCurrentLayer(WallHoleLayerName);
            Active.Editor.Command("_.PLINE");
            SetCurrentLayer(oldLayer);
        }        

        private void ImportTemplate()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThBIMCommon.CadTemplatePath(), DwgOpenMode.ReadOnly, false))
            {
                // 导入图层
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(WallHoleLayerName), true);
            }
        }

        private void OpenLayer()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                acadDb.Database.OpenAILayer("0");
                acadDb.Database.OpenAILayer(WallHoleLayerName);
            }
        }

        private string GetCurrentLayer()
        {
            using (var acdb = AcadDatabase.Active())
            {
                return acdb.Element<LayerTableRecord>(acdb.Database.Clayer).Name;
            }
        }

        private void SetCurrentLayer(string layerName)
        {
            using (var acdb = AcadDatabase.Active())
            {
                acdb.Database.SetCurrentLayer(layerName);
            }
        }
    }
}
