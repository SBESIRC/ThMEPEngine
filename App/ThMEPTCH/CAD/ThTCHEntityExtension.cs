using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.CAD
{
    public static class ThTCHEntityExtension
    {
        public static bool IsWindow(this Entity e)
        {
            return Kind(e).Contains("窗");
        }

        public static bool IsDoor(this Entity e)
        {
            return Kind(e).Contains("门");
        }

        private static string Kind(this Entity e)
        {
            dynamic obj = e.AcadObject;
            return obj.GetKind as string;
        }
    }
}
