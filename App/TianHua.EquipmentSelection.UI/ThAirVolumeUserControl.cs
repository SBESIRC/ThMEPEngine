using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using System.Collections.Generic;
using System.Windows.Forms;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.UI
{
    public  class ThAirVolumeUserControl : UserControl
    {
        public virtual ThFanVolumeModel Data()
        {
            return null;
        }

        public void AddDoorItemInModel(List<ThEvacuationDoor> doors, GridControl control, GridView gridview)
        {
            doors.Add(new ThEvacuationDoor());
            control.DataSource = doors;
            gridview.RefreshData();
        }

        public void DeleteDoorItemFromModel(List<ThEvacuationDoor> doors, GridControl control, GridView operategride)
        {
            foreach (int row in operategride.GetSelectedRows())
            {
                doors.RemoveAt(row);
            }
            control.DataSource = doors;
            operategride.RefreshData();
        }

        public void MoveUpDoorItemInModel(List<ThEvacuationDoor> doors, GridControl control, GridView gridview)
        {
            int index = gridview.GetSelectedRows()[0];
            if (index == 0)
            {
                return;
            }
            var tmp = doors[index];
            doors[index] = doors[index - 1];
            doors[index - 1] = tmp;
            control.DataSource = doors;
            gridview.RefreshData();
            gridview.FocusedRowHandle = index - 1;
        }

        public void MoveDownDoorItemInModel(List<ThEvacuationDoor> doors, GridControl control, GridView gridview)
        {
            int index = gridview.GetSelectedRows()[0];
            if (index == doors.Count - 1)
            {
                return;
            }
            var tmp = doors[index];
            doors[index] = doors[index + 1];
            doors[index + 1] = tmp;
            control.DataSource = doors;
            gridview.RefreshData();
            gridview.FocusedRowHandle = index + 1;
        }

    }
}
