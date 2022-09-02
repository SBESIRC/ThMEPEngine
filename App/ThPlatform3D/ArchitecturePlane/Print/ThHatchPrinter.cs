using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;
using ThPlatform3D.ArchitecturePlane.Service;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    internal class ThHatchPrinter
    {
        private HatchPrintConfig HatchConfig { get; set; }
        private PrintConfig OutlineConfig { get; set; }
        public ThHatchPrinter(HatchPrintConfig hatchConfig, PrintConfig outlineConfig)
        {
            HatchConfig = hatchConfig;
            OutlineConfig = outlineConfig;
        }
        public ObjectIdCollection Print(Database db, Entity entity)
        {
            var results = new ObjectIdCollection();
            if (HatchConfig == null && OutlineConfig==null)
            {
                return results;
            }
            if (entity is Polyline polyline)
            {
                results = Print(db, polyline);
            }
            else if (entity is MPolygon polygon)
            {
                results = Print(db, polygon);
            }
            else if (entity is Curve curve)
            {
                results = Print(db, curve);
            }
            return results;
        }

        public ObjectIdCollection Print(Database db, Curve curve)
        {
            var results = new ObjectIdCollection();
            var outlineId = curve.Print(db, OutlineConfig);
            results.Add(outlineId);
            if (HatchConfig != null)
            {
                var objIds = new ObjectIdCollection { outlineId };
                var hatchId = objIds.Print(db, HatchConfig);
                results.Add(hatchId);
            }
            return results;
        }
        public ObjectIdCollection Print(Database db, Polyline polygon)
        {
            var results = new ObjectIdCollection();
            var outlineId = polygon.Print(db, OutlineConfig);
            results.Add(outlineId);
            if (HatchConfig != null)
            {
                var objIds = new ObjectIdCollection { outlineId };
                var hatchId = objIds.Print(db, HatchConfig);
                results.Add(hatchId);
            }
            return results;
        }
        public ObjectIdCollection Print(Database db, MPolygon polygon)
        {
            var results = new ObjectIdCollection();
            if (polygon == null || polygon.Area <= 1.0)
            {
                return results;
            }
            if (HatchConfig != null && polygon.Hatch != null)
            {
                var hatchIds = polygon.Print(db, OutlineConfig, HatchConfig);
                results.AddRange(hatchIds);
            }
            else if(OutlineConfig!=null && polygon!=null)
            {
                var curves = new List<Curve>();
                curves.Add(polygon.Shell());
                curves.AddRange(polygon.Holes());
                curves.ForEach(c =>
                {
                    results.AddRange(Print(db, c));
                });
            }
            return results;
        }
    }
}
