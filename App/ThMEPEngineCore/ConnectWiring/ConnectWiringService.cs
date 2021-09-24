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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.ConnectWiring.Data;
using ThMEPEngineCore.ConnectWiring.Service;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.ConnectWiring
{
    public class ConnectWiringService
    {
        public List<Polyline> Routing(int count, string systemName)
        {
            var lines = new List<Polyline>();
            GetPickData(out List<Polyline> holes, out Polyline outFrame, out BlockReference block);
            if (outFrame == null || block == null)
            {
                return lines;
            }

            BlockConfigSrervice configSrervice = new BlockConfigSrervice();
            var configInfo = configSrervice.GetLoopInfo(systemName);

            //获取所有的块
            var allConfigBlocks = configInfo.SelectMany(x => x.loopInfoModels.First().blockNames).ToList();
            ThBlockPointsExtractor thBlockPointsExtractor = new ThBlockPointsExtractor(allConfigBlocks);
            using (AcadDatabase db = AcadDatabase.Active())
            {
                thBlockPointsExtractor.Extract(db.Database, outFrame.Vertices());
            }
            var allBlocks = thBlockPointsExtractor.resBlocks;

            var data = GetData(holes, outFrame, block);
            foreach (var info in configInfo)
            {
                var configBlocks = info.loopInfoModels.First().blockNames;
                var blockGeos = GetBlockPts(allBlocks, configBlocks);
                if (data.Count > 0 && blockGeos.Count > 0)
                {
                    ThCableRouterMgd thCableRouter = new ThCableRouterMgd();
                    var allDatas = new List<ThGeometry>(data);
                    allDatas.AddRange(blockGeos);
                    var dataGeoJson = ThGeoOutput.Output(allDatas);
                    var res = thCableRouter.RouteCable(dataGeoJson, count);
                    if (!res.Contains("error"))
                    {
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
                    }
                }
            }

            using (Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase db = AcadDatabase.Active())
            {
                foreach (var item in lines)
                {
                    db.ModelSpace.Add(item);
                }
            }
            return lines;
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
                    MessageForAdding = "选择区域",
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
        public List<ThGeometry> GetData(List<Polyline> holes, Polyline outFrame, BlockReference block)
        {
            List<ThGeometry> geos = new List<ThGeometry>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThFireAlarmWiringDateSetFactory factory = new ThFireAlarmWiringDateSetFactory();
                factory.holes = holes;
                factory.powerBlock = block;
                var data = factory.Create(acadDatabase.Database, outFrame.Vertices());
                geos = data.Container;
            }

            return geos;
        }

        /// <summary>
        /// 获取连接点位
        /// </summary>
        /// <returns></returns>
        private List<ThGeometry> GetBlockPts(List<BlockReference> allBlocks, List<string> blockNames)
        {
            var geos = new List<ThGeometry>();
            if (allBlocks.Count > 0 && blockNames.Count > 0)
            {
                var resBlock = allBlocks.Where(x => !x.BlockTableRecord.IsNull)
                .Where(x =>
                {
                    var name = x.GetEffectiveName();
                    return blockNames.Contains(name);
                }).ToList();
                resBlock.ForEach(o =>
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
    }
}
#endif