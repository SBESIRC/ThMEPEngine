using Autodesk.AutoCAD.Geometry;
using System;
using ThMEPTCH.TCHTables;

namespace ThMEPTCH.Data
{
    class ThSQLHelper
    {
        public static string TabelModelToSqlString(string tableName,object tableModel) 
        {
            string nameStr = "";
            string valueStr = "";
            foreach (var p in tableModel.GetType().GetFields())
            {
                nameStr += p.Name + ",";
                var value = p.GetValue(tableModel);
                var strValue = value ==null?"": value.ToString();
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
        public static string TwtBlockSqlString(TCHTwtBlock tableModel)
        {
            string nameStr = "ID,Type,Number";
            string valueStr = string.Format("{0},'{1}','{2}'",tableModel.ID,tableModel.Type, tableModel.Number.ToString().PadLeft(8, '0'));
            string insertSql = string.Format("INSERT INTO {0} ({1}) values({2})",
                "TwtBlock", nameStr, valueStr);
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
        public static TCHTwtBlock ValueToTwtBlock(ulong id, string type, int number)
        {
            TCHTwtBlock twtBlock = new TCHTwtBlock
            {
                ID = id,
                Type = type,
                Number = number,
            };
            return twtBlock;
        }
    }
}
