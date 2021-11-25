using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using ThControlLibraryWPF.ControlUtils;
using cadGraph = Autodesk.AutoCAD.GraphicsInterface;

namespace ThMEPWSS.ViewModel
{
    public static class BlockConfigService
    {
        public static readonly BlockConfigViewModel Instance = new BlockConfigViewModel();
        public static Dictionary<string, List<string>> GetBlockNameListDict()
        {
            var viewModel = Instance;
            var dic = new Dictionary<string, List<string>>();
            foreach (var key in viewModel.BlockNameConfigList.Keys)
            {
                dic.Add(key, viewModel.BlockNameConfigList[key].First());
                dic[key].AddRange(viewModel.BlockNameConfigList[key].Last());
            }
            foreach (var key in viewModel.BlockNameList.Keys)
            {
                dic[key].AddRange(viewModel.BlockNameList[key].Select(e => e.layerName));
            }
            return dic;
        }
    }
    public class BlockConfigViewModel : NotifyPropertyChangedBase
    {
        public Dictionary<string, List<List<string>>> BlockNameConfigList { get; set; }
        public Dictionary<string, ObservableCollection<BlockNameConfigViewModel>> BlockNameList { get; set; }
        public Dictionary<string, Dictionary<string,DBObjectCollection>> BlockNestedEntityFrames { get; set; }
        private List<string> Blocks { get; set; }

        public BlockConfigViewModel()
        {
            BlockNestedEntityFrames = new Dictionary<string, Dictionary<string, DBObjectCollection>>();
            BlockNameConfigList = new Dictionary<string, List<List<string>>>();
            CreateBlockList();

            BlockNameList = new Dictionary<string, ObservableCollection<BlockNameConfigViewModel>>();
            foreach (string block in Blocks)
            {
                BlockNameList.Add(block, new ObservableCollection<BlockNameConfigViewModel>());
            }
        }

        public BlockConfigSetViewModel SetViewModel { get; set; } = new BlockConfigSetViewModel();

        private void CreateBlockList()
        {
            Blocks = new List<string>();
            Blocks.Add("侧入式雨水斗");
            var ls1 = new List<string>() { "W-drain-2", "W-drain-5" };
            var ls2 = new List<string>();
            BlockNameConfigList.Add("侧入式雨水斗", new List<List<string>>() { ls1, ls2 });

            Blocks.Add("重力流雨水斗");
            var ls3 = new List<string>() { "W-drain-1" };
            var ls4 = new List<string>();
            BlockNameConfigList.Add("重力流雨水斗", new List<List<string>>() { ls3, ls4 });

            Blocks.Add("房屋雨水立管");
            var ls5 = new List<string>() { "W-pipe-1" };
            var ls6 = new List<string>();
            BlockNameConfigList.Add("房屋雨水立管", new List<List<string>>() { ls5, ls6 });

            Blocks.Add("阳台立管");
            var ls7 = new List<string>() { "W-pipe-2" };
            var ls8 = new List<string>();
            BlockNameConfigList.Add("阳台立管", new List<List<string>>() { ls7, ls8 });

            Blocks.Add("冷凝立管");
            var ls9 = new List<string>() { "W-pipe-3" };
            var ls10 = new List<string>();
            BlockNameConfigList.Add("冷凝立管", new List<List<string>>() { ls9, ls10 });

            Blocks.Add("地漏");
            var ls11 = new List<string>() { "W-drain-3", "W-drain-4" };
            var ls12 = new List<string>();
            BlockNameConfigList.Add("地漏", new List<List<string>>() { ls11, ls12 });

            Blocks.Add("拖把池");
            var ls13 = new List<string>() { "A-Kitchen-9" };
            var ls14 = new List<string>();
            BlockNameConfigList.Add("拖把池", new List<List<string>>() { ls13, ls14 });

            Blocks.Add("洗衣机");
            var ls15 = new List<string>() { "A-Toilet-9" };
            var ls16 = new List<string>();
            BlockNameConfigList.Add("洗衣机", new List<List<string>>() { ls15, ls16 });

            Blocks.Add("厨房洗涤盆");
            var ls17 = new List<string>() { "A-Kitchen-4" };
            var ls18 = new List<string>();
            BlockNameConfigList.Add("厨房洗涤盆", new List<List<string>>() { ls17, ls18 });

            Blocks.Add("坐便器");
            var ls19 = new List<string>() { "A-Toilet-5" };
            var ls20 = new List<string>();
            BlockNameConfigList.Add("坐便器", new List<List<string>>() { ls19, ls20 });

            Blocks.Add("单盆洗手台");
            var ls21 = new List<string>() { "A-Toilet-1", "A-Toilet-3", "A-Toilet-4" };
            var ls22 = new List<string>();
            BlockNameConfigList.Add("单盆洗手台", new List<List<string>>() { ls21, ls22 });

            Blocks.Add("双盆洗手台");
            var ls23 = new List<string>() { "A-Toilet-2" };
            var ls24 = new List<string>();
            BlockNameConfigList.Add("双盆洗手台", new List<List<string>>() { ls23, ls24 });

            Blocks.Add("淋浴器");
            var ls25 = new List<string>() { "A-Toilet-6", "A-Toilet-7" };
            var ls26 = new List<string>();
            BlockNameConfigList.Add("淋浴器", new List<List<string>>() { ls25, ls26 });

            Blocks.Add("浴缸");
            var ls27 = new List<string>() { "A-Toilet-8" };
            var ls28 = new List<string>();
            BlockNameConfigList.Add("浴缸", new List<List<string>>() { ls27, ls28 });

            Blocks.Add("集水井");
            var ls29 = new List<string>() { "A-Well-1" };
            var ls30 = new List<string>();
            BlockNameConfigList.Add("集水井", new List<List<string>>() { ls29, ls30 });

            Blocks.Add("非机械车位");
            var ls31 = new List<string>();
            var ls32 = new List<string>();
            BlockNameConfigList.Add("非机械车位", new List<List<string>>() { ls31, ls32 });

            Blocks.Add("机械车位");
            BlockNameConfigList.Add("机械车位", new List<List<string>>() { new List<string>(), new List<string>() });



            Blocks.Add("空调内机--挂机");
            BlockNameConfigList.Add("空调内机--挂机", new List<List<string>>() { new List<string>(), new List<string>() });

            Blocks.Add("空调内机--柜机");
            BlockNameConfigList.Add("空调内机--柜机", new List<List<string>>() { new List<string>(), new List<string>() });


        }
        public void AddToTransient(string blkConfigName)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                if (BlockNestedEntityFrames.ContainsKey(blkConfigName))
                {
                    BlockNestedEntityFrames[blkConfigName].ForEach(o =>
                    {
                        o.Value.OfType<Curve>().ForEach(o =>
                        {
                            tm.AddTransient(o, cadGraph.TransientDrawingMode.Highlight, 128, intCol);
                        });
                    });
                }
            }
        }
        public void ClearTransientGraphics()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                BlockNestedEntityFrames.Values.ForEach(o =>
                {
                    o.ForEach(o =>
                    {
                        o.Value.OfType<Curve>().ForEach(o =>
                        {
                            tm.EraseTransient(o, intCol);
                            //o.Dispose();
                        });
                    });
                });
            }
        }
    }
}


