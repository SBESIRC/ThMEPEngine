using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Model;
using ThMEPHVAC.Algorithm;
using TianHua.FanSelection.Function;
using TianHua.Hvac.UI.Command;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.Hvac.UI
{
    public partial class fmFpm : Form
    {
        public bool isSelectFan;// 选择风机还是选择起点
        public bool isReadAllFans = false;
        public bool isSelectAllFans = false;
        public PortParam portParam;
        public Point3d RoomStartPoint;
        public Point3d notRoomStartPoint;
        public Dictionary<string, FanParam> fans;
        public Dictionary<string, ThDbModelFan> selectFansDic;
        public DBObjectCollection centerlines;                 // 已经转到原点附近
        public DBObjectCollection connNotRoomLines;            // 已经转到原点附近
        // 风机外包框到文字参数的映射( 选择起点时填充此参数)
        public Dictionary<Polyline, ObjectId> allFansDic;      // 外包框移动到原点附近
        private Dictionary<Polyline, ObjectId> shadowFansDic;  // 用于用户反复选取起始点
        private bool initFlag;
        private bool isExhaust;
        private Point3d srtPoint;
        private Matrix3d ucsMat;
        private ThHvacCmdService cmdService;
        private string preKey;//fan的listBox选项更新时需要延迟一下选择
        private double firstRange = 200;
        public fmFpm(PortParam portParam)
        {
            InitializeComponent();
            initFlag = true;
            isSelectFan = true;
            isExhaust = true;
            cmdService = new ThHvacCmdService();
            fans = new Dictionary<string, FanParam>();
            selectFansDic = new Dictionary<string, ThDbModelFan>();
            bypassEnable();
            scenarioCombox.SelectedItem = "平时排风";
            comboScale.SelectedItem = "1:100";
            allFansDic = new Dictionary<Polyline, ObjectId>();
            SetPortSize();
            SetPortSpeed();
            if (portParam.param.airVolume > 0)
                FillUIParam(portParam);
        }

        private void FillUIParam(PortParam portParam)
        {
            scenarioCombox.SelectedItem = portParam.param.scenario;
            if (portParam.portInterval < 1)
            {
                radioPortInterval.Checked = true;
                radioIntervalPortCustom.Checked = false;
                textPortInterval.Enabled = false;
            }
            if (portParam.endCompType == EndCompType.None)
                radioButton1.Checked = true;
            else if (portParam.endCompType == EndCompType.VerticalPipe)
                radioButton2.Checked = true;
            else if (portParam.endCompType == EndCompType.RainProofShutter)
                radioButton3.Checked = true;
            else if (portParam.endCompType == EndCompType.DownFlip45)
                radioButton4.Checked = true;

            textAirVolume.Text = portParam.textAirVolume;
            textAirSpeed.Text = portParam.param.airSpeed.ToString();
            textPortNum.Text = portParam.param.portNum.ToString();
            if (portParam.genStyle == GenerationStyle.Auto)
                radioGenStyle3.Checked = true;
            else if (portParam.genStyle == GenerationStyle.GenerationByPort)
                radioGenStyle2.Checked = true;
            else if (portParam.genStyle == GenerationStyle.GenerationWithPortVolume)
                radioGenStyle1.Checked = true;
            if (portParam.verticalPipeEnable)
                radioVerticalPipe.Checked = true;
            else
            {
                radioPortRange.Checked = true;
                comboPortRange.SelectedItem = portParam.param.portRange;
            }
            comboScale.SelectedItem = portParam.param.scale;
            // 默认填充到服务侧参数 风管Size填充到自定义
            checkBoxRoom.Checked = true;
            // portParam.notRoomDuctSize为NULL是无风机的情况
            radioRoomRecommand.Checked = true;
            ThMEPHVACService.GetWidthAndHeight(portParam.param.portSize, out double portW, out double portH);
            textPortWidth.Text = portW.ToString();
            textPortHeight.Text = portH.ToString();
            ThMEPHVACService.GetWidthAndHeight(portParam.param.portSize, out double w, out double h);
            textPortWidth.Text = w.ToString();
            textPortHeight.Text = h.ToString();
            // 默认填充到服务侧参数 风管Size填充到自定义
            checkBoxRoom.Checked = true;
            ThMEPHVACService.GetWidthAndHeight(portParam.param.inDuctSize, out w, out h);
            textRoomWidth.Text = w.ToString();
            textRoomHeight.Text = h.ToString();
            textRoomElevation.Text = portParam.param.elevation.ToString("0.00");
            textPortElevation.Text = portParam.param.portBottomEle.ToString();
        }

        private void btnSelectFan_Click(object sender, EventArgs e)
        {
            isSelectFan = true;
            Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
            var fanIds = cmdService.GetFans();
            if (fanIds.Count == 0)
                return;
            listBox1.Items.Clear();
            foreach (var id in fanIds)
            {
                var fan = new ThDbModelFan(id);
                if (fan.airVolume <= 0)
                    continue;
                var item = fan.Data.Attributes["设备符号"] + "-" + fan.Data.Attributes["楼层-编号"];
                listBox1.Items.Add(item);
                if (!selectFansDic.ContainsKey(item))
                {
                    selectFansDic.Add(item, fan);
                    textAirVolume.Text = selectFansDic[item].airVolume.ToString();
                    scenarioCombox.Text = selectFansDic[item].scenario;
                }
                var param = RecordFanParam(item);
                if (!fans.ContainsKey(item))
                    fans.Add(item, param);
            }
            if (scenarioCombox.Text == "消防排烟兼平时排风")
            {
                radioVerticalPipe.Enabled = true;
                textPortElevation.Enabled = true;
            }
            if (listBox1.Items.Count > 0)
            {
                listBox1.SelectedIndex = 0;
                label1.Text = listBox1.Items.Count.ToString() + "个对象";
            }
            textAirVolume.Enabled = false;
            Focus();
        }
        private void FillDuctSize(double airVolume)
        {
            if (initFlag)
            {
                UpdateDuctSizeList(airVolume);
                initFlag = false;
            }
        }
        private void UpdateDuctSizeList(double airVolume)
        {
            var ductParam = new ThDuctParameter(airVolume, scenarioCombox.Text);
            var Inner = ductParam.DuctSizeInfor.RecommendInnerDuctSize;
            var Outter = ductParam.DuctSizeInfor.RecommendOuterDuctSize;
            listBoxRoomDuctSize.Items.Clear();
            listBoxNotRoomDuctSize.Items.Clear();
            if (ductParam.DuctSizeInfor.DefaultDuctsSizeString == null)
                return;
            foreach (var s in ductParam.DuctSizeInfor.DefaultDuctsSizeString)
            {
                listBoxRoomDuctSize.Items.Add(s);
                listBoxNotRoomDuctSize.Items.Add(s);
            }
            listBoxRoomDuctSize.SelectedItem = isExhaust ? Outter : Inner;
            listBoxNotRoomDuctSize.SelectedItem = isExhaust ? Inner : Outter;
        }
        // fan names
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.Items.Count == 0)
                return;
            var key = (string)listBox1.SelectedItem;
            if (preKey == key)
                return;
            if (String.IsNullOrEmpty(preKey))
                preKey = key;
            // 先保存前一次离开的数据
            var param = RecordFanParam(key);
            if (fans.ContainsKey(preKey))
                fans.Remove(preKey);
            fans.Add(preKey, param);
            // 更新当前风机的数据
            if (fans.ContainsKey(key))
            {
                var fanParam = fans[key];
                scenarioCombox.Text = fanParam.scenario;
                fanParam.airVolume = selectFansDic[key].airVolume;
                textAirVolume.Text = fanParam.airVolume.ToString();
                var speed = ThFanSelectionUtils.GetDefaultAirSpeed(fanParam.scenario);
                textAirSpeed.Text = speed.ToString();
                FillDuctSize(fanParam.airVolume);
                UpdateRoomDuctSize(fanParam);
                UpdateElevationStyle(fanParam);
                UpdateText(fanParam);
                UpdatePortInterval(fanParam);
            }
            preKey = (string)listBox1.SelectedItem;
        }

        private void UpdatePortInterval(FanParam fanParam)
        {
            if (fanParam.portInterval > 0)
            {
                radioIntervalPortCustom.Checked = true;
                textPortInterval.Text = fanParam.portInterval.ToString();
            }
            else
            {
                radioPortInterval.Checked = true;
            }
        }
        private void UpdateText(FanParam fanParam)
        {
            textRoomElevation.Text = fanParam.roomElevation;
            textNotRoomElevation.Text = fanParam.notRoomElevation;
            comboPortRange.Text = fanParam.portRange;
            if (fanParam.portRange.Contains("侧"))
                textPortNum.Text = (fanParam.portNum * 2).ToString();
            else
                textPortNum.Text = fanParam.portNum.ToString();
            textPortWidth.Text = ThMEPHVACService.GetWidth(fanParam.portSize).ToString();
            textPortHeight.Text = ThMEPHVACService.GetHeight(fanParam.portSize).ToString();
        }
        private void UpdateElevationStyle(FanParam fanParam)
        {
            if (fanParam.roomElevationStyle == ElevationAlignStyle.Bottom)
            {
                radioRoomElvAlign1.Checked = true;
            }
            else if (fanParam.roomElevationStyle == ElevationAlignStyle.Center)
            {
                radioRoomElvAlign2.Checked = true;
            }
            else
            {
                radioRoomElvAlign3.Checked = true;
            }
            if (fanParam.notRoomElevationStyle == ElevationAlignStyle.Bottom)
            {
                radioNotRoomElvAlign1.Checked = true;
            }
            else if (fanParam.notRoomElevationStyle == ElevationAlignStyle.Center)
            {
                radioNotRoomElvAlign2.Checked = true;
            }
            else
            {
                radioNotRoomElvAlign3.Checked = true;
            }
        }

        private void UpdateRoomDuctSize(FanParam fanParam)
        {
            if (fanParam.isRoomReCommandSize)
            {
                radioRoomRecommand.Checked = true;
                listBoxRoomDuctSize.SelectedItem = fanParam.roomDuctSize;
            }
            else
            {
                radioRoomCustom.Checked = true;
                ThMEPHVACService.GetWidthAndHeight(fanParam.roomDuctSize, out double w, out double h);
                textRoomWidth.Text = w.ToString();
                textRoomHeight.Text = h.ToString();

            }
            if (fanParam.isNotRoomReCommandSize)
            {
                radioNotRoomRecommand.Checked = true;
                listBoxNotRoomDuctSize.SelectedItem = fanParam.notRoomDuctSize;
            }
            else
            {
                radioNotRoomCustom.Checked = true;
                ThMEPHVACService.GetWidthAndHeight(fanParam.notRoomDuctSize, out double w, out double h);
                textNotRoomWidth.Text = w.ToString();
                textNotRoomHeight.Text = h.ToString();
            }

        }
        private void roomDuctSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            var airVolume = GetAirVolume(textAirVolume.Text);
            ThMEPHVACService.GetWidthAndHeight(listBoxRoomDuctSize.SelectedItem.ToString(), out double w, out double h);
            double speed = ThHvacUIService.CalcAirSpeed(airVolume, w, h);
            labelRoomAirSpeed.Text = speed.ToString("0.00");
            var scenario = (string)scenarioCombox.SelectedItem;
            SetPortEleByScenario(scenario);
        }

        private void notRoomDuctSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            var airVolume = GetAirVolume(textAirVolume.Text);
            ThMEPHVACService.GetWidthAndHeight(listBoxNotRoomDuctSize.SelectedItem.ToString(), out double w, out double h);
            double speed = ThHvacUIService.CalcAirSpeed(airVolume, w, h);
            labelNotRoomAirSpeed.Text = speed.ToString("0.00");
        }

        private void scenarioCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            bypassEnable();
            var scenario = (string)scenarioCombox.SelectedItem;
            if (scenario == "消防排烟兼平时排风")
            {
                radioVerticalPipe.Enabled = true;
                textPortElevation.Enabled = true;
            }
            else
            {
                radioVerticalPipe.Checked = false;
                radioVerticalPipe.Enabled = false;
                textPortElevation.Enabled = false;
            }
            var speed = ThFanSelectionUtils.GetDefaultAirSpeed(scenario);
            textAirSpeed.Text = speed.ToString();
            SetPortRangeByScenario(scenario);
            SetPortNumByScenario(scenario);
        }

        private void SetPortEleByScenario(string scenario)
        {
            if (!String.IsNullOrEmpty(textRoomElevation.Text) &&
                (!String.IsNullOrEmpty(textRoomHeight.Text) || listBoxRoomDuctSize.SelectedItems.Count > 0))
            {
                var elevation = Double.Parse(textRoomElevation.Text) * 1000;
                var height = radioRoomCustom.Checked ? Double.Parse(textRoomHeight.Text) :
                             ThMEPHVACService.GetHeight((string)listBoxRoomDuctSize.SelectedItem);
                var idx = scenarioCombox.Items.IndexOf(scenario) + 50;
                var num = (elevation + height + idx) / 1000;
                textPortElevation.Text = num.ToString();
            }
        }

        private void SetPortNumByScenario(string scenario)
        {
            int num;
            if (scenario == "消防排烟")
                num = 3;
            else if (scenario == "平时排风" || scenario == "平时送风")
                num = 2;
            else if (scenario == "空调送风" || scenario == "空调新风")
                num = 10;
            else if (scenario == "消防排烟兼平时排风")
                num = 19;
            else
                num = 1;
            textPortNum.Text = num.ToString();
        }

        private void SetPortRangeByScenario(string scenario)
        {
            if (radioPortRange.Checked)
            {
                comboPortRange.SelectedItem = scenario.Contains("排") ? "下回风口" : "下送风口";
            }
        }
        private void GetAirVolume(out double airVolume, out double airHighVolume, out string strAirVolume)
        {
            var volume = textAirVolume.Text;
            strAirVolume = volume;
            if (selectFansDic.Count > 0)
            {
                var fan = selectFansDic.Values.FirstOrDefault();
                volume = fan.strAirVolume.Contains("/") ? fan.strAirVolume : volume;
            }
            if (volume.Contains("/"))
            {
                string[] str = volume.Split('/');
                airVolume = Double.Parse(str[1]);
                airHighVolume = Double.Parse(str[0]);
            }
            else
            {
                airVolume = Double.Parse(volume);
                airHighVolume = 0;
            }
        }
        private void GetPortInfo(out int portNum,
                                 out string scale,
                                 out string scenario,
                                 out string portSize,
                                 out string portName,
                                 out string portRange,
                                 out double airSpeed)
        {
            portNum = Int32.Parse(textPortNum.Text);
            scale = (comboScale.Text == "") ? "1:100" : comboScale.Text;
            scenario = (scenarioCombox.Text == "") ? "消防排烟" : scenarioCombox.Text;
            portSize = textPortWidth.Text + "x" + textPortHeight.Text;
            portName = scenario.Contains("排烟") ? "AH/D" : "AH";
            portRange = (comboPortRange.Text == "") ? "下送风口" : comboPortRange.Text;
            if (radioVerticalPipe.Checked)
                portRange = "侧回风口";
            if (portRange.Contains("侧") || radioVerticalPipe.Checked)
            {
                var num = Math.Round(portNum * 0.5, 0);
                portNum = (int)num;
            }
            airSpeed = Double.Parse(textAirSpeed.Text);
        }
        private void GetDuctSize(out string roomDuctSize, out string notRoomDuctSize)
        {
            if (radioRoomRecommand.Checked)
            {
                if (listBoxRoomDuctSize.Items.Count > 0)
                    roomDuctSize = listBoxRoomDuctSize.SelectedItem.ToString();
                else
                    roomDuctSize = "2000x500";
            }
            else
            {
                if (String.IsNullOrEmpty(textRoomWidth.Text) && String.IsNullOrEmpty(textRoomHeight.Text))
                    roomDuctSize = "2000x500";
                else
                    roomDuctSize = textRoomWidth.Text + "x" + textRoomHeight.Text;
            }
            if (radioNotRoomRecommand.Checked)
            {
                if (listBoxNotRoomDuctSize.Items.Count > 0)
                    notRoomDuctSize = listBoxNotRoomDuctSize.SelectedItem.ToString();
                else
                    notRoomDuctSize = "2000x500";
            }
            else
            {
                if (String.IsNullOrEmpty(textNotRoomWidth.Text) && String.IsNullOrEmpty(textNotRoomHeight.Text))
                    notRoomDuctSize = "2000x500";
                else
                    notRoomDuctSize = textNotRoomWidth.Text + "x" + textNotRoomHeight.Text;
            }
        }
        private void GetElevation(out string roomElevation,
                                  out string notRoomElevation,
                                  out ElevationAlignStyle roomElevationStyle,
                                  out ElevationAlignStyle notRoomElevationStyle)
        {
            roomElevation = textRoomElevation.Text;
            notRoomElevation = textNotRoomElevation.Text;
            roomElevationStyle = ElevationAlignStyle.Bottom;
            if (radioRoomElvAlign2.Checked)
                roomElevationStyle = ElevationAlignStyle.Center;
            if (radioRoomElvAlign3.Checked)
                roomElevationStyle = ElevationAlignStyle.Top;
            notRoomElevationStyle = ElevationAlignStyle.Bottom;
            if (radioNotRoomElvAlign2.Checked)
                notRoomElevationStyle = ElevationAlignStyle.Center;
            if (radioNotRoomElvAlign3.Checked)
                notRoomElevationStyle = ElevationAlignStyle.Top;
        }
        private FanParam RecordFanParam(string key)
        {
            GetPortInfo(out int portNum, out string scale, out string scenario, out string portSize,
                        out string portName, out string portRange, out double airSpeed);
            GetDuctSize(out string roomDuctSize, out string notRoomDuctSize);
            GetAirVolume(out double airVolume, out double airHighVolume, out string strAirVolume);
            double portInterval = radioPortInterval.Checked ? 0 : (Double.Parse(textPortInterval.Text) * 1000);
            GetElevation(out string roomElevation, out string notRoomElevation,
                         out ElevationAlignStyle roomElevationStyle, out ElevationAlignStyle notRoomElevationStyle);
            bool roomEnable = checkBoxRoom.Checked;
            bool notRoomEnable = checkBoxNotRoom.Checked;
            var bypass = new DBObjectCollection();
            if (fans.ContainsKey(key) && fans[key].bypassLines.Count > 0)
            {
                foreach (Line l in fans[key].bypassLines)
                    bypass.Add(l);
            }
            string bypassSize = (scenario == "消防加压送风") ? textBypassWidth.Text + "x" + textBypassHeight.Text : null;
            return new FanParam()
            {
                isRoomReCommandSize = radioRoomRecommand.Checked,
                isNotRoomReCommandSize = radioNotRoomRecommand.Checked,
                roomEnable = roomEnable,
                notRoomEnable = notRoomEnable,
                portNum = portNum,
                scale = scale,
                portSize = portSize,
                portName = portName,
                scenario = scenario,
                portRange = portRange,
                bypassSize = bypassSize,
                bypassPattern = String.Empty,
                roomDuctSize = roomDuctSize,
                notRoomDuctSize = notRoomDuctSize,
                airSpeed = airSpeed,
                airVolume = airVolume,
                airHighVolume = airHighVolume,
                portInterval = portInterval,
                roomElevation = roomElevation,
                notRoomElevation = notRoomElevation,
                centerLines = new DBObjectCollection(),
                bypassLines = bypass,
                roomElevationStyle = roomElevationStyle,
                notRoomElevationStyle = notRoomElevationStyle,
            };
        }
        private void bypassEnable()
        {
            var scenario = (string)scenarioCombox.SelectedItem;
            if (scenario != "消防加压送风")
            {
                label40.Enabled = false;
                label41.Enabled = false;
                labelBypassNum.Enabled = false;
                label43.Enabled = false;
                btnClearSelectBypass.Enabled = false;
                btnSelectBypass.Enabled = false;
                textBypassWidth.Enabled = false;
                textBypassHeight.Enabled = false;
                labelBypassSize.Enabled = false;
                label10.Enabled = false;
            }
            else
            {
                label40.Enabled = true;
                label41.Enabled = true;
                labelBypassNum.Enabled = true;
                label43.Enabled = true;
                btnClearSelectBypass.Enabled = true;
                btnSelectBypass.Enabled = true;
                textBypassWidth.Enabled = true;
                textBypassHeight.Enabled = true;
                labelBypassSize.Enabled = true;
                label10.Enabled = true;
            }
        }

        private void radioRoomRecommand_CheckedChanged(object sender, EventArgs e)
        {
            if (radioRoomRecommand.Checked)
            {
                textRoomWidth.Enabled = false;
                textRoomHeight.Enabled = false;
                labelRoomAirSpeed.Enabled = false;
                listBoxRoomDuctSize.Enabled = true;
            }
        }

        private void radioRoomCustom_CheckedChanged(object sender, EventArgs e)
        {
            if (radioRoomCustom.Checked)
            {
                textRoomWidth.Enabled = true;
                textRoomHeight.Enabled = true;
                labelRoomAirSpeed.Enabled = true;
                listBoxRoomDuctSize.Enabled = false;
            }
        }

        private void radioNotRoomRecommand_CheckedChanged(object sender, EventArgs e)
        {
            if (radioNotRoomRecommand.Checked)
            {
                textNotRoomWidth.Enabled = false;
                textNotRoomHeight.Enabled = false;
                labelNotRoomAirSpeed.Enabled = false;
                listBoxNotRoomDuctSize.Enabled = true;
            }
        }

        private void radioNotRoomCustom_CheckedChanged(object sender, EventArgs e)
        {
            if (radioNotRoomCustom.Checked)
            {
                textNotRoomWidth.Enabled = true;
                textNotRoomHeight.Enabled = true;
                labelNotRoomAirSpeed.Enabled = true;
                listBoxNotRoomDuctSize.Enabled = false;
            }
        }
        private void textRoomWidth_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.IsIntegerStr(textRoomWidth.Text))
                textRoomWidth.Text = "1000";
            else
                SetRoomDuctSpeed();
        }
        private void textRoomHeight_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.IsIntegerStr(textRoomHeight.Text))
                textRoomHeight.Text = "1000";
            else
            {
                SetRoomDuctSpeed();
                var scenario = (string)scenarioCombox.SelectedItem;
                SetPortEleByScenario(scenario);
            }
        }
        private void SetRoomDuctSpeed()
        {
            if (String.IsNullOrEmpty(textAirVolume.Text) || String.IsNullOrEmpty(textAirSpeed.Text))
                return;
            var airVolume = GetAirVolume(textAirVolume.Text);

            if (labelRoomAirSpeed.Enabled)
            {
                if (String.IsNullOrEmpty(textRoomWidth.Text) || String.IsNullOrEmpty(textRoomHeight.Text))
                    return;
                // 自定义更新风速
                double speed = ThHvacUIService.CalcAirSpeed(airVolume, Double.Parse(textRoomWidth.Text), Double.Parse(textRoomHeight.Text));
                labelRoomAirSpeed.Text = speed.ToString("0.00");
            }
            else
            {
                // 更新推荐管径表
                UpdateDuctSizeList(airVolume);
            }
        }

        private void textNotRoomWidth_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.IsIntegerStr(textNotRoomWidth.Text))
                textRoomWidth.Text = "1000";
            else
                SetNotRoomDuctSpeed();
        }

        private void textNotRoomHeight_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.IsIntegerStr(textNotRoomHeight.Text))
                textNotRoomHeight.Text = "1000";
            else
                SetNotRoomDuctSpeed();
        }
        private void SetNotRoomDuctSpeed()
        {
            if (String.IsNullOrEmpty(textAirVolume.Text) || String.IsNullOrEmpty(textAirSpeed.Text))
                return;
            var airVolume = GetAirVolume(textAirVolume.Text);
            if (labelNotRoomAirSpeed.Enabled)
            {
                if (String.IsNullOrEmpty(textNotRoomWidth.Text) || String.IsNullOrEmpty(textNotRoomHeight.Text))
                    return;
                double speed = ThHvacUIService.CalcAirSpeed(airVolume, Double.Parse(textNotRoomWidth.Text), Double.Parse(textNotRoomHeight.Text));
                labelNotRoomAirSpeed.Text = speed.ToString("0.00");
            }
            else
            {
                UpdateDuctSizeList(airVolume);
            }
        }

        private void btnSelectBypass_Click(object sender, EventArgs e)
        {
            Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
            var bypassLine = cmdService.GetBypass();
            var key = (string)listBox1.SelectedItem;
            if (fans.ContainsKey(key))
            {
                fans[key].bypassLines.Clear();
            }
            else
            {
                var param = RecordFanParam(key);
                fans.Add(key, param);
            }
            foreach (Line l in bypassLine)
                fans[key].bypassLines.Add(l);
            labelBypassNum.Text = bypassLine.Count.ToString();
            Focus();
        }
        private void GetNotRoomInfo(DBObjectCollection notRoomLines)
        {
            // 收集非服务侧的起始点和相连的线
            var mat = Matrix3d.Displacement(-RoomStartPoint.GetAsVector());
            if (listBox1.Items.Count > 0)
            {
                var key = listBox1.SelectedItem.ToString();
                var fanModel = selectFansDic[key];
                notRoomStartPoint = fanModel.isExhaust ? fanModel.FanOutletBasePoint : fanModel.FanInletBasePoint;
                notRoomStartPoint = notRoomStartPoint.TransformBy(mat);
                var notRoomDetector = new ThFanCenterLineDetector(false);
                // 非服务侧搜索全部的线
                notRoomDetector.SearchCenterLine(notRoomLines, ref notRoomStartPoint, SearchBreakType.breakWithEndline);
                connNotRoomLines = notRoomDetector.connectLines;
            }
        }
        private void GetFanCenterlinesAndTrans(Point3d otherP, out DBObjectCollection roomLines, out DBObjectCollection notRoomLines)
        {
            // 风机进出口路由可能在平面上共线(仅处理进出口必须有线的情况)
            var diffColorRoutine = GetDiffColorRoutine(RoomStartPoint);
            roomLines = new DBObjectCollection();
            notRoomLines = new DBObjectCollection();
            var mat = Matrix3d.Displacement(-RoomStartPoint.GetAsVector());
            var notRoomP = otherP.TransformBy(mat);
            foreach (var lines in diffColorRoutine)
            {
                var index = new ThCADCoreNTSSpatialIndex(lines);
                var pl1 = ThMEPHVACService.CreateDetector(Point3d.Origin, firstRange);
                var res = index.SelectCrossingPolygon(pl1);
                if (roomLines.Count == 0 && res.Count == 1)
                {
                    roomLines = lines;
                    ExcludeBypass(ref roomLines, mat);
                }
                var pl2 = ThMEPHVACService.CreateDetector(notRoomP, firstRange);
                var res1 = index.SelectCrossingPolygon(pl2);
                if (notRoomLines.Count == 0 && res1.Count == 1)
                {
                    notRoomLines = lines;
                    ExcludeBypass(ref notRoomLines, mat);
                }
            }
            if (roomLines.Count == 0)
                throw new NotImplementedException("风机服务侧未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
            if (notRoomLines.Count == 0)
                throw new NotImplementedException("风机非服务侧未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
        }

        private void GetCenterlinesAndTrans(Point3d p)
        {
            // 用于将指定起点的路由与其他系统分开
            var diffColorRoutine = GetDiffColorRoutine(p);
            var mat = Matrix3d.Displacement(-p.GetAsVector());
            foreach (var lines in diffColorRoutine)
            {
                var index = new ThCADCoreNTSSpatialIndex(lines);
                var pl = ThMEPHVACService.CreateDetector(Point3d.Origin, firstRange);
                var res = index.SelectCrossingPolygon(pl);
                if (res.Count == 1)
                {
                    centerlines = lines;
                    ExcludeBypass(ref centerlines, mat);
                    return;
                }
            }
            if (centerlines.Count == 0)
                throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
        }
        private bool LineContainsPoint(Line l, Point3d p, Tolerance tor)
        {
            return p.IsEqualTo(l.StartPoint, tor) || p.IsEqualTo(l.EndPoint, tor);
        }
        private List<DBObjectCollection> GetDiffColorRoutine(Point3d p)
        {
            centerlines = ThDuctPortsReadComponent.GetCenterlineByLayer(ThHvacCommon.AI_DUCT_ROUTINE);
            var linesDic = SepLineByColor();
            var diffColorRoutine = new List<DBObjectCollection>();
            var mat = Matrix3d.Displacement(-p.GetAsVector());
            foreach (var pair in linesDic)
            {
                foreach (Curve l in pair.Value)
                    l.TransformBy(mat);
                var lines = ThMEPHVACLineProc.PreProc(pair.Value);
                diffColorRoutine.Add(lines);
            }
            linesDic.Clear();
            centerlines.Clear();
            return diffColorRoutine;
        }
        private Dictionary<int, DBObjectCollection> SepLineByColor()
        {
            var dic = new Dictionary<int, DBObjectCollection>();
            foreach (Curve l in centerlines)
            {
                if (dic.ContainsKey(l.ColorIndex))
                    dic[l.ColorIndex].Add(l);
                else
                    dic.Add(l.ColorIndex, new DBObjectCollection() { l });
            }
            return dic;
        }
        private void CheckCenterLine(Point3d roomP, Point3d notRoomP, DBObjectCollection roomLines, DBObjectCollection notRoomLines)
        {
            var index = new ThCADCoreNTSSpatialIndex(roomLines);
            var roomPl = ThMEPHVACService.CreateDetector(roomP, firstRange);
            var roomRes = index.SelectCrossingPolygon(roomPl);
            if (roomRes.Count != 1)
                throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
            var notRoomPl = ThMEPHVACService.CreateDetector(notRoomP, firstRange);
            index = new ThCADCoreNTSSpatialIndex(notRoomLines);
            var notRoomRes = index.SelectCrossingPolygon(notRoomPl);
            if (notRoomRes.Count != 1)
                throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
            var roomLine = roomRes[0] as Line;
            var notRoomLine = notRoomRes[0] as Line;
            if (roomLine.Equals(notRoomLine))
                throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
        }
        private void GetFanConnectLine(out DBObjectCollection notRoomLines)
        {
            RoomStartPoint = GetBasePoint(out Point3d otherP);
            GetFanCenterlinesAndTrans(otherP, out DBObjectCollection roomLines, out notRoomLines);
            var mat = Matrix3d.Displacement(-RoomStartPoint.GetAsVector());
            var reverseMat = Matrix3d.Displacement(RoomStartPoint.GetAsVector());
            foreach (string key in listBox1.Items)
            {
                var fan = fans[key];
                var fanModel = selectFansDic[key];
                var roomPoint = fanModel.isExhaust ? fanModel.FanInletBasePoint : fanModel.FanOutletBasePoint;
                var roomNotPoint = fanModel.isExhaust ? fanModel.FanOutletBasePoint : fanModel.FanInletBasePoint;
                roomPoint = roomPoint.TransformBy(mat);
                roomNotPoint = roomNotPoint.TransformBy(mat);
                CheckCenterLine(roomPoint, roomNotPoint, roomLines, notRoomLines);
                var roomDetector = new ThFanCenterLineDetector(false);
                // 服务侧搜索全部的线
                roomDetector.SearchCenterLine(roomLines, ref roomPoint, SearchBreakType.breakWithEndline);
                var notRoomDetector = new ThFanCenterLineDetector(false);
                // 非服务侧搜索截至到三通和四通的线
                notRoomDetector.SearchCenterLine(notRoomLines, ref roomNotPoint, SearchBreakType.breakWithTeeAndCross);
                if (fanModel.isExhaust)
                {
                    fanModel.FanInletBasePoint = roomPoint.TransformBy(reverseMat);
                    fanModel.FanOutletBasePoint = roomNotPoint.TransformBy(reverseMat);
                }
                else
                {
                    fanModel.FanInletBasePoint = roomNotPoint.TransformBy(reverseMat);
                    fanModel.FanOutletBasePoint = roomPoint.TransformBy(reverseMat);
                }
                if (fan.scenario == "消防加压送风")
                {
                    foreach (Line l in fan.bypassLines)
                        l.TransformBy(mat);
                    fan.bypassPattern = ThMEPHVACService.ClassifyBypassPattern(
                        roomDetector.connectLines, notRoomDetector.connectLines, fan.bypassLines);
                    foreach (Line l in fan.bypassLines)
                        l.TransformBy(reverseMat);
                }
                fan.lastNotRoomLine = notRoomDetector.lastLine;
                fan.roomLines = roomDetector.connectLines;
                fan.notRoomLines = notRoomDetector.connectLines;
                CollectFanConnectLine(fan, roomDetector, notRoomDetector);
                foreach (Line l in fan.centerLines)
                    l.TransformBy(reverseMat);
                foreach (Line l in fan.roomLines)
                    l.TransformBy(reverseMat);
                foreach (Line l in fan.notRoomLines)
                    l.TransformBy(reverseMat);
            }
        }
        private void CollectFanConnectLine(FanParam fan,
                                           ThFanCenterLineDetector roomDetector,
                                           ThFanCenterLineDetector notRoomDetector)
        {
            // 搜索到的connectLines是指向原线的引用，此处需要拷贝
            if (fan.roomEnable && fan.notRoomEnable)
            {
                foreach (Line l in roomDetector.connectLines)
                    fan.centerLines.Add(l.Clone() as Line);
                foreach (Line l in notRoomDetector.connectLines)
                    fan.centerLines.Add(l.Clone() as Line);
            }
            else if (fan.roomEnable && !fan.notRoomEnable)
            {
                foreach (Line l in roomDetector.connectLines)
                    fan.centerLines.Add(l.Clone() as Line);
            }
            else if (!fan.roomEnable && fan.notRoomEnable)
            {
                foreach (Line l in notRoomDetector.connectLines)
                    fan.centerLines.Add(l.Clone() as Line);
            }
            else
                throw new NotImplementedException("[Check Error]: Select generation side!");
        }
        private void ExcludeBypass(ref DBObjectCollection centerlines, Matrix3d mat)
        {
            foreach (string key in listBox1.Items)
            {
                var fan = fans[key];
                foreach (Line l in fan.bypassLines)
                {
                    var shadow = l.Clone() as Line;
                    shadow.TransformBy(mat);
                    foreach (Line judger in centerlines)
                    {
                        if (ThMEPHVACService.IsSameLine(shadow, judger))
                        {
                            centerlines.Remove(judger);
                            break;
                        }
                    }
                }
            }
        }
        private Point3d GetBasePoint(out Point3d otherP)
        {
            foreach (string key in listBox1.Items)
            {
                var fanModel = selectFansDic[key];
                otherP = !fanModel.isExhaust ? fanModel.FanInletBasePoint : fanModel.FanOutletBasePoint;
                return fanModel.isExhaust ? fanModel.FanInletBasePoint : fanModel.FanOutletBasePoint;
            }
            throw new NotImplementedException("No fan was selected!");
        }
        private void textPortNum_TextChanged(object sender, EventArgs e)
        {
            if (ThHvacUIService.IsIntegerStr(textPortNum.Text))
            {
                if (!String.IsNullOrEmpty(textPortNum.Text))
                {
                    SetPortSpeed();
                    SetPortSize();
                }
            }
            else
                textPortNum.Text = "3";
        }

        private void textPortWidth_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.IsIntegerStr(textPortWidth.Text))
                textPortWidth.Text = "500";
            else
                SetPortSpeed();
        }

        private void textPortHeight_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.IsIntegerStr(textPortHeight.Text))
                textPortHeight.Text = "320";
            else
                SetPortSpeed();
        }

        private void textBypassWidth_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.IsIntegerStr(textBypassWidth.Text))
                textBypassWidth.Text = "500";
            else
                SetBypassSpeed();
        }
        private void textBypassHeight_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.IsIntegerStr(textBypassHeight.Text))
                textBypassHeight.Text = "320";
            else
                SetBypassSpeed();
        }
        private void textAirVolume_TextChanged(object sender, EventArgs e)
        {
            if (!isSelectFan)
            {
                if (!ThHvacUIService.IsIntegerStr(textAirVolume.Text) && !textAirVolume.Text.Contains("/"))
                    textAirVolume.Text = "20000";
                else
                {
                    if (checkBoxRoom.Checked)
                        SetRoomDuctSpeed();
                    else
                        SetNotRoomDuctSpeed();
                }
            }
        }
        private void textAirSpeed_TextChanged(object sender, EventArgs e)
        {
            if (!isSelectFan)
            {
                if (!ThHvacUIService.IsIntegerStr(textAirSpeed.Text))
                    textAirSpeed.Text = "8";
                else
                {
                    if (checkBoxRoom.Checked)
                        SetRoomDuctSpeed();
                    else
                        SetNotRoomDuctSpeed();
                }
            }
        }
        private void SetBypassSpeed()
        {
            if (String.IsNullOrEmpty(textBypassWidth.Text) || String.IsNullOrEmpty(textBypassHeight.Text) ||
                String.IsNullOrEmpty(textAirVolume.Text) || String.IsNullOrEmpty(textAirSpeed.Text))
                return;
            var airVolume = GetAirVolume(textAirVolume.Text);
            double speed = ThHvacUIService.CalcAirSpeed(airVolume, Double.Parse(textBypassWidth.Text), Double.Parse(textBypassHeight.Text));
            labelBypassSpeed.Text = speed.ToString("0.00");
        }

        private void SetPortSize()
        {
            if (String.IsNullOrEmpty(textPortWidth.Text) || String.IsNullOrEmpty(textPortHeight.Text) ||
                String.IsNullOrEmpty(textAirVolume.Text) || String.IsNullOrEmpty(textAirSpeed.Text))
                return;
            var portNum = (int)Double.Parse(textPortNum.Text);
            var airVolume = GetAirVolume(textAirVolume.Text);
            if (portNum == 0)
                return;
            double avgAirVolume = airVolume / portNum;
            var size = GetPortHeight(avgAirVolume);
            textPortWidth.Text = size.Item1.ToString();
            textPortHeight.Text = size.Item2.ToString();
        }

        private Tuple<double, double> GetPortHeight(double airVolume)
        {
            var selector = new ThPortParameter(airVolume, 0, PortRecommendType.PORT);
            return new Tuple<double, double>(selector.DuctSizeInfor.DuctWidth, selector.DuctSizeInfor.DuctHeight);
        }

        private void SetPortSpeed()
        {
            if (String.IsNullOrEmpty(textPortWidth.Text) || String.IsNullOrEmpty(textPortHeight.Text) ||
                String.IsNullOrEmpty(textAirVolume.Text) || String.IsNullOrEmpty(textAirSpeed.Text))
                return;
            var portNum = (int)Double.Parse(textPortNum.Text);
            var airVolume = GetAirVolume(textAirVolume.Text);
            if (portNum == 0)
                return;
            double avgAirVolume = airVolume / portNum;
            double speed = ThHvacUIService.CalcAirSpeed(avgAirVolume, Double.Parse(textPortWidth.Text), Double.Parse(textPortHeight.Text));
            labelPortSpeed.Text = speed.ToString("0.00");
        }

        private void textPortInterval_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.IsFloat2Decimal(textPortInterval.Text))
                textPortInterval.Text = "3";
        }

        private void radioPortInterval_CheckedChanged(object sender, EventArgs e)
        {
            if (radioPortInterval.Checked)
            {
                textPortInterval.Enabled = false;
                radioIntervalPortCustom.Checked = false;
            }
        }

        private void radioIntervalPortCustom_CheckedChanged(object sender, EventArgs e)
        {
            if (radioIntervalPortCustom.Checked)
            {
                textPortInterval.Enabled = true;
                radioPortInterval.Checked = false;
            }
        }

        private void btnSelectSrtPoint_Click(object sender, EventArgs e)
        {
            Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
            var ptRes = ThHvacCmdService.GetPointFromPrompt("选择起点", out Matrix3d ucsMat);
            this.ucsMat = ucsMat;
            if (ptRes.HasValue)
            {
                srtPoint = ptRes.Value;
                isSelectFan = false;
                checkBoxRoom.Checked = true;
                checkBoxNotRoom.Checked = false;
                splitContainer6.Panel1.Enabled = false;
                splitContainer5.Panel1.Enabled = true;
                textAirVolume.Enabled = true;
                DetectCrossFan();
                GetAirVolume(out double airVolume, out _, out _);
                FillDuctSize(airVolume);
            }
        }
        private void DetectCrossFan()
        {
            if (!isReadAllFans)
            {
                ThDuctPortsInterpreter.GetFanDic(out shadowFansDic);
                isReadAllFans = true;
            }
            var toZeroMat = Matrix3d.Displacement(-srtPoint.GetAsVector());
            allFansDic.Clear();
            foreach (var fan in shadowFansDic)
                allFansDic.Add(fan.Key, fan.Value);
            foreach (Polyline b in allFansDic.Keys.ToCollection())
                b.TransformBy(toZeroMat);
            var fanIndex = new ThCADCoreNTSSpatialIndex(allFansDic.Keys.ToCollection());
            var pl = ThMEPHVACService.CreateDetector(Point3d.Origin);
            var res = fanIndex.SelectCrossingPolygon(pl);
            if (res.Count == 1)
            {
                var id = allFansDic[res[0] as Polyline];
                var fan = new ThDbModelFan(id);
                textAirVolume.Text = fan.airVolume.ToString();
                scenarioCombox.Text = fan.scenario;
            }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            if (isSelectFan)
            {
                if (listBox1.Items.Count == 0)
                    return;
                var key = (string)listBox1.SelectedItem;
                var param = RecordFanParam(key);
                if (fans.ContainsKey(key))
                    fans.Remove(key);
                fans.Add(key, param);
                GetFanConnectLine(out DBObjectCollection notRoomLines);
                GetNotRoomInfo(notRoomLines);
                RecordPortParam();
            }
            else
            {
                GetCenterlinesAndTrans(srtPoint);
                RecordPortParam();
                DetectConnectLineAndTrans(srtPoint);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        private void DetectConnectLineAndTrans(Point3d p)
        {
            var mat = Matrix3d.Displacement(-p.GetAsVector());
            var searchP = p.TransformBy(mat);
            var detector = new ThFanCenterLineDetector(false);
            detector.SearchCenterLine(centerlines, ref searchP, SearchBreakType.breakWithEndline);
            portParam.centerLines = detector.connectLines;
        }
        private void RecordPortParam()
        {
            var genStyle = GenerationStyle.Auto;
            if (radioGenStyle2.Checked)
                genStyle = GenerationStyle.GenerationByPort;
            if (radioGenStyle1.Checked)
                genStyle = GenerationStyle.GenerationWithPortVolume;
            GetPortInfo(out int portNum, out string scale, out string scenario, out string portSize,
                        out string portName, out string portRange, out double airSpeed);
            GetDuctSize(out string roomDuctSize, out string notRoomDuctSize);
            GetAirVolume(out double airVolume, out double airHighVolume, out string strAirVolume);
            double portInterval = radioPortInterval.Checked ? 0 : (Double.Parse(textPortInterval.Text) * 1000);
            GetElevation(out string roomElevation, out string notRoomElevation, out ElevationAlignStyle _, out ElevationAlignStyle _);
            var flag = checkBoxRoom.Checked;
            var elevation = flag ? roomElevation : notRoomElevation;
            var ductSize = flag ? roomDuctSize : notRoomDuctSize;
            var endCompType = GetEndCompType();
            var portBottomEle = Double.Parse(textPortElevation.Text);
            var p = new ThMEPHVACParam()
            {
                portNum = portNum,
                airSpeed = airSpeed,
                airVolume = airVolume,
                highAirVolume = airHighVolume,
                elevation = Double.Parse(elevation),
                mainHeight = ThMEPHVACService.GetHeight(ductSize),
                scale = scale,
                scenario = scenario,
                portSize = portSize,
                portName = portName,
                portRange = portRange,
                inDuctSize = ductSize,
                portBottomEle = portBottomEle,
            };
            portParam = new PortParam()
            {
                verticalPipeEnable = radioVerticalPipe.Checked,
                param = p,
                genStyle = genStyle,
                srtPoint = srtPoint,
                endCompType = endCompType,
                portInterval = portInterval,
                textAirVolume = strAirVolume
            };
        }
        private EndCompType GetEndCompType()
        {
            if (radioButton2.Checked)
                return EndCompType.VerticalPipe;
            if (radioButton3.Checked)
                return EndCompType.RainProofShutter;
            if (radioButton4.Checked)
                return EndCompType.DownFlip45;
            return EndCompType.None;
        }
        private void checkBoxRoom_CheckedChanged(object sender, EventArgs e)
        {
            if (!isSelectFan)
            {
                if (checkBoxRoom.Checked)
                {
                    checkBoxNotRoom.Checked = false;
                    splitContainer6.Panel1.Enabled = false;
                    splitContainer5.Panel1.Enabled = true;
                }
            }
        }

        private void checkBoxNotRoom_CheckedChanged(object sender, EventArgs e)
        {
            if (!isSelectFan)
            {
                if (checkBoxNotRoom.Checked)
                {
                    checkBoxRoom.Checked = false;
                    splitContainer6.Panel1.Enabled = true;
                    splitContainer5.Panel1.Enabled = false;
                }
            }
        }

        private void radioGenStyle1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioGenStyle1.Checked)
            {
                UpdateUIPortInfo(false);
                btnTotalAirVolume.Enabled = true;
                comboPortRange.Enabled = false;
            }
        }

        private void radioGenStyle2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioGenStyle2.Checked)
            {
                UpdateUIPortInfo(false);
                btnTotalAirVolume.Enabled = false;
                comboPortRange.Enabled = false;
            }
        }

        private void radioGenStyle3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioGenStyle3.Checked)
            {
                UpdateUIPortInfo(true);
                btnTotalAirVolume.Enabled = false;
                comboPortRange.Enabled = true;
            }
        }
        private void UpdateUIPortInfo(bool status)
        {
            textPortNum.Enabled = status;
            textPortWidth.Enabled = status;
            textPortHeight.Enabled = status;
            radioPortInterval.Enabled = status;
            radioIntervalPortCustom.Enabled = status;
            textPortInterval.Enabled = status;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnTotalAirVolume_Click(object sender, EventArgs e)
        {
            if (radioGenStyle1.Checked)
            {
                RecordPortParam();
                if (fans.Values.Count > 0)
                {
                    var param = selectFansDic.Values.First();
                    srtPoint = param.FanInletBasePoint;
                }
                GetCenterlinesAndTrans(srtPoint);
                if (centerlines.Count == 0)
                    return;
                var detector = new ThFanCenterLineDetector(false);
                var p = Point3d.Origin;
                detector.SearchCenterLine(centerlines, ref p, SearchBreakType.breakWithEndline);
                portParam.centerLines = detector.connectLines;
                var anay = new ThDuctPortsAnalysis();
                var airVolume = anay.CalcAirVolume(portParam);
                airVolume = ThMEPHVACService.RoundNum(airVolume, 50);
                textAirVolume.Text = airVolume.ToString();
            }
        }

        private void groupBox4_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        private void radioVerticalPipe_CheckedChanged(object sender, EventArgs e)
        {
            if (radioVerticalPipe.Checked)
                comboPortRange.Enabled = false;
        }

        private void radioPortRange_CheckedChanged(object sender, EventArgs e)
        {
            if (radioPortRange.Checked)
            {
                comboPortRange.Enabled = true;
            }
        }

        private double GetAirVolume(string strVolume)
        {
            if (strVolume.Contains("/"))
            {
                var strs = strVolume.Split('/');
                if (strs.Count() == 2 && !String.IsNullOrEmpty(strs[1]) && ThHvacUIService.IsIntegerStr(strs[1]))
                    return Double.Parse(strs[1]);
                else
                    return 20000;
            }
            return Double.Parse(strVolume);
        }

        private void textPortElevation_TextChanged(object sender, EventArgs e)
        {
            if (!ThHvacUIService.IsFloat2Decimal(textPortElevation.Text))
                textPortElevation.Text = "3";
        }

        private void btnClearSelectBypass_Click(object sender, EventArgs e)
        {
            var key = (string)listBox1.SelectedItem;
            if (fans.ContainsKey(key))
            {
                fans[key].bypassLines.Clear();
                labelBypassNum.Text = fans[key].bypassLines.Count.ToString();
                ThMEPHVACService.PromptMsg("清除 " + key + " 旁通");
            }
            else
            {
                ThMEPHVACService.PromptMsg("当前风机：" + key + " 路由未包含旁通");
            }
        }
    }
}