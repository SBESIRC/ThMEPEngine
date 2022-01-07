using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
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
            string layerName = "AI-圈注";
            //Step1 选择房间框线 获取房间内外轮廓信息
            var ucs = Active.Editor.CurrentUserCoordinateSystem;
            var selectAreas = SelectPolyline();
            var indoorFanLayout = new IndoorFanLayoutCmd(selectAreas, ucs.CoordinateSystem3d.Xaxis, ucs.CoordinateSystem3d.Yaxis,false);
            indoorFanLayout.Execute();
            var cloudLines = indoorFanLayout.ErrorRoomPolylines;
            if (null == cloudLines || cloudLines.Count < 1)
                return;
            var cloudIds = new List<ObjectId>();
            using (var acdb = AcadDatabase.Active())
            {
                LayerTableRecord layerRecord = null;
                foreach (var layer in acdb.Layers)
                {
                    if (layer.Name.ToUpper().Equals(layerName))
                    {
                        layerRecord = acdb.Layers.Element(layerName);
                        break;
                    }
                }

                // 创建新的图层
                if (layerRecord == null)
                {
                    layerRecord = acdb.Layers.Create(layerName);
                    layerRecord.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0); ;
                    layerRecord.IsPlottable = false;
                }
                foreach (var item in cloudLines)
                {
                    var id = acdb.ModelSpace.Add(item);
                    if (id == null || !id.IsValid)
                        continue;
                    item.Layer = layerName;
                    cloudIds.Add(id);
                }
            }
            ShowErrroPolylines(cloudIds);
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
            var indoorFanLayout = new IndoorFanLayoutCmd(selectAreas, ucs.CoordinateSystem3d.Xaxis, ucs.CoordinateSystem3d.Yaxis, true);
            indoorFanLayout.Execute();
        }
        private void ShowErrroPolylines(List<ObjectId> cloudLineIds) 
        {
            if (null == cloudLineIds || cloudLineIds.Count < 1)
                return;
            //revcloud can only print to the current layer.
            //so it changes the active layer to the required layer, then changes back.
            //画云线。 云线只能画在当前图层。所以先转图层画完在转回来。\
            var oriLayer = Active.Database.Clayer;
            using (var acdb = AcadDatabase.Active())
            {
                foreach (var id in cloudLineIds)
                {
                    var pline = acdb.ModelSpace.Element(id);
                    if (null == pline)
                        continue;
                    ObjectId revcloud = ObjectId.Null;
                    void handler(object s, ObjectEventArgs e)
                    {
                        if (e.DBObject is Polyline polyline)
                        {
                            revcloud = e.DBObject.ObjectId;
                        }
                    }
                    acdb.Database.ObjectAppended += handler;

#if ACAD_ABOVE_2014
                    Active.Editor.Command("_.REVCLOUD", "_arc", 500, 500, "_Object", id, "_No");
#else
                    ResultBuffer args = new ResultBuffer(
                       new TypedValue((int)LispDataType.Text, "_.REVCLOUD"),
                       new TypedValue((int)LispDataType.Text, "_ARC"),
                       new TypedValue((int)LispDataType.Text, "500"),
                       new TypedValue((int)LispDataType.Text, "500"),
                       new TypedValue((int)LispDataType.Text, "_Object"),
                       new TypedValue((int)LispDataType.ObjectId, id),
                       new TypedValue((int)LispDataType.Text, "_No"));
                    Active.Editor.AcedCmd(args);
#endif
                    acdb.Database.ObjectAppended -= handler;

                    // 设置运行属性
                    var revcloudObj = acdb.Element<Entity>(revcloud, true);
                    revcloudObj.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0);
                    revcloudObj.Layer = "AI-圈注";
                }
            }
            Active.Database.Clayer = oriLayer;
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
