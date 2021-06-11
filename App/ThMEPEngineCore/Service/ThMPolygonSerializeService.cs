using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThMPolygonSerializeService
    {
        public static List<List<string>> Serialize(MPolygon mPolygon)
        {
            //点集合，凸度集合
            if (mPolygon == null || mPolygon.Area == 0.0)
            {
                return new List<List<string>>();
            }
            return mPolygon.Loops().SelectMany(o => ThPolylineSerializeService.Serialize(o)).ToList();
        }
    }
}
