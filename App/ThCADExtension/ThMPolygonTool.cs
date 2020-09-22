using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThCADExtension
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
