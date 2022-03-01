using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using Linq2Acad;
using AcHelper;
using GeometryExtensions;
using Autodesk.AutoCAD.DatabaseServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using NFox.Cad;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm.FrameComparer;

namespace TianHua.Electrical.UI.FrameComparer
{
    public partial class UIFrameComparer : Form
    {
        private bool isFirst = true;
        private List<ObjectId> ids;
        public Point3dCollection fence;
        
        public UIFrameComparer()
        {
            InitializeComponent();
            fence = new Point3dCollection();
            ids = new List<ObjectId>();
            AddPath();
        }

        private void AddPath()
        {
            foreach (acadApp.Document document in acadApp.Application.DocumentManager)
            {
                GraphPath.Items.Add(Path.GetFileName(document.Name));
            }
        }

        public void DoAddFrame(ThMEPFrameComparer comp, Dictionary<int, ObjectId> dicCode2Id, string frameType)
        {
            DoAddChangeFrame(comp.AppendedFrame.ToCollection(), dicCode2Id, "新增区域", frameType);
            DoAddChangeFrame(comp.ErasedFrame, dicCode2Id, "删除区域", frameType);
            DoAddChangeFrame(comp.ChangedFrame, dicCode2Id, frameType);
        }

        private void DoAddChangeFrame(Dictionary<Polyline, Tuple<Polyline, double>> changedFrame, Dictionary<int, ObjectId> dicCode2Id, string frameType)
        {
            foreach (var it in changedFrame)
            {
                var item = new ListViewItem();
                item.SubItems[0].Text = it.Value.Item2 > 1 ? "功能变化" : "框线变化";
                item.SubItems.Add(frameType);
                ids.Add(dicCode2Id[it.Key.GetHashCode()]);
                listViewComparerRes.Items.Add(item);
            }
        }

        private void DoAddChangeFrame(DBObjectCollection frames, Dictionary<int, ObjectId> dicCode2Id, string regionName, string frameType)
        {
            foreach (var frame in frames)
            {
                var item = new ListViewItem();
                item.SubItems[0].Text = regionName;
                item.SubItems.Add(frameType);
                ids.Add(dicCode2Id[frame.GetHashCode()]);
                listViewComparerRes.Items.Add(item);
            }
        }

        private void listViewComparerRes_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            using (var db = AcadDatabase.Active())
            {
                if (listViewComparerRes.SelectedItems.Count == 1)
                {
                    var idx = listViewComparerRes.SelectedIndices[0];
                    if (idx < ids.Count)
                    {
                        var entity = db.ModelSpace.ElementOrDefault(ids[idx]);
                        if (entity != null)
                        {
                            Active.Editor.ZoomToObjects(new Entity[] { entity }, 2.0);
                        }
                    }
                }
                Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
            }
        }

        private void btnDoComparer_Click(object sender, System.EventArgs e)
        {
            if (isFirst)
            {
                isFirst = false;
                frameUpdate.Enabled = true;
            }
            Hide();
            var fen = SelectRect();
            fence = new Point3dCollection() { fen.Item1, fen.Item2 };
            var painter = new ThFramePainter();
            DoRoomComparer(painter, fence, CompareFrameType.ROOM, out ThFrameExactor roomExactor, out ThMEPFrameComparer roomComp);
            DoComparer(painter, fence, CompareFrameType.DOOR, out ThFrameExactor doorExactor, out ThMEPFrameComparer doorComp);
            DoComparer(painter, fence, CompareFrameType.WINDOW, out ThFrameExactor windowExactor, out ThMEPFrameComparer windowComp);
            DoComparer(painter, fence, CompareFrameType.FIRECOMPONENT, out ThFrameExactor fireExactor, out ThMEPFrameComparer fireComp);

            DoAddFrame(roomComp, roomExactor.dicCode2Id, "房间框线");
            DoAddFrame(doorComp, doorExactor.dicCode2Id, "门");
            DoAddFrame(windowComp, windowExactor.dicCode2Id, "窗");
            DoAddFrame(fireComp, fireExactor.dicCode2Id, "防火分区");
            Show();
        }

        private void DoRoomComparer(ThFramePainter painter, Point3dCollection fence, CompareFrameType type, out ThFrameExactor frameExactor, out ThMEPFrameComparer frameComp)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                frameExactor = new ThFrameExactor(type, fence);
                frameComp = new ThMEPFrameComparer(frameExactor.curGraph, frameExactor.reference);
                var textExactor = new ThFrameTextExactor();
                _ = new ThMEPFrameTextComparer(frameComp, textExactor);// 对房间框线需要对文本再进行比对
                painter.Draw(frameComp, frameExactor.dicCode2Id, type);
            }
        }
        private void DoComparer(ThFramePainter painter, Point3dCollection fence, CompareFrameType type, out ThFrameExactor frameExactor, out ThMEPFrameComparer frameComp)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                frameExactor = new ThFrameExactor(type, fence);
                frameComp = new ThMEPFrameComparer(frameExactor.curGraph, frameExactor.reference);
                painter.Draw(frameComp, frameExactor.dicCode2Id, type);
            }
        }
        public Tuple<Point3d, Point3d> SelectRect()
        {
            var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt.TransformBy(Active.Editor.UCS2WCS()), leftDownPt.TransformBy(Active.Editor.UCS2WCS()));
            }

            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                return Tuple.Create(leftDownPt.TransformBy(Active.Editor.UCS2WCS()), ptRightRes.Value.TransformBy(Active.Editor.UCS2WCS()));
            }
            else
            {
                return Tuple.Create(leftDownPt.TransformBy(Active.Editor.UCS2WCS()), leftDownPt.TransformBy(Active.Editor.UCS2WCS()));
            }
        }

        private void GraphPath_SelectedIndexChanged(object sender, EventArgs e)
        {
            listViewComparerRes.Items.Clear();
        }

        private void frameUpdate_Click(object sender, EventArgs e)
        {
            listViewComparerRes.Items.Clear();
        }
    }
}
