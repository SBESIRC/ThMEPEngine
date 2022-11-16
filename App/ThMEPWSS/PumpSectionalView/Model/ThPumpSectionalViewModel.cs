using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Data;
using System.Globalization;

using CommunityToolkit.Mvvm.Input;
using AcHelper;
using AcHelper.Commands;
using Linq2Acad;
using DotNetARX;
using ThControlLibraryWPF.ControlUtils;
using System.Windows;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using cadGraph = Autodesk.AutoCAD.GraphicsInterface;

using ThMEPEngineCore;
using System.Text.RegularExpressions;
using ThMEPWSS.PumpSectionalView.Utils;
using static DotNetARX.Preferences;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Org.BouncyCastle.Asn1.Utilities;
using NetTopologySuite.Noding;
using ThCADExtension;
using ThMEPEngineCore.IO.ExcelService;
using System.IO;

namespace ThMEPWSS.PumpSectionalView.Model
{

    public class PumpSectionalViewModel : NotifyPropertyChangedBase
    {
        public PumpSectionalViewModel()
        {
            canContinue = true;//
            //屋顶水箱
            Length = 0;
            Width = 0;
            High = 0;
            Volume = 0;
            BaseHigh = 0;

            Type1 = "";
            Type2 = "";
            HasPump = false;
            HasRoof = false;

            //TypeList = new ObservableCollection<string>() { "有顶有稳压泵", "有顶无稳压泵", "无顶有稳压泵", "无顶无稳压泵", "有顶", "露天" };

            //生活水箱
            BaseHigh_Life = 0;
            IsAutoChooseLife = false;
            LifeBaseInfoList = new ObservableCollection<LifeBaseInfo>();
            LifePumpInfoList = new ObservableCollection<LifePumpInfo>();

            LifeBaseNum = 1;
            LifeBaseInfo info = new LifeBaseInfo();   //我自己的数据表实例类
            info.CheckNo = 1;
            info.No = "生活水箱" + 1;
            LifeBaseInfoList.Add(info);

            LifePumpNum = 1;
            LifePumpInfo info1 = new LifePumpInfo();   //我自己的数据表实例类
            info1.CheckNo = 1;
            info1.No = "生活泵组" + 1;
            LifePumpInfoList.Add(info1);



            //消防泵房
            BuildingFinishHeight_Fire = 0.0;
            RoofHeight_Fire = 0.0;
            BaseHeight_Fire = 0.0;
            PoolArea_Fire = 0.0;
            Depth_Fire = 0.0;
            Volume_Fire = 0.0;
            FirePressure_Fire = 0.0;
            WaterPressure_Fire = 0.0;

            IsAutoChooseFire = false;
            FirePumpInfoList = new ObservableCollection<FirePumpInfo>();

            FirePumpNum = 1;
            FirePumpInfo info2 = new FirePumpInfo();   //我自己的数据表实例类
            info2.CheckNo = 1;
            info2.No = "消火栓泵组" + 1;
            FirePumpInfoList.Add(info2);
        }


        private bool canContinue { get; set; }//是否继续
        //高位消防水箱 输入
        private double? _Length { get; set; }
        public double? Length
        {
            get { return _Length; }
            set
            {
                _Length = value;
                this.RaisePropertyChanged();
            }
        }

        private double? _Width { get; set; }
        public double? Width
        {
            get { return _Width; }
            set
            {
                _Width = value;
                this.RaisePropertyChanged();
            }
        }


        private double? _High { get; set; }
        public double? High
        {
            get { return _High; }
            set
            {
                _High = value;
                this.RaisePropertyChanged();
            }
        }

        private double? _Volume { get; set; }
        public double? Volume
        {
            get { return _Volume; }
            set
            {
                _Volume = value;
                this.RaisePropertyChanged();
            }
        }

        private double? _BaseHigh { get; set; }
        public double? BaseHigh
        {
            get { return _BaseHigh; }
            set
            {
                _BaseHigh = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _HasPump { get; set; }
        public bool HasPump
        {
            get { return _HasPump; }
            set
            {
                _HasPump = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _HasRoof { get; set; }
        public bool HasRoof
        {
            get { return _HasRoof; }
            set
            {
                _HasRoof = value;
                this.RaisePropertyChanged();
            }
        }

        private string _Type1 { get; set; }

        public string Type1
        {
            get { return _Type1; }
            set
            {
                _Type1 = value;
                this.RaisePropertyChanged();
            }
        }

        private string _Type2 { get; set; }

        public string Type2
        {
            get { return _Type2; }
            set
            {
                _Type2 = value;
                this.RaisePropertyChanged();
            }
        }


        public ICommand CallHighWaterTankCmd => new RelayCommand(CallHighWaterTank);
        public void CallHighWaterTank()
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();
            bool flag = true;
            if (!IsValue1(Length ?? 0.0))
            {
                Length = 0;
                dic.Add(1, "屋顶水箱长须大于0");
                flag = false;
            }
            if (!IsValue1(Width ?? 0.0))
            {
                Width = 0;
                dic.Add(2, "屋顶水箱宽须大于0");
                flag = false;
            }
            if (!IsValue1(High ?? 0.0))
            {
                High = 0;
                dic.Add(3, "屋顶水箱高须大于0");
                flag = false;
            }
            if (!IsValue2(BaseHigh ?? 0.0))
            {
                BaseHigh = 0;
                dic.Add(4, "屋顶水箱基础高度须大于0");
                flag = false;
            }
            if (!IsValue2(Volume ?? 0.0))
            {
                Volume = 0;
                dic.Add(5, "有效容积须大于0");
                flag = false;
            }

         
            double l = Length ?? 0.0;
            double w = Width ?? 0.0;
            double h = High ?? 0.0;
            double v = l * w * (h-0.8);
            if (v < Volume)
            {
                dic.Add(6, "消防水箱尺寸不满足有效容积要求，是否继续？ ");
            }

            if (dic.Count > 0)//有错误
            {
                string message = "";
                foreach (var i in dic)//汇总错误信息
                {
                    message += i.Value + "\n";
                }

                if (flag)//不需要终止
                {
                    MessageBoxResult result = MessageBox.Show(message, "", MessageBoxButton.OKCancel);
                    switch (result)
                    {
                        case MessageBoxResult.OK:
                           
                            Draw();
                            break;
                        case MessageBoxResult.Cancel:
                            break;
                    }
                }
                else
                    MessageBox.Show(message);
            }
            else
            {
              
                Draw();
            }
        }

        /// <summary>
        /// 画图
        /// </summary>
        private void Draw()
        {
            SetType1();
            SetType2();
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var cmd = new ThHighFireWaterTankCmd())
                {
                    //double[]length=new double[2] {}
                    cmd.setInput(Length ?? 0.0, Width ?? 0.0, High ?? 0.0, Volume ?? 0.0, BaseHigh ?? 0.0, Type1, Type2);
                    cmd.Execute();

                    //cmd.setInput(Length ?? 0.0, Width ?? 0.0, High ?? 0.0, Volume ?? 0.0, BaseHigh ?? 0.0, Type2);
                    //cmd.Execute();
                }
            }
        }
        private void SetType1()
        {
            if (HasPump && HasRoof)
                Type1 = "有顶有稳压泵";
            else if (!HasPump && HasRoof)
                Type1 = "有顶无稳压泵";
            else if (HasPump && !HasRoof)
                Type1 = "无顶有稳压泵";
            else if (!HasPump && !HasRoof)
                Type1 = "无顶无稳压泵";
            else Type1 = "";

        }

        private void SetType2()
        {
            if (HasRoof)
                Type2 = "有顶";
            else if (!HasRoof)
                Type2 = "露天";
            else Type2 = "";

        }

        private bool IsValue1(double v)//大于0且是0.5倍数
        {
            Regex re = new Regex("^[1-9]\\d*\\.[5]$|0\\.[5]$|^[1-9]\\d*$");//判断是否是0.5倍数
            if (!re.IsMatch(v.ToString()) || v <= 0)
            {
                return false;
            }
            else return true;


        }

        private bool IsValue2(double v)//大于0
        {
            if (v <= 0)
            {
                //MessageBox.Show("111");
                return false;
            }
            else return true;
        }



        //生活水箱

        private bool _IsAutoChooseLife { get; set; }//自动选泵
        public bool IsAutoChooseLife
        {
            get { return _IsAutoChooseLife; }
            set
            {
                _IsAutoChooseLife = value;
                this.RaisePropertyChanged();
            }
        }
        private double? _BaseHigh_Life { get; set; }
        public double? BaseHigh_Life
        {
            get { return _BaseHigh_Life; }
            set
            {
                _BaseHigh_Life = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<LifeBaseInfo> _LifeBaseInfoList { get; set; }
        public ObservableCollection<LifeBaseInfo> LifeBaseInfoList
        {
            get { return _LifeBaseInfoList; }
            set
            {
                _LifeBaseInfoList = value;
                this.RaisePropertyChanged();
            }
        }

        //生活水箱泵组信息
        private ObservableCollection<LifePumpInfo> _LifePumpInfoList { get; set; }
        public ObservableCollection<LifePumpInfo> LifePumpInfoList
        {
            get { return _LifePumpInfoList; }
            set
            {
                _LifePumpInfoList = value;
                this.RaisePropertyChanged();
            }
        }

        private int LifeBaseNum { set; get; }//生活水箱编号
        public ICommand btnAddBase_Cmd => new RelayCommand(btnAddBase);
        public void btnAddBase()
        {
            LifeBaseInfo info = new LifeBaseInfo();   //我自己的数据表实例类
            info.CheckNo = ++LifeBaseNum;
            info.No = "生活水箱" + LifeBaseNum;
            LifeBaseInfoList.Add(info);
        }

        public ICommand btnDelBase_Cmd => new RelayCommand(btnDelBase);
        public void btnDelBase()
        {
            if (LifeBaseInfoList.Count == 0)
                return;
            LifeBaseInfoList.RemoveAt(LifeBaseInfoList.Count - 1);
        }

        private int LifePumpNum { set; get; }//生活水箱泵房组编号
        public ICommand btnAddPump_Cmd => new RelayCommand(btnAddPump);
        public void btnAddPump()
        {
            LifePumpInfo info = new LifePumpInfo();   //我自己的数据表实例类
            info.CheckNo = ++LifePumpNum;
            info.No = "生活泵组" + LifePumpNum;
            LifePumpInfoList.Add(info);
        }

        public ICommand btnDelPump_Cmd => new RelayCommand(btnDelPump);
        public void btnDelPump()
        {
            if (LifePumpInfoList.Count == 0)
                return;
            LifePumpInfoList.RemoveAt(LifePumpInfoList.Count - 1);
        }

        /// <summary>
        /// 自动选泵-生活水箱
        /// </summary>
        public ICommand AutoChooseLife_Cmd => new RelayCommand(AutoChooseLife);
        public void AutoChooseLife()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThLifePumpCommon.Button_Name = "自动选泵";
                using (var cmd = new ThLifePumpCmd())
                {
                    cmd.Execute();//只进入统计而已
                }
            }

            string path = ThCADCommon.LifePumpDataTablePath();

            var errMsg = ReadFileDataLife(path);
            if (errMsg.Count > 0)
            {
                string message = "";
                foreach (var i in errMsg)
                {
                    message += i.Value + "\n";
                }
                MessageBox.Show(message);

            }

        }


        /// <summary>
        /// 绘制生活水箱
        /// </summary>
        public ICommand CallLifePumpCmd => new RelayCommand(CallLifePump);
        public void CallLifePump()
        {
            Dictionary<int, string> dic = isRightInputLifePump();
            if (LifePumpInfoList.Count == 0 || LifeBaseInfoList.Count == 0)
            {
                MessageBox.Show("生活泵组和生活水箱请至少输入一组数据！");
            }
            else if (dic.Count > 0)//有错误
            {
                string message = "";
                foreach (var i in dic)//汇总错误信息
                {
                    message += i.Value + "\n";
                }

                if (canContinue)//不需要终止
                {
                    MessageBoxResult result = MessageBox.Show(message, "", MessageBoxButton.OKCancel);
                    switch (result)
                    {
                        case MessageBoxResult.OK:
                            drawLifePump("生成剖面图");
                            break;
                        case MessageBoxResult.Cancel:
                            break;
                    }
                }
                else
                    MessageBox.Show(message);
            }
            else
                drawLifePump("生成剖面图");

        }

        private void drawLifePump(string name)
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThLifePumpCommon.Button_Name =name;
                using (var cmd = new ThLifePumpCmd())
                {

                    setInputLifePump();
                    cmd.Execute();
                }
            }
        }

        /// <summary>
        /// 设置输入
        /// </summary>
        private void setInputLifePump()
        {
            //泵组
            ThLifePumpCommon.Input_PumpList.Clear();
            int index = 0;
            for (int i = 0; i < LifePumpInfoList.Count; i++)
            {
                Pump_Arr p = new Pump_Arr();
                p.No = LifePumpInfoList[i].No;
                p.Flow_Info = LifePumpInfoList[i].Flow_Info ?? 0.0;
                p.Head = LifePumpInfoList[i].Head ?? 0.0;
                p.Power = LifePumpInfoList[i].Power ?? 0.0;
                p.Num = LifePumpInfoList[i].Num ?? 0;
                p.NoteSelect = LifePumpInfoList[i].NoteSelect;
                p.Note = LifePumpInfoList[i].Note;
                ThLifePumpCommon.Input_PumpList.Add(p);

                if (i != 0 && LifePumpInfoList[i].Flow_Info > LifePumpInfoList[index].Flow_Info)
                    index = i;
            }
            ThLifePumpCommon.PumpOutletHorizontalPipeDiameterChoice = ThLifePumpCommon.PumpOutletPipeDiameterChoice = ThLifePumpCommon.PumpSuctionPipeDiameterChoice = index + 1;

            //基本信息
            ThLifePumpCommon.Input_BasicHeight = BaseHigh_Life ?? 0.0;
            index = 0;
            for (int i = 1; i < LifeBaseInfoList.Count; i++)
            {

                if (LifeBaseInfoList[i].Volume > LifeBaseInfoList[index].Volume)
                    index = i;
                else if (LifeBaseInfoList[i].Volume == LifeBaseInfoList[index].Volume)
                {
                    double wi = (double)(LifeBaseInfoList[i].Width < LifeBaseInfoList[i].Length ? LifeBaseInfoList[i].Width : LifeBaseInfoList[i].Length);
                    double windex = (double)(LifeBaseInfoList[index].Width < LifeBaseInfoList[index].Length ? LifeBaseInfoList[index].Width : LifeBaseInfoList[index].Length);
                    if (wi > windex)
                        index = i;
                }
            }
            if (LifeBaseInfoList[index].Width < LifeBaseInfoList[index].Length)
            {
                ThLifePumpCommon.Input_Length = LifeBaseInfoList[index].Length ?? 0.0;
                ThLifePumpCommon.Input_Width = LifeBaseInfoList[index].Width ?? 0.0;
            }
            else
            {
                ThLifePumpCommon.Input_Length = LifeBaseInfoList[index].Width ?? 0.0;
                ThLifePumpCommon.Input_Width = LifeBaseInfoList[index].Length ?? 0.0;
            }
            ThLifePumpCommon.Input_Height = LifeBaseInfoList[index].Height ?? 0.0;
            ThLifePumpCommon.Input_Volume = LifeBaseInfoList[index].Volume ?? 0.0;
            ThLifePumpCommon.Input_No = LifeBaseInfoList[index].No;
            ThLifePumpCommon.Input_Num = LifeBaseInfoList[index].Num ?? 0;
            ThLifePumpCommon.Input_Note = LifeBaseInfoList[index].Note;
        }

        /// <summary>
        /// 检查所有输入是否输入正确
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, string> isRightInputLifePump()
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();
            canContinue = true;

            if (BaseHigh_Life == null || BaseHigh_Life <= 0)
            {
                dic.Add(1, "基础高度须大于0");
                canContinue = false;
            }

            foreach (var i in LifeBaseInfoList)
            {
                if (i.No == null || i.No.Trim() == "")
                {
                    if (!dic.ContainsKey(2))
                        dic.Add(2, "水箱名称不能为空");
                    canContinue = false;
                }

                if (!isAboveZeroAndHalf(i.Length ?? 0.0) || !isAboveZeroAndHalf(i.Width ?? 0.0) || !isAboveZeroAndHalf(i.Height ?? 0.0))
                {
                    if (!dic.ContainsKey(3))
                        dic.Add(3, "水箱尺寸须为0.5的倍数");
                    canContinue = false;
                }

                if (i.Volume == null || i.Volume <= 0)
                {
                    if (!dic.ContainsKey(4))
                        dic.Add(4, "水箱有效容积须大于0");
                    canContinue = false;
                }
                else 
                {
                    //避免精度丢失
                    double l = i.Length ?? 0.0;
                    double w = i.Width ?? 0.0;
                    double h = i.Height ?? 0.0;
                    double v = i.Volume??0.0;
                    if(v>l*w*(h-0.85))
                        dic.Add(0 - (int)i.CheckNo, i.No + "尺寸不满足有效容积要求，是否继续？");
                }

                if (i.Num == null || i.Num <= 0)
                {
                    if (!dic.ContainsKey(5))
                        dic.Add(5, "水箱数量须大于0");
                    canContinue = false;
                }
            }

            foreach (var i in LifePumpInfoList)
            {
                if (i.No == null || i.No.Trim() == "")
                {
                    if (!dic.ContainsKey(11))
                        dic.Add(11, "泵组编号不能为空");
                    canContinue = false;
                }

                if (i.Flow_Info == null || i.Flow_Info <= 0)
                {
                    if (!dic.ContainsKey(12))
                        dic.Add(12, "水泵扬程须大于0 ");
                    canContinue = false;
                }

                if (i.Head == null || i.Head <= 0)
                {
                    if (!dic.ContainsKey(13))
                        dic.Add(13, "水泵流量须大于0");
                    canContinue = false;
                }

                if (i.Power == null || i.Power <= 0)
                {
                    if (!dic.ContainsKey(14))
                        dic.Add(14, "水泵功率应大于0");
                    canContinue = false;
                }

                if (i.Num == null || i.Num <= 0)
                {
                    if (!dic.ContainsKey(15))
                        dic.Add(15, "水泵数量应大于0");
                    canContinue = false;
                }

                if (i.NoteSelect == null || i.NoteSelect.Trim() == "")
                {
                    if (!dic.ContainsKey(16))
                        dic.Add(16, "备注1不能为空");
                    canContinue = false;
                }
            }
            return dic;
        }

        private bool isAboveZeroAndHalf(double d)
        {
            Regex halfRegex = new Regex("^[1-9]\\d*\\.[5]$|0\\.[5]$|^[1-9]\\d*$");//判断是否是0.5倍数
            if (d <= 0 || !halfRegex.IsMatch(d.ToString()))
            {
                return false;

            }
            return true;
        }

        //生成说明
        public ICommand CallLifePumpWordCmd => new RelayCommand(CallLifePumpWord);
        public void CallLifePumpWord()
        {
            Dictionary<int, string> dic = isRightInputLifePumpWord();
            if (LifePumpInfoList.Count == 0 )
            {
                MessageBox.Show("生活泵组请至少输入一组数据！");
            }
            else if (dic.Count > 0)//有错误
            {
                string message = "";
                foreach (var i in dic)//汇总错误信息
                {
                    message += i.Value + "\n";
                }

                if (canContinue)//不需要终止
                {
                    MessageBoxResult result = MessageBox.Show(message, "", MessageBoxButton.OKCancel);
                    switch (result)
                    {
                        case MessageBoxResult.OK:
                            drawLifePump("生成说明");
                            break;
                        case MessageBoxResult.Cancel:
                            break;
                    }
                }
                else
                    MessageBox.Show(message);
            }
            else
                drawLifePump("生成说明");
        }

        /// <summary>
        /// 检查涉及说明文字部分是否输入正确
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, string> isRightInputLifePumpWord()
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();
            canContinue = true;

            if (BaseHigh_Life == null || BaseHigh_Life <= 0)
            {
                dic.Add(1, "基础高度须大于0");
                canContinue = false;
            }


            foreach (var i in LifePumpInfoList)
            {
                if (i.No == null || i.No.Trim() == "")
                {
                    if (!dic.ContainsKey(11))
                        dic.Add(11, "泵组编号不能为空");
                    canContinue = false;
                }

                if (i.Flow_Info == null || i.Flow_Info <= 0)
                {
                    if (!dic.ContainsKey(12))
                        dic.Add(12, "水泵扬程须大于0 ");
                    canContinue = false;
                }

                if (i.Head == null || i.Head <= 0)
                {
                    if (!dic.ContainsKey(13))
                        dic.Add(13, "水泵流量须大于0");
                    canContinue = false;
                }

                if (i.Power == null || i.Power <= 0)
                {
                    if (!dic.ContainsKey(14))
                        dic.Add(14, "水泵功率应大于0");
                    canContinue = false;
                }

                if (i.Num == null || i.Num <= 0)
                {
                    if (!dic.ContainsKey(15))
                        dic.Add(15, "水泵数量应大于0");
                    canContinue = false;
                }

                if (i.NoteSelect == null || i.NoteSelect.Trim() == "")
                {
                    if (!dic.ContainsKey(16))
                        dic.Add(16, "备注1不能为空");
                    canContinue = false;
                }
            }
            return dic;
        }

        //生成材料
        public ICommand CallLifePumpGraphCmd => new RelayCommand(CallLifePumpGraph);
        public void CallLifePumpGraph()
        {
            Dictionary<int, string> dic = isRightInputLifePumpGraph();
            if (LifePumpInfoList.Count == 0 || LifeBaseInfoList.Count == 0)
            {
                MessageBox.Show("生活泵组和生活水箱请至少输入一组数据！");
            }
            else if (dic.Count > 0)//有错误
            {
                string message = "";
                foreach (var i in dic)//汇总错误信息
                {
                    message += i.Value + "\n";
                }

                if (canContinue)//不需要终止
                {
                    MessageBoxResult result = MessageBox.Show(message, "", MessageBoxButton.OKCancel);
                    switch (result)
                    {
                        case MessageBoxResult.OK:
                            drawLifePump("生成材料表");
                            break;
                        case MessageBoxResult.Cancel:
                            break;
                    }
                }
                else
                    MessageBox.Show(message);
            }
            else
                drawLifePump("生成材料表");
        }

        /// <summary>
        /// 检查所有输入是否输入正确
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, string> isRightInputLifePumpGraph()
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();
            canContinue = true;


            foreach (var i in LifeBaseInfoList)
            {
                if (i.No == null || i.No.Trim() == "")
                {
                    if (!dic.ContainsKey(2))
                        dic.Add(2, "水箱名称不能为空");
                    canContinue = false;
                }

                if (!isAboveZeroAndHalf(i.Length ?? 0.0) || !isAboveZeroAndHalf(i.Width ?? 0.0) || !isAboveZeroAndHalf(i.Height ?? 0.0))
                {
                    if (!dic.ContainsKey(3))
                        dic.Add(3, "水箱尺寸须为0.5的倍数");
                    canContinue = false;
                }

                if (i.Volume == null || i.Volume <= 0)
                {
                    if (!dic.ContainsKey(4))
                        dic.Add(4, "水箱有效容积须大于0");
                    canContinue = false;
                }
                else
                {
                    //避免精度丢失
                    double l = i.Length ?? 0.0;
                    double w = i.Width ?? 0.0;
                    double h = i.Height ?? 0.0;
                    double v = i.Volume ?? 0.0;
                    if (v > l * w * (h - 0.85))
                        dic.Add(0 - (int)i.CheckNo, i.No + "尺寸不满足有效容积要求，是否继续？");
                }


                if (i.Num == null || i.Num <= 0)
                {
                    if (!dic.ContainsKey(5))
                        dic.Add(5, "水箱数量须大于0");
                    canContinue = false;
                }
            }

            foreach (var i in LifePumpInfoList)
            {
                if (i.No == null || i.No.Trim() == "")
                {
                    if (!dic.ContainsKey(11))
                        dic.Add(11, "泵组编号不能为空");
                    canContinue = false;
                }

                if (i.Flow_Info == null || i.Flow_Info <= 0)
                {
                    if (!dic.ContainsKey(12))
                        dic.Add(12, "水泵扬程须大于0 ");
                    canContinue = false;
                }

                if (i.Head == null || i.Head <= 0)
                {
                    if (!dic.ContainsKey(13))
                        dic.Add(13, "水泵流量须大于0");
                    canContinue = false;
                }

                if (i.Power == null || i.Power <= 0)
                {
                    if (!dic.ContainsKey(14))
                        dic.Add(14, "水泵功率应大于0");
                    canContinue = false;
                }

                if (i.Num == null || i.Num <= 0)
                {
                    if (!dic.ContainsKey(15))
                        dic.Add(15, "水泵数量应大于0");
                    canContinue = false;
                }

                if (i.NoteSelect == null || i.NoteSelect.Trim() == "")
                {
                    if (!dic.ContainsKey(16))
                        dic.Add(16, "备注1不能为空");
                    canContinue = false;
                }
            }
            return dic;
        }


        /// <summary>
        /// 读取生活泵房excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        private Dictionary<int, string> ReadFileDataLife(string filePath, bool isDefault = false)
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();

            //防止当前文档正在打开中，解析会报错，将Excel复制到临时位置进行堆区
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                dic.Add(1, "文件路径为空或文件不存在");
                return dic;
            }

            try
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                ExcelHelper excelService = new ExcelHelper();
                var dataSet = excelService.ReadExcelToDataSet(filePath);
                if (dataSet == null)
                {
                    dic.Add(2, "excel不存在");
                    return dic;
                }

                string tableName = "Table 1";
                var chooseDs = dataSet.Tables[tableName];
                for (int j = 0; j < LifePumpInfoList.Count; j++)
                {

                    //string tableName= "Table 1";
                    if (LifePumpInfoList[j].Flow_Info == null || LifePumpInfoList[j].Flow_Info < 1)
                    {
                        if (!dic.ContainsKey(4))
                            dic.Add(4, "流量太小未在选型库，无法自动选泵");
                        continue;
                    }
                    if (LifePumpInfoList[j].Head == null || LifePumpInfoList[j].Head <= 0)
                    {
                        if (!dic.ContainsKey(5))
                            dic.Add(5, "扬程太小未在选型库，无法自动选泵");
                        continue;
                    }

                    //查表
                    //var chooseDs = dataSet.Tables[tableName];
                    double flow = (double)LifePumpInfoList[j].Flow_Info;
                    double head = (double)LifePumpInfoList[j].Head;
                    int i = 1;
                    double row0 = 0.0;
                    for (; i < chooseDs.Rows.Count; i++)
                    {
                        var row = chooseDs.Rows[i] as System.Data.DataRow;
                        if (!row[0].Equals(DBNull.Value))
                            row0 = double.Parse(((string)row[0]).Replace(" ", ""));
                        if (row0 >= flow && double.Parse(((string)row[2]).Replace(" ", "")) >= head)
                        {

                            LifePumpInfoList[j].Power = double.Parse(((string)row[1]).Replace(" ", ""));

                            break;
                        }
                    }
                    if (i == chooseDs.Rows.Count)//找不到
                    {
                        if (!dic.ContainsKey(6))
                            dic.Add(6, "流量、扬程太大，未在选型库，无法自动选泵");
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            { }
            return dic;
        }





        //消防泵房
        private double? _BuildingFinishHeight_Fire { get; set; }
        public double? BuildingFinishHeight_Fire
        {
            get { return _BuildingFinishHeight_Fire; }
            set
            {
                _BuildingFinishHeight_Fire = value;
                this.RaisePropertyChanged();
            }
        }
        private double? _RoofHeight_Fire { get; set; }
        public double? RoofHeight_Fire
        {
            get { return _RoofHeight_Fire; }
            set
            {
                _RoofHeight_Fire = value;
                this.RaisePropertyChanged();
            }
        }
        private double? _BaseHeight_Fire { get; set; }
        public double? BaseHeight_Fire
        {
            get { return _BaseHeight_Fire; }
            set
            {
                _BaseHeight_Fire = value;
                this.RaisePropertyChanged();
            }
        }

        private double? _PoolArea_Fire { get; set; }
        public double? PoolArea_Fire
        {
            get { return _PoolArea_Fire; }
            set
            {
                _PoolArea_Fire = value;
                this.RaisePropertyChanged();
            }
        }
        private double? _Depth_Fire { get; set; }
        public double? Depth_Fire
        {
            get { return _Depth_Fire; }
            set
            {
                _Depth_Fire = value;
                this.RaisePropertyChanged();
            }
        }
        private double? _Volume_Fire { get; set; }
        public double? Volume_Fire
        {
            get { return _Volume_Fire; }
            set
            {
                _Volume_Fire = value;
                this.RaisePropertyChanged();
            }
        }
        private double? _FirePressure_Fire { get; set; }
        public double? FirePressure_Fire
        {
            get { return _FirePressure_Fire; }
            set
            {
                _FirePressure_Fire = value;
                this.RaisePropertyChanged();
            }
        }
        private double? _WaterPressure_Fire { get; set; }
        public double? WaterPressure_Fire
        {
            get { return _WaterPressure_Fire; }
            set
            {
                _WaterPressure_Fire = value;
                this.RaisePropertyChanged();
            }
        }

        private bool _IsAutoChooseFire { get; set; }//自动选泵
        public bool IsAutoChooseFire
        {
            get { return _IsAutoChooseFire; }
            set
            {
                _IsAutoChooseFire = value;
                this.RaisePropertyChanged();
            }
        }

        //消防泵房泵组信息
        private ObservableCollection<FirePumpInfo> _FirePumpInfoList { get; set; }
        public ObservableCollection<FirePumpInfo> FirePumpInfoList
        {
            get { return _FirePumpInfoList; }
            set
            {
                _FirePumpInfoList = value;
                this.RaisePropertyChanged();
            }
        }


        //生成剖面图
        public ICommand CallFirePumpCmd => new RelayCommand(CallFirePump);
        public void CallFirePump()
        {
            Dictionary<int, string> dic = isRightInputFirePump();
            if (FirePumpInfoList.Count == 0)
            {
                MessageBox.Show("消火栓组请至少输入一组数据！");

            }
            else if (dic.Count > 0)//有错误
            {
                string message = "";
                foreach (var i in dic)//汇总错误信息
                {
                    message += i.Value + "\n";
                }


                if (canContinue)//不需要终止
                {
                    MessageBoxResult result = MessageBox.Show(message, "", MessageBoxButton.OKCancel);
                    switch (result)
                    {
                        case MessageBoxResult.OK:
                            drawFirePump("生成剖面图");
                            break;

                        case MessageBoxResult.Cancel:
                            break;
                    }
                }
                else
                    MessageBox.Show(message);
            }
            else
                drawFirePump("生成剖面图");

        }

        private void drawFirePump(string name)
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThFirePumpCommon.Button_Name = name;
                using (var cmd = new ThFirePumpCmd())
                {
                    setInputFirePump();
                    cmd.Execute();
                }
            }
        }

        /// <summary>
        /// 设置输入
        /// </summary>
        private void setInputFirePump()
        {
            //泵组
            ThFirePumpCommon.Input_PumpList.Clear();
            int index = 0;
            for (int i = 0; i < FirePumpInfoList.Count; i++)
            {
                Pump_Arr p = new Pump_Arr();
                p.No = FirePumpInfoList[i].No;
                p.Flow_Info = FirePumpInfoList[i].Flow_Info ?? 0.0;
                p.Head = FirePumpInfoList[i].Head ?? 0.0;
                p.Power = FirePumpInfoList[i].Power ?? 0.0;
                p.Num = FirePumpInfoList[i].Num ?? 0;
                p.NoteSelect = FirePumpInfoList[i].NumSelect;
                p.Note = FirePumpInfoList[i].Note;
                p.Hole = FirePumpInfoList[i].Hole ?? 0.0;
                p.Type = FirePumpInfoList[i].TypeSelect;
                ThFirePumpCommon.Input_PumpList.Add(p);

                if (i != 0 && FirePumpInfoList[i].Flow_Info > FirePumpInfoList[index].Flow_Info)
                    index = i;
            }
            //ThFirePumpCommon.PumpOutletHorizontalPipeDiameterChoice = ThFirePumpCommon.PumpOutletPipeDiameterChoice = ThFirePumpCommon.PumpSuctionPipeDiameterChoice = index + 1;
            ThFirePumpCommon.choice = index + 1;
            ThFirePumpCommon.Input_Type = FirePumpInfoList[index].TypeSelect;

            //基本信息
            ThFirePumpCommon.Input_BuildingFinishHeight = (double)BuildingFinishHeight_Fire;
            ThFirePumpCommon.Input_RoofHeight = RoofHeight_Fire ?? 0;
            ThFirePumpCommon.Input_PoolArea = PoolArea_Fire ?? 0;
            ThFirePumpCommon.Input_Volume = Volume_Fire ?? 0;
            ThFirePumpCommon.Input_EffectiveDepth = Depth_Fire ?? 0;
            ThFirePumpCommon.Input_BasicHeight = BaseHeight_Fire ?? 0;
            ThFirePumpCommon.Input_FirePressure = FirePressure_Fire ?? 0;
            ThFirePumpCommon.Input_WaterPressure = WaterPressure_Fire ?? 0;

        }

        /// <summary>
        /// 检查所有是否输入正确
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, string> isRightInputFirePump()
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();
            canContinue = true;

            if (BuildingFinishHeight_Fire == null)
            {
                dic.Add(1, "请输入建筑完成面标高数据");
                canContinue = false;
            }
            else if (BuildingFinishHeight_Fire < -10)
                dic.Add(1, "根据规范要求消防水池高度不可低于-10m，是否继续？");


            if (RoofHeight_Fire == null)
            {
                dic.Add(2, "请输入顶板下标高数据");
                canContinue = false;
            }
            else if (RoofHeight_Fire <= BuildingFinishHeight_Fire)
            {
                dic.Add(2, "建筑完成面标高须小于顶板下标高");
                canContinue = false;
            }

            if (BaseHeight_Fire == null)
            {
                dic.Add(3, "请输入基础高度数据");
                canContinue = false;
            }
            else if (BaseHeight_Fire == 0)
            {
                dic.Add(3, "基础高度不可为0");
                canContinue = false;
            }


            if (PoolArea_Fire == null)
            {
                dic.Add(4, "请输入水池面积数据");
                canContinue = false;
            }
            else if (PoolArea_Fire <= 0)
            {
                dic.Add(4, "水池面积不可为0");
                canContinue = false;
            }


            if (Depth_Fire == null)
            {
                dic.Add(5, "请输入有效水深数据");
                canContinue = false;
            }
            else if (Depth_Fire <= 0)
            {
                dic.Add(5, "有效水深不可为0");
                canContinue = false;
            }


            if (Volume_Fire == null)
            {
                dic.Add(6, "请输入有效容积数据");
                canContinue = false;
            }
            else if (Volume_Fire <= 0)
            {
                dic.Add(6, "有效容积不可为0");
                canContinue = false;
            }
            else if (Volume_Fire > PoolArea_Fire * Depth_Fire)
                dic.Add(6, "当前水池尺寸不满足有效容积要求，是否继续？");

            bool isOutdoor = false;
            foreach (var i in FirePumpInfoList)
            {
                if (string.IsNullOrEmpty(i.No.Trim()))
                {
                    if (!dic.ContainsKey(7))
                    {
                        dic.Add(7, "请输入泵组编号");
                        canContinue = false;

                    }
                }
                else if (i.No.Contains("室外"))
                    isOutdoor = true;



                if (i.Flow_Info == null)
                {
                    if (!dic.ContainsKey(8))
                        dic.Add(8, "请输入流量数据");
                    canContinue = false;
                }
                else if (i.Flow_Info <= 0)
                {
                    if (!dic.ContainsKey(8))
                        dic.Add(8, "流量不可为0");
                    canContinue = false;
                }

                if (i.Head == null)
                {
                    if (!dic.ContainsKey(9))
                        dic.Add(9, "请输入扬程数据");
                    canContinue = false;
                }
                else if (i.Head <= 0)
                {
                    if (!dic.ContainsKey(9))
                        dic.Add(9, "扬程不可为0");
                    canContinue = false;
                }

                if (i.Power == null)
                {
                    if (!dic.ContainsKey(10))
                        dic.Add(10, "请输入功率数据");
                    canContinue = false;
                }
                else if (i.Power <= 0)
                {
                    if (!dic.ContainsKey(10))
                        dic.Add(10, "功率不可为0");
                    canContinue = false;
                }

                if (i.Num == null)
                {
                    if (!dic.ContainsKey(11))
                        dic.Add(11, "请输入泵数量数据");
                    canContinue = false;
                }
                else if (i.Num <= 0)
                {
                    if (!dic.ContainsKey(11))
                        dic.Add(11, "泵数量不可为0");
                    canContinue = false;
                }


                if (i.Hole == null)
                {
                    if (!dic.ContainsKey(12))
                        dic.Add(12, "请输入放气孔高度数据");
                    canContinue = false;
                }
                else if (i.Hole <= 0)
                {
                    if (!dic.ContainsKey(12))
                        dic.Add(12, "放气孔高度不可为0");
                    canContinue = false;
                }

                if (i.NumSelect == null || i.NumSelect == "")
                {
                    if (!dic.ContainsKey(13))
                        dic.Add(13, "请输入使用台数");
                    canContinue = false;
                }

                if (i.TypeSelect == null || i.TypeSelect == "")
                {
                    if (!dic.ContainsKey(14))
                        dic.Add(14, "请选择泵类型");
                    canContinue = false;
                }

            }

            if (isOutdoor && BuildingFinishHeight_Fire < -6)
            {
                if (dic.ContainsKey(1))
                    dic[1] = "根据规范要求消防水池储存室外用水时高度不可低于-6m，是否继续？";
                else
                    dic.Add(1, "根据规范要求消防水池储存室外用水时高度不可低于-6m，是否继续？");
            }

            return dic;
        }

        //生成说明
        public ICommand CallFirePumpWordCmd => new RelayCommand(CallFirePumpWord);
        public void CallFirePumpWord()
        {
            Dictionary<int, string> dic = isRightInputFirePumpWord();
            if (FirePumpInfoList.Count == 0)
            {
                MessageBox.Show("消火栓组请至少输入一组数据！");

            }
            else if (dic.Count > 0)//有错误
            {
                string message = "";
                foreach (var i in dic)//汇总错误信息
                {
                    message += i.Value + "\n";
                }


                if (canContinue)//不需要终止
                {
                    MessageBoxResult result = MessageBox.Show(message, "", MessageBoxButton.OKCancel);
                    switch (result)
                    {
                        case MessageBoxResult.OK:
                            drawFirePump("生成说明");
                            break;

                        case MessageBoxResult.Cancel:
                            break;
                    }
                }
                else
                    MessageBox.Show(message);
            }
            else
                drawFirePump("生成说明");
        }

        /// <summary>
        /// 检查涉及说明文字部分是否输入正确
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, string> isRightInputFirePumpWord()
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();
            canContinue = true;

            foreach (var i in FirePumpInfoList)
            {
                if (string.IsNullOrEmpty(i.No.Trim()))
                    if (!dic.ContainsKey(7))
                    {
                        dic.Add(7, "请输入泵组编号");
                        canContinue = false;

                    }

                if (i.Flow_Info == null)
                {
                    if (!dic.ContainsKey(8))
                        dic.Add(8, "请输入流量数据");
                    canContinue = false;
                }
                else if (i.Flow_Info <= 0)
                {
                    if (!dic.ContainsKey(8))
                        dic.Add(8, "流量不可为0");
                    canContinue = false;
                }

                if (i.Head == null)
                {
                    if (!dic.ContainsKey(9))
                        dic.Add(9, "请输入扬程数据");
                    canContinue = false;
                }
                else if (i.Head <= 0)
                {
                    if (!dic.ContainsKey(9))
                        dic.Add(9, "扬程不可为0");
                    canContinue = false;
                }

                if (i.Power == null)
                {
                    if (!dic.ContainsKey(10))
                        dic.Add(10, "请输入功率数据");
                    canContinue = false;
                }
                else if (i.Power <= 0)
                {
                    if (!dic.ContainsKey(10))
                        dic.Add(10, "功率不可为0");
                    canContinue = false;
                }

                if (i.Num == null)
                {
                    if (!dic.ContainsKey(11))
                        dic.Add(11, "请输入泵数量数据");
                    canContinue = false;
                }
                else if (i.Num <= 0)
                {
                    if (!dic.ContainsKey(11))
                        dic.Add(11, "泵数量不可为0");
                    canContinue = false;
                }


                if (i.Hole == null)
                {
                    if (!dic.ContainsKey(12))
                        dic.Add(12, "请输入放气孔高度数据");
                    canContinue = false;
                }
                else if (i.Hole <= 0)
                {
                    if (!dic.ContainsKey(12))
                        dic.Add(12, "放气孔高度不可为0");
                    canContinue = false;
                }

                if (i.NumSelect == null || i.NumSelect == "")
                {
                    if (!dic.ContainsKey(13))
                        dic.Add(13, "请输入使用台数");
                    canContinue = false;
                }

                if (i.TypeSelect == null || i.TypeSelect == "")
                {
                    if (!dic.ContainsKey(14))
                        dic.Add(14, "请选择泵类型");
                    canContinue = false;
                }

            }

            return dic;
        }

        //生成材料
        public ICommand CallFirePumpGraphCmd => new RelayCommand(CallFirePumpGraph);
        public void CallFirePumpGraph()
        {
            Dictionary<int, string> dic = isRightInputFirePumpGraph();
            if (FirePumpInfoList.Count == 0)
            {
                MessageBox.Show("消火栓组请至少输入一组数据！");

            }
            else if (dic.Count > 0)//有错误
            {
                string message = "";
                foreach (var i in dic)//汇总错误信息
                {
                    message += i.Value + "\n";
                }


                if (canContinue)//不需要终止
                {
                    MessageBoxResult result = MessageBox.Show(message, "", MessageBoxButton.OKCancel);
                    switch (result)
                    {
                        case MessageBoxResult.OK:
                            drawFirePump("生成材料表");
                            break;

                        case MessageBoxResult.Cancel:
                            break;
                    }
                }
                else
                    MessageBox.Show(message);
            }
            else
                drawFirePump("生成材料表");
        }

        /// <summary>
        /// 检查涉及材料表部分是否输入正确
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, string> isRightInputFirePumpGraph()
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();
            canContinue = true;

            if (PoolArea_Fire == null)
            {
                dic.Add(4, "请输入水池面积数据");
                canContinue = false;
            }
            else if (PoolArea_Fire <= 0)
            {
                dic.Add(4, "水池面积不可为0");
                canContinue = false;
            }


            if (Depth_Fire == null)
            {
                dic.Add(5, "请输入有效水深数据");
                canContinue = false;
            }
            else if (Depth_Fire <= 0)
            {
                dic.Add(5, "有效水深不可为0");
                canContinue = false;
            }


            if (Volume_Fire == null)
            {
                dic.Add(6, "请输入有效容积数据");
                canContinue = false;
            }
            else if (Volume_Fire <= 0)
            {
                dic.Add(6, "有效容积不可为0");
                canContinue = false;
            }
            else if (Volume_Fire > PoolArea_Fire * Depth_Fire)
                dic.Add(6, "当前水池尺寸不满足有效容积要求，是否继续？");


            foreach (var i in FirePumpInfoList)
            {
                if (string.IsNullOrEmpty(i.No.Trim()))
                    if (!dic.ContainsKey(7))
                    {
                        dic.Add(7, "请输入泵组编号");
                        canContinue = false;

                    }

                if (i.Flow_Info == null)
                {
                    if (!dic.ContainsKey(8))
                        dic.Add(8, "请输入流量数据");
                    canContinue = false;
                }
                else if (i.Flow_Info <= 0)
                {
                    if (!dic.ContainsKey(8))
                        dic.Add(8, "流量不可为0");
                    canContinue = false;
                }

                if (i.Head == null)
                {
                    if (!dic.ContainsKey(9))
                        dic.Add(9, "请输入扬程数据");
                    canContinue = false;
                }
                else if (i.Head <= 0)
                {
                    if (!dic.ContainsKey(9))
                        dic.Add(9, "扬程不可为0");
                    canContinue = false;
                }

                if (i.Power == null)
                {
                    if (!dic.ContainsKey(10))
                        dic.Add(10, "请输入功率数据");
                    canContinue = false;
                }
                else if (i.Power <= 0)
                {
                    if (!dic.ContainsKey(10))
                        dic.Add(10, "功率不可为0");
                    canContinue = false;
                }

                if (i.Num == null)
                {
                    if (!dic.ContainsKey(11))
                        dic.Add(11, "请输入泵数量数据");
                    canContinue = false;
                }
                else if (i.Num <= 0)
                {
                    if (!dic.ContainsKey(11))
                        dic.Add(11, "泵数量不可为0");
                    canContinue = false;
                }


                if (i.Hole == null)
                {
                    if (!dic.ContainsKey(12))
                        dic.Add(12, "请输入放气孔高度数据");
                    canContinue = false;
                }
                else if (i.Hole <= 0)
                {
                    if (!dic.ContainsKey(12))
                        dic.Add(12, "放气孔高度不可为0");
                    canContinue = false;
                }

                if (i.NumSelect == null || i.NumSelect == "")
                {
                    if (!dic.ContainsKey(13))
                        dic.Add(13, "请输入使用台数");
                    canContinue = false;
                }

                if (i.TypeSelect == null || i.TypeSelect == "")
                {
                    if (!dic.ContainsKey(14))
                        dic.Add(14, "请选择泵类型");
                    canContinue = false;
                }

            }

            return dic;

        }

        private int FirePumpNum { set; get; }//消防泵房组编号
        public ICommand btnAddFire_Cmd => new RelayCommand(btnAddFire);
        public void btnAddFire()
        {
            FirePumpInfo info = new FirePumpInfo();   //我自己的数据表实例类
            info.CheckNo = ++FirePumpNum;
            info.No = "消火栓泵组" + FirePumpNum;
            FirePumpInfoList.Add(info);
        }

        public ICommand btnDelFire_Cmd => new RelayCommand(btnDelFire);
        public void btnDelFire()
        {
            if (FirePumpInfoList.Count == 0)
                return;
            FirePumpInfoList.RemoveAt(FirePumpInfoList.Count - 1);
        }

        /// <summary>
        /// 自动选泵-消防泵房 
        /// </summary>
        public ICommand AutoChooseFire_Cmd => new RelayCommand(AutoChooseFire);
        public void AutoChooseFire()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThFirePumpCommon.Button_Name = "自动选泵";
                using (var cmd = new ThFirePumpCmd())
                {
                    cmd.Execute();//只进入统计而已
                }
            }

            string path = ThCADCommon.FirePumpDataTablePath();
            //string path = "111";

            var errMsg = ReadFileDataFire(path);
            if (errMsg.Count > 0)
            {
                string message = "";
                foreach (var i in errMsg)
                {
                    message += i.Value + "\n";
                }
                MessageBox.Show(message);
            }



        }

        /// <summary>
        /// 读取消防泵房excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        private Dictionary<int, string> ReadFileDataFire(string filePath, bool isDefault = false)
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();

            //防止当前文档正在打开中，解析会报错，将Excel复制到临时位置进行堆区
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                dic.Add(1, "文件路径为空或文件不存在");
                return dic;
            }

            try
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                ExcelHelper excelService = new ExcelHelper();
                var dataSet = excelService.ReadExcelToDataSet(filePath);
                if (dataSet == null)
                {
                    dic.Add(2, "excel不存在");
                    return dic;
                }

                for (int j = 0; j < FirePumpInfoList.Count; j++)
                {
                    string tableName;
                    if (FirePumpInfoList[j].TypeSelect == "立式泵" || FirePumpInfoList[j].TypeSelect == "卧式泵")
                        tableName = FirePumpInfoList[j].TypeSelect + "功率选择";
                    else
                    {
                        if (!dic.ContainsKey(3))
                            dic.Add(3, "泵类型不在选型库");
                        continue;
                    }

                    if (FirePumpInfoList[j].Flow_Info == null || FirePumpInfoList[j].Flow_Info <= 0)
                    {
                        if (!dic.ContainsKey(4))
                            dic.Add(4, "流量太小未在选型库，无法自动选泵");
                        continue;
                    }
                    if (FirePumpInfoList[j].Head == null || FirePumpInfoList[j].Head <= 0)
                    {
                        if (!dic.ContainsKey(5))
                            dic.Add(5, "扬程太小未在选型库，无法自动选泵");
                        continue;
                    }

                    //查表
                    var chooseDs = dataSet.Tables[tableName];
                    double flow = (double)FirePumpInfoList[j].Flow_Info;
                    double head = (double)FirePumpInfoList[j].Head / 100.0;
                    int i = 1;

                    for (; i < chooseDs.Rows.Count; i++)
                    {
                        var row = chooseDs.Rows[i] as System.Data.DataRow;
                        if (double.Parse(((string)row[2]).Replace(" ", "")) >= flow && double.Parse(((string)row[3]).Replace(" ", "")) >= head)
                        {

                            FirePumpInfoList[j].Power = double.Parse(((string)row[4]).Replace(" ", ""));
                            FirePumpInfoList[j].Hole = double.Parse(((string)row[5]).Replace(" ", ""));
                            break;
                        }

                    }
                    if (i == chooseDs.Rows.Count)//找不到
                    {
                        if (!dic.ContainsKey(6))
                            dic.Add(6, "流量、扬程太大，未在选型库，无法自动选泵");
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            { }
            return dic;
        }
    }


    //生活水箱基础信息
    public class LifeBaseInfo
    {
        public LifeBaseInfo()
        {
            No = "";
            Length = 0.0;
            Width = 0.0;
            Height = 0.0;
            Volume = 0.0;
            Note = "";
            Num = 0;
        }
        public int? CheckNo { set; get; }//编号
        public string No { set; get; }//编号
        public double? Length { set; get; } //长
        public double? Width { set; get; }//宽
        public double? Height { set; get; }//高
        public double? Volume { set; get; } //容积
        public string Note { set; get; }//备注
        public int? Num { set; get; }//数量
    }

    //生活水箱泵组信息
    public class LifePumpInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public LifePumpInfo()
        {
            No = "";
            Flow_Info = 0.0;
            Head = 0.0;
            Power = 0.0;
            Num = 0;
            Note = "";
            NoteSelect = "";
            NoteList = new ObservableCollection<string>();
        }



        private int? _CheckNo;//数据真实编号
        public int? CheckNo
        {
            get { return _CheckNo; }
            set { _CheckNo = value; NotifyPropertyChanged(); }
        }

        private string _No;//名称
        public string No
        {
            get { return _No; }
            set { _No = value; NotifyPropertyChanged(); }
        }


        private double? _Flow_Info;//信息流量
        public double? Flow_Info
        {
            get { return _Flow_Info; }
            set { _Flow_Info = value; NotifyPropertyChanged(); }
        }

        private double? _Head;//扬程
        public double? Head
        {
            get { return _Head; }
            set { _Head = value; NotifyPropertyChanged(); }
        }

        private double? _Power;//功率
        public double? Power
        {
            get { return _Power; }
            set { _Power = value; NotifyPropertyChanged(); }
        }
        //public int Num { set; get; }  
        private int? _Num;//泵数量
        public int? Num
        {
            get { return _Num; }
            set { _Num = value; NotifyPropertyChanged(); NotifyPropertyChanged("NoteList"); }
        }

        private string _Note;//备注
        public string Note
        {
            get { return _Note; }
            set { _Note = value; NotifyPropertyChanged(); }
        }

        private string _NoteSelect;
        public string NoteSelect//下拉备注
        {
            get { return _NoteSelect; }
            set { _NoteSelect = value; NotifyPropertyChanged(); }
        }

        private ObservableCollection<string> _NoteList { set; get; }//下拉备注
        public ObservableCollection<string> NoteList
        {
            set { _NoteList = value; NotifyPropertyChanged(); }
            get
            {
                if (_NoteList.Count != 0)
                    _NoteList.Clear();
                if (Num > 1)
                {
                    string s1 = getCountRefundInfoInChanese((Num - 1).ToString());
                    string s2 = getCountRefundInfoInChanese(Num.ToString());


                    _NoteList.Add(s2 + "用");
                    _NoteList.Add(s1 + "用一备");
                }
                else if (Num == 1)
                {

                    _NoteList.Add("一用");
                }

                return _NoteList;
            }
        }

        //数字转换为中文
        private string getCountRefundInfoInChanese(string inputNum)
        {
            string[] intArr = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", };
            string[] strArr = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", };
            string[] Chinese = { "", "十", "百", "千", "万", "十", "百", "千", "亿" };
            //金额
            //string[] Chinese = { "元", "十", "百", "千", "万", "十", "百", "千", "亿" };
            char[] tmpArr = inputNum.ToString().ToArray();
            string tmpVal = "";
            for (int i = 0; i < tmpArr.Length; i++)
            {
                tmpVal += strArr[tmpArr[i] - 48];//ASCII编码 0为48
                tmpVal += Chinese[tmpArr.Length - 1 - i];//根据对应的位数插入对应的单位
            }

            return tmpVal;
        }
    }


    //生活水箱泵组信息
    /*public class LifePumpInfo : NotifyPropertyChangedBase
    {
        public LifePumpInfo()
        {
            No = "";
            Flow_Info = 0.0;
            Head = 0.0;
            Power = 0.0;
            Num = 0;
            Note = "";
            NoteSelect = "";
            NoteList = new ObservableCollection<string>();
        }
        public int? CheckNo { set; get; }//数据真实编号
        public string No{ set; get; }//名称
        public double? Flow_Info { set; get; }//信息流量
        public double? Head { set; get; }//扬程
        private double? _Power { set; get; }
        public double? Power { set { this._Power = value; this.RaisePropertyChanged(); } get { return this._Power; } }//功率
        public int? Num { set; get; }  //泵数量
        public string Note { set; get; }//自定义备注
        public string NoteSelect { set; get; }//下拉备注
      
        public ObservableCollection<string> NoteList { set; get; }//下拉备注选项框
        
    }
    */


    //消防泵房泵组信息
    /*public class FirePumpInfo : NotifyPropertyChangedBase
    {
        public FirePumpInfo()
        {
            No = "";
            Flow_Info = 0.0;
            Head = 0.0;
            Power = 0.0;
            Num = 0;
            Hole = 0;
            NumSelect = "";
            NumList = new ObservableCollection<string>();
            TypeSelect = "";
            TypeList = new ObservableCollection<string>() { "立式泵", "卧式泵" };
            Note = "";
        }
        public int CheckNo { set; get; }//数据真实编号
        public string No { set; get; }//泵组编号
        public double? Flow_Info { set; get; }//信息流量
        public double? Head { set; get; }//扬程
        private double? _Power { set; get; }
        public double? Power { set { this._Power = value; this.RaisePropertyChanged(); } get {return this._Power; } }//功率
        public int? Num { set; get; }  //泵数量
        public double? _Hole { set; get; }
        public double? Hole { set { this._Hole = value; this.RaisePropertyChanged(); } get { return this._Hole; } }  //放气孔高度

        public string NumSelect { set; get; }//使用台数
        public ObservableCollection<string> NumList { set; get; }//使用台数选项框

        public string TypeSelect { set; get; }//泵类型
        public ObservableCollection<string> TypeList { set; get; }//泵类型选项框

        public string Note { set; get; }//备注


    }
    */

    //消防泵房泵组信息
    public class FirePumpInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public FirePumpInfo()
        {
            No = "";
            Flow_Info = 0.0;
            Head = 0.0;
            Power = 0.0;
            Num = 0;
            Hole = 0;
            NumSelect = "";
            NumList = new ObservableCollection<string>();
            TypeSelect = "";
            TypeList = new ObservableCollection<string>() { "立式泵", "卧式泵" };
            Note = "";
        }

        private int? _CheckNo;//数据真实编号
        public int? CheckNo
        {
            get { return _CheckNo; }
            set { _CheckNo = value; NotifyPropertyChanged(); }
        }

        private string _No { set; get; }//泵组编号
        public string No
        {
            get { return _No; }
            set { _No = value; NotifyPropertyChanged(); }
        }

        private double? _Flow_Info { set; get; }//信息流量
        public double? Flow_Info
        {
            get { return _Flow_Info; }
            set { _Flow_Info = value; NotifyPropertyChanged(); }
        }

        private double? _Head { set; get; }//扬程
        public double? Head
        {
            get { return _Head; }
            set { _Head = value; NotifyPropertyChanged(); }
        }

        private double? _Power { set; get; }//功率
        public double? Power
        {
            set { this._Power = value; NotifyPropertyChanged(); }
            get { return this._Power; }
        }

        private int? _Num;//泵数量
        public int? Num
        {
            get { return _Num; }
            set { _Num = value; NotifyPropertyChanged(); NotifyPropertyChanged("NumList"); }
        }

        private double? _Hole { set; get; } //放气孔高度
        public double? Hole
        {
            set { this._Hole = value; NotifyPropertyChanged(); }
            get { return this._Hole; }
        }

        private string _NumSelect { set; get; }//使用台数
        public string NumSelect
        {
            get { return _NumSelect; }
            set { _NumSelect = value; NotifyPropertyChanged(); }
        }

        private ObservableCollection<string> _NumList { set; get; }//使用台数选项框
        public ObservableCollection<string> NumList
        {
            set { _NumList = value; NotifyPropertyChanged(); }
            get
            {
                if (_NumList.Count != 0)
                    _NumList.Clear();
                if (Num > 1)
                {
                    string s1 = getCountRefundInfoInChanese((Num - 1).ToString());
                    string s2 = getCountRefundInfoInChanese(Num.ToString());


                    _NumList.Add(s2 + "用");
                    _NumList.Add(s1 + "用一备");
                }
                else if (Num == 1)
                {

                    _NumList.Add("一用");
                }

                return _NumList;
            }
        }

        private string _TypeSelect { set; get; }//泵类型
        public string TypeSelect
        {
            get { return _TypeSelect; }
            set { _TypeSelect = value; NotifyPropertyChanged(); }
        }

        private ObservableCollection<string> _TypeList { set; get; }//泵类型选项框
        public ObservableCollection<string> TypeList
        {
            get { return _TypeList; }
            set { _TypeList = value; NotifyPropertyChanged(); }
        }

        private string _Note { set; get; }//备注
        public string Note
        {
            get { return _Note; }
            set { _Note = value; NotifyPropertyChanged(); }
        }


        //数字转换为中文
        private string getCountRefundInfoInChanese(string inputNum)
        {
            string[] intArr = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", };
            string[] strArr = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", };
            string[] Chinese = { "", "十", "百", "千", "万", "十", "百", "千", "亿" };
            //金额
            //string[] Chinese = { "元", "十", "百", "千", "万", "十", "百", "千", "亿" };
            char[] tmpArr = inputNum.ToString().ToArray();
            string tmpVal = "";
            for (int i = 0; i < tmpArr.Length; i++)
            {
                tmpVal += strArr[tmpArr[i] - 48];//ASCII编码 0为48
                tmpVal += Chinese[tmpArr.Length - 1 - i];//根据对应的位数插入对应的单位
            }

            return tmpVal;
        }
    }
}
