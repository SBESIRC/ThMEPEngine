using AcHelper;
using Linq2Acad;
using ThCADExtension;
using System.Collections.Generic;

namespace ThPlatform3D.Command
{
    public class ThRailDrawCmd : ThDrawBaseCmd
    {
        private const string RailLayerName = "TH-栏杆";

        public ThRailDrawCmd()
        {
        }

        public override void Execute()
        {
            ImportTemplate();

            OpenLayer(new List<string> { "0", RailLayerName });

            DrawRail();
        }

        private void DrawRail()
        {
            var ltr = GetLTR(RailLayerName);
            if(ltr==null)
            {
                return;
            }
            else
            {
                Active.Document.Window.Focus();
                SetCurrentDbConfig(ltr);
                Active.Database.ObjectAppended += Database_ObjectAppended;
                Active.Editor.Command("_.PLINE");
                Active.Database.ObjectAppended -= Database_ObjectAppended;
                SetStyle(RailLayerName); // 设置绘制对象的样式
            }
        }

        private void ImportTemplate()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThBIMCommon.CadTemplatePath(), DwgOpenMode.ReadOnly, false))
            {
                // 导入图层
                acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(RailLayerName), true);
            }
        }
    }
}
