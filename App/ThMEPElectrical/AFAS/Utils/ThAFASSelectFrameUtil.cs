using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.Service;

namespace ThMEPElectrical.AFAS.Utils
{
    public class ThAFASSelectFrameUtil
    {
        public static Point3dCollection GetFrame()
        {
            Point3dCollection pts = new Point3dCollection();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var frame = ThMEPEngineCore.CAD.ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });

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
                    var frame = ThElectricalCommonService.GetFrameBlkPolyline(frameBlock);
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
    }
}
