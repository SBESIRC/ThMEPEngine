using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPHVAC.Command;

namespace ThMEPHVAC
{
    class ThHAVCIndoorFanCmds
    {
        [CommandMethod("TIANHUACAD", "THSNJBZ", CommandFlags.Modal)]
        public void THIndoorFanLayout()
        {
            //Step1 选择房间框线 获取房间内外轮廓信息
            var ucs = Active.Editor.CurrentUserCoordinateSystem;
            var selectAreas = SelectPolyline();
            var indoorFanLayout = new IndoorFanLayoutCmd(selectAreas, ucs.CoordinateSystem3d.Xaxis, ucs.CoordinateSystem3d.Yaxis,false);
            indoorFanLayout.Execute();
        }
        [CommandMethod("TIANHUACAD", "THSNJFZ", CommandFlags.Modal)]
        public void THIndoorFanPlace() 
        {
            var placeFan = new IndoorFanPlace();
            placeFan.Execute();
        }
        [CommandMethod("TIANHUACAD", "THSNJArea", CommandFlags.Modal)]
        public void THIndoorFanTest()
        {
            var ucs = Active.Editor.CurrentUserCoordinateSystem;
            var selectAreas = SelectPolyline();
            var indoorFanLayout = new IndoorFanLayoutCmd(selectAreas, ucs.CoordinateSystem3d.Xaxis, ucs.CoordinateSystem3d.Yaxis,true);
            indoorFanLayout.Execute();
        }
        private Dictionary<Polyline, List<Polyline>> SelectPolyline()
        {
            var selectPLines = new Dictionary<Polyline, List<Polyline>>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取房间框线
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var layerNames = new string[]
                {
                    "AI-房间框线",
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, layerNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return selectPLines;
                }
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }
                var pt = frameLst.First().StartPoint;
                var originTransformer = new ThMEPOriginTransformer(pt);
                frameLst = frameLst.Select(x =>
                {
                    originTransformer.Transform(x);
                    return ThMEPFrameService.Normalize(x as Polyline) as Curve;
                }).ToList();
                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);
                foreach (var pline in holeInfo)
                {
                    var copyOut = (Polyline)pline.Key.Clone();
                    originTransformer.Reset(copyOut);
                    var innerPLines = new List<Polyline>();
                    if (pline.Value != null)
                    {
                        foreach (var item in pline.Value)
                        {
                            var copyInner = (Polyline)item.Clone();
                            originTransformer.Reset(copyInner);
                            innerPLines.Add(copyInner);
                        }
                    }
                    selectPLines.Add(copyOut, innerPLines);
                }
            }
            return selectPLines;
        }
        /// <summary>
        /// 计算外包框和其中的洞
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        private Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            var holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);
                firFrame = firFrame.DPSimplify(1);
                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));
                holeDic.Add(firFrame, holes);
            }
            return holeDic;
        }
        /// <summary>
        /// 处理外包框线
        /// </summary>
        /// <param name="frameLst"></param>
        /// <returns></returns>
        private List<Polyline> HandleFrame(List<Curve> frameLst)
        {
            var resPolys = new List<Polyline>();
            foreach (var frame in frameLst)
            {
                if (frame.Area < 10)
                    continue;
                if (frame is Polyline poly && poly.Closed)
                {
                    resPolys.Add(poly);
                }
                else if (frame is Polyline secPoly && !secPoly.Closed && secPoly.StartPoint.DistanceTo(secPoly.EndPoint) < 1000)
                {
                    secPoly.Closed = true;
                    resPolys.Add(secPoly);
                }
            }
            return resPolys;
        }
    }
}
