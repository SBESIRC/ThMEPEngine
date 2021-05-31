using System.IO;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPWSS.FlushPoint
{
    public class ThFlushPointUtils
    {
        public static void OutputGeo(string activeDocName,List<ThGeometry> geos)
        {
            // 输出GeoJson文件
            var fileInfo = new FileInfo(activeDocName);
            var path = fileInfo.Directory.FullName;
            ThGeoOutput.Output(geos, path, fileInfo.Name);
        }
        public static List<Point3d> GetPoints(double[] coords)
        {
            var results = new List<Point3d>();
            for (int i = 0; i < coords.Length; i += 2)
            {
                results.Add(new Point3d(coords[i], coords[i + 1], 0));
            }
            return results;
        }
    }
}
