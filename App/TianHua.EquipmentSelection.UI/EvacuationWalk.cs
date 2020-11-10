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

namespace TianHua.FanSelection.UI
{
    /// <summary>
    /// 封闭避难层（间）、避难走道
    /// </summary>
    public partial class EvacuationWalk : ThAirVolumeUserControl
    {
        private RefugeRoomAndCorridorModel Model { get; set; }
        private ModelValidator valid = new ModelValidator();
        public EvacuationWalk(RefugeRoomAndCorridorModel model)
        {
            InitializeComponent();
            Model = model;

            if (model.Area_Net != 0)
            {
                AreaInput.Text = model.Area_Net.ToString();
                Volume.Text = model.AirVol_Spec.ToString();
            }

        }

        public override ThFanVolumeModel Data()
        {
            return Model;
        }

        private void AreaInput_EditValueChanged(object sender, EventArgs e)
        {
            if (!Regex.IsMatch(AreaInput.Text, "^[0-9]+$"))
            {
                return;
            }
            Model.Area_Net = Convert.ToInt32(AreaInput.Text);
            UpdateWithModel(Model);
        }

        private void Volume_EditValueChanged(object sender, EventArgs e)
        {
            if (!Regex.IsMatch(Volume.Text, "^[0-9]+$"))
            {
                return;
            }
            if (Convert.ToInt32(Volume.Text) < 30)
            {
                Volume.Text = "30";
                Model.AirVol_Spec = 30;
                UpdateWithModel(Model);
                return;
            }
            Model.AirVol_Spec = Convert.ToInt32(Volume.Text);
            UpdateWithModel(Model);

        }

        private void UpdateWithModel(RefugeRoomAndCorridorModel model)
        {
            Result.Text = "   "+model.WindVolume.ToString();
        }
    }
}
