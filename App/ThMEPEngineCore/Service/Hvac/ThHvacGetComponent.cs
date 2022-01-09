using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPEngineCore.Service.Hvac
{
    public class ThHvacGetComponent
    {
        public static List<ObjectId> ReadDuctIds()
        {
            using (var db = AcadDatabase.Active())
            {
                return ReadDuctIds(db.Database);
            }
        }
        public static List<ObjectId> ReadDuctIds(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var ids = new List<ObjectId>();
                foreach (var g in db.Groups)
                {
                    if (g.NumEntities < 1)
                        continue;
                    var id = g.ObjectId;
                    var list = id.GetXData(ThHvacCommon.RegAppName_Duct_Info);
                    if (list != null)
                    {
                        var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                        var type = (string)values.ElementAt(0).Value;
                        if (type == "Duct")
                            ids.Add(id);
                    }
                }
                return ids;
            }
        }
        public static List<ObjectId> ReadConnectorIds()
        {
            using (var db = AcadDatabase.Active())
            {
                return ReadConnectorIds(db.Database);
            }
        }
        public static List<ObjectId> ReadConnectorIds(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var connectors = new List<ObjectId>();
                foreach (var g in db.Groups)
                {
                    if (g.NumEntities < 1)
                        continue;
                    var id = g.ObjectId;
                    var list = id.GetXData(ThHvacCommon.RegAppName_Duct_Info);
                    if (list != null)
                    {
                        var values = list.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                        var type = (string)values.ElementAt(0).Value;
                        if (type == "Tee" || type == "Cross" || type == "Reducing" || type == "Elbow")
                            connectors.Add(id);
                    }
                }
                return connectors;
            }
        }
    }
}
