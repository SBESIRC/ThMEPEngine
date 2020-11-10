using System;
using System.Windows.Forms;
using System.Collections.Generic;
using TianHua.FanSelection.Model;

namespace TianHua.FanSelection.UI
{
    public partial class fmFanVolumeCalc : DevExpress.XtraEditors.XtraForm
    {
        private List<ThAirVolumeUserControl> Views { get; set; }
        public fmFanVolumeCalc(FanDataModel fandatamodel)
        {
            InitializeComponent();
            this.ShowIcon = false;
            middlePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            var model = new FireFrontModel();
            var modelNatural = new FontroomNaturalModel();
            var modelWind = new FontroomWindModel();
            var modelStairNoWind = new StaircaseNoAirModel();
            var modelStairWind = new StaircaseAirModel();
            var modelRefugeLayer = new RefugeRoomAndCorridorModel();
            var modelRefugeFont = new RefugeFontRoomModel();

            int comboBox1index = 0;
            if (fandatamodel.FanVolumeModel is FireFrontModel)
            {
                model = fandatamodel.FanVolumeModel as FireFrontModel;
            }
            else if (fandatamodel.FanVolumeModel is FontroomNaturalModel)
            {
                modelNatural = fandatamodel.FanVolumeModel as FontroomNaturalModel;
                comboBox1index = 1;
            }
            else if (fandatamodel.FanVolumeModel is FontroomWindModel)
            {
                modelWind = fandatamodel.FanVolumeModel as FontroomWindModel;
                comboBox1index = 2;
            }
            else if (fandatamodel.FanVolumeModel is StaircaseNoAirModel)
            {
                modelStairNoWind = fandatamodel.FanVolumeModel as StaircaseNoAirModel;
                comboBox1index = 3;
            }
            else if (fandatamodel.FanVolumeModel is StaircaseAirModel)
            {
                modelStairWind = fandatamodel.FanVolumeModel as StaircaseAirModel;
                comboBox1index = 4;
            }
            else if (fandatamodel.FanVolumeModel is RefugeRoomAndCorridorModel)
            {
                modelRefugeLayer = fandatamodel.FanVolumeModel as RefugeRoomAndCorridorModel;
                comboBox1index = 5;
            }
            else if (fandatamodel.FanVolumeModel is RefugeFontRoomModel)
            {
                modelRefugeFont = fandatamodel.FanVolumeModel as RefugeFontRoomModel;
                comboBox1index = 6;
            }

            Views = new List<ThAirVolumeUserControl>()
            {
                new FireElevatorFrontRoom(model),            //消防电梯前室
                new SeparateOrSharedNatural(modelNatural),    //独立或合用前室（楼梯间自然）
                new SeparateOrSharedWind(modelWind),           //独立或合用前室（楼梯间送风）
                new StaircaseNoWind(modelStairNoWind),          //楼梯间（前室不送风）
                new StaircaseWind(modelStairWind),                 //楼梯间（前室送风）
                new EvacuationWalk(modelRefugeLayer),                 //封闭避难层（间）、避难走道
                new EvacuationFront(modelRefugeFont)                      //避难走道前室
            };
            comboBox1.SelectedIndex = comboBox1index;
        }
        public string CurrentScenairo { get; set; }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            middlePanel.Controls.Clear();
            middlePanel.Controls.Add(Views[comboBox.SelectedIndex]);
            CurrentScenairo = comboBox.Text;
        }

        public ThFanVolumeModel Model
        {
            get
            {
                switch (comboBox1.Text)
                {
                    case "消防电梯前室" :
                        return Views[0].Data();
                    case "独立或合用前室（楼梯间自然）":
                        return Views[1].Data();
                    case "独立或合用前室（楼梯间送风）":
                        return Views[2].Data();
                    case "楼梯间（前室不送风）":
                        return Views[3].Data();
                    case "楼梯间（前室送风）":
                        return Views[4].Data();
                    case "封闭避难层（间）、避难走道":
                        return Views[5].Data();
                    case "避难走道前室":
                        return Views[6].Data();
                    default:
                        break;
                }
                return null;
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            //
        }      
    }
}
