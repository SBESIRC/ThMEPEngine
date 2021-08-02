using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThFpArchitectureWallExtractor : ThArchitectureExtractor
    {
        private List<ThCanArrangedElement> CanArrangedElements { get; set; }
        public ThFpArchitectureWallExtractor(List<ThCanArrangedElement> canArrangedElements)
        {
            CanArrangedElements = canArrangedElements;
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var outputArchWalls = GetOutPutArchitectureWalls();
            outputArchWalls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private List<Entity> GetOutPutArchitectureWalls()
        {
            if (!CanArrangedElements.Contains(ThCanArrangedElement.IsolatedArchitectureWall) &&
                !CanArrangedElements.Contains(ThCanArrangedElement.NonIsolatedArchitectureWall))
            {
                return new List<Entity>();
            }

            if (CanArrangedElements.Contains(ThCanArrangedElement.IsolatedArchitectureWall) &&
                CanArrangedElements.Contains(ThCanArrangedElement.NonIsolatedArchitectureWall))
            {
                return Walls;
            }
            else
            {
                var isolateArchwalls = ThElementIsolateFilterService.Filter(Walls, Rooms);
                if (CanArrangedElements.Contains(ThCanArrangedElement.IsolatedArchitectureWall))
                {
                    return isolateArchwalls;
                }
                else
                {
                    return Walls.Where(o => !isolateArchwalls.Contains(o)).ToList();
                }
            }
        }
    }
}
