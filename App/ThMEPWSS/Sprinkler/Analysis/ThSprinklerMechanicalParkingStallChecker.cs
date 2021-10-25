using System;
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
    public class ThSprinklerMechanicalParkingStallChecker : ThSprinklerChecker
    {
        public DBObjectCollection ParkingStalls { get; set; }
        public Dictionary<string, List<string>> BlockNameDict { get; set; }

        public ThSprinklerMechanicalParkingStallChecker()
        {
            ParkingStalls = new DBObjectCollection();
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            if (ParkingStalls.Count > 0) 
            {
                var results = Check(sprinklers, pline);
                if (results.Count > 0)
                {
                    Present(results);
                }
            }
        }

        private DBObjectCollection Check(List<ThIfcDistributionFlowElement> sprinklers, Polyline pline)
        {
            var results = new DBObjectCollection();
            var sideSprinkler = sprinklers
                    .OfType<ThSprinkler>()
                    .Where(o => o.Category == Category);
            var sprinklersPosition = sideSprinkler
                    .Where(o => pline.Contains(o.Position))
                    .Select(o => new DBPoint(o.Position))
                    .ToCollection();
            var sprinklersIndex = new ThCADCoreNTSSpatialIndex(sprinklersPosition);
            ParkingStalls.OfType<Polyline>().ForEach(o =>
            {
                var lines = new DBObjectCollection();
                o.Explode(lines);
                var list = lines.OfType<Line>().OrderBy(l => l.Length).ToList();
                var range = new DBObjectCollection { list[0], list[1] }.Buffer(50);
                var dirction = list[1].GetCenter() - list[0].GetCenter();
                var count = 0;
                range.OfType<Polyline>().ForEach(pline =>
                {
                    sprinklersIndex.SelectCrossingPolygon(pline)
                                   .OfType<DBPoint>()
                                   .Select(point => point.Position)
                                   .ForEach(point =>
                                   {
                                       sideSprinkler.ForEach(sprinkler =>
                                       {
                                           if (sprinkler.Position.DistanceTo(point) < 1.0)
                                           {
                                               if(sprinkler.Direction.GetAngleTo(dirction) < Math.PI / 36)
                                               {
                                                   count++;
                                               }
                                           }
                                       });

                                   });
                    dirction = list[0].GetCenter() - list[1].GetCenter();
                });
                if (count < 1)
                {
                    results.Add(o);
                }
            });
            return results;
        }

        public override void Clean(Polyline pline)
        {
            CleanPline(ThWSSCommon.Mechanical_Parking_Stall_Checker_LayerName, pline);
        }

        private void Present(DBObjectCollection objs)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAISprinklerMechanicalParkingStallCheckerLayer();
                objs.OfType<Polyline>().ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.LayerId = layerId;
                    o.ConstantWidth = 100;
                });
            }
        }

        public override void Extract(Database database, Polyline pline)
        {
            //提取停车位
            var parkingStallBlkNames = new List<string>();
            parkingStallBlkNames.AddRange(QueryBlkNames("机械车位"));
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
