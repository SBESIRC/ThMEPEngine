using System;
using System.Linq;
using NFox.Cad;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPHVAC.LoadCalculation.Model;

namespace ThMEPHVAC.Service
{
    public class ThQueryRoomAirVolumeService
    {
        private DBObjectCollection Tables { get; set; }
        private DBObjectCollection MarkLines { get; set; }
        public ThQueryRoomAirVolumeService()
        {
            // 建议支持一次性对单个房间查询
            Tables = GetTables(AcHelper.Active.Database);
            MarkLines = GetMarkLines(AcHelper.Active.Database);
        }

        public object Query(ThIfcRoom room,string keyWord)
        {
            if (room == null || room.Boundary == null ||
                string.IsNullOrEmpty(keyWord))
            {
                return null;
            }
            var table = room.GetLoadCalculationTable(
                MarkLines.OfType<Curve>().ToList(),
                Tables.OfType<Table>().ToList());
            if (table != null)
            {
                var cellPos = Read(table, keyWord);
                return Read(table, cellPos.Item1, cellPos.Item2 + 1);
            }            
            return null;
        }

        public double ConvertToDouble(object value)
        {
            if (value != null)
            {
                double outValue = 0.0;
                if (double.TryParse(value.ToString(), out outValue))
                {
                    return outValue;
                }
            }
            return 0.0;
        }

        public string ConvertToString(object value)
        {
            if (value != null)
            {
                return value.ToString();
            }
            return "";
        }

        private Tuple<int,int> Read(Table table,string content)
        {
            if(table.Rows.Count==0 || table.Columns.Count==0)
            {
                return Tuple.Create(-1, -1);
            }
            for(int i=0;i<table.Rows.Count;i++)
            {
                if(table.Cells[i, 0].Value==null)
                {
                    continue;
                }
                if(table.Cells[i, 0].Value.ToString().Contains(content))
                {
                    return Tuple.Create(i, 0);
                }
            }
            return Tuple.Create(-1, -1);
        }

        private object Read(Table table, int rowIndex,int columnIndex)
        {
            if(rowIndex>=0 && rowIndex<table.Rows.Count)
            {
                if(columnIndex >= 0 && columnIndex < table.Columns.Count)
                {
                    return table.Cells[rowIndex, columnIndex].Value;
                }
            }
            return null;
        }

        private DBObjectCollection GetTables(Database database)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                return acadDb.ModelSpace
                .OfType<Table>()
                .Where(o => o.Layer == LoadCalculationParameterFromConfig.LoadCalculationTableLayer &&
                o.TableStyleName == LoadCalculationParameterFromConfig.LoadCalculationTableName)
                .Select(o=>o.Clone() as Table)
                .ToCollection();
            }
        }

        private DBObjectCollection GetMarkLines(Database database)
        {
            // 获取图层为“AI-负荷通风标注”的线
            using (var acadDb = AcadDatabase.Use(database))
            {
                return acadDb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == LoadCalculationParameterFromConfig.LoadCalculationTableLayer)
                .Select(o=>o.Clone() as Curve)
                .ToCollection();
            }
        }
    }
}
