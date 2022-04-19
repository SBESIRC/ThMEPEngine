using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ThCADExtension;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.UI.UserContorls;
namespace TianHua.Electrical.PDS.UI.Services
{
    public static class PDSColorBrushes
    {
        public static readonly SolidColorBrush Servere = new((Color)ColorConverter.ConvertFromString("#E57373"));
        public static readonly SolidColorBrush Moderate = new((Color)ColorConverter.ConvertFromString("#FFBF07"));
        public static readonly SolidColorBrush Mild = new((Color)ColorConverter.ConvertFromString("#4DD0E2"));
        public static readonly SolidColorBrush Safe = new((Color)ColorConverter.ConvertFromString("#8AC348"));
    }
    public class ThPDSInfoCompareService
    {
        public void Init(ThPDSInfoCompare panel)
        {
            var g = Project.PDSProjectVM.Instance.InformationMatchViewModel.Graph;
            panel.btnRefresh.Click += (s, e) =>
            {
                {
                    var info = new CircuitDiffInfo() { Items = new(), };
                    foreach (var edge in g.Edges)
                    {
                        info.Items.Add(new()
                        {
                            CircuitId = edge.Circuit.ID.CircuitNumber.LastOrDefault(),
                            CircuitType = edge.Target.Load.CircuitType.GetDescription(),
                            ParentBox = edge.Circuit.ID.SourcePanelID.LastOrDefault(),
                            Dwg = edge.Circuit.Location?.ReferenceDWG,
                        });
                    }
                    panel.dg1.DataContext = info;
                }
                {
                    var info = new LoadDiffInfo() { Items = new(), };
                    foreach (var node in g.Vertices)
                    {
                        info.Items.Add(new()
                        {
                            LoadId = node.Load.ID.LoadID,
                            LoadType = node.Load.LoadTypeCat_1.ToString(),
                            LoadPower = node.Details.LowPower.ToString(),
                            Dwg = node.Load.Location?.ReferenceDWG,
                        });
                    }
                    panel.dg2.DataContext = info;
                }
            };
        }
    }
    public class CircuitDiffInfo
    {
        public List<CircuitDiffItem> Items { get; set; }
    }
    public class LoadDiffInfo
    {
        public List<LoadDiffItem> Items { get; set; }
    }
    public class CircuitDiffItem
    {
        public string CircuitId { get; set; }
        public string CircuitType { get; set; }
        public string ParentBox { get; set; }
        public string Dwg { get; set; }
        public string Note { get; set; }
    }
    public class LoadDiffItem
    {
        public string LoadId { get; set; }
        public string LoadType { get; set; }
        public string LoadPower { get; set; }
        public string Dwg { get; set; }
        public string Note { get; set; }
    }
}