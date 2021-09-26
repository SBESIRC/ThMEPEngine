using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Catel.Collections;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Sprinkler.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDistanceBetweenSprinklerChecker : ThSprinklerChecker
    {
        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries)
        {
            var distanceCheck = DistanceCheck(sprinklers, 1800.0);
            var buildingCheck = BuildingCheck(geometries, distanceCheck);
            Present(buildingCheck);
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, double tolerance)
        {
            var sprinklersClone = new List<ThIfcDistributionFlowElement>();
            sprinklers.ForEach(o => sprinklersClone.Add(o));
            var result = new HashSet<Line>();
            while (sprinklersClone.Count > 0) 
            {
                var position = (sprinklersClone[0] as ThSprinkler).Position;
                sprinklersClone.RemoveAt(0);
                var kdTree = new ThCADCoreNTSKdTree(1.0);
                sprinklersClone.Cast<ThSprinkler>().ForEach(o => kdTree.InsertPoint(o.Position));
                var closePointList = ThSprinklerKdTreeService.QueryOther(kdTree,position,tolerance);
                closePointList.ForEach(o => result.Add(new Line(position, o)));
            }
            
            return result;
        }

        private HashSet<Line> BuildingCheck(List<ThGeometry> geometries, HashSet<Line> lineList)
        {
            var geometriesFilter = new DBObjectCollection();
            geometries.ForEach(g =>
            {
                if ((g.Properties.ContainsKey("BottomDistanceToFloor") && Convert.ToInt32(g.Properties["BottomDistanceToFloor"]) < BeamHeight)
                 || (g.Properties["Category"] as string).Contains("Room"))
                {
                    return;
                }
                geometriesFilter.Add(g.Boundary);
            });

            var result = new HashSet<Line>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometriesFilter);
            lineList.ForEach(o =>
            {
                var line = new Line(o.StartPoint, o.EndPoint);
                var filter = spatialIndex.SelectCrossingPolygon(line.Buffer(1.0));
                if(filter.Count == 0)
                {
                    result.Add(o);
                }
            });
            return result;
        }

        private void Present(HashSet<Line> result)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAISprinklerDistanceCheckerLayer();
                Present(result, layerId);
            }
        }

       
    }
}
