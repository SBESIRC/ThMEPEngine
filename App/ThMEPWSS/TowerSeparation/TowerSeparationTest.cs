using Autodesk.AutoCAD.Runtime;
using Linq2Acad;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Command;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.TowerSeparation.TowerExtract;
using ThMEPWSS.DrainageSystemDiagram;

namespace ThMEPWSS.SprinklerConnect.Cmd
{
    public partial class ThSprinklerConnectNoUICmd
    {
        [CommandMethod("TIANHUACAD", "THSeparateTowerTest", CommandFlags.Modal)]
        public void SeparateTower()
        {
            var cmd = new TowerSeparationTest();
            cmd.Execute();
        }
    }

    public class TowerSeparationTest : ThMEPBaseCommand
    {
        public TowerSeparationTest()
        {
        }
        public override void SubExecute()
        {
            TowerSeparateExecute();
        }

        public void TowerSeparateExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var frame = ThSprinklerDataService.GetFrame();
                if (frame == null || frame.Area < 10)
                {
                    return;
                }

                var dataset = new ThSprinklerConnectDataFactory();
                var geos = dataset.Create(acadDatabase.Database, frame.Vertices()).Container;
                var dataQuery = new ThSprinklerDataQueryService(geos);
                dataQuery.ClassifyData();
                var TowerExtractor = new TowerExtractor();

                var shearWalls = TowerExtractor.Extractor(dataQuery.ShearWallList, frame);
                DrawUtils.ShowGeometry(shearWalls, "testForExtractor", 1);

                //foreach(Polyline l in shearWalls)
                //{
                //    l.ColorIndex = 5;
                //}
                //acadDatabase.ModelSpace.Add(shearWalls);
            }
        }
    }
}
