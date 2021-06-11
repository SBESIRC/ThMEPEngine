using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPXRefService
    {
        public static string OriginalFromXref(string xrefLayer)
        {
            // 已绑定外参
            if (xrefLayer.Matches("*`$#`$*"))
            {
                return xrefLayer.Substring(xrefLayer.LastIndexOf('$') + 1);
            }

            // 未绑定外参
            if (xrefLayer.Matches("*|*"))
            {
                return xrefLayer.Substring(xrefLayer.LastIndexOf('|') + 1);
            }

            // 其他非外参
            return xrefLayer;
        }
    }
}
