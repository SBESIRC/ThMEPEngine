using System;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using NetTopologySuite.IO;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Features;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Service
{
    public class ThHydrantResultParseService
    {
        public static Tuple<List<Entity>,List<Entity>> Parse(string[] regions)
        {
            var originPolygons = new List<Entity>();
            var islatedPolygons = new List<Entity>();
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
                                        originPolygons.Add(polygon.ToDbMPolygon());
                                        islatedPolygons.Add(polygon.ToDbMPolygon());
                                    }
                                    else if (f.Geometry is MultiPolygon mPolygon)
                                    {
                                        originPolygons.Add(mPolygon.ToDbMPolygon());
                                        mPolygon.Geometries.Cast<Polygon>().ForEach(m => islatedPolygons.Add(m.ToDbMPolygon()));
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
            return Tuple.Create(originPolygons, islatedPolygons);
        }
        public static List<Point3d> ParsePoints(string[] regions)
        {
            var points = new List<Point3d>();
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
                            if (f.Geometry is Point point)
                            {
                                points.Add(point.ToAcGePoint3d());
                            }
                        }
                    });
                }
            });
            return points;
        } 
    }
}
