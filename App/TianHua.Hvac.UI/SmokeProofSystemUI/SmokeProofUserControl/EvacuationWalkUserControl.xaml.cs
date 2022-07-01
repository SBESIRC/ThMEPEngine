﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TianHua.Hvac.UI.SmokeProofSystemUI.ViewModels;

namespace TianHua.Hvac.UI.SmokeProofSystemUI.SmokeProofUserControl
{
    /// <summary>
    /// EvacuationWalkUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class EvacuationWalkUserControl : UserControl
    {
        EvacuationWalkViewModel evacuationWalkViewModel;
        public EvacuationWalkUserControl()
        {
            InitData();
            this.DataContext = evacuationWalkViewModel;
            InitializeComponent();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void InitData()
        {
            if (evacuationWalkViewModel == null)
            {
                evacuationWalkViewModel = new EvacuationWalkViewModel();
            }
        }
    }
}