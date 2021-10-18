using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.FlushPoint.Data;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerParkingStallChecker : ThSprinklerChecker
    {
        public DBObjectCollection ParkingStalls { get; set; }
        public Dictionary<string, List<string>> BlockNameDict { get; set; }

        public ThSprinklerParkingStallChecker()
        {
            ParkingStalls = new DBObjectCollection();
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var results = Check(sprinklers, pline);
            if (results.Count > 0) 
            {
                Present(results);
            }
        }

        private DBObjectCollection Check(List<ThIfcDistributionFlowElement> sprinklers, Polyline pline)
        {
            var results = new DBObjectCollection();
            var objs = sprinklers
                    .OfType<ThSprinkler>()
                    .Where(o => o.Category == Category)
                    .Where(o => pline.Contains(o.Position))
                    .Select(o => new DBPoint(o.Position))
                    .ToCollection();
            var sprinklerIndex = new ThCADCoreNTSSpatialIndex(objs);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(ParkingStalls);
            var parkingStalls = spatialIndex.SelectCrossingPolygon(pline);
            parkingStalls.OfType<Polyline>().ForEach(o =>
            {
                var sprinklersInStall = sprinklerIndex.SelectCrossingPolygon(o);
                if (sprinklersInStall.Count < 2)
                {
                    results.Add(o);
                }
            });
            return results;
        }

        private void Present(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAISprinklerParkingStallCheckerLayer();
                objs.OfType<Polyline>().ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.LayerId = layerId;
                    o.ConstantWidth = 100;
                });
            }
        }

        public override void Clean(Polyline pline)
        {
            CleanPline(ThSprinklerCheckerLayer.Parking_Stall_Checker_LayerName, pline);
        }

        public override void Extract(Database database, Polyline pline)
        {
            //提取停车位
            var parkingStallBlkNames = new List<string>();
            parkingStallBlkNames.AddRange(QueryBlkNames("机械车位"));
            parkingStallBlkNames.AddRange(QueryBlkNames("非机械车位"));
            var parkingStallExtractor = new ThParkingStallExtractor()
            {
                BlockNames = parkingStallBlkNames,
            };
            parkingStallExtractor.Extract(database, pline.Vertices());
            ParkingStalls = parkingStallExtractor.ParkingStalls.OfType<Polyline>().ToCollection();
        }

        private List<string> QueryBlkNames(string category)
        {
            if (BlockNameDict.ContainsKey(category))
            {
                return BlockNameDict[category].Distinct().ToList();
            }
            else
            {
                return new List<string>();
            }
        }
    }
}