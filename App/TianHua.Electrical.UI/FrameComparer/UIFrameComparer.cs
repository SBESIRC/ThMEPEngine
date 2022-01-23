using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThMEPEngineCore.Algorithm;

namespace TianHua.Electrical.UI.FrameComparer
{
    public partial class UIFrameComparer : Form
    {
        public UIFrameComparer(ThMEPFrameComparer comp)
        {
            InitializeComponent();
            DoAddChangeFrame(comp.AppendedFrame.ToCollection(), "新增区域");
            DoAddChangeFrame(comp.ErasedFrame, "删除区域");
            DoAddChangeFrame(comp.unChangedFrame.ToCollection(), "未改变区域");
            DoAddChangeFrame(comp.ChangedFrame.Keys.ToCollection(), "变化区域");
        }
        private void DoAddChangeFrame(DBObjectCollection frames, string regionName)
        {
            foreach (var frame in frames)
            {
                var item = new ListViewItem();
                item.SubItems[0].Text = regionName;
                item.SubItems.Add("房间框线");
                item.SubItems.Add("转到");
                listViewComparerRes.Items.Add(item);
            }
        }
    }
}
