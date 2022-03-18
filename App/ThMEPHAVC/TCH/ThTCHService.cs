using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.TCH
{
    public class ThTCHService
    {
        public static Point3d GetMidPoint(Line l)
        {
            var sp = l.StartPoint;
            var ep = l.EndPoint;
            return new Point3d((sp.X + ep.X) * 0.5, (sp.Y + ep.Y) * 0.5, 0);
        }

        public static void RecordPortInfo(ThSQLiteHelper sqliteHelper, List<TCHInterfaceParam> interfaces)
        {
            sqliteHelper.Conn();
            foreach (var p in interfaces)
            {
                string recordInfo = $"INSERT INTO " + ThTCHCommonTables.interfaceTableName +
                                 " VALUES ('" + p.ID.ToString() + "'," +
                                         "'" + p.sectionType.ToString() + "'," +
                                         "'" + p.height.ToString() + "'," +
                                         "'" + p.width.ToString() + "'," +
                                         "'" + CovertVector(p.normalVector) + "'," +
                                         "'" + CovertVector(p.heighVector) + "'," +
                                         "'" + CovertPoint(p.centerPoint) + "')";
                sqliteHelper.Query<TCHInterfaceParam>(recordInfo);
            }
            
        }
        public static string CovertPoint(Point3d p)
        {
            return $@"{{""X"":{Math.Round(p.X, 6)},""Y"":{Math.Round(p.Y, 6)},""Z"":{Math.Round(p.Z, 6)}}}";
        }
        private static string CovertVector(Vector3d v)
        {
            return $@"{{""X"":{Math.Round(v.X, 4)},""Y"":{Math.Round(v.Y, 4)},""Z"":{Math.Round(v.Z, 4)}}}";
        }
    }
}
