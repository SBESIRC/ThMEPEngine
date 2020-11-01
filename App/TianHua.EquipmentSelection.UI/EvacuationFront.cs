using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.UI
{
    /// <summary>
    /// 避难走道前室
    /// </summary>
    public partial class EvacuationFront : ThAirVolumeUserControl
    {
        private RefugeFontRoomModel Model { get; set; }
        public EvacuationFront(RefugeFontRoomModel model)
        {
            InitializeComponent();
            Model = model;
            gridControl1.DataSource = model.FrontRoomDoors2.ElementAt(0).Value;
            gridControl2.DataSource = model.FrontRoomDoors2.ElementAt(1).Value;
            gridControl3.DataSource = model.FrontRoomDoors2.ElementAt(2).Value;
            UpdateWithModel();
        }

        public override ThFanVolumeModel Data()
        {
            return Model;
        }

        private void Add_Click(object sender, EventArgs e)
        {
            switch (xtraTabControl1.SelectedTabPage.Text)
            {
                case "楼层一":
                    AddDoorItemInModel(Model.FrontRoomDoors2.ElementAt(0).Value, gridControl1, gridView1);
                    break;
                case "楼层二":
                    AddDoorItemInModel(Model.FrontRoomDoors2.ElementAt(1).Value, gridControl2, gridView2);
                    break;
                case "楼层三":
                    AddDoorItemInModel(Model.FrontRoomDoors2.ElementAt(2).Value, gridControl3, gridView3);
                    break;
                default:
                    break;
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            switch (xtraTabControl1.SelectedTabPage.Text)
            {
                case "楼层一":
                    DeleteDoorItemFromModel(Model.FrontRoomDoors2.ElementAt(0).Value, gridControl1, gridView1);
                    break;
                case "楼层二":
                    DeleteDoorItemFromModel(Model.FrontRoomDoors2.ElementAt(1).Value, gridControl2, gridView2);
                    break;
                case "楼层三":
                    DeleteDoorItemFromModel(Model.FrontRoomDoors2.ElementAt(2).Value, gridControl3, gridView3);
                    break;
                default:
                    break;
            }
            UpdateWithModel();
        }

        private void MoveUp_Click(object sender, EventArgs e)
        {
            switch (xtraTabControl1.SelectedTabPage.Text)
            {
                case "楼层一":
                    MoveUpDoorItemInModel(Model.FrontRoomDoors2.ElementAt(0).Value, gridControl1, gridView1);
                    break;
                case "楼层二":
                    MoveUpDoorItemInModel(Model.FrontRoomDoors2.ElementAt(1).Value, gridControl2, gridView2);
                    break;
                case "楼层三":
                    MoveUpDoorItemInModel(Model.FrontRoomDoors2.ElementAt(2).Value, gridControl3, gridView3);
                    break;
                default:
                    break;
            }
        }

        private void MoveDown_Click(object sender, EventArgs e)
        {
            switch (xtraTabControl1.SelectedTabPage.Text)
            {
                case "楼层一":
                    MoveDownDoorItemInModel(Model.FrontRoomDoors2.ElementAt(0).Value, gridControl1, gridView1);
                    break;
                case "楼层二":
                    MoveDownDoorItemInModel(Model.FrontRoomDoors2.ElementAt(1).Value, gridControl2, gridView2);
                    break;
                case "楼层三":
                    MoveDownDoorItemInModel(Model.FrontRoomDoors2.ElementAt(2).Value, gridControl3, gridView3);
                    break;
                default:
                    break;
            }
        }

        private void UpdateWithModel()
        {
            Result.Text = Model.DoorOpeningVolume.ToString();
        }

        private void gridView1_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            var _ThEvacuationDoor = gridView1.GetRow(gridView1.FocusedRowHandle) as ThEvacuationDoor;
            if (_ThEvacuationDoor == null) { return; }
            UpdateWithModel();
        }

        //private void gridView1_CellValueChanging(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        //{
        //    var _ThEvacuationDoor = gridView1.GetRow(gridView1.FocusedRowHandle) as ThEvacuationDoor;
        //    if (_ThEvacuationDoor == null) { return; }
        //    UpdateWithModel(Model);
        //}

        private void DoorInfoChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            UpdateWithModel();
        }
    }
}
