using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using AcHelper;
using GeometryExtensions;
using Autodesk.AutoCAD.DatabaseServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using NFox.Cad;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Algorithm.FrameComparer;

namespace TianHua.Electrical.UI.FrameComparer
{
    public partial class UIFrameComparer : Form
    {
        private bool isFirst = true;
        private List<ObjectId> ids;
        private Dictionary<ObjectId, ObjectId> changedIdMaps;
        private ThFramePainter painter;
        public Point3dCollection fence;
        public UIFrameComparer()
        {
            InitializeComponent();
            fence = new Point3dCollection();
            ids = new List<ObjectId>();
            changedIdMaps = new Dictionary<ObjectId, ObjectId>();
            painter = new ThFramePainter();
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
                var id = dicCode2Id[it.Value.Item1.GetHashCode()];
                ids.Add(id);
                changedIdMaps.Add(id, dicCode2Id[it.Key.GetHashCode()]);
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
        private void ClearItems()
        {
            using (var db = AcadDatabase.Active())
            {
                using (db.Database.GetDocument().LockDocument())
                {
                    for (int i = 0; i < listViewComparerRes.Items.Count; ++i)
                    {
                        ListViewItem item = listViewComparerRes.Items[i];
                        if (item.SubItems[0].Text.Contains("新增"))
                        {
                            RecoverAppendFrame(i);
                        }
                        else if (item.SubItems[0].Text.Contains("删除"))
                        {
                            RecoverDeleteFrame(item, i, db);
                        }
                        else if (item.SubItems[0].Text.Contains("变化"))
                        {
                            RecoverChangeFrame(item, i, db);
                        }
                        else
                            throw new NotImplementedException("无" + item.SubItems[0].Text + "类型框线");
                    }
                    listViewComparerRes.Items.Clear();
                    ids.Clear();
                    changedIdMaps.Clear();
                }
            }
            Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
        }

        private void RecoverChangeFrame(ListViewItem item, int i, AcadDatabase db)
        {
            var layer = GetLayer(item.SubItems[1].Text);
            var id = ids[i];
            var mepId = changedIdMaps[ids[i]];
            var entity = db.ModelSpace.ElementOrDefault(id);
            var mapEntity = db.ModelSpace.ElementOrDefault(mepId);
            Dreambuild.AutoCAD.Modify.Erase(id);
            Dreambuild.AutoCAD.Modify.Erase(mepId);
            var dicCode2Id = new Dictionary<int, ObjectId>();
            if (entity != null)
            {
                var entitys = new DBObjectCollection() { entity };
                painter.DrawLines(ref entitys, ColorIndex.BYLAYER, LineTypeInfo.ByLayer, layer, dicCode2Id); ;
            }
            if (mapEntity != null)
            {
                var mapEntitys = new DBObjectCollection() { mapEntity };
                painter.DrawLines(ref mapEntitys, ColorIndex.BYLAYER, LineTypeInfo.ByLayer, layer, dicCode2Id); ;
            }
        }

        private void RecoverDeleteFrame(ListViewItem item, int idx, AcadDatabase db)
        {
            var layer = GetLayer(item.SubItems[1].Text);
            var entity = db.ModelSpace.ElementOrDefault(ids[idx]);
            Dreambuild.AutoCAD.Modify.Erase(ids[idx]);
            if (entity != null)
            {
                var entitys = new DBObjectCollection() { entity };
                var dicCode2Id = new Dictionary<int, ObjectId>();
                painter.DrawLines(ref entitys, ColorIndex.BYLAYER, LineTypeInfo.ByLayer, layer, dicCode2Id); ;
            }
        }

        private void RecoverAppendFrame(int idx)
        {
            Dreambuild.AutoCAD.Modify.Erase(ids[idx]);
        }
        private void btnDoComparer_Click(object sender, System.EventArgs e)
        {
            if (frameUpdate.Enabled)
            {
                ClearItems();
                frameUpdate.Enabled = false;
                isFirst = true;
                return;
            }
            if (isFirst)
            {
                isFirst = false;
                frameUpdate.Enabled = true;
            }
            Hide();
            var fen = SelectRect();
            fence = new Point3dCollection() { fen.Item1, fen.Item2 };
            
            DoRoomComparer(fence, CompareFrameType.ROOM, out ThFrameExactor roomExactor, out ThMEPFrameComparer roomComp);
            DoComparer(fence, CompareFrameType.DOOR, out ThFrameExactor doorExactor, out ThMEPFrameComparer doorComp);
            DoComparer(fence, CompareFrameType.WINDOW, out ThFrameExactor windowExactor, out ThMEPFrameComparer windowComp);
            DoComparer(fence, CompareFrameType.FIRECOMPONENT, out ThFrameExactor fireExactor, out ThMEPFrameComparer fireComp);

            DoAddFrame(roomComp, roomExactor.dicCode2Id, FrameType.ROOM);
            DoAddFrame(doorComp, doorExactor.dicCode2Id, FrameType.DOOR);
            DoAddFrame(windowComp, windowExactor.dicCode2Id, FrameType.WINDOWS);
            DoAddFrame(fireComp, fireExactor.dicCode2Id, FrameType.FIRECOMP);
            Show();
        }

        private void DoRoomComparer(Point3dCollection fence, CompareFrameType type, out ThFrameExactor frameExactor, out ThMEPFrameComparer frameComp)
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
        private void DoComparer(Point3dCollection fence, CompareFrameType type, out ThFrameExactor frameExactor, out ThMEPFrameComparer frameComp)
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
            ClearItems();
            isFirst = true;
            frameUpdate.Enabled = false;
        }

        private void frameUpdate_Click(object sender, EventArgs e)
        {
            using (var db = AcadDatabase.Active())
            {
                using (db.Database.GetDocument().LockDocument())
                {
                    if (listViewComparerRes.SelectedItems.Count == 1)
                    {
                        var idx = listViewComparerRes.SelectedIndices[0];
                        var item = listViewComparerRes.SelectedItems[0];
                        if (idx < ids.Count)
                        {
                            if (item.SubItems[0].Text.Contains("新增"))
                            {
                                ProcAppendFrame(item, idx, db);
                            }
                            else if (item.SubItems[0].Text.Contains("删除"))
                            {
                                ProcDeleteFrame(item, idx, db);
                            }
                            else if (item.SubItems[0].Text.Contains("变化"))
                            {
                                ProcChangeFrame(item, idx, db);
                            }
                            listViewComparerRes.Items.RemoveAt(idx);
                            ids.RemoveAt(idx);
                        }
                    }
                }
                Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
            }
        }

        private void ProcChangeFrame(ListViewItem item, int idx, AcadDatabase db)
        {
            var id = ids[idx];
            var layer = GetLayer(item.SubItems[1].Text);
            var entity = db.ModelSpace.ElementOrDefault(changedIdMaps[id]);
            Dreambuild.AutoCAD.Modify.Erase(id);
            if (entity != null)
            {
                var entitys = new DBObjectCollection() { entity };
                var dicCode2Id = new Dictionary<int, ObjectId>();
                painter.DrawLines(ref entitys, ColorIndex.BYLAYER, LineTypeInfo.ByLayer, layer, dicCode2Id); ;
            }
        }

        private void ProcDeleteFrame(ListViewItem item, int idx, AcadDatabase db)
        {
            Dreambuild.AutoCAD.Modify.Erase(ids[idx]);
        }
        private void ProcAppendFrame(ListViewItem item, int idx, AcadDatabase db)
        {
            var layer = GetLayer(item.SubItems[1].Text);
            var entity = db.ModelSpace.ElementOrDefault(ids[idx]);
            Dreambuild.AutoCAD.Modify.Erase(ids[idx]);            
            if (entity != null)
            {
                var entitys = new DBObjectCollection() { entity };
                var dicCode2Id = new Dictionary<int, ObjectId>();
                painter.DrawLines(ref entitys, ColorIndex.BYLAYER, LineTypeInfo.ByLayer, layer, dicCode2Id); ;
            }
        }
        private string GetLayer(string frameType)
        {
            if (frameType == FrameType.DOOR)
                return ThMEPEngineCoreLayerUtils.DOOR;
            else if (frameType == FrameType.WINDOWS)
                return ThMEPEngineCoreLayerUtils.WINDOW;
            else if (frameType == FrameType.ROOM)
                return ThMEPEngineCoreLayerUtils.ROOMOUTLINE;
            else if (frameType == FrameType.FIRECOMP)
                return ThMEPEngineCoreLayerUtils.FIRECOMPARTMENT;
            else
                throw new NotImplementedException("不支持" + frameType + "类型的框线");
        }
    }
}
