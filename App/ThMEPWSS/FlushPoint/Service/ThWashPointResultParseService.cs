using System;
using System.IO;
using ThCADCore.NTS;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using NetTopologySuite.IO;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThWashPointResultParseService
    {
        public static List<Point3d> Parse(string content)
        {
            var results = new List<Point3d>();
            if(IsValid(content))
            {
                var serializer = GeoJsonSerializer.Create();
                using (var stringReader = new StringReader(content))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                    features.ForEach(f =>
                    {
                        if (f.Geometry != null)
                        {
                            if (f.Geometry is Point point)
                            {
                                results.Add(point.ToAcGePoint3d());
                            }
                        }
                    });
                }
            }
            return results;
        }
        private static bool IsValid(string content)
        {
            string pattern = "(\"features\")\\s{0,}[:]{1,1}\\s{0,}(null)";
            return Regex.Matches(content, pattern).Count == 0;
        }
    }
}
