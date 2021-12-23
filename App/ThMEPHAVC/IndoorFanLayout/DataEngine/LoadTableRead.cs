using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace ThMEPHVAC.IndoorFanLayout.DataEngine
{
    class LoadTableRead
    {
        public bool ReadRoomLoad(Table roomTable, out string roomArea, out string roomLoad)
        {
            roomArea = "";
            roomLoad = "";
            if (null == roomTable || roomTable.Rows.Count < 1)
                return false;
            if (roomTable.Columns.Count < 2)
                return false;
            //房间负荷表为两列数据,第一列为字段，第二列为值，这里不考虑异常表格
            //面积,冷/热负荷;
            for (int rowIndex = 0; rowIndex < roomTable.Rows.Count; rowIndex++)
            {
                var keyCell = roomTable.Cells[rowIndex, 0];
                var valueCell = roomTable.Cells[rowIndex, 1];
                var strKey = ThTableCellTool.TableCellToStringValue(keyCell);
                var strValue = ThTableCellTool.TableCellToStringValue(valueCell);
                // Create an MText to access the fragments
                if (strKey.Contains("面积"))
                {
                    roomArea = strValue;
                }
                else if (strKey.Contains("冷/热负荷"))
                {
                    roomLoad = strValue;
                }
            }
            return true;
        }
    }
}
