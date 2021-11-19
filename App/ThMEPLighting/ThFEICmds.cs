﻿using AcHelper;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.DSFEI.ThEmgPilotLamp;
using ThMEPLighting.DSFEL;
using ThMEPLighting.DSFEL.Service;
using ThMEPLighting.FEI;
using ThMEPLighting.FEI.EvacuationPath;
using ThMEPLighting.FEI.PrintEntity;
using ThMEPLighting.FEI.Service;
using ThMEPLighting.FEI.ThEmgPilotLamp;

namespace ThMEPLighting
{
    public class ThFEICmds
    {
        [CommandMethod("TIANHUACAD", "THSSLJ", CommandFlags.Modal)]
        public void THSSLJ()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
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
                    return;
                }

                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                var pt = frameLst.First().StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                frameLst = frameLst.Select(x =>
                {
                    originTransformer.Transform(x);
                    return ThMEPFrameService.Normalize(x as Polyline) as Curve;
                }).ToList();

                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);
                foreach (var pline in holeInfo)
                {
                    //清除原有构建
                    ClearComponentService.ClearLines(pline.Key, originTransformer);

                    GetPrimitivesService primitivesService = new GetPrimitivesService(originTransformer);
                    //获取车道线信息
                    var xLanes = primitivesService.GetLanes(pline.Key, out List<List<Line>> yLines);
                    if (xLanes.Count == 0 && yLines.Count == 0)
                    {
                        continue;
                    }

                    //获取墙柱信息
                    primitivesService.GetStructureInfo(pline.Key, out List<Polyline> columns, out List<Polyline> walls);

                    //计算洞口
                    List<Polyline> holes = new List<Polyline>(pline.Value);
                    holes.AddRange(columns);
                    holes.AddRange(walls);

                    //获取出入口图块
                    var enterBlcok = primitivesService.GetEvacuationExitBlock(pline.Key);

                    //规划路径
                    ExtendLinesService extendLines = new ExtendLinesService();
                    var paths = extendLines.CreateExtendLines(xLanes, yLines, enterBlcok, pline.Key, holes);

                    //打印路径
                    PrintService printService = new PrintService();
                    var allLanes = xLanes.SelectMany(x => x.Select(y => y)).ToList();
                    allLanes.AddRange(yLines.SelectMany(x => x.Select(y => y)));
                    printService.PrintPath(paths, allLanes, originTransformer);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THDSFEL", CommandFlags.Modal)]
        public void ThDSFEI()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
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
                    return;
                }

                Dictionary<BlockReference, ObjectIdCollection> frameLst = new Dictionary<BlockReference, ObjectIdCollection>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<BlockReference>(obj);
                    ObjectIdCollection dBObject = new ObjectIdCollection();
                    dBObject.Add(obj);
                    frameLst.Add(frame.Clone() as BlockReference, dBObject);
                }

                foreach (var frameBlockDic in frameLst)
                {
                    var frameBlock = frameBlockDic.Key;
                    var frameBlockId = frameBlockDic.Value;
                    var frame = GetBlockInfo(frameBlock).Where(x => x is Polyline).Cast<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                    if (frame == null)
                    {
                        continue;
                    }

                    var pt = frame.StartPoint;
                    ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                    originTransformer.Transform(frame);
                    var outFrame = ThMEPFrameService.Normalize(frame);

                    DSFELGetPrimitivesService dsFELGetPrimitivesService = new DSFELGetPrimitivesService(originTransformer);
                    //获取房间
                    var rooms = dsFELGetPrimitivesService.GetRoomInfo(outFrame);

                    //获取门 
                    var doors = dsFELGetPrimitivesService.GetDoor(outFrame);

                    //获取中心线
                    var centerLines = dsFELGetPrimitivesService.GetCentterLines(outFrame, rooms.Select(x => x.Key.Boundary).OfType<Polyline>().ToList());

                    //获取结构信息
                    dsFELGetPrimitivesService.GetStructureInfo(outFrame, out List<Polyline> columns, out List<Polyline> walls);

                    //计算洞口
                    List<Polyline> holes = new List<Polyline>();
                    holes.AddRange(columns);
                    holes.AddRange(walls);
                    holes.AddRange(rooms.SelectMany(x => x.Value).ToList());

                    //布置
                    var thRooms = rooms.Select(x => x.Key).ToList();
                    LayoutService layoutService = new LayoutService();
                    var paths = layoutService.LayoutFELService(thRooms, doors, centerLines, holes, originTransformer);

                    ////打印路径
                    //PrintPathService printService = new PrintPathService();
                    //printService.PrintPath(paths.SelectMany(x => x.evacuationPaths).ToList(), centerLines, originTransformer);
                }  
            }
        }

        [CommandMethod("TIANHUACAD", "THSSZSDBZ", CommandFlags.Modal)]
        public void THSSZSDBZ()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
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
                    return;
                }
                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }
                var pt = frameLst.First().StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                frameLst = frameLst.Where(c => c.Area > 10).Select(x =>
                {
                    originTransformer.Transform(x);
                    return ThMEPFrameService.Normalize(x as Polyline) as Curve;
                }).ToList();

                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);

                var lampLight = new ThEmgPilotLampCommand();
                lampLight.InitData(originTransformer, holeInfo);
                lampLight.Execute();

                var cloudLineIds = lampLight.AddPolyLineIds;
                if (null == cloudLineIds || cloudLineIds.Count < 1)
                    return;
                //revcloud can only print to the current layer.
                //so it changes the active layer to the required layer, then changes back.
                //画云线。 云线只能画在当前图层。所以先转图层画完在转回来。
                var oriLayer = Active.Database.Clayer;
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
                    revcloudObj.ColorIndex = ThMEPLightingCommon.EMGPILOTREVCLOUD_CORLOR_INDEX;
                    revcloudObj.Layer = ThMEPLightingCommon.REVCLOUD_LAYER;
                }
                Active.Database.Clayer = oriLayer;

            }
        }

        [CommandMethod("TIANHUACAD", "THDSSSZSDBZ", CommandFlags.Modal)]
        public void THDSSSZSDBZ()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
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
                    return;
                }
                //获取外包框
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }
                var pt = frameLst.First().StartPoint;
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(pt);
                frameLst = frameLst.Where(c => c.Area > 10).Select(x =>
                {
                    originTransformer.Transform(x);
                    return ThMEPFrameService.Normalize(x as Polyline) as Curve;
                }).ToList();

                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);

                var lampLight = new ThDSEmgPilotLampCommand();
                lampLight.InitData(originTransformer, holeInfo);
                lampLight.Execute();


                var cloudLineIds = lampLight.AddPolyLineIds;
                if (null == cloudLineIds || cloudLineIds.Count < 1)
                    return;
                //revcloud can only print to the current layer.
                //so it changes the active layer to the required layer, then changes back.
                //画云线。 云线只能画在当前图层。所以先转图层画完在转回来。
                var oriLayer = Active.Database.Clayer;
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
                    revcloudObj.ColorIndex = ThMEPLightingCommon.EMGPILOTREVCLOUD_CORLOR_INDEX;
                    revcloudObj.Layer = ThMEPLightingCommon.REVCLOUD_LAYER;
                }
                Active.Database.Clayer = oriLayer;

            }
        }

        /// <summary>
        /// 计算外包框和其中的洞
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        private Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            Dictionary<Polyline, List<Polyline>> holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
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
            List<Polyline> resPolys = new List<Polyline>();
            foreach (var frame in frameLst)
            {
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

        /// <summary>
        /// 获取块内信息
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        private List<Entity> GetBlockInfo(BlockReference blockReference)
        {
            var matrix = blockReference.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var results = new List<Entity>();
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.BlockTableRecord);
                foreach (var objId in blockTableRecord)
                {
                    var dbObj = acadDatabase.Element<Entity>(objId).Clone() as Entity;
                    dbObj.TransformBy(matrix);
                    results.Add(dbObj);
                }
                return results;
            }
        }
    }
}  
