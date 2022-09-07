using AcHelper;
using Linq2Acad;
using ThCADExtension;
using System.Collections.Generic;

namespace ThPlatform3D.Command
{
    public class ThWallHoleDrawCmd : ThDrawBaseCmd
    {
        private const string WallHoleLayerName = "TH-墙洞";        
        public ThWallHoleDrawCmd()
        {
        }

        public override void Execute()
        {
            ImportTemplate();

            OpenLayer(new List<string> { "0", WallHoleLayerName });

            DrawWallHole();
        }

        private void DrawWallHole()
        {
            var ltr = GetLTR(WallHoleLayerName);
            if(ltr ==null)
            {
                return;
            }
            else
            {
                SetCurrentDbConfig(ltr);
                Active.Database.ObjectAppended += Database_ObjectAppended;
                Active.Editor.Command("_.PLINE");
                Active.Database.ObjectAppended -= Database_ObjectAppended;
                SetStyle(WallHoleLayerName);
            }
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
    }
}
