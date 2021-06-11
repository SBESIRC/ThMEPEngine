using ThMEPEngineCore.CAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThPolylineSerializeService
    {
        public static List<List<string>> Serialize(Polyline poly)
        {
            //点集合，凸度集合
            var results = new List<List<string>>();
            if (poly == null || poly.Length == 0.0)
            {
                return results;
            }
            var pts = new List<string>();
            var bulges = new List<double>();
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                pts.Add(poly.GetPoint2dAt(i).PointToString());
                bulges.Add(poly.GetBulgeAt(i));
            }
            var firstItem = new List<string>();
            firstItem.Add(string.Join(";", pts.ToArray()));
            firstItem.Add(string.Join(",", bulges.ToArray()));
            results.Add(firstItem);
            return results;
        }
    }
}
