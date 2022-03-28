using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using GeometryExtensions;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.Common
{
    internal class ThSelectFrameUtil
    {
        public static Point3dCollection GetFrame(string hint = "请框选一个范围")
        {
            Point3dCollection pts = new Point3dCollection();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var frame = ThMEPEngineCore.CAD.ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { hint });

                if (frame.Area > 1e-4)
                {
                    pts = frame.Vertices();
                }

                return pts;
            }
        }

        public static Point3dCollection GetFrameBlk()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                Point3dCollection pts = new Point3dCollection();

                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return pts;
                }

                List<BlockReference> frameLst = new List<BlockReference>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.ElementOrDefault<BlockReference>(obj);
                    if (frame != null)
                    {
                        frameLst.Add(frame);
                    }
                }

                List<Polyline> frames = new List<Polyline>();
                foreach (var frameBlock in frameLst)
                {
                    var frame = GetFrameBlkPolyline(frameBlock);
                    if (frame != null)
                    {
                        frames.Add(frame);
                    }
                }

                var frameL = frames.OrderByDescending(x => x.Area).FirstOrDefault();
                if (frameL != null && frameL.Area > 10)
                {
                    frameL = ProcessFrame(frameL);
                }
                if (frameL != null && frameL.Area > 10)
                {
                    pts = frameL.Vertices();
                }

                return pts;
            }
        }

        public static Point3dCollection GetRoomFrame()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                Point3dCollection pts = new Point3dCollection();
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择框线",
                    RejectObjectsOnLockedLayers = true,
                    SingleOnly = true,
                };
                var dxfNames = new string[]
                {
                Autodesk.AutoCAD.Runtime.RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return pts;
                }

                var frameList = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acdb.Element<Polyline>(obj);
                    var nFrame = ProcessFrame(frameTemp);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    frameList.Add(nFrame);
                }
                var frame = frameList.OrderByDescending(x => x.Area).First();
                pts = frame.Vertices();
                return pts;
            }
        }

        public static Entity GetPolyline()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                Entity frame = null;
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    Autodesk.AutoCAD.Runtime.RXClass.GetClass(typeof(Polyline)).DxfName,
                    Autodesk.AutoCAD.Runtime.RXClass.GetClass(typeof(MPolygon)).DxfName,
                };
                var filter = ThCADExtension.ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return frame;
                }

                var frameList = new List<Entity>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frameTemp = acdb.Element<Entity>(obj);
                    frameList.Add(frameTemp);
                }

                frame = frameList.First();
                return frame;

            }
        }

        public static MPolygon GetMPolygon()
        {
            var frameTemp = GetPolyline();


            MPolygon frame = null;
            if (frameTemp is Polyline pl)
            {
                frame = ThMPolygonTool.CreateMPolygon(pl);
            }
            if (frameTemp is MPolygon mpl)
            {
                frame = mpl;
            }
            return frame;
        }

        public static Polyline GetRoomFramePolyline()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                Polyline frame = new Polyline();
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "请选择框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return frame;
                }

                var frameList = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    //获取外包框
                    var frameTemp = acdb.Element<Polyline>(obj);
                    var nFrame = ProcessFrame(frameTemp);
                    if (nFrame.Area < 1)
                    {
                        continue;
                    }

                    frameList.Add(nFrame);
                }
                frame = frameList.OrderByDescending(x => x.Area).First();
                return frame;
            }
        }

        private static Polyline ProcessFrame(Polyline frame)
        {
            Polyline nFrame = null;
            Polyline nFrameNormal = ThMEPFrameService.Normalize(frame);
            if (nFrameNormal.Area > 10)
            {
                nFrameNormal = nFrameNormal.DPSimplify(1);
                nFrame = nFrameNormal;
            }
            return nFrame;
        }

        private static Polyline GetFrameBlkPolyline(BlockReference blockReference)
        {
            var objs = new DBObjectCollection();
            blockReference.Explode(objs);
            return objs.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
        }

        public static Point3d SelectPoint(string commandSuggestStr)
        {
            var ptLeftRes = Active.Editor.GetPoint(commandSuggestStr);
            Point3d pt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                pt = ptLeftRes.Value;
                pt = pt.TransformBy(Active.Editor.UCS2WCS());
            }
            return pt;
        }

        public static Point3dCollection SelectFramePointCollection(string commandSuggestStrLeft, string commandSuggestStrRight)
        {
            Point3dCollection pts = new Point3dCollection();
            var pl = SelectFramePL(commandSuggestStrLeft, commandSuggestStrRight);
            if (pl.NumberOfVertices > 0)
            {
                pts = pl.Vertices();
            }

            return pts;
        }

        public static Polyline SelectFramePL(string commandSuggestStrLeft, string commandSuggestStrRight)
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
    }
}
