﻿using System;
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
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.UI
{
    /// <summary>
    /// 楼梯间（前室送风）
    /// </summary>
    public partial class StaircaseWind : ThAirVolumeUserControl
    {
        private StaircaseAirModel Model { get; set; }
        private ModelValidation subview;
        private ModelValidator valid = new ModelValidator();
        public StaircaseWind(StaircaseAirModel model)
        {
            InitializeComponent();
            Residence.Enabled = false;
            Business.Enabled = false;
            Model = model;
            gridControl1.DataSource = model.FrontRoomDoors2.ElementAt(0).Value;
            gridControl2.DataSource = model.FrontRoomDoors2.ElementAt(1).Value;
            gridControl3.DataSource = model.FrontRoomDoors2.ElementAt(2).Value;
            CheckPanel.Controls.Clear();
            subview = new ModelValidation(Model);
            CheckPanel.Controls.Add(subview);
            N2Value.Text = Model.N2.ToString();

            switch (model.Load)
            {
                case StaircaseAirModel.LoadHeight.LoadHeightLow:
                    lowLoad.Checked = true;
                    break;
                case StaircaseAirModel.LoadHeight.LoadHeightMiddle:
                    middleLoad.Checked = true;
                    break;
                case StaircaseAirModel.LoadHeight.LoadHeightHigh:
                    highLoad.Checked = true;
                    break;
                default:
                    break;
            }

            switch (model.Stair)
            {
                case StaircaseAirModel.StairLocation.OnGround:
                    OnGound.Checked = true;
                    break;
                case StaircaseAirModel.StairLocation.UnderGound:
                    UnderGound.Checked = true;
                    break;
                default:
                    break;
            }

            switch (model.Type_Area)
            {
                case StaircaseAirModel.SpaceState.Residence:
                    Residence.Checked = true;
                    break;
                case StaircaseAirModel.SpaceState.Business:
                    Business.Checked = true;
                    break;
                default:
                    break;
            }

            if (model.Count_Floor != 0)
            {
                layerCount.Text = model.Count_Floor.ToString();
            }

            if (model.Load== StaircaseAirModel.LoadHeight.LoadHeightLow)
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
            Model.StairN1 = GetN1Value();
            if (lowLoad.Checked)
            {
                Model.Load = StaircaseAirModel.LoadHeight.LoadHeightLow;
                UpdateWithModel();
                CheckPanel.Controls.Clear();
                CheckPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            }
        }

        private void middleLoad_CheckedChanged(object sender, EventArgs e)
        {
            Model.StairN1 = GetN1Value();
            if (middleLoad.Checked)
            {
                Model.Load = StaircaseAirModel.LoadHeight.LoadHeightMiddle;
                UpdateWithModel();
            }
        }

        private void highLoad_CheckedChanged(object sender, EventArgs e)
        {
            Model.StairN1 = GetN1Value();
            if (highLoad.Checked)
            {
                Model.Load = StaircaseAirModel.LoadHeight.LoadHeightHigh;
                UpdateWithModel();
            }
        }

        private void layerCount_EditValueChanged_1(object sender, EventArgs e)
        {
            if (!Regex.IsMatch(layerCount.Text, "^[0-9]+$"))
            {
                return;
            }
            Model.Count_Floor = Convert.ToInt32(layerCount.Text);
            UpdateWithModel();
        }

        private void OnGound_CheckedChanged(object sender, EventArgs e)
        {
            Residence.Enabled = false;
            Business.Enabled = false;
            Model.StairN1 = GetN1Value();
            Model.Stair = StaircaseAirModel.StairLocation.OnGround;
            UpdateWithModel();
        }
        private void UnderGound_CheckedChanged(object sender, EventArgs e)
        {
            Residence.Enabled = true;
            Business.Enabled = true;
            Model.StairN1 = GetN1Value();
            Model.Stair = StaircaseAirModel.StairLocation.UnderGound;
            UpdateWithModel();
        }

        private void Residence_CheckedChanged(object sender, EventArgs e)
        {
            Model.StairN1 = GetN1Value();
            Model.Type_Area = StaircaseAirModel.SpaceState.Residence;
            UpdateWithModel();
        }

        private void Business_CheckedChanged(object sender, EventArgs e)
        {
            Model.StairN1 = GetN1Value();
            Model.Type_Area = StaircaseAirModel.SpaceState.Business;
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

        private void StairacaseWind_Load(object sender, EventArgs e)
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
            Model.StairN1 = GetN1Value();
            UpdateWithModel();
            subview.Refresh();
        }

        private void CountFloorChanged(object sender, EventArgs e)
        {
            if (GetN1Value() == -1)
            {
                return;
            }
            Model.StairN1 = GetN1Value();
        }

        private int GetN1Value()
        {
            if (!Regex.IsMatch(layerCount.Text, "^[0-9]+$"))
            {
                return 0;
            }

            if (OnGound.Checked)
            {
                if (lowLoad.Checked)
                {
                    return 2;
                }

                return 3;
            }
            else
            {
                if (Residence.Checked)
                {
                    return 1;
                }
                return Math.Min(3, Convert.ToInt32(layerCount.Text));
            }

        }

        private void DoorTypeChanged(object sender, EventArgs e)
        {
            gridView1.PostEditor();
            gridView2.PostEditor();
            gridView3.PostEditor();
            Model.StairN1 = GetN1Value();
            UpdateWithModel();
            subview.Refresh();
        }

        private void N2Value_EditValueChanged(object sender, EventArgs e)
        {
            Model.N2 = N2Value.Text.NullToDouble();
            UpdateWithModel();
        }
    }
}
