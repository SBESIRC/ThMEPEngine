using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public class ThMPolygonTool
    {
        public static void Initialize()
        {
            string ver = Application.GetSystemVariable("ACADVER").ToString().Substring(0, 2);
            SystemObjects.DynamicLinker.LoadModule("AcMPolygonObj" + ver + ".dbx", false, false);
        }
    }
}
