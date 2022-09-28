using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.IO.SVG;

namespace ThPlatform3D.ArchitecturePlane.Service
{
    internal static class ThComponentQuerier
    {
        public static List<ThComponentInfo> GetDoorComponents(this List<ThComponentInfo> components)
        {
            return components.Where(o => o.Type.IsDoor()).ToList();
        }

        public static List<ThComponentInfo> GetWindowComponents(this List<ThComponentInfo> components)
        {
            return components.Where(o => o.Type.IsWindow()).ToList();
        }
    }
}
