using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.AutoCAD.Utility.ExtensionTools;

namespace ThCADCore
{
    public static class ThCADHatchExtension
    {
        public static DBObjectCollection MergeHatches(this DBObjectCollection hatches)
        {
            var plines = new DBObjectCollection();
            foreach (var obj in hatches)
            {
                if (obj is Hatch hatch)
                {
                    hatch.ToPolylines().ForEach(o => plines.Add(o));
                }
            }
            return plines.Boundaries();
        }
    }
}
