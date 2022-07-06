using System.Windows.Controls;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.UI.Services;

namespace TianHua.Electrical.PDS.UI.UserContorls
{
    /// <summary>
    /// ThPDSInfoCompare.xaml 的交互逻辑
    /// </summary>
    public partial class ThPDSInfoCompare : UserControl
    {
        private readonly ThPDSInfoCompareService Service = new();
        public ThPDSInfoCompare()
        {
            InitializeComponent();
            Service.Init(this);
            PDSProject.Instance.DataChanged += () =>
            {
                Service.UpdateView(this);
            };
        }
    }
}
