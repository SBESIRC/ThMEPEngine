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
using System.Text.RegularExpressions;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace TianHua.FanSelection.UI
{
    /// <summary>
    ///独立或合用前室（楼梯间送风）
    /// </summary>
    public partial class SeparateOrSharedWind : ThAirVolumeUserControl
    {
        private ModelValidation subview;
        private FontroomWindModel Model { get; set; }
        private ModelValidator valid = new ModelValidator();

        public SeparateOrSharedWind(FontroomWindModel  model)
        {
            InitializeComponent();
            Model = model;
            gridControl1.DataSource = model.FrontRoomDoors2.ElementAt(0).Value;
            gridControl2.DataSource = model.FrontRoomDoors2.ElementAt(1).Value;
            gridControl3.DataSource = model.FrontRoomDoors2.ElementAt(2).Value;
            CheckPanel.Controls.Clear();
            subview = new ModelValidation(Model);
            CheckPanel.Controls.Add(subview);

            if (model.Count_Floor != 0)
            {
                layerCount.Text = model.Count_Floor.ToString();
                length.Text = model.Length_Valve.ToString();
                wide.Text = model.Width_Valve.ToString();
            }

            switch (model.Load)
            {
                case FontroomWindModel.LoadHeight.LoadHeightLow:
                    lowLoad.Checked = true;
                    break;
                case FontroomWindModel.LoadHeight.LoadHeightMiddle:
                    middleLoad.Checked = true;
                    break;
                case FontroomWindModel.LoadHeight.LoadHeightHigh:
                    highLoad.Checked = true;
                    break;
                default:
                    break;
            }

            if (model.Load == FontroomWindModel.LoadHeight.LoadHeightLow)
            {
                UpdateWithModel();
                CheckPanel.Controls.Clear();
                CheckPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            }
        }

        public override ThFanVolumeModel Data()
        {
            return Model;
        }

        private void lowLoad_CheckedChanged(object sender, EventArgs e)
        {
            if (lowLoad.Checked)
            {
                Model.Load = Model.Load = FontroomWindModel.LoadHeight.LoadHeightLow; 
                UpdateWithModel();
                CheckPanel.Controls.Clear();
                CheckPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            }
           
        }

        private void middleLoad_CheckedChanged(object sender, EventArgs e)
        {
            if (middleLoad.Checked)
            {
                Model.Load = Model.Load = FontroomWindModel.LoadHeight.LoadHeightMiddle;
                UpdateWithModel();
            }
           
        }

        private void highLoad_CheckedChanged(object sender, EventArgs e)
        {
            if (highLoad.Checked)
            {
                Model.Load = Model.Load = FontroomWindModel.LoadHeight.LoadHeightHigh;
                UpdateWithModel();
            }
            
        }

        private void layerCount_EditValueChanged(object sender, EventArgs e)
        {
            if (!Regex.IsMatch(layerCount.Text, "^[0-9]+$"))
            {
                return;
            }
            Model.Count_Floor = Convert.ToInt32(layerCount.Text);
            UpdateWithModel();
          
        }

        private void length_EditValueChanged(object sender, EventArgs e)
        {
            if (!Regex.IsMatch(length.Text, "^[0-9]+$"))
            {
                return;
            }
            Model.Length_Valve = Convert.ToInt32(length.Text);
            UpdateWithModel();
        }

        private void wide_EditValueChanged(object sender, EventArgs e)
        {
            if (!Regex.IsMatch(wide.Text, "^[0-9]+$"))
            {
                return;
            }
            Model.Width_Valve = Convert.ToInt32(wide.Text);
            UpdateWithModel();        
        }
        private void UpdateWithModel()
        {
            Lj.Text = Convert.ToString(Model.TotalVolume);
            L1.Text = Convert.ToString(Model.DoorOpeningVolume);
            L3.Text = Convert.ToString(Model.LeakVolume);
            if (lowLoad.Checked)
            {
                Tips.Text = " ";
            }
            else if (middleLoad.Checked)
            {
                CheckLjValue(Model.AAAA, Model.BBBB);
                CheckPanel.Controls.Clear();
                subview = new ModelValidation(Model);
                CheckPanel.Controls.Add(subview);
                CheckPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            }
            else if (highLoad.Checked)
            {
                CheckLjValue(Model.CCCC, Model.DDDD);
                CheckPanel.Controls.Clear();
                subview = new ModelValidation(Model);
                CheckPanel.Controls.Add(subview);
                CheckPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            }
            subview.SetFinalValue();
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

        private void SeparateOrSharedWind_Load(object sender, EventArgs e)
        {
            //
        }

        private void CheckLjValue(double minvalue, double maxvalue)
        {
            if (Model.OverAk > 3.2)
            {
                if (Model.TotalVolume < minvalue)
                {
                    Tips.Text = "计算值不满足规范";
                    Tips.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    Tips.Text = "计算值满足规范";
                    Tips.ForeColor = System.Drawing.Color.Green;
                }
            }
            else
            {
                if (Model.TotalVolume < 0.75 * minvalue)
                {
                    Tips.Text = "计算值不满足规范";
                    Tips.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    Tips.Text = "计算值满足规范";
                    Tips.ForeColor = System.Drawing.Color.Green;
                }

            }
        }

        private void DoorInfoChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            UpdateWithModel();
            subview.Refresh();
        }

    }
}
