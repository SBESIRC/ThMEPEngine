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
        public static List<Tuple<Entity,Point3d,List<Entity>>> Parse(string[] regions)
        {
            var results = new List<Tuple<Entity, Point3d, List<Entity>>>();
            var serializer = GeoJsonSerializer.Create();
            regions.ForEach(o =>
            {
                using (var stringReader = new StringReader(o))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    Entity coverArea = null;
                    Point3d position = Point3d.Origin;
                    var islatedPolygons = new List<Entity>();
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
                                        coverArea = ThHydrantUtils.MakeValid(polygon);                                        ;
                                        islatedPolygons.Add(coverArea.Clone() as Entity);
                                    }
                                    else if (f.Geometry is MultiPolygon mPolygon)
                                    {
                                        coverArea = ThHydrantUtils.MakeValid(mPolygon);
                                        mPolygon.Geometries
                                        .Cast<Polygon>()
                                        .ForEach(m => islatedPolygons.Add(ThHydrantUtils.MakeValid(m)));
                                    }
                                    else
                                    {
                                        throw new NotSupportedException();
                                    }
                                }
                            }
                            else if (f.Geometry is Point point)
                            {
                                position=point.ToAcGePoint3d();
                            }
                        }
                    });
                    if(coverArea!=null)
                    {
                        results.Add(Tuple.Create(coverArea, position, islatedPolygons));
                    }
                }
            });            
            return results;
        }        
    }
}
