#if (ACAD2016 || ACAD2018)
using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CLI;
using Linq2Acad;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.ConnectWiring.Data;
using ThMEPEngineCore.ConnectWiring.Model;
using ThMEPEngineCore.ConnectWiring.Service;
using ThMEPEngineCore.ConnectWiring.Service.ConnectFactory;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using Dreambuild.AutoCAD;

namespace ThMEPEngineCore.ConnectWiring
{
    public class ConnectWiringService
    {
        public void Routing(List<WiringLoopModel> configInfo, bool wall = false, bool column = false, List<string> allConfigBlocks = null)
        {
            //获取所有的块
            var ConfigBlocks = configInfo.SelectMany(x => x.loopInfoModels.First().blocks.Select(y => y.blockName)).ToList();
            if (!ConfigBlocks.Any())
            {
                return;
            }
            if (allConfigBlocks.IsNull())
            {
                allConfigBlocks = ConfigBlocks;
            }
            GetPickData(out List<Polyline> holes, out Polyline outFrame, out BlockReference block);
            if (outFrame == null || block == null)
            {
                return;
            }
            ThBlockPointsExtractor thBlockPointsExtractor = new ThBlockPointsExtractor(allConfigBlocks);
            using (AcadDatabase db = AcadDatabase.Active())
            {
                db.Database.CreateAILayer("0", 255);
                thBlockPointsExtractor.Extract(db.Database, outFrame.Vertices());
            }
            var allBlocks = thBlockPointsExtractor.resBlocks.Where(x => !x.BlockTableRecord.IsNull && !(x.Database is null)).ToList();
            //BranchConnectingService branchConnecting = new BranchConnectingService();
            BranchConnectingFactory connectingFactory = new BranchConnectingFactory();
            MultiLoopService multiLoopService = new MultiLoopService();
            var data = GetData(holes, outFrame, block, !wall, !column);

            var CenterLine = new List<ThGeometry>();
            if (Convert.ToInt16(Application.GetSystemVariable("USERR3")) == 1)
            {
                CenterLine = GetCenterLinePolylines(out DBObjectCollection objs);
                //CenterLine.AddRange(GetUCSPolylines(outFrame,objs));
            }

            //新增 处理超远问题
            ThMEPOriginTransformer Transformer = new ThMEPOriginTransformer(outFrame.StartPoint);
            data.ForEach(o => Transformer.Transform(o.Boundary));
            CenterLine.ForEach(o => Transformer.Transform(o.Boundary));

            string Inputpath = "{0}_{1}_MAInput.geojson";
            string Outputpath = "{0}_{1}_Output.geojson";
            foreach (var info in configInfo)
            {
                DeleteOriginalLine(outFrame.Clone() as Polyline, Transformer, info.loopInfoModels.Select(o => o.LineType).ToList());
                var LoopName = info.loopInfoModels[0].LineContent;
                var blockInfos = info.loopInfoModels.First().blocks;
                var configBlocks = blockInfos.Select(x => x.blockName);
                var resBlocks = allBlocks
                .Where(x =>
                {
                    var name = x.Name;
                    return configBlocks.Contains(name);
                }).ToList();
                if (data.Count > 0 && resBlocks.Count > 0)
                {
                    var maxNum = info.loopInfoModels.First().PointNum;
                    if (info.loopInfoModels.First().LineType == "E-FAS-WIRE4")
                    {
                        maxNum = 1;
                    }

                    var blockGeos = GetBlockPts(resBlocks);
                    ThCableRouterMgd thCableRouter = new ThCableRouterMgd();
                    ThCableRouteContextMgd context = new ThCableRouteContextMgd()
                    {
                        MaxLoopCount = maxNum,
                    };
                    var allDatas = new List<ThGeometry>(data);
                    allDatas.AddRange(CenterLine);
                    blockGeos.ForEach(o => Transformer.Transform(o.Boundary));
                    allDatas.AddRange(blockGeos);
                    var blockHoles = GetBlockHoles(allBlocks, resBlocks);
                    blockHoles.ForEach(o => Transformer.Transform(o.Boundary));
                    allDatas.AddRange(blockHoles);
                    var dataGeoJson = ThGeoOutput.Output(allDatas);

                    if (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1)
                    {
                        string path = Path.Combine(Active.DocumentDirectory, string.Format(Inputpath, Active.DocumentName, LoopName));
                        File.WriteAllText(path, dataGeoJson);
                    }
                    var res = thCableRouter.RouteCable(dataGeoJson, context);
                    if (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1)
                    {
                        string path = Path.Combine(Active.DocumentDirectory, string.Format(Outputpath, Active.DocumentName, LoopName));
                        File.WriteAllText(path, res);
                    }
                    if (!res.Contains("error"))
                    {
                        var lines = new List<Polyline>();
                        var serializer = GeoJsonSerializer.Create();
                        using (var stringReader = new StringReader(res))
                        using (var jsonReader = new JsonTextReader(stringReader))
                        {
                            var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                            foreach (var f in features)
                            {
                                if (f.Geometry is LineString line)
                                {
                                    lines.Add(line.ToDbPolyline());
                                }
                            }
                        }

                        Dictionary<LoopInfoModel, List<Polyline>> loops = new Dictionary<LoopInfoModel, List<Polyline>>() { { info.loopInfoModels.First(), lines } };
                        resBlocks = resBlocks.Select(o =>
                        {
                            var entity = o.Clone() as BlockReference;
                            Transformer.Transform(entity);
                            entity.ProjectOntoXYPlane();
                            return entity;
                        }).ToList();
                        if (info.loopInfoModels.Count > 1)
                        {
                            loops = multiLoopService.CreateLoop(info, lines, resBlocks);
                        }
                        foreach (var loop in loops)
                        {
                            List<Polyline> resLines = new List<Polyline>();
                            foreach (var line in loop.Value)
                            {
                                //var wiring = branchConnecting.CreateBranch(line, resBlocks);
                                var wiring = connectingFactory.BranchConnect(line, resBlocks, blockInfos);
                                if (wiring.NumberOfVertices > 1)
                                {
                                    resLines.Add(wiring);
                                }
                            }
                            resLines.ForEach(o => Transformer.Reset(o));
                            //插入线
                            LineTypeService.InsertConnectPipe(resLines, loop.Key.LineType);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取交互数据
        /// </summary>
        /// <param name="frameLst"></param>
        /// <param name="block"></param>
        public void GetPickData(out List<Polyline> holes, out Polyline outFrame, out BlockReference block)
        {
            block = null;
            holes = new List<Polyline>();
            outFrame = null;
            var frameLst = new List<Polyline>();
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
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(obj);
                    frameLst.Add(ThMEPFrameService.Normalize(frame.Clone() as Polyline));
                }

                //获取电源箱
                PromptSelectionOptions blockOptions = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择电源箱",
                    RejectObjectsOnLockedLayers = true,
                    SingleOnly = true,
                };
                var blockDxfNames = new string[]
                {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
                };
                var blockFilter = ThSelectionFilterTool.Build(blockDxfNames);
                var blockResult = Active.Editor.GetSelection(blockOptions, blockFilter);
                if (blockResult.Status != PromptStatus.OK)
                {
                    return;
                }
                block = acadDatabase.Element<BlockReference>(blockResult.Value.GetObjectIds().First());

                var holeInfos = CalHoles(frameLst);
                outFrame = holeInfos.First().Key;
                holes = holeInfos.First().Value;
            }
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <returns></returns>
        public List<ThGeometry> GetData(List<Polyline> holes, Polyline outFrame, BlockReference block, bool wall, bool column)
        {
            List<ThGeometry> geos = new List<ThGeometry>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThFireAlarmWiringDateSetFactory factory = new ThFireAlarmWiringDateSetFactory(wall, column);
                factory.holes = holes;
                factory.powerBlock = block;
                var data = factory.Create(acadDatabase.Database, outFrame.Vertices());
                geos = data.Container;
            }

            return geos;
        }

        /// <summary>
        /// 除连线块意外其他块当作洞口处理
        /// </summary>
        /// <param name="allBlocks"></param>
        /// <param name="resBlocks"></param>
        /// <returns></returns>
        private List<ThGeometry> GetBlockHoles(List<BlockReference> allBlocks, List<BlockReference> resBlocks)
        {
            var holeBlocks = allBlocks.Except(resBlocks).ToList();
            var geos = new List<ThGeometry>();
            if (holeBlocks.Count > 0)
            {
                holeBlocks.ForEach(o =>
                {
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Hole.ToString());
                    geometry.Boundary = o.ToOBB(o.BlockTransform);
                    geos.Add(geometry);
                });
            }

            return geos;
        }

        /// <summary>
        /// 获取连接点位
        /// </summary>
        /// <returns></returns>
        private List<ThGeometry> GetBlockPts(List<BlockReference> allBlocks)
        {
            var geos = new List<ThGeometry>();
            if (allBlocks.Count > 0)
            {
                allBlocks.ForEach(o =>
                {
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.WiringPosition.ToString());
                    geometry.Boundary = new DBPoint(o.Position);
                    geos.Add(geometry);
                });
            }

            return geos;
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
        /// 获取ucs框线
        /// </summary>
        /// <returns></returns>
        private List<ThGeometry> GetUCSPolylines(DBObjectCollection centerPolygon)
        {
            var geos = new List<ThGeometry>();
            List<Polyline> allUCSPolys = new List<Polyline>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择UCS框线区域",
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
                    return geos;
                }
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(obj);
                    allUCSPolys.Add(ThMEPFrameService.Normalize(frame.Clone() as Polyline));
                }
            }
            ThCADCoreNTSSpatialIndex thbeamsSpatialIndex = new ThCADCoreNTSSpatialIndex(centerPolygon);
            if (allUCSPolys.Count > 0)
            {
                allUCSPolys.ForEach(o =>
                {
                    var polys = thbeamsSpatialIndex.SelectCrossingPolygon(o);
                    var resPoly = ThMPolygonTool.CreateMPolygon(o, polys.Cast<Curve>().ToList());
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.UCSPolyline.ToString());
                    geometry.Properties.Add(ThExtractorPropertyNameManager.VectorName, new Vector3d(1, 0, 0).ToString());
                    geometry.Boundary = resPoly;
                    geos.Add(geometry);
                });
            }

            return geos;
        }

        private List<ThGeometry> GetUCSPolylines(Polyline frame, DBObjectCollection centerPolygon)
        {
            var geos = new List<ThGeometry>();
            List<Entity> allUCSPolys = new List<Entity>();
            var objs = frame.DifferenceMP(centerPolygon);
            if (objs.Count > 0)
            {
                foreach (Entity entity in objs)
                {
                    if (entity is Polyline polyline)
                    {
                        if (polyline.Area < 10000)
                            continue;
                    }
                    else if (entity is MPolygon mPolygon)
                    {
                        if (mPolygon.Area < 10000)
                            continue;
                    }
                    else
                    {
                        continue;
                    }
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.UCSPolyline.ToString());
                    geometry.Properties.Add(ThExtractorPropertyNameManager.VectorName, new Vector3d(1, 0, 0).ToString());
                    geometry.Boundary = entity;
                    geos.Add(geometry);
                }
            }
            return geos;
        }

        /// <summary>
        /// 获取中心线区域框线
        /// </summary>
        /// <returns></returns>
        private List<ThGeometry> GetCenterLinePolylines(out DBObjectCollection objs)
        {
            objs = new DBObjectCollection();
            var geos = new List<ThGeometry>();
            List<Polyline> allUCSPolys = new List<Polyline>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择中心线框线区域",
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
                    return geos;
                }
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(obj);
                    allUCSPolys.Add(ThMEPFrameService.Normalize(frame.Clone() as Polyline));
                }
            }

            foreach (var item in allUCSPolys)
            {
                objs.Add(item);
            }
            MPolygon mPolygon = objs.BuildMPolygon();
            var geometry = new ThGeometry();
            geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.CenterPolyline.ToString());
            geometry.Boundary = mPolygon;
            geos.Add(geometry);
            //if (allUCSPolys.Count > 0)
            //{
            //    allUCSPolys.ForEach(o =>
            //    {
            //        var geometry = new ThGeometry();
            //        geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.CenterPolyline.ToString());
            //        geometry.Boundary = o;
            //        geos.Add(geometry);
            //    });
            //}

            return geos;
        }

        /// <summary>
        /// 删除原有线
        /// </summary>
        /// <param name="Boundary"></param>
        private void DeleteOriginalLine(Polyline Boundary, ThMEPOriginTransformer Transformer, List<string> layers)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                var lines = acad.ModelSpace
                    .OfType<Polyline>()
                    .Where(p => layers.Contains(p.Layer));
                var LineDic = lines.ToDictionary(key => key.Clone() as Polyline, value => value.Id);
                var objs = new DBObjectCollection();
                LineDic.ForEach(x =>
                {
                    var transCurve = x.Key;
                    Transformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
                Transformer.Transform(Boundary);
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var dbobjs = spatialIndex.SelectWindowPolygon(Boundary);
                var objIDs =LineDic.Where(o => dbobjs.Contains(o.Key)).Select(o => o.Value).ToObjectIdCollection();
                foreach (ObjectId objId in objIDs)
                {
                    acad.Element<Polyline>(objId, true).Erase();
                }
            }
        }
    }
}
#endif