﻿using System;
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

namespace TianHua.Electrical.UI.FrameComparer
{
    public partial class UIFrameComparer : Form
    {
        public Point3dCollection fence;
        public bool isModel = true;
        public UIFrameComparer()
        {
            InitializeComponent();
            fence = new Point3dCollection();
            AddPath();
        }
        public void DoAddFrame(ThMEPFrameComparer comp, Dictionary<int, ObjectId> dicCode2Id, string frameType)
        {
            DoAddChangeFrame(comp.AppendedFrame.ToCollection(), dicCode2Id, "新增区域", frameType);
            DoAddChangeFrame(comp.ErasedFrame, dicCode2Id, "删除区域", frameType);
            DoAddChangeFrame(comp.ChangedFrame.Keys.ToCollection(), dicCode2Id, "变化区域", frameType);
        }
        private void AddPath()
        {
            foreach (acadApp.Document document in acadApp.Application.DocumentManager)
            {
                GraphPath.Items.Add(document.Name);
            }
        }

        private void DoAddChangeFrame(DBObjectCollection frames, Dictionary<int, ObjectId> dicCode2Id, string regionName, string frameType)
        {
            foreach (var frame in frames)
            {
                var item = new ListViewItem();
                item.SubItems[0].Text = regionName;
                item.SubItems.Add(frameType);
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
                Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
            }
        }

        private void btnDoComparer_Click(object sender, System.EventArgs e)
        {
            var fen = SelectRect();
            fence = new Point3dCollection() { fen.Item1, fen.Item2 };
            if (isModel)
            {
                isModel = false;
                Close();
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
    }
}
