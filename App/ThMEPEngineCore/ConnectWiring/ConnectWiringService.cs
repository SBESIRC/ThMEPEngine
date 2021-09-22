#if (ACAD2016 || ACAD2018)
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
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

namespace ThMEPEngineCore.ConnectWiring
{
    public class ConnectWiringService
    {
        public List<Polyline> Routing(int count, string systemName)
        {
            var lines = new List<Polyline>();
            BlockConfigSrervice configSrervice = new BlockConfigSrervice();
            var configInfo = configSrervice.GetLoopInfo(systemName);
            foreach (var info in configInfo)
            {
                var configBlocks = info.loopInfoModels.First().blockNames;
                var data = GetData(configBlocks);
                if (data != null)
                {
                    ThCableRouterMgd thCableRouter = new ThCableRouterMgd();
                    var res = thCableRouter.RouteCable(data, count);
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



            //var lines = new List<Polyline>();
            //var data = GetData();
            //if (data != null)
            //{
            //    ThCableRouterMgd thCableRouter = new ThCableRouterMgd();
            //    var res = thCableRouter.RouteCable(data, count);
            //    var serializer = GeoJsonSerializer.Create();
            //    using (var stringReader = new StringReader(res))
            //    using (var jsonReader = new JsonTextReader(stringReader))
            //    {
            //        var features = serializer.Deserialize<FeatureCollection>(jsonReader);
            //        foreach (var f in features)
            //        {
            //            if (f.Geometry is LineString line)
            //            {
            //                lines.Add(line.ToDbPolyline());
            //            }
            //        }
            //    }
            //}

            return lines;
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <returns></returns>
        public string GetData(List<string> configBlocks)
        {
            string geoJson = null;
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
                    return geoJson;
                }
                List<Polyline> frameLst = new List<Polyline>();
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
                    return geoJson;
                }
                var block = acadDatabase.Element<BlockReference>(blockResult.Value.GetObjectIds().First());

                var holeInfos = CalHoles(frameLst);
                var outFrame = holeInfos.First().Key;
                var holes = holeInfos.First().Value;
                ThFireAlarmWiringDateSetFactory factory = new ThFireAlarmWiringDateSetFactory(configBlocks);
                factory.holes = holes;
                factory.powerBlock = block;
                var data = factory.Create(acadDatabase.Database, outFrame.Vertices());

                geoJson = ThGeoOutput.Output(data.Container);
            }

            return geoJson;
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