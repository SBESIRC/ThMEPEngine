using System;
using System.IO;
using System.Text;
using ThCADCore.NTS;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using NetTopologySuite.IO;
using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using System.Text.RegularExpressions;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThGeometryJsonReader
    {
        public static List<ThGeometry> ReadFromContent(string content)
        {
            var results = new List<ThGeometry>();
            if(!IsValid(content))
            {
                return results;
            }
            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new StringReader(content))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                features.ForEach(f =>
                {
                    var geo = new ThGeometry();
                    if (f.Geometry != null)
                    {
                        if (f.Geometry is Point point)
                        {
                            geo.Boundary = new DBPoint(point.ToAcGePoint3d());
                        }
                        else if (f.Geometry is Polygon polygon)
                        {
                            geo.Boundary = polygon.ToDbMPolygon();
                        }
                        else if(f.Geometry is LineString lineString)
                        {
                            geo.Boundary = lineString.ToDbPolyline();
                        }
                        else if(f.Geometry is MultiPolygon multiPolygon)
                        {
                            geo.Boundary = multiPolygon.ToDbMPolygon();
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }

                    if (f.Attributes != null)
                    {
                        foreach(string name in f.Attributes.GetNames())
                        {
                            geo.Properties.Add(name, f.Attributes[name]);
                        }
                    }
                    results.Add(geo);
                });
            }
            return results;
        }
        public static List<ThGeometry> ReadFromFile(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            if(!fi.Exists)
            {
                return new List<ThGeometry>();
            }
            var content = File.ReadAllText(fileName, Encoding.UTF8);
            return ReadFromContent(content);
        }
        private static bool IsValid(string content)
        {
            string pattern = "(\"" + "features" + "\")" + @"\s{0,}[:]{1}\s{0,}(null)";
            var rg = new Regex(pattern);
            return !rg.IsMatch(content);
        }
    }
}
