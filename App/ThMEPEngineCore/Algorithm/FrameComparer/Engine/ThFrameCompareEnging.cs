using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

using NFox.Cad;
using Linq2Acad;
using AcHelper;
using GeometryExtensions;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Diagnostics;

using ThMEPEngineCore.Algorithm.FrameComparer.Model;

namespace ThMEPEngineCore.Algorithm.FrameComparer
{
    public class ThFrameCompareEnging
    {
        public List<ThFrameChangeItem> ResultList { get; set; }

        private List<ThFrameCompareModel> CompareList;
        public ThFrameCompareEnging()
        {
            ResultList = new List<ThFrameChangeItem>();
            CompareList = new List<ThFrameCompareModel>();
        }

        public void Excute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var selectPts = SelectFramePointCollection("框选范围", "框选范围");
                if (selectPts.Count == 0)
                {
                    return;
                }
                var fence = selectPts;
                ResultList.Clear();

                ThFramePainter.InitialPainter();

                InitialCompare();
                foreach (var compare in CompareList)
                {
                    compare.DoCompare(fence);
                    compare.GenerateResult();
                    ResultList.AddRange(compare.ResultList);
                }
            }
        }

        private void InitialCompare()
        {
            var roomCompare = new ThFrameCompareModel(ThFrameChangedCommon.CompareFrameType.ROOM, true);
            var doorCompare = new ThFrameCompareModel(ThFrameChangedCommon.CompareFrameType.DOOR, false);
            var windowCompare = new ThFrameCompareModel(ThFrameChangedCommon.CompareFrameType.WINDOW, false);
            var fireCompare = new ThFrameCompareModel(ThFrameChangedCommon.CompareFrameType.FIRECOMPONENT, false);
            CompareList.Clear();
            CompareList.Add(roomCompare);
            CompareList.Add(doorCompare);
            CompareList.Add(windowCompare);
            CompareList.Add(fireCompare);
        }


        public void UpdateResult(ThFrameChangeItem changeItem)
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var comparer = CompareList.Where(x => x.FrameType == changeItem.FrameType).FirstOrDefault();
                if (comparer != null)
                {
                    comparer.UpdateFrame(changeItem);
                    ResultList.Remove(changeItem);
                }
            }
        }


        #region SelectFrame
        private static Point3dCollection SelectFramePointCollection(string commandSuggestStrLeft, string commandSuggestStrRight)
        {
            Point3dCollection pts = new Point3dCollection();
            var pl = SelectFramePL(commandSuggestStrLeft, commandSuggestStrRight);
            if (pl.NumberOfVertices > 0)
            {
                pts = pl.Vertices();
            }

            return pts;
        }

        private static Polyline SelectFramePL(string commandSuggestStrLeft, string commandSuggestStrRight)
        {
            var resultPl = new Polyline();

            var ptLeftRes = Active.Editor.GetPoint(commandSuggestStrLeft);
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }

            var ptRightRes = Active.Editor.GetCorner(commandSuggestStrRight, leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                var rightTopPt = ptRightRes.Value;
                leftDownPt = leftDownPt.TransformBy(Active.Editor.UCS2WCS());
                rightTopPt = rightTopPt.TransformBy(Active.Editor.UCS2WCS());
                resultPl = ToFrame(leftDownPt, rightTopPt);
            }

            return resultPl;

        }

        private static Polyline ToFrame(Point3d left, Point3d right)
        {
            var pl = new Polyline();
            if (left != Point3d.Origin && right != Point3d.Origin)
            {

                var ptRT = new Point2d(right.X, left.Y);
                var ptLB = new Point2d(left.X, right.Y);

                pl.AddVertexAt(pl.NumberOfVertices, left.ToPoint2D(), 0, 0, 0);
                pl.AddVertexAt(pl.NumberOfVertices, ptRT, 0, 0, 0);
                pl.AddVertexAt(pl.NumberOfVertices, right.ToPoint2D(), 0, 0, 0);
                pl.AddVertexAt(pl.NumberOfVertices, ptLB, 0, 0, 0);

                pl.Closed = true;

            }
            return pl;
        }
        #endregion 

    }
}
