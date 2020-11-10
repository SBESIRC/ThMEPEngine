using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TianHua.FanSelection.Function;
using TianHua.FanSelection.Model;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.UI
{
    public partial class fmScenario : DevExpress.XtraEditors.XtraForm
    {
        public FanDataModel Model { get; set; }
        private Dictionary<string, ExhaustCalcModel> ExhaustModels { get; set; }

        public string m_ScenarioType { get; set; }

        public fmScenario()
        {
            InitializeComponent();
        }

        private void TxtCalcValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        public void InitForm(FanDataModel _FanDataModel)
        {
            var _Json = FuncJson.Serialize(_FanDataModel);
            Model = FuncJson.Deserialize<FanDataModel>(_Json);

            ExhaustModels = new Dictionary<string, ExhaustCalcModel>()
            {
                { "空间-净高小于等于6m", new ExhaustCalcModel(){ SpatialTypes = "办公室、学校、客厅、走道"} },
                { "空间-净高大于6m", new ExhaustCalcModel(){ SpatialTypes = "办公室、学校、客厅、走道"}},
                { "空间-汽车库", new ExhaustCalcModel(){ SpatialTypes = "办公室、学校、客厅、走道"}},
                { "走道回廊-仅走道或回廊设置排烟", new ExhaustCalcModel(){ SpatialTypes = "办公室、学校、客厅、走道"}},
                { "走道回廊-房间内和走道或回廊都设置排烟", new ExhaustCalcModel(){ SpatialTypes = "办公室、学校、客厅、走道"}},
                { "中庭-周围场所设有排烟系统", new ExhaustCalcModel(){ SpatialTypes = "办公室、学校、客厅、走道"}},
                { "中庭-周围场所不设排烟系统", new ExhaustCalcModel(){ SpatialTypes = "办公室、学校、客厅、走道"}}
            };

            if (!Model.ExhaustModel.IsNull())
            {
                TxtCalcValue.Text = ExhaustModelCalculator.GetTxtCalcValue(Model.ExhaustModel);
                m_ScenarioType = Model.ExhaustModel.ExhaustCalcType;
                if (Model.ExhaustModel.SpaceHeight.IsNullOrEmptyOrWhiteSpace())
                {
                    this.RadLessThan.Checked = true;
                }
                else
                {
                    ExhaustModels[m_ScenarioType] = Model.ExhaustModel;
                    switch (Model.ExhaustModel.ExhaustCalcType)
                    {
                        case "空间-净高小于等于6m":
                            this.RadLessThan.Checked = true;
                            break;
                        case "空间-净高大于6m":
                            this.RadGreaterThan6.Checked = true;
                            break;
                        case "空间-汽车库":
                            this.RadGarage.Checked = true;
                            break;
                        case "走道回廊-仅走道或回廊设置排烟":
                            this.RadCloister.Checked = true;
                            break;
                        case "走道回廊-房间内和走道或回廊都设置排烟":
                            this.RadCloistersAndRooms.Checked = true;
                            break;
                        case "中庭-周围场所设有排烟系统":
                            this.RadSmokeExtraction.Checked = true;
                            break;
                        case "中庭-周围场所不设排烟系统":
                            this.RadNoSmokeExtraction.Checked = true;
                            break;

                        default:
                            break;
                    }
                }
            }
            else
            {
                this.RadLessThan.Checked = true;
                m_ScenarioType = this.RadLessThan.Text;
                TxtCalcValue.Text = "无";
            }
        }

        private void BtnCalc_Click(object sender, EventArgs e)
        {
            fmExhaustCalc _fmExhaustCalc = new fmExhaustCalc();
            Model.ExhaustModel = ExhaustModels[m_ScenarioType];
            _fmExhaustCalc.InitForm(Model, m_ScenarioType);
            if (_fmExhaustCalc.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            ExhaustModels[m_ScenarioType] = _fmExhaustCalc.Model.ExhaustModel;
            Model.ExhaustModel = _fmExhaustCalc.Model.ExhaustModel;
            if (!Model.ExhaustModel.IsNull())
            {
                TxtCalcValue.Text = ExhaustModelCalculator.GetTxtCalcValue(Model.ExhaustModel);
            }
            else
            {
                TxtCalcValue.Text = "无";
            }
        }

        private void Rad_CheckedChanged(object sender, EventArgs e)
        {
            var _Rad = sender as RadioButton;
            if (_Rad == null) { return; }
            Model.ExhaustModel = ExhaustModels[_Rad.Text];
            m_ScenarioType = _Rad.Text;
            TxtCalcValue.Text = ExhaustModelCalculator.GetTxtCalcValue(Model.ExhaustModel);
        }
    }
}
