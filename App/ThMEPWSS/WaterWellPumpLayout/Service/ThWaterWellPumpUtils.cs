using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.WaterWellPumpLayout.Model;

namespace ThMEPWSS.WaterWellPumpLayout.Service
{
    public class ThWaterWellPumpUtils
    {
        public static double TesslateLength = 50.0;
        public static List<Line> ToLines(List<Entity> entities)
        {
            //要设置分割长度TesslateLength
            var results = new List<Line>();
            entities.ForEach(o =>
            {
                if (o is Polyline polyline)
                {
                    results.AddRange(polyline.ToLines());
                }
                else if (o is MPolygon mPolygon)
                {
                    results.AddRange(mPolygon.Loops().SelectMany(l => l.ToLines()));
                }
                else if (o is Circle circle)
                {
                    results.AddRange(circle.Tessellate(TesslateLength).ToLines());
                }
                else if (o is Line line)
                {
                    if (line.Length > 0)
                    {
                        results.Add(line);
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return results;
        }

        public static void GetPumpIndex(out ThCADCoreNTSSpatialIndex pumpIndex, out Dictionary<int, ThWaterPumpModel> pumpDict)
        {
            //获取潜水泵
            var pumpList = GetDeepWellPumpList();
            pumpDict = new Dictionary<int, ThWaterPumpModel>();
            var objs = new DBObjectCollection();
            foreach (var pump in pumpList)
            {
                pumpDict.Add(pump.OBB.GetHashCode(), pump);
                objs.Add(pump.OBB);
            }
            pumpIndex = new ThCADCoreNTSSpatialIndex(objs);
        }

        public static List<ThWaterPumpModel> GetDeepWellPumpList()
        {
            List<ThWaterPumpModel> deepWellPump = new List<ThWaterPumpModel>();
            using (var database = AcadDatabase.Active())
            using (var engine = new ThWDeepWellPumpEngine())
            {
                var range = new Point3dCollection();
                engine.RecognizeMS(database.Database, range);
                foreach (ThIfcDistributionFlowElement element in engine.Elements)
                {
                    ThWaterPumpModel pump = ThWaterPumpModel.Create(element.Outline);
                    deepWellPump.Add(pump);
                }
            }
            return deepWellPump;
        }
    }
}
