using System;
using System.Windows;
using System.Windows.Controls;
using ThMEPHVAC.LoadCalculation.Model;
using ThControlLibraryWPF.CustomControl;
using ThMEPHVAC.LoadCalculation.Service;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.Hvac.UI.LoadCalculation.UI
{
    /// <summary>
    /// ColdNormConfig.xaml 的交互逻辑
    /// </summary>
    public partial class OutdoorParameterSetting : ThCustomWindow
    {
        OutdoorParameterData rowData;
        ModelDataDbSourceService dbSourceService;
        public OutdoorParameterSetting()
        {
            InitializeComponent();
            dbSourceService = new ModelDataDbSourceService();
            using (Linq2Acad.AcadDatabase acad=Linq2Acad.AcadDatabase.Active())
            {
                dbSourceService.Load(acad.Database);
                rowData = dbSourceService.dataModel;
            }
            OutdoorParameterTable.Items.Add(rowData);
            CityCmb.SelectedIndex = rowData.SelectIndex;
        }

        private void CancleButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            using (Linq2Acad.AcadDatabase acad = Linq2Acad.AcadDatabase.Active())
            using (var doclock = AcApp.DocumentManager.MdiActiveDocument.LockDocument())
            {
                dbSourceService.Save(acad.Database);
            }
            this.Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!CityCmb.IsNull() && !OutdoorParameterTable.IsNull())
            {
                rowData.SelectIndex = CityCmb.SelectedIndex;
            }
        }
    }
    }
