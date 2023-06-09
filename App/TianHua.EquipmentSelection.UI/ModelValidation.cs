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
using DevExpress.XtraEditors;

namespace TianHua.FanSelection.UI
{
    public partial class ModelValidation : UserControl
    {
        public ThFanVolumeModel Model { get; set; }
        public ModelValidation(dynamic model)
        {
            InitializeComponent();
            Model = model;
            Query.ReadOnly = false;
            Query.Text = model.QueryValue.ToString();
            if (Convert.ToInt32(model.Load)==0)
            {
                Query.ReadOnly = true;
            }
            else
            {
                Query.ReadOnly = false;
            }
            bool hasDoubleTypeDoor = false;
            switch (Model.FireScenario)
            {
                case "消防电梯前室":
                case "独立或合用前室（楼梯间自然）":
                case "独立或合用前室（楼梯间送风）":
                    hasDoubleTypeDoor = Model.FrontRoomDoors2.Values.Any(f => f.Where(s => s.Count_Door_Q * s.Width_Door_Q * s.Height_Door_Q !=0).Any(d => d.Type.ToString() == "双扇"));
                    break;
                case "楼梯间（前室不送风）":
                case "楼梯间（前室送风）":
                    hasDoubleTypeDoor = Model.FrontRoomDoors2.Values.Any(f => f.Where(s => s.Count_Door_Q * s.Height_Door_Q * s.Width_Door_Q * s.Crack_Door_Q != 0).Any(d => d.Type.ToString() == "双扇"));
                    break;
                default:
                    break;
            }
            
            List<ThResult> results = new List<ThResult>()
            {
                new ThResult()
                {
                    Name ="24<h≤50",
                    //Result=model.OverAk>=3.2 ? model.AAAA.ToString()+"-"+model.BBBB.ToString() : (0.75*model.AAAA).ToString()+"-"+model.BBBB.ToString()
                    Result=hasDoubleTypeDoor ? model.AAAA.ToString()+"-"+model.BBBB.ToString() : (0.75*model.AAAA).ToString()+"-"+model.BBBB.ToString()
                },
                new ThResult()
                {
                    Name ="50<h≤100",
                    //Result=model.OverAk>=3.2 ? model.CCCC.ToString()+"-"+model.DDDD.ToString() : (0.75*model.CCCC).ToString()+"-"+(model.DDDD).ToString()
                    Result=hasDoubleTypeDoor ? model.CCCC.ToString()+"-"+model.DDDD.ToString() : (0.75*model.CCCC).ToString()+"-"+(model.DDDD).ToString()
                },
            };
            gridControl1.DataSource = results;
            gridView1.RefreshData();
        }

        private void QueryValueChanged(object sender, EventArgs e)
        {
            var thisTextEdit = sender as TextEdit;
            try
            {
                Model.QueryValue = Convert.ToDouble(thisTextEdit.Text);
            }
            catch (Exception)
            {
                Model.QueryValue = 0;
            }
            SetFinalValue();
        }

        public void SetFinalValue()
        {
            FinalValue.Text = Math.Max(Model.TotalVolume, Convert.ToDouble(Model.QueryValue)).ToString();
        }
    }
}
