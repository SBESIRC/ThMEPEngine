using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThBuildingElementRecognitionEngine : IDisposable
    {
        public List<ThIfcBuildingElement> Elements { get; set; }
        public DBObjectCollection Geometries
        {
            get
            {
                return Elements.Select(e => e.Outline).ToCollection();
            }
        }
        public ThBuildingElementRecognitionEngine()
        {
            Elements = new List<ThIfcBuildingElement>();
        }
        public void Dispose()
        {
        }

        public abstract void Recognize(List<ThRawIfcBuildingElementData> objs, Point3dCollection polygon);
        public abstract void Recognize(Database database, Point3dCollection polygon);
        public abstract void RecognizeEditor(Point3dCollection polygon);
        public abstract void RecognizeMS(Database database, Point3dCollection polygon);

        public IEnumerable<ThIfcBuildingElement> FilterByOutline(DBObjectCollection objs)
        {
            return Elements.Where(o => objs.Contains(o.Outline));
        }
        public ThIfcBuildingElement FilterByOutline(DBObject obj)
        {
            return Elements.Where(o => o.Outline.Equals(obj)).FirstOrDefault();
        }
        public void ResetSpatialIndex(ThCADCoreNTSSpatialIndex spatialIndex)
        {
            spatialIndex.Reset(Geometries);
        }
        protected DBObjectCollection Preprocess(DBObjectCollection curves)
        {
            var results = new DBObjectCollection();
            curves.Cast<Entity>().ForEach(e =>
            {
                if (e is Polyline polyline)
                {
                    var objs = polyline.MakeValid().Cast<Polyline>();
                    if(objs.Count()>0)
                    {
                        results.Add(objs.OrderByDescending(o => o.Area).First());
                    }
                }
                else
                {
                    results.Add(e);
                }
            });
            return results;
        }
    }
}
