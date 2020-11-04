using AcHelper;
using DotNetARX;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace TianHua.AutoCAD.ThCui
{
    public static class ThCuiEditorExtension
    {
        public static void Cuiunload(this Editor editor)
        {
            // 先卸载ThCAD
            using (var ov = new ThCuiFileDiaOverride())
            {
#if ACAD_ABOVE_2014
                Active.Editor.Command("_.CUIUNLOAD", ThCADCommon.CuixMenuGroup);
#else
                ResultBuffer args = new ResultBuffer(
                   new TypedValue((int)LispDataType.Text, "_.CUIUNLOAD"),
                   new TypedValue((int)LispDataType.Text, ThCADCommon.CuixMenuGroup)
                   );
                Active.Editor.AcedCmd(args);
#endif
            }
        }
    }
}
