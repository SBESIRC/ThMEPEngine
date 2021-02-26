using System;
using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using ThCADExtension;
using DevExpress.XtraEditors;
using TianHua.Publics.BaseCode;
using System.Collections.Generic;

namespace TianHua.FanSelection.UI
{
    public partial class fmFanModel : DevExpress.XtraEditors.XtraForm
    {
        public FanDataModel m_Fan = new FanDataModel();

        public List<FanDataModel> m_ListFan = new List<FanDataModel>();

        public List<AxialFanEfficiency> m_ListAxialFanEfficiency = new List<AxialFanEfficiency>();

        public List<FanEfficiency> m_ListFanEfficiency = new List<FanEfficiency>();

        public List<MotorPower> m_ListMotorPower = new List<MotorPower>();

        public List<MotorPower> m_ListMotorPowerDouble = new List<MotorPower>();

        public fmFanModel()
        {
            InitializeComponent();


            InitData();
        }

        private void fmFanModel_Load(object sender, EventArgs e)
        {
            this.Size = new Size(488, 520);

        }

        public void InitData()
        {
            var _JsonFanEfficiency = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Efficiency));
            m_ListFanEfficiency = FuncJson.Deserialize<List<FanEfficiency>>(_JsonFanEfficiency);


            var _JsonAxialFanEfficiency = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.AXIAL_Efficiency));
            m_ListAxialFanEfficiency = FuncJson.Deserialize<List<AxialFanEfficiency>>(_JsonAxialFanEfficiency);

            var _JsonMotorPower = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.MOTOR_POWER));
            m_ListMotorPower = FuncJson.Deserialize<List<MotorPower>>(_JsonMotorPower);

            var _JsonMotorPowerDouble = ReadTxt(Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.MOTOR_POWER_Double));
            m_ListMotorPowerDouble = FuncJson.Deserialize<List<MotorPower>>(_JsonMotorPowerDouble);

            if (m_ListFanEfficiency != null && m_ListFanEfficiency.Count > 0)
            {
                m_ListFanEfficiency.ForEach(p =>
                {
                    if (p.No_Max == string.Empty) p.No_Max = "9999";
                    if (p.No_Min == string.Empty) p.No_Max = "0";
                    if (p.Rpm_Max == string.Empty) p.Rpm_Max = "9999";
                    if (p.Rpm_Min == string.Empty) p.Rpm_Min = "0";
                });
            }

            if (m_ListAxialFanEfficiency != null && m_ListAxialFanEfficiency.Count > 0)
            {
                m_ListAxialFanEfficiency.ForEach(p =>
                {
                    if (p.No_Max == string.Empty) p.No_Max = "9999";
                    if (p.No_Min == string.Empty) p.No_Max = "0";
                });
            }


        }

        public void InitForm(FanDataModel _FanDataModel, List<FanDataModel> _ListFan)
        {
            m_Fan = _FanDataModel;
            m_ListFan = _ListFan;
            if (FuncStr.NullToStr(_FanDataModel.VentStyle) == "轴流")
            {
                layouLX.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layouZL.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
            }
            else
            {
                layouZL.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layouLX.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
            }


            LabModelNum.Text = _FanDataModel.FanModelNum;
            LabCCFC.Text = _FanDataModel.FanModelCCCF;
            LabAir.Text = FuncStr.NullToStr(_FanDataModel.AirVolume);
            LabPa.Text = FuncStr.NullToStr(_FanDataModel.WindResis);
            LabMotorPower.Text = _FanDataModel.FanModelMotorPower;
            LabNoise.Text = _FanDataModel.FanModelNoise;
            LabFanSpeed.Text = _FanDataModel.FanModelFanSpeed;
            LabPower.Text = _FanDataModel.FanModelPower;

            LabLength.Text = _FanDataModel.FanModelLength;
            LabWidth.Text = _FanDataModel.FanModelWidth;
            LabHeight.Text = _FanDataModel.FanModelHeight;
            LabWeight.Text = _FanDataModel.FanModelWeight;


            LabZLLength.Text = _FanDataModel.FanModelLength;
            LabZLWeight.Text = _FanDataModel.FanModelWeight;
            LabDIA.Text = _FanDataModel.FanModelDIA;

            RGroupFanControl.EditValue = _FanDataModel.Control;
            CheckFrequency.EditValue = _FanDataModel.IsFre;
            RGroupPower.EditValue = _FanDataModel.PowerType;

            if (FuncStr.NullToStr(_FanDataModel.Scenario).Contains("消防") || FuncStr.NullToStr(_FanDataModel.Scenario).Contains("事故"))
            {
                RGroupPower.Enabled = false;
            }
            else
            {
                RGroupPower.Enabled = true;
            }


            if (FuncStr.NullToStr(_FanDataModel.FanModelInputMotorPower) != string.Empty)
            {
                if (FuncStr.NullToStr(_FanDataModel.FanModelInputMotorPower).Contains("/"))
                {
                    var _Split = _FanDataModel.FanModelInputMotorPower.Split('/');
                    if (_Split.Count() == 2)
                    {
                        TxtSingle.Text = FuncStr.NullToStr(_Split[0]);
                        TxtDouble.Text = FuncStr.NullToStr(_Split[1]);
                    }
                }
                else
                {
                    TxtSingle.Text = FuncStr.NullToStr(_FanDataModel.FanModelInputMotorPower);
                }
            }
            else
            {
                TxtSingle.Text = string.Empty;
                TxtDouble.Text = string.Empty;
            }

            if (_FanDataModel.IsInputMotorPower)
            {
                RidInput.Checked = true;
            }
            else
            {
                RadCalc.Checked = true;
            }


   

            RadCalc_CheckedChanged(null, null);

            //if (_FanDataModel.Scenario == "平时送风" || _FanDataModel.Scenario == "平时排风")
            //{
            //    var _List = _ListFan.FindAll(p => p.PID == _FanDataModel.ID);
            //    if (_List != null && _List.Count > 0)
            //    {
            //        RGroupFanControl.EditValue = "双速";

            //        RGroupFanControl.Enabled = false;
            //    }
            //    else
            //    {
            //        RGroupFanControl.Enabled = true;
            //    }

            //}



            if (_FanDataModel.FanSelectionStateInfo != null)
            {

                this.TxtPrompt.AppearanceItemCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
                this.TxtPrompt.AppearanceItemCaption.Options.UseForeColor = true;
                TxtPrompt1.Text = " ";
                if (_FanDataModel.FanSelectionStateInfo.fanSelectionState == FanSelectionState.HighUnsafe)
                {
                    TxtPrompt.Text = " 高速挡输入的总阻力偏小.";
                }
                else if (_FanDataModel.FanSelectionStateInfo.fanSelectionState == FanSelectionState.LowUnsafe)
                {
                    TxtPrompt.Text = " 低速挡输入的总阻力偏小.";
                }
                else if (_FanDataModel.FanSelectionStateInfo.fanSelectionState == FanSelectionState.HighAndLowBothUnsafe)
                {
                    TxtPrompt.Text = " 高、低速档输入的总阻力都偏小.";
                }
                else if (_FanDataModel.FanSelectionStateInfo.fanSelectionState == FanSelectionState.LowNotFound && _FanDataModel.FanSelectionStateInfo.RecommendPointInLow.Count == 2)
                {
                    TxtPrompt.Text = string.Format(" 低速挡的工况点与高速挡差异过大,低速档风量的推荐值在{0}m³/h左右, ",
                    _FanDataModel.FanSelectionStateInfo.RecommendPointInLow[0]);
                    TxtPrompt1.Text = string.Format(" 总阻力的推荐值小于{0}Pa.", _FanDataModel.FanSelectionStateInfo.RecommendPointInLow[1]);
                    this.TxtPrompt.AppearanceItemCaption.ForeColor = Color.Red;
                    this.TxtPrompt1.AppearanceItemCaption.ForeColor = Color.Red;
                }
                else
                {
                    TxtPrompt.Text = " ";
                    TxtPrompt1.Text = " ";
                    this.TxtPrompt.AppearanceItemCaption.ForeColor = Color.Transparent;

                }

            }



       
            RGroupFanControl_SelectedIndexChanged(null, null);


            _FanDataModel.AirVolumeDescribe = LabAir.Text;

            _FanDataModel.WindResisDescribe = LabPa.Text;

            if (_FanDataModel.IsInputMotorPower)
            {
                _FanDataModel.FanModelPowerDescribe = _FanDataModel.FanModelInputMotorPower;
            }
            else
            {
                _FanDataModel.FanModelPowerDescribe = LabMotorPower.Text;
            }



        }

        private void InitSonFan()
        {

            if (m_Fan == null || m_ListFan == null || m_ListFan.Count == 0 || FuncStr.NullToStr(RGroupFanControl.EditValue) == "单速")
            {
                LabAir.Text = FuncStr.NullToStr(m_Fan.AirVolume);
                LabPa.Text = FuncStr.NullToStr(m_Fan.WindResis);
                LabMotorPower.Text = m_Fan.FanModelMotorPower;
                return;
            }

            FanDataModel _FanSon = null;

            FanDataModel _FanMain = new FanDataModel();

            if (m_Fan.PID == "0")
            {
                _FanSon = m_ListFan.Find(p => p.PID == m_Fan.ID);
            }
            else
            {
                var _Json = FuncJson.Serialize(m_Fan);
                _FanSon = FuncJson.Deserialize<FanDataModel>(_Json);
                m_Fan = m_ListFan.Find(p => p.ID == _FanSon.PID);
                _FanMain = m_ListFan.Find(p => p.ID == _FanSon.PID);
            }





            if (_FanSon == null || m_Fan == null)
            {
                LabAir.Text = FuncStr.NullToStr(m_Fan.AirVolume);
                LabPa.Text = FuncStr.NullToStr(m_Fan.WindResis);
                return;
            }

            LabAir.Text = m_Fan.AirVolume + "/" + _FanSon.AirVolume;



            LabPa.Text = m_Fan.WindResis + "/" + _FanSon.WindResis;




            double _SafetyFactor = 0;

            double _Flow = Math.Round(FuncStr.NullToDouble(_FanSon.AirVolume) / 3600, 5);

            var _SpecificSpeed = 5.54 * FuncStr.NullToDouble(m_Fan.FanModelFanSpeed) * Math.Pow(_Flow, 0.5) / Math.Pow(_FanSon.WindResis, 0.75);

            var _FanEfficiency = m_ListFanEfficiency.Find(p => FuncStr.NullToInt(p.No_Min) <= FuncStr.NullToInt(m_Fan.FanModelNum) && FuncStr.NullToInt(p.No_Max) >= FuncStr.NullToInt(m_Fan.FanModelNum)
                 && FuncStr.NullToInt(p.Rpm_Min) <= FuncStr.NullToInt(_SpecificSpeed)
                  && FuncStr.NullToInt(p.Rpm_Max) >= FuncStr.NullToInt(_SpecificSpeed) && m_Fan.VentLev == p.FanEfficiencyLevel);
            if (_FanEfficiency == null) { LabMotorPower.Text = m_Fan.FanModelMotorPower + "/" + "0"; return; }
            var _FanInternalEfficiency = FuncStr.NullToInt(_FanEfficiency.FanInternalEfficiency * 0.9);
            var _ShaftPower = _FanSon.AirVolume * _FanSon.WindResis / _FanInternalEfficiency * 100 / 0.855 / 1000 / 3600;
            if (_ShaftPower <= 0.5)
            {
                _SafetyFactor = 1.5;
            }
            else if (_ShaftPower <= 1)
            {
                _SafetyFactor = 1.4;
            }
            else if (_ShaftPower <= 2)
            {
                _SafetyFactor = 1.3;
            }
            else if (_ShaftPower <= 5)
            {
                _SafetyFactor = 1.2;
            }
            else if (_ShaftPower <= 20)
            {
                _SafetyFactor = 1.15;
            }
            else
            {
                _SafetyFactor = 1.1;
            }

            var _ListMotor = GetListMotorPowerBySon(m_Fan);
            var _MotorEfficiency = PubVar.g_ListMotorEfficiency.Find(p => p.Key == m_Fan.VentConnect);
            var _Tmp = _ShaftPower / 0.85;
            var _ListMotorPower = _ListMotor.FindAll(p => FuncStr.NullToDouble(p.Power) >= _Tmp && p.MotorEfficiencyLevel == m_Fan.EleLev && p.Rpm == FuncStr.NullToStr(m_Fan.MotorTempo));
            var _MotorPower = _ListMotorPower.OrderBy(p => FuncStr.NullToDouble(p.Power)).First();

            var _EstimatedMotorPower = _ShaftPower / FuncStr.NullToDouble(_MotorPower.MotorEfficiency) / FuncStr.NullToDouble(_MotorEfficiency.Value) * _SafetyFactor * 100;
            _ListMotorPower = _ListMotor.FindAll(p => FuncStr.NullToDouble(p.Power) >= _EstimatedMotorPower && p.MotorEfficiencyLevel == m_Fan.EleLev && p.Rpm == FuncStr.NullToStr(m_Fan.MotorTempo));
            _MotorPower = _ListMotorPower.OrderBy(p => FuncStr.NullToDouble(p.Power)).First();

            if (_MotorPower != null)
            {
                LabMotorPower.Text = m_Fan.FanModelMotorPower + "/" + _MotorPower.Power;

            }

            _FanMain.AirVolumeDescribe = LabAir.Text;

            _FanMain.WindResisDescribe = LabPa.Text;

            _FanMain.FanModelPowerDescribe = LabMotorPower.Text;
        }

        public void CalcFanEfficiency(FanDataModel _FanDataModel)
        {
            //比转速	等于5.54*风机转速（查询）*比转数下的流量的0.5次方 /全压输入值的0.75次方		
            //轴功率    风量乘以全压除以风机内效率除以传动效率（0.855）除以1000					
            //电机容量安全系数 =IF(AZ6<=0.5,1.5, IF(AZ6<=1,1.4,IF(AZ6<=2,1.3,IF(AZ6<=5,1.2,IF(AZ6<=20,1.15,1.1)))))
            if (_FanDataModel.VentStyle == "轴流")
            {
                double _SafetyFactor = 0;
                double _Flow = Math.Round(FuncStr.NullToDouble(_FanDataModel.AirVolume) / 3600, 5);
                var _SpecificSpeed = 5.54 * FuncStr.NullToDouble(_FanDataModel.FanModelFanSpeed) * Math.Pow(_Flow, 0.5) / Math.Pow(_FanDataModel.WindResis, 0.75);
                var _NoSplit = _FanDataModel.FanModelName.Split('-');
                double _No = 0;
                if (_NoSplit.Count() == 3)
                {
                    _No = FuncStr.NullToDouble(_NoSplit[2]);
                }
                var _AxialFanEfficiency = m_ListAxialFanEfficiency.Find(p => FuncStr.NullToInt(p.No_Min) <= _No && FuncStr.NullToInt(p.No_Max) >= _No
                   && _FanDataModel.VentLev == p.FanEfficiencyLevel);
                if (_AxialFanEfficiency == null) { return; }
                var  _FanEfficiency = FuncStr.NullToInt(_AxialFanEfficiency.FanEfficiency * 0.9);
                var _ShaftPower = _FanDataModel.AirVolume * _FanDataModel.WindResis / _FanEfficiency * 100 / 0.855 / 1000 / 3600;

                if (_ShaftPower <= 0.5)
                {
                    _SafetyFactor = 1.5;
                }
                else if (_ShaftPower <= 1)
                {
                    _SafetyFactor = 1.4;
                }
                else if (_ShaftPower <= 2)
                {
                    _SafetyFactor = 1.3;
                }
                else if (_ShaftPower <= 5)
                {
                    _SafetyFactor = 1.2;
                }
                else if (_ShaftPower <= 20)
                {
                    _SafetyFactor = 1.15;
                }
                else
                {
                    _SafetyFactor = 1.1;
                }


                var _ListMotor = GetListMotorPower(_FanDataModel);
                var _MotorEfficiency = PubVar.g_ListMotorEfficiency.Find(p => p.Key == _FanDataModel.VentConnect);
                var _Tmp = _ShaftPower / 0.85;
                var _ListMotorPower = _ListMotor.FindAll(p => FuncStr.NullToDouble(p.Power) >= _Tmp && p.MotorEfficiencyLevel == _FanDataModel.EleLev && p.Rpm == FuncStr.NullToStr(_FanDataModel.MotorTempo));
                var _MotorPower = _ListMotorPower.OrderBy(p => FuncStr.NullToDouble(p.Power)).First();

                var _EstimatedMotorPower = _ShaftPower / FuncStr.NullToDouble(_MotorPower.MotorEfficiency) / FuncStr.NullToDouble(_MotorEfficiency.Value) * _SafetyFactor * 100;
                _ListMotorPower = _ListMotor.FindAll(p => FuncStr.NullToDouble(p.Power) >= _EstimatedMotorPower && p.MotorEfficiencyLevel == _FanDataModel.EleLev && p.Rpm == FuncStr.NullToStr(_FanDataModel.MotorTempo));
                _MotorPower = _ListMotorPower.OrderBy(p => FuncStr.NullToDouble(p.Power)).First();

                if (_MotorPower != null)
                {
                    LabMotorPower.Text = _MotorPower.Power;
                    _FanDataModel.FanModelMotorPower = _MotorPower.Power;
                    _FanDataModel.FanInternalEfficiency = FuncStr.NullToStr(_AxialFanEfficiency.FanEfficiency);
                }

                GetPower(_FanDataModel, _AxialFanEfficiency);

            }
            else
            {
                double _SafetyFactor = 0;
                double _Flow = Math.Round(FuncStr.NullToDouble(_FanDataModel.AirVolume) / 3600, 5);
                var _SpecificSpeed = 5.54 * FuncStr.NullToDouble(_FanDataModel.FanModelFanSpeed) * Math.Pow(_Flow, 0.5) / Math.Pow(_FanDataModel.WindResis, 0.75);

                var _FanEfficiency = m_ListFanEfficiency.Find(p => FuncStr.NullToInt(p.No_Min) <= FuncStr.NullToInt(_FanDataModel.FanModelNum) && FuncStr.NullToInt(p.No_Max) >= FuncStr.NullToInt(_FanDataModel.FanModelNum)
                     && FuncStr.NullToInt(p.Rpm_Min) <= FuncStr.NullToInt(_SpecificSpeed)
                      && FuncStr.NullToInt(p.Rpm_Max) >= FuncStr.NullToInt(_SpecificSpeed) && _FanDataModel.VentLev == p.FanEfficiencyLevel);
                if (_FanEfficiency == null) { return; }
                var _FanInternalEfficiency = FuncStr.NullToInt(_FanEfficiency.FanInternalEfficiency * 0.9);
                var _ShaftPower = _FanDataModel.AirVolume * _FanDataModel.WindResis / _FanInternalEfficiency * 100 / 0.855 / 1000 / 3600;
                if (_ShaftPower <= 0.5)
                {
                    _SafetyFactor = 1.5;
                }
                else if (_ShaftPower <= 1)
                {
                    _SafetyFactor = 1.4;
                }
                else if (_ShaftPower <= 2)
                {
                    _SafetyFactor = 1.3;
                }
                else if (_ShaftPower <= 5)
                {
                    _SafetyFactor = 1.2;
                }
                else if (_ShaftPower <= 20)
                {
                    _SafetyFactor = 1.15;
                }
                else
                {
                    _SafetyFactor = 1.1;
                }
                var _ListMotor = GetListMotorPower(_FanDataModel);
                var _MotorEfficiency = PubVar.g_ListMotorEfficiency.Find(p => p.Key == _FanDataModel.VentConnect);
                var _Tmp = _ShaftPower / 0.85;
                var _ListMotorPower = _ListMotor.FindAll(p => FuncStr.NullToDouble(p.Power) >= _Tmp && p.MotorEfficiencyLevel == _FanDataModel.EleLev && p.Rpm == FuncStr.NullToStr(_FanDataModel.MotorTempo));
                var _MotorPower = _ListMotorPower.OrderBy(p => FuncStr.NullToDouble(p.Power)).First();

                var _EstimatedMotorPower = _ShaftPower / FuncStr.NullToDouble(_MotorPower.MotorEfficiency) * 100 / FuncStr.NullToDouble(_MotorEfficiency.Value) * _SafetyFactor;
                _ListMotorPower = _ListMotor.FindAll(p => FuncStr.NullToDouble(p.Power) >= _EstimatedMotorPower && p.MotorEfficiencyLevel == _FanDataModel.EleLev && p.Rpm == FuncStr.NullToStr(_FanDataModel.MotorTempo));
                _MotorPower = _ListMotorPower.OrderBy(p => FuncStr.NullToDouble(p.Power)).First();

                if (_MotorPower != null)
                {
                    LabMotorPower.Text = _MotorPower.Power;
                    _FanDataModel.FanModelMotorPower = _MotorPower.Power;
                    _FanDataModel.FanInternalEfficiency = FuncStr.NullToStr(_FanEfficiency.FanInternalEfficiency);
                }

                GetPower(_FanDataModel, _FanEfficiency);

            }


        }

        private List<MotorPower> GetListMotorPowerBySon(FanDataModel _FanMian)
        {
            if (_FanMian.FanModelName == string.Empty) { return m_ListMotorPowerDouble; }
            if (_FanMian.VentStyle == "轴流")
            {
                if (_FanMian.FanModelName.Contains("II"))
                {
                    return m_ListMotorPowerDouble.FindAll(p => p.Axial2LowSpeed == "1");
                }
                if (_FanMian.FanModelName.Contains("IV"))
                {
                    return m_ListMotorPowerDouble.FindAll(p => p.Axial4LowSpeed == "1");
                }
            }
            else
            {
                if (_FanMian.FanModelName.Contains("II"))
                {
                    return m_ListMotorPowerDouble.FindAll(p => p.Centrifuge2LowSpeed == "1");
                }
                if (_FanMian.FanModelName.Contains("IV"))
                {
                    return m_ListMotorPowerDouble.FindAll(p => p.Centrifuge4LowSpeed == "1");
                }
            }
            return m_ListMotorPowerDouble;
        }





        private List<MotorPower> GetListMotorPower(FanDataModel _FanDataModel)
        {
            if (_FanDataModel.Control == "双速")
            {
                if (_FanDataModel.FanModelName == string.Empty) { return m_ListMotorPowerDouble; }

                if (_FanDataModel.VentStyle == "轴流")
                {
                    if (_FanDataModel.PID == "0")
                    {
                        if (_FanDataModel.FanModelName.Contains("II"))
                        {
                            return m_ListMotorPowerDouble.FindAll(p => p.Axial2HighSpeed == "1");
                        }
                        if (_FanDataModel.FanModelName.Contains("IV"))
                        {
                            return m_ListMotorPowerDouble.FindAll(p => p.Axial4HighSpeed == "1");
                        }
                    }
                    else
                    {
                        if (_FanDataModel.FanModelName.Contains("II"))
                        {
                            return m_ListMotorPowerDouble.FindAll(p => p.Axial2LowSpeed == "1");
                        }
                        if (_FanDataModel.FanModelName.Contains("IV"))
                        {
                            return m_ListMotorPowerDouble.FindAll(p => p.Axial4LowSpeed == "1");
                        }
                    }
                }
                else
                {
                    if (_FanDataModel.PID == "0")
                    {
                        if (_FanDataModel.FanModelName.Contains("II"))
                        {
                            return m_ListMotorPowerDouble.FindAll(p => p.Centrifuge2HighSpeed == "1");
                        }
                        if (_FanDataModel.FanModelName.Contains("IV"))
                        {
                            return m_ListMotorPowerDouble.FindAll(p => p.Centrifuge4HighSpeed == "1");
                        }
                    }
                    else
                    {
                        if (_FanDataModel.FanModelName.Contains("II"))
                        {
                            return m_ListMotorPowerDouble.FindAll(p => p.Centrifuge2LowSpeed == "1");
                        }
                        if (_FanDataModel.FanModelName.Contains("IV"))
                        {
                            return m_ListMotorPowerDouble.FindAll(p => p.Centrifuge4LowSpeed == "1");
                        }
                    }
                }






                return m_ListMotorPowerDouble;
            }
            else
            {
                return m_ListMotorPower;
            }

        }

        private void GetPower(FanDataModel _FanDataModel, AxialFanEfficiency _AxialFanEfficiency)
        {
            if (_FanDataModel.Scenario == "消防排烟" || _FanDataModel.Scenario == "消防补风" || _FanDataModel.Scenario == "" || _FanDataModel.Scenario == "消防加压送风" || _FanDataModel.Scenario == "厨房排油烟" ||
                _FanDataModel.Scenario == "事故排风" || _FanDataModel.Scenario == "事故补风")
            {
                _FanDataModel.FanModelPower = "-";
                LabPower.Text = _FanDataModel.FanModelPower;
                return;
            }
            if (_FanDataModel.Scenario == "厨房排油烟补风")
            {
                var _FanModelPower = _FanDataModel.WindResis / (3600 * _AxialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;
                _FanDataModel.FanModelPower = FuncStr.NullToDouble(_FanModelPower).ToString("0.##");

                LabPower.Text = _FanDataModel.FanModelPower;
                return;
            }
            if (_FanDataModel.Scenario == "平时送风" || _FanDataModel.Scenario == "平时排风")
            {
                //有低速、也可以没有
                var _FanModelPower = _FanDataModel.WindResis / (3600 * _AxialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;

                _FanDataModel.FanModelPower = FuncStr.NullToDouble(_FanModelPower).ToString("0.##");

                LabPower.Text = _FanDataModel.FanModelPower;

                var _SonFan = m_ListFan.Find(p => p.PID == _FanDataModel.ID);

                if (_SonFan != null && FuncStr.NullToStr(RGroupFanControl.EditValue) != "单速")
                {
                    var _SonPower = _SonFan.WindResis / (3600 * _AxialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;

                    _FanDataModel.FanModelPower = FuncStr.NullToDouble(_FanModelPower).ToString("0.##") + "/" + FuncStr.NullToDouble(_SonPower).ToString("0.##");

                    LabPower.Text = _FanDataModel.FanModelPower;
                }
                return;
            }
            if (_FanDataModel.Scenario == "消防排烟兼平时排风" || _FanDataModel.Scenario == "消防补风兼平时送风")
            {

                _FanDataModel.FanModelPower = "-";

                LabPower.Text = _FanDataModel.FanModelPower;

                var _SonFan = m_ListFan.Find(p => p.PID == _FanDataModel.ID);

                if (_SonFan != null &&  FuncStr.NullToStr(RGroupFanControl.EditValue) != "单速")
                {
                    var _SonPower = _SonFan.WindResis / (3600 * _AxialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;

                    _FanDataModel.FanModelPower = "-" + "/" + FuncStr.NullToDouble(_SonPower).ToString("0.##");

                    LabPower.Text = _FanDataModel.FanModelPower;
                }
                return;
            }
            if (_FanDataModel.Scenario == "平时送风兼事故补风" || _FanDataModel.Scenario == "平时排风兼事故补风")
            {
                var _FanModelPower = _FanDataModel.WindResis / (3600 * _AxialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;

                _FanDataModel.FanModelPower = FuncStr.NullToDouble(_FanDataModel.FanModelPower).ToString("0.##");

                var _SonFan = m_ListFan.Find(p => p.PID == _FanDataModel.ID);

                if (_SonFan != null && FuncStr.NullToStr(RGroupFanControl.EditValue) != "单速")
                {
                    var _SonPower = _SonFan.WindResis / (3600 * _AxialFanEfficiency.FanEfficiency * 0.855 * 0.98) * 100;

                    if (_FanDataModel.Use == "平时排风")
                    {
                        //_FanDataModel.FanModelPower = FuncStr.NullToDouble(_FanModelPower).ToString("0.##") + "/" + FuncStr.NullToDouble(_SonPower).ToString("0.##");
                        _FanDataModel.FanModelPower = "-/" + FuncStr.NullToDouble(_FanModelPower).ToString("0.##");

                        LabPower.Text = _FanDataModel.FanModelPower;
                    }
                    else
                    {
                        //_FanDataModel.FanModelPower = FuncStr.NullToDouble(_SonPower).ToString("0.##") + "/" + FuncStr.NullToDouble(_FanModelPower).ToString("0.##");
                        _FanDataModel.FanModelPower = "-/" + FuncStr.NullToDouble(_SonPower).ToString("0.##");
                        LabPower.Text = _FanDataModel.FanModelPower;
                    }
                }
                return;

            }


        }

        private void GetPower(FanDataModel _FanDataModel, FanEfficiency _FanEfficiency)
        {
            if (_FanDataModel.Scenario == "消防排烟" || _FanDataModel.Scenario == "消防补风" || _FanDataModel.Scenario == "" || _FanDataModel.Scenario == "消防加压送风" || _FanDataModel.Scenario == "厨房排油烟" ||
                  _FanDataModel.Scenario == "事故排风" || _FanDataModel.Scenario == "事故补风")
            {
                _FanDataModel.FanModelPower = "-";
                LabPower.Text = _FanDataModel.FanModelPower;
                return;
            }
            if (_FanDataModel.Scenario == "厨房排油烟补风")
            {
                var _FanModelPower = _FanDataModel.WindResis / (3600 * _FanEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;
                _FanDataModel.FanModelPower = FuncStr.NullToDouble(_FanModelPower).ToString("0.##");


                LabPower.Text = _FanDataModel.FanModelPower;
                return;
            }
            if (_FanDataModel.Scenario == "平时送风" || _FanDataModel.Scenario == "平时排风")
            {
                //有低速、也可以没有
                var _FanModelPower = _FanDataModel.WindResis / (3600 * _FanEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;

                _FanDataModel.FanModelPower = FuncStr.NullToDouble(_FanModelPower).ToString("0.##");

                LabPower.Text = _FanDataModel.FanModelPower;

                var _SonFan = m_ListFan.Find(p => p.PID == _FanDataModel.ID);

                if (_SonFan != null && FuncStr.NullToStr(RGroupFanControl.EditValue) != "单速")
                {

                    double _Flow = Math.Round(FuncStr.NullToDouble(_SonFan.AirVolume) / 3600, 5);
                    var _SpecificSpeed = 5.54 * FuncStr.NullToDouble(_FanDataModel.FanModelFanSpeed) * Math.Pow(_Flow, 0.5) / Math.Pow(_SonFan.WindResis, 0.75);

                    var _SonEfficiency = m_ListFanEfficiency.Find(p => FuncStr.NullToInt(p.No_Min) < FuncStr.NullToInt(_FanDataModel.FanModelNum) && FuncStr.NullToInt(p.No_Max) > FuncStr.NullToInt(_FanDataModel.FanModelNum)
                         && FuncStr.NullToInt(p.Rpm_Min) < FuncStr.NullToInt(_SpecificSpeed)
                          && FuncStr.NullToInt(p.Rpm_Max) > FuncStr.NullToInt(_SpecificSpeed) && _FanDataModel.VentLev == p.FanEfficiencyLevel);
                    if (_SonEfficiency == null) { return; }


                    var _SonPower = _SonFan.WindResis / (3600 * _SonEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;

                    _FanDataModel.FanModelPower = FuncStr.NullToDouble(_FanModelPower).ToString("0.##") + "/" + FuncStr.NullToDouble(_SonPower).ToString("0.##");

                    LabPower.Text = _FanDataModel.FanModelPower;
                }
                return;
            }
            if (_FanDataModel.Scenario == "消防排烟兼平时排风" || _FanDataModel.Scenario == "消防补风兼平时送风")
            {
                _FanDataModel.FanModelPower = "-";

                LabPower.Text = _FanDataModel.FanModelPower;

                var _SonFan = m_ListFan.Find(p => p.PID == _FanDataModel.ID);

                if (_SonFan != null && FuncStr.NullToStr(RGroupFanControl.EditValue) != "单速")
                {
                    double _Flow = Math.Round(FuncStr.NullToDouble(_SonFan.AirVolume) / 3600, 5);
                    var _SpecificSpeed = 5.54 * FuncStr.NullToDouble(_FanDataModel.FanModelFanSpeed) * Math.Pow(_Flow, 0.5) / Math.Pow(_SonFan.WindResis, 0.75);

                    var _SonEfficiency = m_ListFanEfficiency.Find(p => FuncStr.NullToInt(p.No_Min) <= FuncStr.NullToInt(_FanDataModel.FanModelNum) && FuncStr.NullToInt(p.No_Max) >= FuncStr.NullToInt(_FanDataModel.FanModelNum)
                         && FuncStr.NullToInt(p.Rpm_Min) <= FuncStr.NullToInt(_SpecificSpeed)
                          && FuncStr.NullToInt(p.Rpm_Max) >= FuncStr.NullToInt(_SpecificSpeed) && _FanDataModel.VentLev == p.FanEfficiencyLevel);
                    if (_SonEfficiency == null) { return; }

                    var _SonPower = _SonFan.WindResis / (3600 * _SonEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;

                    _FanDataModel.FanModelPower = "-" + "/" + FuncStr.NullToDouble(_SonPower).ToString("0.##");

                    LabPower.Text = _FanDataModel.FanModelPower;
                }
                return;
            }
            if (_FanDataModel.Scenario == "平时送风兼事故补风" || _FanDataModel.Scenario == "平时排风兼事故排风")
            {
                var _FanModelPower = _FanDataModel.WindResis / (3600 * _FanEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;

                _FanDataModel.FanModelPower = FuncStr.NullToDouble(_FanModelPower).ToString("0.##");

                var _SonFan = m_ListFan.Find(p => p.PID == _FanDataModel.ID);

                if (_SonFan != null && FuncStr.NullToStr(RGroupFanControl.EditValue) != "单速")
                {
                    double _Flow = Math.Round(FuncStr.NullToDouble(_SonFan.AirVolume) / 3600, 5);
                    var _SpecificSpeed = 5.54 * FuncStr.NullToDouble(_FanDataModel.FanModelFanSpeed) * Math.Pow(_Flow, 0.5) / Math.Pow(_SonFan.WindResis, 0.75);

                    var _SonEfficiency = m_ListFanEfficiency.Find(p => FuncStr.NullToInt(p.No_Min) < FuncStr.NullToInt(_FanDataModel.FanModelNum) && FuncStr.NullToInt(p.No_Max) > FuncStr.NullToInt(_FanDataModel.FanModelNum)
                         && FuncStr.NullToInt(p.Rpm_Min) < FuncStr.NullToInt(_SpecificSpeed)
                          && FuncStr.NullToInt(p.Rpm_Max) > FuncStr.NullToInt(_SpecificSpeed) && _FanDataModel.VentLev == p.FanEfficiencyLevel);
                    if (_SonEfficiency == null) { _FanDataModel.FanModelPower = string.Empty; LabPower.Text = "-"; return; }

                    var _SonPower = _SonFan.WindResis / (3600 * _SonEfficiency.FanInternalEfficiency * 0.855 * 0.98) * 100;

                    if (_FanDataModel.Use == "平时排风")
                    {
                        _FanDataModel.FanModelPower = "-/" + FuncStr.NullToDouble(_FanModelPower).ToString("0.##");

                        LabPower.Text = _FanDataModel.FanModelPower;
                    }
                    else
                    {
                        _FanDataModel.FanModelPower = "-/" + FuncStr.NullToDouble(_SonPower).ToString("0.##");

                        LabPower.Text = _FanDataModel.FanModelPower;
                    }

                }
                return;

            }

        }

        private void BtnOK_Click(object sender, EventArgs e)
        {

            if (RidInput.Checked)
            {
                m_Fan.FanModelPowerDescribe = m_Fan.FanModelInputMotorPower;
            }
            else
            {
                m_Fan.FanModelPowerDescribe = LabMotorPower.Text;
            }

            m_Fan.FanModelNum = FuncStr.NullToStr(LabModelNum.Text);

            m_Fan.FanModelCCCF = FuncStr.NullToStr(LabCCFC.Text);

            //m_Fan.AirVolume = FuncStr.NullToInt(LabAir.Text);

            //m_Fan.WindResis = FuncStr.NullToInt(LabPa.Text);

            m_Fan.FanModelMotorPower = FuncStr.NullToStr(LabMotorPower.Text);
            m_Fan.FanModelNoise = FuncStr.NullToStr(LabNoise.Text);
            m_Fan.FanModelFanSpeed = FuncStr.NullToStr(LabFanSpeed.Text);


            m_Fan.FanModelPower = FuncStr.NullToStr(LabPower.Text);

            m_Fan.FanModelLength = FuncStr.NullToStr(LabLength.Text);
            m_Fan.FanModelWidth = FuncStr.NullToStr(LabWidth.Text);
            m_Fan.FanModelHeight = FuncStr.NullToStr(LabHeight.Text);
            m_Fan.FanModelWeight = FuncStr.NullToStr(LabWeight.Text);


            m_Fan.Control = FuncStr.NullToStr(RGroupFanControl.EditValue);

            m_Fan.IsFre = CheckFrequency.Checked;
            m_Fan.PowerType = FuncStr.NullToStr(RGroupPower.EditValue);


        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {

        }

        private void RGroupFanControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(RGroupFanControl.EditValue) == "单速")
            {
                CheckFrequency.Enabled = true;
                layoutTmp.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                layoutDouble.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
            }
            else
            {
                CheckFrequency.Checked = false;
                CheckFrequency.Enabled = false;
                layoutTmp.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                layoutDouble.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
            }

            var _SonFan = m_ListFan.Find(p => p.PID == m_Fan.ID);
            if(_SonFan != null)
            {
                _SonFan.Control = FuncStr.NullToStr(RGroupFanControl.EditValue);
            }


          
            CalcFanEfficiency(m_Fan);
            InitSonFan();
        }


        public string ReadTxt(string _Path)
        {
            try
            {
                using (StreamReader _StreamReader = File.OpenText(_Path))
                {
                    return _StreamReader.ReadToEnd();
                }
            }
            catch
            {
                XtraMessageBox.Show("数据文件读取时发生错误！");
                return string.Empty;

            }
        }

        private void RadCalc_CheckedChanged(object sender, EventArgs e)
        {
            if (RadCalc.Checked)
            {
                TxtSingle.Enabled = false;
                TxtDouble.Enabled = false;
                labTmp.Enabled = false;
                m_Fan.IsInputMotorPower = false;
            }

        }

        private void RidInput_CheckedChanged(object sender, EventArgs e)
        {
            if (RidInput.Checked)
            {
                TxtSingle.Enabled = true;
                TxtDouble.Enabled = true;
                labTmp.Enabled = true;
                m_Fan.IsInputMotorPower = true;
            }
        }

        private void TxtSingle_EditValueChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(TxtSingle.Text) != string.Empty)
                m_Fan.FanModelInputMotorPower = TxtSingle.Text;
            if (FuncStr.NullToStr(RGroupFanControl.EditValue) == "双速" && FuncStr.NullToStr(TxtDouble.Text) != string.Empty)
            {
                m_Fan.FanModelInputMotorPower = TxtSingle.Text + "/" + TxtDouble.Text;
            }

        }

        private void TxtDouble_EditValueChanged(object sender, EventArgs e)
        {
            if (FuncStr.NullToStr(TxtSingle.Text) != string.Empty)
                m_Fan.FanModelInputMotorPower = TxtSingle.Text;
            if (FuncStr.NullToStr(RGroupFanControl.EditValue) == "双速" && FuncStr.NullToStr(TxtDouble.Text) != string.Empty)
            {
                m_Fan.FanModelInputMotorPower = TxtSingle.Text + "/" + TxtDouble.Text;
            }
        }
    }
}
