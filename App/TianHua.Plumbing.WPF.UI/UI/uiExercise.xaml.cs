using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ThControlLibraryWPF;
using ThControlLibraryWPF.ControlUtils;
using ThControlLibraryWPF.CustomControl;
using ThMEPWSS.Command;
using ThMEPWSS.Common;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Model;
using ThMEPWSS.Service;
using ThMEPWSS.ViewModel;

namespace TianHua.Plumbing.WPF.UI.UI
{
    /// <summary>
    /// uiDrainageSystemAboveGround.xaml 的交互逻辑
    /// </summary>
    public partial class uiExercise: ThCustomWindow
    {
        static ExerciseViewmodel Viewmodel = new ExerciseViewmodel();
        bool _createFrame = false;
        public uiExercise()
        {
            InitializeComponent();
        }

        private void btnLayoutPipe_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                ExerciseCommand exerciseCommand = new ExerciseCommand(Viewmodel);
                exerciseCommand.Execute();
            }
            catch (Exception ex)
            {
                ;
            }
        }

        private void btnReadFloor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Viewmodel.InitListDatas();
            }
            catch { }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

}
