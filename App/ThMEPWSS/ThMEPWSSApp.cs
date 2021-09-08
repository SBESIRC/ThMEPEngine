using Linq2Acad;
using ThCADExtension;
using ThMEPWSS.Command;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Runtime;
using ThMEPWSS.FlushPoint.Data;
using System.Collections.Generic;

namespace ThMEPWSS
{
    public class ThMEPWSSApp : IExtensionApplication
    {
        public void Initialize()
        {
        }

        public void Terminate()
        {
        }
        [CommandMethod("TIANHUACAD", "THExtractDrainageWell", CommandFlags.Modal)]
        public void THExtractDrainageWell()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var frame = ThWindowInteraction.GetPolyline(
                  PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
                if (frame.Area < 1e-4)
                {
                    return;
                }
                var pts = frame.Vertices();              
                var drainageWellBlkNames = new List<string>();
                if (THLayoutFlushPointCmd.FlushPointVM.Parameter.BlockNameDict.ContainsKey("集水井"))
                {
                    drainageWellBlkNames = THLayoutFlushPointCmd.FlushPointVM.Parameter.BlockNameDict["集水井"];
                }
                var drainFacilityExtractor = new ThDrainFacilityExtractor()
                {
                    ColorIndex = 5,
                    DrainageBlkNames = drainageWellBlkNames
                };
                drainFacilityExtractor.Extract(acadDb.Database, pts);
                drainFacilityExtractor.CollectingWells.CreateGroup(acadDb.Database, 5);
                drainFacilityExtractor.DrainageDitches.CreateGroup(acadDb.Database, 6);
            }
        }
    }
}