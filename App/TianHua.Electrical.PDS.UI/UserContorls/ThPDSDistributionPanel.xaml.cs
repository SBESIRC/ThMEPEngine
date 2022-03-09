﻿using QuickGraph;
using System.Windows;
using System.Windows.Controls;
using TianHua.Electrical.PDS.UI.ViewModels;
using TianHua.Electrical.PDS.Project.Module;
using System.Windows.Data;
using System.ComponentModel;
using System;
using System.Linq;
using TianHua.Electrical.PDS.UI.Models;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    public partial class ThPDSDistributionPanel : UserControl
    {
        public WpfServices.ThPDSDistributionPanelService Service = new();
        public ThPDSDistributionPanel()
        {
            InitializeComponent();
            if (Service == null)
            {
                this.Loaded += ThPDSDistributionPanel_Loaded;
                this.tv.SelectedItemChanged += Tv_SelectedItemChanged;
            }
            else
            {
                Service.Panel = this;
                Service.TreeView = tv;
                Service.Canvas = canvas;
                Service.propertyGrid = propertyGrid;
                Service.Graph = Graph;
                Service.Init();
            }
        }
        ThPDSComponentGraph graph = new ThPDSComponentGraph();

        private void Tv_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdatePropertyGrid(this.tv.SelectedItem);
            UpdateCanvas();
        }

        private void ThPDSDistributionPanel_Loaded(object sender, RoutedEventArgs e)
        {
            graph.Build(Graph);
            var builder = new ThPDSCircuitGraphTreeBuilder();
            this.tv.DataContext = builder.Build(Graph);
            if (new Services.ThPDSCircuitGraphComponentGenerator().IN(null) is null)
            {
                UpdateCanvas();
            }
        }

        private void UpdateCanvas()
        {
            if (this.tv.SelectedItem is not ThPDSCircuitGraphTreeModel sel) return;
            //var left = new TianHua.Electrical.PDS.UI.Services.ThPDSCircuitGraphComponentGenerator().ConvertToString(Graph.Vertices.ToList()[sel.Id].nodeDetails.CircuitFormType) ?? "一路进线";
            var left = ThCADExtension.ThEnumExtension.GetDescription(Graph.Vertices.ToList()[sel.Id].Details.CircuitFormType.CircuitFormType) ?? "1路进线";
            var v = Graph.Vertices.ToList()[sel.Id];
            //var rights = Graph.Vertices.Select(v => new TianHua.Electrical.PDS.UI.Services.ThPDSCircuitGraphComponentGenerator().ConvertToString(v.nodeDetails.CircuitFormType) ?? "常规");
            //var rights = Graph.Vertices.Select(v => v.nodeDetails?.CircuitFormType ?? "常规");
            var rights = Graph.Edges.Where(eg => eg.Source == Graph.Vertices.ToList()[sel.Id]).Select(eg => ThCADExtension.ThEnumExtension.GetDescription(eg.Details.CircuitForm.CircuitFormType) ?? "常规").Select(x => x.Replace("(", "（").Replace(")", "）")).ToList();
            var rd = new ThPDSCircuitGraphWpfRenderer() { Left = left, Rights = rights, PDSBlockInfos = Services.ThPDSCircuitGraphComponentGenerator.PDSBlockInfos };
            rd.Render(Graph, Graph.Vertices.FirstOrDefault(), new ThPDSCircuitWpfGraphRenderContext() { Canvas = canvas, });
        }

        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph { get; set; }
        private void btnBalance(object sender, RoutedEventArgs e)
        {
        }
        public void UpdatePropertyGrid(object vm)
        {
            if (vm is null)
            {
                propertyGrid.Tag = null;
                propertyGrid.Content = null;
                return;
            }
            var gh = ConvertObjToUi(vm);
            propertyGrid.Tag = vm;
            propertyGrid.Content = gh.Grid;
        }
        private void btnSelectFast(object sender, RoutedEventArgs e)
        {
        }
        private void btnGenMulti(object sender, RoutedEventArgs e)
        {
        }
        private void btnGenSingle(object sender, RoutedEventArgs e)
        {
        }
        public static ThPDSCircuitGraphLayoutEngine ConvertObjToUi(object obj)
        {
            var gh = new ThPDSCircuitGraphLayoutEngine();
            gh.AddColDef_ByPixel(80);
            gh.AddColDef_ByPixel(90);
            foreach (var p in obj.GetType().GetProperties())
            {
                gh.AddRowDef();
                var name = p.GetCustomAttributes(typeof(DisplayNameAttribute), false).OfType<DisplayNameAttribute>().FirstOrDefault()?.DisplayName ?? p.Name;
                gh.Add(new TextBlock() { Text = name, HorizontalAlignment = HorizontalAlignment.Center, });
                if (p.PropertyType == typeof(string))
                {
                    var tbx = new TextBox() { };
                    var bd = new Binding() { Path = new PropertyPath(p.Name), Source = obj, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                    tbx.SetBinding(TextBox.TextProperty, bd);
                    gh.Add(tbx);
                }
                else if (p.PropertyType.IsEnum)
                {
                    var cbx = new ComboBox();
                    cbx.ItemsSource = Enum.GetValues(p.PropertyType);
                    var bd = new Binding() { Path = new PropertyPath(p.Name), Source = obj, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                    cbx.SetBinding(ComboBox.SelectedItemProperty, bd);
                    gh.Add(cbx);
                }
                else if (p.PropertyType == typeof(bool))
                {
                    var cbx = new CheckBox();
                    var bd = new Binding() { Path = new PropertyPath(p.Name), Source = obj, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                    cbx.SetBinding(CheckBox.IsCheckedProperty, bd);
                    gh.Add(cbx);
                }
                else if (p.PropertyType == typeof(int) || p.PropertyType == typeof(long) || p.PropertyType == typeof(float) || p.PropertyType == typeof(double) || p.PropertyType == typeof(decimal))
                {
                    var tbx = new TextBox() { };
                    var bd = new Binding() { Path = new PropertyPath(p.Name), Source = obj, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, };
                    tbx.SetBinding(TextBox.TextProperty, bd);
                    gh.Add(tbx);
                }
                else
                {
                    gh.Add(new TextBox() { Text = p.GetValue(obj)?.ToString(), });
                }
                gh.MoveToNextRow();
            }
            return gh;
        }

    }
}