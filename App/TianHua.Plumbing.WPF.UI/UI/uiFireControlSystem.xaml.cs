using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiFireControlSystem.xaml 的交互逻辑
    /// </summary>
    public partial class uiFireControlSystem : ThCustomWindow
    {
        static FireControlSystemDiagramViewModel vm ;

        public uiFireControlSystem()
        {
            InitializeComponent();
            if (null == vm)
                vm = new FireControlSystemDiagramViewModel();
            this.DataContext = vm;
        }
        private List<string> CheckFloorInput() 
        {
            var errorMsgs = new List<string>();
            bool haveBreak = false;
            var zoneLists = vm.ZoneConfigs.ToList();
            for (int i = 0; i < zoneLists.Count; i++) 
            {
                var zone = zoneLists[i];
                bool thisHaveNull = string.IsNullOrEmpty(zone.StartFloor) || string.IsNullOrEmpty(zone.EndFloor);
                if (!zone.IsEffective())
                {
                    errorMsgs.Add(string.Format("第{0}个分区起始值同时有数据，或同时为空,且起始层不能大于结束层", zone.ZoneID));
                }
                else if (thisHaveNull)
                {
                    haveBreak = true;
                    if (zone.ZoneID == 1)
                    {
                        errorMsgs.Add(string.Format("第1个分区必须要有起始值"));
                    }
                }
                else 
                {
                    //判断数据是否符合要求
                    if (i == 0)
                        continue;
                    if (haveBreak)
                    {
                        errorMsgs.Add("分区信息中，请不要跨行输入，请调整后在进行后续操作");
                        haveBreak = false;
                        continue;
                    }

                    var preZone = zoneLists[i - 1];
                    if (preZone.IsEffective()) 
                    {
                        int? end = preZone.GetIntEndFloor();
                        if (null != end && end > zone.GetIntStartFloor())
                        {
                            errorMsgs.Add(string.Format("第{0}个分区的起始层要大于等于上一分区的结束层", zone.ZoneID));
                        }
                        else if (null != end && Math.Abs(end.Value - zone.GetIntStartFloor().Value) > 1) 
                        {
                            errorMsgs.Add(string.Format("第{0}个分区的起始层出现了断层的楼层，该开始楼层，等于上一楼层的结束，或为上一楼层的下一个楼层", zone.ZoneID));
                        }
                    }
                }
            }
            return errorMsgs;
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            //输入框数据校验
            if (!base.CheckInputData())
            {
                MessageBox.Show("输入的数据有错误，请检查输入后在进行后续操作", "天华-提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try 
            {
                FormUtil.DisableForm(gridForm);
                var errorList = CheckFloorInput();
                if (null != errorList && errorList.Count > 0)
                {
                    string showMsg = "";
                    errorList.ForEach(c => showMsg += c + "\n");
                    MessageBox.Show(showMsg, "天华-警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                //避难层消火栓数量>=普通层的消火栓数量
                if (vm.CountsGeneral > vm.CountsRefuge)
                {
                    vm.CountsRefuge = vm.CountsGeneral;
                }
                ThMEPWSS.Common.Utils.FocusToCAD();
                var cmd = new ThFireControlSystemDiagramCmd(vm);
                cmd.Execute();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "天华-错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally 
            {
                FormUtil.EnableForm(gridForm);
            }
            
        }

        private void btnHeights_Click(object sender, RoutedEventArgs e)
        {
            FloorHeightSettingWindow.ShowModelSingletonWindow();
        }
    }
}
