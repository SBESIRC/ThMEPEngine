using System;
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
    public class ThSprinklerMechanicalParkingStallChecker : ThSprinklerChecker
    {
        public DBObjectCollection ParkingStalls { get; set; }
        public Dictionary<string, List<string>> BlockNameDict { get; set; }

        public ThSprinklerMechanicalParkingStallChecker()
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

        private DBObjectCollection Check(List<ThIfcDistributionFlowElement> sprinklers, Entity entity)
        {
            var results = new DBObjectCollection();
            var sideSprinkler = sprinklers
                    .OfType<ThSprinkler>()
                    .Where(o => o.Category == Category);
            var sprinklersPosition = sideSprinkler
                    .Where(o => entity.EntityContains(o.Position))
                    .Select(o => new DBPoint(o.Position))
                    .ToCollection();
            var sprinklersIndex = new ThCADCoreNTSSpatialIndex(sprinklersPosition);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(ParkingStalls);
            var parkingStalls = spatialIndex.SelectCrossingPolygon(entity);
            parkingStalls.OfType<Polyline>().ForEach(o =>
            {
                var lines = new DBObjectCollection();
                o.Explode(lines);
                var list = lines.OfType<Line>().OrderBy(l => l.Length).ToList();
                var range = new DBObjectCollection { list[0], list[1] }.Buffer(50.0);
                var dirction = list[1].GetCenter() - list[0].GetCenter();
                var countCheck = true;
                range.OfType<Polyline>().ForEach(pline =>
                {
                    var count = 0;
                    sprinklersIndex.SelectCrossingPolygon(pline)
                                   .OfType<DBPoint>()
                                   .Select(point => point.Position)
                                   .ForEach(point =>
                                   {
                                       sideSprinkler.ForEach(sprinkler =>
                                       {
                                           if (sprinkler.Position.DistanceTo(point) < 1.0)
                                           {
                                               if (sprinkler.Direction.GetAngleTo(dirction) < Math.PI / 36)
                                               {
                                                   count++;
                                               }
                                           }
                                       });

                                   });
                    dirction = list[0].GetCenter() - list[1].GetCenter();
                    if(count < 1)
                    {
                        countCheck = false;
                    }
                });
                if (!countCheck)
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
