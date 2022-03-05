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
        public fmFpm()
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
            ThDuctPortsInterpreter.GetFanDic(out shadowFansDic);
            allFansDic = new Dictionary<Polyline, ObjectId>();
            SetPortSpeed();
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
            var airVolume = (int)Double.Parse(textAirVolume.Text);
            ThMEPHVACService.GetWidthAndHeight(listBoxRoomDuctSize.SelectedItem.ToString(), out double w, out double h);
            double speed = ThHvacUIService.CalcAirSpeed(airVolume, w, h);
            labelRoomAirSpeed.Text = speed.ToString("0.00");
        }

        private void notRoomDuctSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            var airVolume = (int)Double.Parse(textAirVolume.Text);
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
        }
        private void GetAirVolume(out double airVolume, out double airHighVolume)
        {
            var volume = textAirVolume.Text;
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
                portNum /= 2;
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
                if (listBoxNotRoomDuctSize.Items.Count > 0)
                    notRoomDuctSize = listBoxNotRoomDuctSize.SelectedItem.ToString();
                else
                    notRoomDuctSize = "2000x500";
            }
            else
            {
                if (String.IsNullOrEmpty(textRoomWidth.Text) && String.IsNullOrEmpty(textRoomHeight.Text))
                    roomDuctSize = "2000x500";
                else
                    roomDuctSize = textRoomWidth.Text + "x" + textRoomHeight.Text;
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
            GetAirVolume(out double airVolume, out double airHighVolume);
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
                button6.Enabled = false;
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
                button6.Enabled = true;
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
                SetRoomDuctSpeed();
        }
        private void SetRoomDuctSpeed()
        {
            if (String.IsNullOrEmpty(textAirVolume.Text) || String.IsNullOrEmpty(textAirSpeed.Text))
                return;
            var airVolume = (int)Double.Parse(textAirVolume.Text);

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
            var airVolume = (int)Double.Parse(textAirVolume.Text);
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
        private void GetNotRoomInfo()
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
                notRoomDetector.SearchCenterLine(centerlines, ref notRoomStartPoint, SearchBreakType.breakWithEndline);
                connNotRoomLines = notRoomDetector.connectLines;
            }
        }
        private void GetCenterlinesAndTrans(Point3d p)
        {
            centerlines = ThDuctPortsReadComponent.GetCenterlineByLayer(ThHvacCommon.AI_DUCT_ROUTINE);
            ExcludeBypass(ref centerlines);
            var linesDic = SepLineByColor();
            var procLines = new List<DBObjectCollection>();
            var mat = Matrix3d.Displacement(-p.GetAsVector());
            foreach (var pair in linesDic)
            {
                foreach (Curve l in pair.Value)
                    l.TransformBy(mat);
                var lines = ThMEPHVACLineProc.PreProc(pair.Value);
                procLines.Add(lines);
            }
            linesDic.Clear();
            centerlines.Clear();
            var tor = new Tolerance(firstRange, firstRange);
            foreach (var lines in procLines)
            {
                foreach (Line l in lines)
                {
                    // 此处一定有包含原点的线，所以就不做容差
                    if (Point3d.Origin.IsEqualTo(l.StartPoint, tor) || Point3d.Origin.IsEqualTo(l.EndPoint, tor))
                    {
                        centerlines = lines;
                        return;
                    }
                }
            }
            if (centerlines.Count == 0)
                throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
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
        private void CheckCenterLine(Point3d roomP, Point3d notRoomP)
        {
            var index = new ThCADCoreNTSSpatialIndex(centerlines);
            var roomPl = ThMEPHVACService.CreateDetector(roomP, firstRange);
            var roomRes = index.SelectCrossingPolygon(roomPl);
            if (roomRes.Count != 1)
                throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
            var notRoomPl = ThMEPHVACService.CreateDetector(notRoomP, firstRange);
            var notRoomRes = index.SelectCrossingPolygon(notRoomPl);
            if (notRoomRes.Count != 1)
                throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
            var roomLine = roomRes[0] as Line;
            var notRoomLine = notRoomRes[0] as Line;
            if (roomLine.Equals(notRoomLine))
                throw new NotImplementedException("风机出入口未搜寻到正确的风管路由线，请确保风管路由线的起点为进、出风口夹点！！！");
        }
        private void GetFanConnectLine()
        {
            RoomStartPoint = GetBasePoint();
            GetCenterlinesAndTrans(RoomStartPoint);
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
                CheckCenterLine(roomPoint, roomNotPoint);
                var roomDetector = new ThFanCenterLineDetector(false);
                // 服务侧搜索全部的线
                roomDetector.SearchCenterLine(centerlines, ref roomPoint, SearchBreakType.breakWithEndline);
                var notRoomDetector = new ThFanCenterLineDetector(false);
                // 非服务侧搜索截至到三通和四通的线
                notRoomDetector.SearchCenterLine(centerlines, ref roomNotPoint, SearchBreakType.breakWithTeeAndCross);
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
                CollectFanConnectLine(fan, roomDetector, notRoomDetector);
                foreach (Line l in fan.centerLines)
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
        private void ExcludeBypass(ref DBObjectCollection centerlines)
        {
            foreach (string key in listBox1.Items)
            {
                var fan = fans[key];
                foreach (Line l in fan.bypassLines)
                {
                    foreach (Line judger in centerlines)
                    {
                        if (ThMEPHVACService.IsSameLine(l, judger))
                        {
                            centerlines.Remove(judger);
                            break;
                        }
                    }
                }
            }
        }
        private Point3d GetBasePoint()
        {
            foreach (string key in listBox1.Items)
            {
                var fanModel = selectFansDic[key];
                return fanModel.isExhaust ? fanModel.FanInletBasePoint : fanModel.FanOutletBasePoint;
            }
            throw new NotImplementedException("No fan was selected!");
        }
        private void textPortNum_TextChanged(object sender, EventArgs e)
        {
            if (ThHvacUIService.IsIntegerStr(textPortNum.Text))
            {
                if (!String.IsNullOrEmpty(textPortNum.Text))
                    SetPortSpeed();
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
                if (!ThHvacUIService.IsIntegerStr(textAirVolume.Text))
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
            var airVolume = (int)Double.Parse(textAirVolume.Text);
            double speed = ThHvacUIService.CalcAirSpeed(airVolume, Double.Parse(textBypassWidth.Text), Double.Parse(textBypassHeight.Text));
            labelBypassSpeed.Text = speed.ToString("0.00");
        }
        private void SetPortSpeed()
        {
            if (String.IsNullOrEmpty(textPortWidth.Text) || String.IsNullOrEmpty(textPortHeight.Text) ||
                String.IsNullOrEmpty(textAirVolume.Text) || String.IsNullOrEmpty(textAirSpeed.Text))
                return;
            var portNum = (int)Double.Parse(textPortNum.Text);
            var airVolume = (int)Double.Parse(textAirVolume.Text);
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
                GetAirVolume(out double airVolume, out _);
                FillDuctSize(airVolume);
            }
        }
        private void DetectCrossFan()
        {
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
                GetFanConnectLine();
                GetNotRoomInfo();
                RecordPortParam();
                // 在集成系统中将风机参数转换到port参数
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
            GetAirVolume(out double airVolume, out double airHighVolume);
            double portInterval = radioPortInterval.Checked ? 0 : (Double.Parse(textPortInterval.Text) * 1000);
            GetElevation(out string roomElevation, out string notRoomElevation,
                         out ElevationAlignStyle roomElevationStyle, out ElevationAlignStyle notRoomElevationStyle);
            var flag = checkBoxRoom.Checked;
            var elevation = flag ? roomElevation : notRoomElevation;
            var ductSize = flag ? roomDuctSize : notRoomDuctSize;
            var endCompType = GetEndCompType();
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
                inDuctSize = ductSize
            };
            portParam = new PortParam()
            {
                verticalPipeEnable = radioVerticalPipe.Checked,
                param = p,
                genStyle = genStyle,
                srtPoint = srtPoint,
                endCompType = endCompType,
                portInterval = portInterval
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
            }
        }

        private void radioGenStyle2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioGenStyle2.Checked)
            {
                UpdateUIPortInfo(false);
                btnTotalAirVolume.Enabled = false;
            }
        }

        private void radioGenStyle3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioGenStyle3.Checked)
            {
                UpdateUIPortInfo(true);
                btnTotalAirVolume.Enabled = false;
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
                comboPortRange.Enabled = true;
        }
    }
}