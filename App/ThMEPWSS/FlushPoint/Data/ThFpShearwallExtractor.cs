using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThFpShearwallExtractor : ThShearwallExtractor
    {
        private List<ThCanArrangedElement> CanArrangedElements { get; set; }
        public ThFpShearwallExtractor(List<ThCanArrangedElement> canArrangedElements)
        {
            CanArrangedElements = canArrangedElements;
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var outputWalls = GetOutPutShearwalls();
            outputWalls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }
        private List<Entity> GetOutPutShearwalls()
        {
            if(!CanArrangedElements.Contains(ThCanArrangedElement.IsolatedShearwall) &&
                !CanArrangedElements.Contains(ThCanArrangedElement.NonIsolatedShearwall))
            {
                return new List<Entity>();
            }
            if (CanArrangedElements.Contains(ThCanArrangedElement.IsolatedShearwall) &&
                CanArrangedElements.Contains(ThCanArrangedElement.NonIsolatedShearwall))
            {
                return Walls;
            }
            else
            {
                var isolateShearwalls = ThElementIsolateFilterService.Filter(Walls, Rooms);
                if (CanArrangedElements.Contains(ThCanArrangedElement.IsolatedShearwall))
                {
                    return isolateShearwalls;
                }
                else 
                {
                    return Walls.Where(o => !isolateShearwalls.Contains(o)).ToList();
                }
            }
        }
    }
}
