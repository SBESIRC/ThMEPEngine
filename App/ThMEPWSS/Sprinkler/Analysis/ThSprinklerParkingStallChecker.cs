using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.FlushPoint.Data;
using ThMEPWSS.Sprinkler.Service;

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

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Entity entity)
        {
            if (ParkingStalls.Count > 0)
            {
                var results = Check(sprinklers, entity);
                if (results.Count > 0)
                {
                    Present(results);
                }
            }
        }

        private DBObjectCollection Check(List<ThIfcDistributionFlowElement> sprinklers, Entity frame)
        {
            var frameEx = frame.Clone() as Entity;
            if(frame is Polyline polyline)
            {
                var frameExTemp = polyline.Buffer(3000.0).OfType<Polyline>().OrderByDescending(o => o.Area).FirstOrDefault();
                if(frameExTemp != null)
                {
                    frameEx = frameExTemp;
                }
            }
            else if(frame is MPolygon polygon)
            {
                var frameExTemp = polygon.Buffer(3000.0).OfType<MPolygon>().OrderByDescending(o => o.Area).FirstOrDefault();
                if(frameExTemp != null)
                {
                    frameEx = frameExTemp;
                }
            }
            var results = new DBObjectCollection();
            var objs = sprinklers
                    .OfType<ThSprinkler>()
                    .Where(o => o.Category == Category)
                    .Where(o => frameEx.EntityContains(o.Position))
                    .Select(o => new DBPoint(o.Position))
                    .ToCollection();
            var sprinklerIndex = new ThCADCoreNTSSpatialIndex(objs);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(ParkingStalls);
            var parkingStalls = spatialIndex.SelectCrossingPolygon(frame);
            parkingStalls.OfType<Polyline>().ForEach(o =>
            {
                var parkingStallEx = o.Buffer(100.0).OfType<Polyline>().First();
                var sprinklersInStall = sprinklerIndex.SelectCrossingPolygon(parkingStallEx);
                if (sprinklersInStall.Count < 2)
                {
                    results.Add(o.Clone() as Entity);
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
            CleanPline(ThWSSCommon.Parking_Stall_Checker_LayerName, pline);
        }

        public override void Extract(Database database, Polyline pline)
        {
            //提取停车位
            var parkingStallBlkNames = new List<string>();
            parkingStallBlkNames.AddRange(QueryBlkNames("机械车位"));
            parkingStallBlkNames.AddRange(QueryBlkNames("非机械车位"));
            if (parkingStallBlkNames.Count > 0)
            {
                var parkingStallExtractor = new ThParkingStallExtractor()
                {
                    BlockNames = parkingStallBlkNames,
                    LayerNames = new List<string>(),
                };
                parkingStallExtractor.Extract(database, pline.Vertices());
                ParkingStalls = parkingStallExtractor.ParkingStalls.OfType<Polyline>().ToCollection();
            }
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