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
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    public partial class ThPDSCreateLoad : Window
    {
        public ThPDSCreateLoad()
        {
            InitializeComponent();
            defaultKV.SelectedItem = "0.38";
            defaultKV.ItemsSource = new List<string>() { "0.38", "0.22" };
        }

        private void btnInsert(object sender, RoutedEventArgs e)
        {
            new ThPDSUpdateToDwgService().AddLoadDimension(ThPDSProjectGraphService.CreatNewLoad(/*defaultKV: double.Parse(defaultKV.SelectedItem.ToString()), */defaultLoadID: defaultLoadID.Text, defaultPower: double.Parse(defaultPower.Text), defaultDescription: defaultDescription.Text, defaultFireLoad: defaultFireLoad.IsChecked == true));
            Close();
        }

        private void btnSave(object sender, RoutedEventArgs e)
        {
            Project.PDSProjectVM.Instance.InformationMatchViewModel.Graph.AddVertex(ThPDSProjectGraphService.CreatNewLoad(/*defaultKV: double.Parse(defaultKV.SelectedItem.ToString()), */defaultLoadID: defaultLoadID.Text, defaultPower: double.Parse(defaultPower.Text), defaultDescription: defaultDescription.Text, defaultFireLoad: defaultFireLoad.IsChecked == true));
            Close();
        }
    }
}
