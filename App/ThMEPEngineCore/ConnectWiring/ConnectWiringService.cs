using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.ConnectWiring.Data;
using ThMEPEngineCore.IO;

namespace ThMEPEngineCore.ConnectWiring
{
    public class ConnectWiringService
    {
        public void Routing()
        {
            
        }

        public void GetData()
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
                    return;
                }
                var block = acadDatabase.Element<BlockReference>(blockResult.Value.GetObjectIds().First());

                var holeInfos = CalHoles(frameLst);
                var outFrame = holeInfos.First().Key;
                var holes = holeInfos.First().Value;
                ThFireAlarmWiringDateSetFactory factory = new ThFireAlarmWiringDateSetFactory();
                factory.holes = holes;
                factory.powerBlock = block;
                var data = factory.Create(acadDatabase.Database, outFrame.Vertices());

                var geoJson = ThGeoOutput.Output(data.Container);
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

                var bufferFrames = firFrame.Buffer(1)[0] as Polyline;
                var holes = frames.Where(x => bufferFrames.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));

                holeDic.Add(firFrame, holes);
            }

            return holeDic;
        }
    }
}
