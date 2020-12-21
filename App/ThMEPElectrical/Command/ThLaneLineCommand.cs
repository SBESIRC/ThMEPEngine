using System;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.Lane;

namespace ThMEPElectrical.Command
{
    public class ThLaneLineCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    RejectObjectsOnLockedLayers = true,
                    MessageForAdding = "请选择需要提取车道中心线的范围框线"
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,

                };
                var filterlist = OpFilter.Bulid(o =>
                    o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames)
                );
                var result = Active.Editor.GetSelection(options, filterlist);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                acadDatabase.Database.CreateLaneLineLayer();
                foreach (var frameId in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(frameId);
                    acadDatabase.Database.LaneLines(frame).Cast<Entity>().ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.Layer = ThMEPCommon.LANELINE_LAYER_NAME;
                    });
                }
            }
        }
    }
}
