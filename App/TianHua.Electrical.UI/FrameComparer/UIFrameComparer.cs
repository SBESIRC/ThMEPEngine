using System.Windows.Forms;
using System.Collections.Generic;
using Linq2Acad;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;

namespace TianHua.Electrical.UI.FrameComparer
{
    public partial class UIFrameComparer : Form
    {
        public UIFrameComparer(ThMEPFrameComparer comp, Dictionary<int, ObjectId> dicCode2Id)
        {
            InitializeComponent();
            DoAddChangeFrame(comp.AppendedFrame.ToCollection(), dicCode2Id, "新增区域");
            DoAddChangeFrame(comp.ErasedFrame, dicCode2Id, "删除区域");
            DoAddChangeFrame(comp.ChangedFrame.Keys.ToCollection(), dicCode2Id, "变化区域");
        }

        private void DoAddChangeFrame(DBObjectCollection frames, Dictionary<int, ObjectId> dicCode2Id, string regionName)
        {
            foreach (var frame in frames)
            {
                var item = new ListViewItem();
                item.SubItems[0].Text = regionName;
                item.SubItems.Add("房间框线");
                item.SubItems.Add(dicCode2Id[frame.GetHashCode()].ToString());
                listViewComparerRes.Items.Add(item);
            }
        }

        private void listViewComparerRes_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            using (var db = AcadDatabase.Active())
            {
                if (listViewComparerRes.SelectedItems.Count == 1)
                {
                    var item = listViewComparerRes.SelectedItems[0];
                    var strId = item.SubItems[2].Text;
                    var idNum = strId.Substring(1, strId.Length - 2);
                    var id = new ObjectId(new System.IntPtr(long.Parse(idNum)));
                    var entity = db.ModelSpace.ElementOrDefault(id);
                    if (entity != null)
                    {
                        Active.Editor.ZoomToObjects(new Entity[] { entity }, 2.0);
                    }
                }
            }
        }
    }
}
