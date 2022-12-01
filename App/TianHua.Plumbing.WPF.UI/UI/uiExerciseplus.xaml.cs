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
    //public partial class uiExercise: ThCustomWindow
    //{
    //    public static ExerciseViewmodel Viewmodel = null;
    //    bool _createFrame = false;
    //    public uiExercise()
    //    {
    //        if (Viewmodel == null)
    //        {
    //            Viewmodel = new ExerciseViewmodel();
    //        }
    //        DataContext = Viewmodel;
    //        InitializeComponent();
    //    }

    //    private void btnLayoutPipe_Click(object sender, RoutedEventArgs e)
    //    {

    //        try
    //        {
    //            ExerciseCommand exerciseCommand = new ExerciseCommand(Viewmodel);
    //            exerciseCommand.Execute();
    //        }
    //        catch (Exception ex)
    //        {
    //            ;
    //        }
    //    }

    //    private void btnReadFloor_Click(object sender, RoutedEventArgs e)
    //    {
    //        try
    //        {
    //            Viewmodel.InitListDatas();
    //        }
    //        catch { }
    //    }

    //    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //    {

    //    }
    //}
   
    public partial class uiExercisePlus : ThCustomWindow
    { 
        public static ExerciseViewmodel ViewmodelPlus { get; set; }
        bool _CreateFramePlus = false;
        public uiExercisePlus()
        {
            if (ViewmodelPlus == null)
            {
                ViewmodelPlus = new ExerciseViewmodel();
            }
            DataContext = ViewmodelPlus;
            InitializeComponent();

        }

        private void btnLayoutPipe_Click(object sender, RoutedEventArgs e)
        {

                try
                {
                    ExerciseCommand exerciseCommand = new ExerciseCommand(ViewmodelPlus);
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
                    ViewmodelPlus.InitListDatas();
                }
                catch { }
            }

            private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {

            }
    }


}
