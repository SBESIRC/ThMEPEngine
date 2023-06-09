﻿using System.IO;
using System.Linq;
using System.Collections.Generic;

using NetTopologySuite.IO;
using NetTopologySuite.Features;
using Newtonsoft.Json;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDDeserializeGJsonService
    {
        public static List<ThToiletGJson> getGroupPt(string GeoJsonString)
        {
            List<ThToiletGJson> groupedSupplyPt = new List<ThToiletGJson>();

            var serializer = GeoJsonSerializer.Create();

            using (var stringReader = new StringReader(GeoJsonString))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                var features = serializer.Deserialize<FeatureCollection>(jsonReader);

                foreach (var f in features)
                {
                    if (f.Attributes.Exists("Category") && f.Attributes["Category"].ToString() == ThDrainageSDCommon.GJWaterSupplyPoint)
                    {
                        var coordinates = f.Geometry.Coordinates;
                        var dirArr = f.Attributes["Direction"] as List<object>;
                        var dirVector = new Vector3d(double.Parse(dirArr[0].ToString()), double.Parse(dirArr[1].ToString()), 0).GetNormal();

                        var item = new ThToiletGJson()
                        {
                            Pt = new Point3d(coordinates[0].X, coordinates[0].Y, 0),
                            Direction = dirVector,
                            Id = f.Attributes["Id"] as string,
                            AreaId = f.Attributes["AreaId"] as string,
                            GroupId = f.Attributes["GroupId"] as string,
                        };

                        groupedSupplyPt.Add(item);
                    }
                }
            }


            return groupedSupplyPt;

        }

        public static List<Line> getBranchLineList(string GeoJsonString)
        {
            List<Line> branchList = new List<Line>();

            var serializer = GeoJsonSerializer.Create();

            try
            {
                using (var stringReader = new StringReader(GeoJsonString))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    var features = serializer.Deserialize<FeatureCollection>(jsonReader);

                    foreach (var f in features)
                    {
                        if (f.Attributes.Exists("Category") && f.Attributes["Category"].ToString() == ThDrainageSDCommon.GJPipe)
                        {
                            if (f.Geometry.GeometryType.Equals("LineString"))
                            {
                                var coordinates = f.Geometry.Coordinates;
                                var linePts = new List<Point3d>();
                                foreach (var coord in coordinates)
                                {
                                    linePts.Add(new Point3d(coord.X, coord.Y, 0));
                                }

                                var list = Enumerable.Range(0, linePts.Count - 1)
                                          .Select(index => new Line(linePts[index], linePts[index + 1]))
                                          .ToList();

                                branchList.AddRange(list);
                            }
                        }
                    }
                }
            }
            catch
            {
               
            }
            
            return branchList;
        }
    }
}
