using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System.Linq;
using ThParkingStall.Core.MPartitionLayout;
using ThCADExtension;

namespace TianHua.Architecture.WPI.UI
{
    public class MultiProcessTestCommand
    {
        [CommandMethod("TIANHUACAD", "THMPTest", CommandFlags.Modal)]
        public void THMPTest()
        {
            MParkingPartitionPro mParkingPartitionPro = new MParkingPartitionPro();
            mParkingPartitionPro.GenerateLanes();

        }
        [CommandMethod("TIANHUACAD", "THMPTestControlGroup", CommandFlags.Modal)]
        public void THMPTestControlGroup()
        {

        }
        private void read()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = result.Value
                   .GetObjectIds()
                   .Select(o => adb.Element<Entity>(o))
                   .Where(o => o is Polyline)
                   .ToList();
                Extents3d ext = new Extents3d();
                foreach (var o in objs)
                {
                    ext.AddExtents(o.GeometricExtents);
                }
                ext.ToRectangle();
            }
        }

    }
}
