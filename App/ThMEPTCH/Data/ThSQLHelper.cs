using Autodesk.AutoCAD.Geometry;
using System;
using ThMEPTCH.TCHTables;

namespace ThMEPTCH.Data
{
    class ThSQLHelper
    {
        public static string TabelModelToSqlString(string tableName, object tableModel)
        {
            string nameStr = "";
            string valueStr = "";
            foreach (var p in tableModel.GetType().GetFields())
            {
                nameStr += p.Name + ",";
                var value = p.GetValue(tableModel);
                var strValue = value == null ? "" : value.ToString();
                var type = p.FieldType;
                if (type == typeof(int) || type == typeof(double) || type == typeof(ulong))
                {
                    valueStr += string.Format("{0},", strValue);
                }
                else
                {
                    valueStr += string.Format("'{0}',", strValue);
                }
            }
            nameStr = nameStr.Substring(0, nameStr.LastIndexOf(","));
            valueStr = valueStr.Substring(0, valueStr.LastIndexOf(","));
            string insertSql = string.Format("INSERT INTO {0} ({1}) values({2})",
                tableName, nameStr, valueStr);
            return insertSql;
        }
        public static TCHTwtPoint PointToTwtPointModel(ulong Id, Point3d point)
        {
            TCHTwtPoint twtPoint = new TCHTwtPoint
            {
                ID = Id,
                X = Math.Round(point.X, 6).ToString(),
                Y = Math.Round(point.Y, 6).ToString(),
                Z = Math.Round(point.Z, 6).ToString(),
            };
            return twtPoint;
        }
        public static TCHTgPoint PointToTCHTgPoint(ulong Id, Point3d point)
        {
            TCHTgPoint tCHTgPoint = new TCHTgPoint
            {
                ID = ((int)Id),
                X = Math.Round(point.X, 6).ToString(),
                Y = Math.Round(point.Y, 6).ToString(),
                Z = Math.Round(point.Z, 6).ToString(),
            };
            return tCHTgPoint;
        }
        public static TCHTgPublicList ConvertToTCHTgPublicList(ulong Id, int pointID, int nextID)
        {
            TCHTgPublicList tCHTgPublicList = new TCHTgPublicList
            {
                ID = ((int)Id),
                PointID = pointID,
                NextID = nextID,
            };
            return tCHTgPublicList;
        }

        public static TCHTwtPublicList ConvertToTCHTwtPublicList(ulong Id, int pointID, int pipeID, string value, int nextID)
        {
            TCHTwtPublicList tCHTgPublicList = new TCHTwtPublicList
            {
                ID = ((int)Id),
                PipeID = pipeID,
                PointID = pointID,
                Value = value,
                NextID = nextID,
            };
            return tCHTgPublicList;
        }

        public static TCHTwtVector VectorToTwtVectorModel(ulong Id, Vector3d vector)
        {
            TCHTwtVector tvwVector = new TCHTwtVector
            {
                ID = Id,
                X = Math.Round(vector.X, 6).ToString(),
                Y = Math.Round(vector.Y, 6).ToString(),
                Z = Math.Round(vector.Z, 6).ToString(),
            };
            return tvwVector;
        }
    }
}
