using System;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using NetTopologySuite.IO;
using NetTopologySuite.Features;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThHydrantResultParseService
    {
        public static List<Polygon> Parse(string[] regions)
        {
            var polygons = new List<Polygon>();
            var serializer = GeoJsonSerializer.Create();
            regions.ForEach(o =>
            {
                using (var stringReader = new StringReader(o))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                    features.ForEach(f =>
                    {
                        if (f.Geometry != null)
                        {
                            if (f.Attributes.Exists("Name"))
                            {
                                if (f.Attributes["Name"] as string == "Covered region")
                                {
                                    if (f.Geometry is Polygon polygon)
                                    {
                                        polygons.Add(polygon);                                       
                                    }
                                    else if (f.Geometry is MultiPolygon mPolygon)
                                    {
                                        polygons.AddRange(mPolygon.Geometries.Cast<Polygon>().ToList());
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                            }
                        }
                    });
                }
            });            
            return polygons;
        }
        public static List<Entity> ToDbEntities(List<Polygon> polygons)
        {
            var results = new List<Entity>();
            polygons.ForEach(o =>
            {
                if(o.InteriorRings.Length==0)
                {
                    results.Add(o.Shell.ToDbPolyline());
                }
                else
                {
                    results.Add(o.ToDbMPolygon());
                }
            });
            return results;
        }
    }
}
