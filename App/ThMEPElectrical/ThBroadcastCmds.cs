using NFox.Cad;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPElectrical.Model;
using ThMEPEngineCore.Engine;
using ThMEPElectrical.Broadcast;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using System;
using ThMEPElectrical.Broadcast.Service.ClearService;
using ThMEPElectrical.Broadcast.Service;
using DotNetARX;
using ThMEPElectrical.Business.Procedure;
using ThMEPEngineCore.Service;
using ThMEPElectrical.ConnectPipe;
using ThMEPElectrical.BlockConvert;
using Autodesk.AutoCAD.ApplicationServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using ThMEPEngineCore.LaneLine;
using ThMEPElectrical.Service;
using System.Windows;

namespace ThMEPElectrical
{
    public class ThBroadcastCmds
    {
        readonly double bufferLength = 100;
        public double BlindAreaRadius = 12500;

        [CommandMethod("TIANHUACAD", "THGBBZ", CommandFlags.Modal)]
        public void ThBroadcast()
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

                //处理外包框线
                var plines = HandleFrame(frameLst);
                var holeInfo = CalHoles(plines);
                foreach (var plInfo in holeInfo)
                {
                    var plFrame = plInfo.Key.Buffer(200)[0] as Polyline;
                    //删除原有构建
                    var otherFrames = plFrame.GetInnerFrames(originTransformer);
                    plFrame.ClearBroadCast(originTransformer, otherFrames);
                    plFrame.ClearBlindArea(originTransformer);
                    plFrame.ClearPipeLines(originTransformer);

                    //获取车道线
                    var lanes = GetLanes(plFrame, acdb, originTransformer);
                    if (lanes.Count <= 0)
                    {
                        MessageBox.Show("未找到车道中心线！");
                        continue;
                    }

                    //处理车道线
                    var handleLines = ThMEPLineExtension.LineSimplifier(lanes.ToCollection(), 500, 300.0, 2.0, Math.PI / 180.0);
                    var parkingLinesService = new ParkingLinesService();
                    var parkingLines = parkingLinesService.CreateNodedParkingLines(plFrame, handleLines, out List<List<Line>> otherPLines);
                   
                    //获取构建信息
                    var bufferFrame = plFrame.Buffer(bufferLength)[0] as Polyline;
                    GetStructureInfo(acdb, bufferFrame, out List<Polyline> columns, out List<Polyline> walls, originTransformer);

                    //主车道布置信息
                    LayoutWithParkingLineService layoutService = new LayoutWithParkingLineService();
                    var layoutInfo = layoutService.LayoutBraodcast(plFrame, parkingLines, columns, walls);

                    //副车道布置信息
                    LayoutWithSecondaryParkingLineService layoutWithSecondaryParkingLineService = new LayoutWithSecondaryParkingLineService();
                    var resLayoutInfo = layoutWithSecondaryParkingLineService.LayoutBraodcast(layoutInfo, otherPLines, columns, walls, plFrame);

                    //计算广播盲区
                    var layoutPts = resLayoutInfo.SelectMany(x => x.Value.Keys).ToList();
                    PrintBlindAreaService blindAreaService = new PrintBlindAreaService();
                    blindAreaService.PrintBlindArea(layoutPts, plInfo, ThElectricalUIService.Instance.thGBParameter.BlindRadius, originTransformer);

                    //放置广播
                    InsertBroadcastService.scaleNum = ThElectricalUIService.Instance.thGBParameter.Scale;
                    var broadcasts = InsertBroadcastService.InsertSprayBlock(resLayoutInfo, originTransformer);

                    //车道广播连管
                    var transBroadcasts = broadcasts.Select(x => {
                        var transBlock = x.Clone() as BlockReference;
                        originTransformer.Transform(transBlock);
                        return transBlock;
                    }).ToList();
                    ConnetPipeService connetPipeService = new ConnetPipeService();
                    var resPolys = connetPipeService.ConnetPipe(plInfo, handleLines, transBroadcasts);
                    var polyObjs = resPolys.ToCollection();
                    originTransformer.Reset(polyObjs);

                    //创建连管线
                    InsertConnectPipeService.InsertConnectPipe(polyObjs.Cast<Polyline>().ToList());
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THGBMQ", CommandFlags.Modal)] 
        public void ThBroadcastBlindArea()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
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
                
                acadDatabase.Database.UnFrozenLayer(ThMEPCommon.BroadcastLayerName);
                acadDatabase.Database.UnLockLayer(ThMEPCommon.BroadcastLayerName);
                acadDatabase.Database.UnOffLayer(ThMEPCommon.BroadcastLayerName);
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(obj);
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
                foreach (var plInfo in holeInfo)
                {
                    var pline = plInfo.Key;
                    //删除原有盲区
                    pline.ClearBlindArea(originTransformer);

                    //获取广播布置点
                    var pts = GetLayoutBroadcastPoints(acadDatabase, pline, originTransformer);

                    //打印盲区
                    PrintBlindAreaService blindAreaService = new PrintBlindAreaService();
                    blindAreaService.PrintBlindArea(pts, plInfo, ThElectricalUIService.Instance.thGBParameter.BlindRadius, originTransformer);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THGBLX", CommandFlags.Modal)]
        public void ThBroadcastConnetPipe()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
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

                acadDatabase.Database.UnFrozenLayer(ThMEPCommon.BroadcastLayerName);
                acadDatabase.Database.UnLockLayer(ThMEPCommon.BroadcastLayerName);
                acadDatabase.Database.UnOffLayer(ThMEPCommon.BroadcastLayerName);
                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(obj);
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
                    //删除原有连接线
                    pline.Key.ClearPipeLines(originTransformer);

                    //获取广播图块
                    var broadcast = GetBroadcastBlocks(pline.Key, originTransformer);

                    //获取车道线
                    var lanes = GetLanes(pline.Key, acadDatabase, originTransformer);
                    if (lanes.Count <= 0)
                    {
                        MessageBox.Show("未找到车道中心线！");
                        continue;
                    }

                    //处理车道线
                    var handleLines = ThMEPLineExtension.LineSimplifier(lanes.ToCollection(), 500, 20.0, 2.0, Math.PI / 180.0);

                    //车道广播连管
                    ConnetPipeService connetPipeService = new ConnetPipeService();
                    var resPolys = connetPipeService.ConnetPipe(pline, handleLines, broadcast);
                    var polyObjs = resPolys.ToCollection();
                    originTransformer.Reset(polyObjs);

                    //创建连管线
                    InsertConnectPipeService.InsertConnectPipe(polyObjs.Cast<Polyline>().ToList());
                }
            }
        }

        /// <summary>
        /// 处理外包框线
        /// </summary>
        /// <param name="frameLst"></param>
        /// <returns></returns>
        private List<Polyline> HandleFrame(List<Curve> frameLst)
        {
            var polygonInfos = NoUserCoordinateWorker.MakeNoUserCoordinateWorker(frameLst);
            List<Polyline> resPLines = new List<Polyline>();
            foreach (var pInfo in polygonInfos)
            {
                resPLines.Add(pInfo.ExternalProfile);
                resPLines.AddRange(pInfo.InnerProfiles);
            }

            List<Polyline> resPolys = new List<Polyline>();
            foreach (var frame in resPLines)
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

                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));

                holeDic.Add(firFrame, holes);
            }

            return holeDic;
        }

        /// <summary>
        /// 获取车道线
        /// </summary>
        /// <param name="polyline"></param>
        public List<Curve> GetLanes(Polyline polyline, AcadDatabase acdb, ThMEPOriginTransformer originTransformer)
        {
            var objs = new DBObjectCollection();
            var laneLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == ThMEPCommon.LANELINE_LAYER_NAME);
            laneLines.ForEach(x => { 
                var transCurve = x.Clone() as Curve;
                originTransformer.Transform(transCurve); 
                objs.Add(transCurve); 
            });

            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();

            return sprayLines.SelectMany(x => polyline.Trim(x).Cast<Entity>().Where(y => y is Curve).Cast<Curve>().ToList()).ToList();
        }

        /// <summary>
        /// 获取构建信息
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="beams"></param>
        /// <param name="walls"></param>
        private void GetStructureInfo(AcadDatabase acdb, Polyline polyline, out List<Polyline> columns, out List<Polyline> walls, ThMEPOriginTransformer originTransformer)
        {
            var ColumnExtractEngine = new ThColumnExtractionEngine();
            ColumnExtractEngine.Extract(acdb.Database);
            ColumnExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
            var ColumnEngine = new ThColumnRecognitionEngine();
            ColumnEngine.Recognize(ColumnExtractEngine.Results, polyline.Vertices());

            // 启动墙识别引擎
            var ShearWallExtractEngine = new ThShearWallExtractionEngine();
            ShearWallExtractEngine.Extract(acdb.Database);
            ShearWallExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
            var ShearWallEngine = new ThShearWallRecognitionEngine();
            ShearWallEngine.Recognize(ShearWallExtractEngine.Results, polyline.Vertices());

            var archWallExtractEngine = new ThDB3ArchWallExtractionEngine();
            archWallExtractEngine.Extract(acdb.Database);
            archWallExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
            var archWallEngine = new ThDB3ArchWallRecognitionEngine();
            archWallEngine.Recognize(archWallExtractEngine.Results, polyline.Vertices());

            ////获取柱
            columns = new List<Polyline>();
            columns = ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            //获取剪力墙
            walls = new List<Polyline>();
            walls = ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            //获取建筑墙
            foreach (var o in archWallEngine.Elements)
            {
                if (o.Outline is Polyline wall)
                {
                    walls.Add(wall);
                }
            }
        }

        /// <summary>
        /// 获取广播图块
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<BlockReference> GetBroadcastBlocks(Polyline polyline, ThMEPOriginTransformer originTransformer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(ThMEPCommon.BroadcastLayerName);
                acadDatabase.Database.UnLockLayer(ThMEPCommon.BroadcastLayerName);
                acadDatabase.Database.UnOffLayer(ThMEPCommon.BroadcastLayerName);

                //获取广播
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.LayerName) == ThMEPCommon.BroadcastLayerName &
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
                var braodcasts = new List<BlockReference>();
                var allBraodcasts = Active.Editor.SelectAll(filterlist);
                if (allBraodcasts.Status == PromptStatus.OK)
                {
                    using (AcadDatabase acdb = AcadDatabase.Active())
                    {
                        foreach (ObjectId obj in allBraodcasts.Value.GetObjectIds())
                        {
                            var broadcast = acdb.Element<BlockReference>(obj);
                            var transBlock = broadcast.Clone() as BlockReference;
                            originTransformer.Transform(transBlock);
                            braodcasts.Add(transBlock);
                        }
                    }
                }
                var objs = new DBObjectCollection();
                return braodcasts.Where(o => polyline.Contains(o.Position)).ToList();
            }
        }

        /// <summary>
        /// 获取广播布置点位
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Point3d> GetLayoutBroadcastPoints(AcadDatabase acdb, Polyline polyline, ThMEPOriginTransformer originTransformer)
        {
            //获取广播
            var dxfNames = new string[]
            {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
            };
            var filterlist = OpFilter.Bulid(o =>
            o.Dxf((int)DxfCode.LayerName) == ThMEPCommon.BroadcastLayerName &
            o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
            var braodcasts = new List<BlockReference>();
            var allBraodcasts = Active.Editor.SelectAll(filterlist);
            if (allBraodcasts.Status == PromptStatus.OK)
            {
                foreach (ObjectId obj in allBraodcasts.Value.GetObjectIds())
                {
                    var transBlock = acdb.Element<BlockReference>(obj).Clone() as BlockReference;
                    originTransformer.Transform(transBlock);
                    braodcasts.Add(transBlock);
                }
            }
            var objs = new DBObjectCollection();
            braodcasts.Where(o => polyline.Contains(o.Position)).ForEachDbObject(o => objs.Add(o));

            return objs.Cast<BlockReference>().Select(o => o.Position).ToList();
        }
    }

    #region test
    public class EntityInfo
    {
        public string cad_type { get; set; }
        //public double x_side { get; set; }
        //public double y_side { get; set; }
        public double id { get; set; }
        public string entity_layoutName { get; set; }
        public string entity_layersName { get; set; }
        public List<Point3d> entity_geomExtents { get; set; }
    }
    #endregion
}
