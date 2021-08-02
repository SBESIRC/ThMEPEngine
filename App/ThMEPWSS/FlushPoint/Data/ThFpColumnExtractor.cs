using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPWSS.FlushPoint.Data
{
    public class ThFpColumnExtractor :ThColumnExtractor
    {
        private List<ThCanArrangedElement> CanArrangedElements { get; set; }
        public ThFpColumnExtractor(List<ThCanArrangedElement> canArrangedElements)
        {
            CanArrangedElements = canArrangedElements;
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var outputColumns = GetOutPutColumns();
            outputColumns.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private List<Polyline> GetOutPutColumns()
        {
            if (CanArrangedElements.Contains(ThCanArrangedElement.IsolatedColumn) &&
                CanArrangedElements.Contains(ThCanArrangedElement.NonIsolatedArchitectureWall))
            {
                return Columns;
            }

            var isolateColumns = ThElementIsolateFilterService.Filter(Columns.Cast<Entity>().ToList(), Rooms);
            if (CanArrangedElements.Contains(ThCanArrangedElement.IsolatedColumn))
            {
                return isolateColumns.Cast<Polyline>().ToList();
            }
            else if(CanArrangedElements.Contains(ThCanArrangedElement.NonIsolatedColumn))
            {
                return Columns.Where(o=>!isolateColumns.Contains(o)).ToList();
            }
            else
            {
                return new List<Polyline>();
            }
        }
    }
}
