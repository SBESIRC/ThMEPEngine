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
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Colors;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using ThMEPEngineCore.Algorithm;
using ThCADCore.NTS;
using ThCADExtension;
using System;
using ThMEPEngineCore.Engine;
using System.IO;

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
        public Dictionary<string, List<Polyline>> BlockNestedEntityFrames { get; set; }
        private List<string> Blocks { get; set; }

        public BlockConfigViewModel()
        {
            BlockNestedEntityFrames = new Dictionary<string, List<Polyline>>();
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
            var ls29 = new List<string>() { };
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

            Blocks.Add("门块");
            //什么鬼XD，写作List实际上是Dict
            BlockNameConfigList.Add("门块", new List<List<string>>() { new List<string>(), new List<string>() });

            UpdateBlockList();
        }
        static readonly Dictionary<int, string> dict = new Dictionary<int, string>() { { 3, "洗脸盆" }, { 4, "洗涤槽" }, { 5, "拖把池" }, { 6, "洗衣机" }, { 8, "淋浴房" }, { 9, "转角淋浴房" }, { 10, "浴缸" }, { 11, "喷头" }, { 0, "坐便器" }, { 1, "小便器" }, { 2, "蹲便器" }, { 12, "地漏" } };
        public void UpdateBlockList()
        {
            var file = @"E:\feng\企业微信文件\WXWork\1688850279863586\Cache\File\2022-09\1 - 副本.csv";
            foreach (var line in File.ReadAllLines(file).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var arr = line.Split(',');
                if (!int.TryParse(arr[1], out var typeId)) continue;
                var blkName = arr[0].Replace(".jpg", "");
                try
                {
                    (BlockNameConfigList[dict[typeId]] ??= new List<List<string>>() { new List<string>(), new List<string>() })[1].Add(blkName);
                }
                catch { }
            }
        }
        public void AddToTransient(string blkConfigName)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tm = cadGraph.TransientManager.CurrentTransientManager;
                IntegerCollection intCol = new IntegerCollection();
                if (BlockNestedEntityFrames.ContainsKey(blkConfigName))
                {
                    BlockNestedEntityFrames[blkConfigName].OfType<Curve>().ForEach(o =>
                    {
                        tm.AddTransient(o, cadGraph.TransientDrawingMode.Highlight, 1, intCol);
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
                    o.OfType<Curve>().ForEach(o =>
                    {
                        tm.EraseTransient(o, intCol);
                    });
                });
            }
        }
        public void Show(string blockName, Database db)
        {
            if (BlockNestedEntityFrames.ContainsKey(blockName))
            {
                BlockNestedEntityFrames[blockName].Clear();
                BlockNestedEntityFrames.Remove(blockName);
            }
            List<string> blockNames = new List<string>();
            BlockNameList[blockName].ForEach(o =>
            {
                blockNames.Add(o.layerName);
            });
            var blks = ExtractBlocks(db, blockNames);
            var bufferService = new ThNTSBufferService();
            List<Polyline> newFrames = new List<Polyline>();
            foreach (BlockReference blk in blks)
            {
                var obb = CreateFrame(blk);
                var newFrame = bufferService.Buffer(obb, 100.0) as Polyline;
                newFrame.Color = Color.FromRgb(255, 0, 0);
                newFrame.ConstantWidth = 100;
                newFrames.Add(newFrame);
            }
            BlockNestedEntityFrames.Add(blockName, newFrames);
        }
        public Polyline CreateFrame(BlockReference br)
        {
            var objs = ThDrawTool.Explode(br);
            var curves = objs.OfType<Entity>()
                .Where(e => e is Curve).ToCollection();
            curves = Tesslate(curves);
            curves = curves.OfType<Curve>().Where(o => o != null && o.GetLength() > 1e-6).ToCollection();
            var transformer = new ThMEPOriginTransformer(curves);
            transformer.Transform(curves);
            var obb = curves.GetMinimumRectangle();
            transformer.Reset(obb);
            return obb;
        }

        private DBObjectCollection Tesslate(DBObjectCollection curves,
            double arcLength = 50.0, double chordHeight = 50.0)
        {
            var results = new DBObjectCollection();
            curves.OfType<Curve>().ToList().ForEach(o =>
            {
                if (o is Line)
                {
                    results.Add(o);
                }
                else if (o is Arc arc)
                {
                    results.Add(arc.TessellateArcWithArc(arcLength));
                }
                else if (o is Circle circle)
                {
                    results.Add(circle.TessellateCircleWithArc(arcLength));
                }
                else if (o is Polyline polyline)
                {
                    results.Add(polyline.TessellatePolylineWithArc(arcLength));
                }
                else if (o is Ellipse ellipse)
                {
                    results.Add(ellipse.Tessellate(chordHeight));
                }
                else if (o is Spline spline)
                {
                    results.Add(spline.Tessellate(chordHeight));
                }
            });
            return results;
        }
        private DBObjectCollection ExtractBlocks(Database db, List<string> blockNames)
        {
            Func<Entity, bool> IsBlkNameQualified = (e) =>
            {
                if (e is BlockReference br)
                {
                    var blkName = br.GetEffectiveName().ToUpper();
                    return blockNames.Where(o => blkName.EndsWith(o.ToUpper())).Any();
                }
                return false;
            };
            var blkVisitor = new ThBlockReferenceExtractionVisitor();
            blkVisitor.CheckQualifiedLayer = (e) => true;
            blkVisitor.CheckQualifiedBlockName = IsBlkNameQualified;

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(blkVisitor);
            extractor.ExtractFromMS(db);
            extractor.Extract(db);
            return blkVisitor.Results.Select(o => o.Geometry).ToCollection();
        }
    }
    public class ThBlockReferenceExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        public ThBlockReferenceExtractionVisitor()
        {
            CheckQualifiedLayer = base.CheckLayerValid;
            CheckQualifiedBlockName = (Entity entity) => true;
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                elements.AddRange(Handle(br, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements,
            BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        private List<ThRawIfcDistributionElementData> Handle(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(br) && CheckLayerValid(br))
            {
                var clone = br.Clone() as BlockReference;
                if (clone != null)
                {
                    clone.TransformBy(matrix);
                    results.Add(new ThRawIfcDistributionElementData()
                    {
                        Geometry = clone,
                    });
                }
            }
            return results;
        }
        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                return false;
            }
        }
        public override bool IsDistributionElement(Entity entity)
        {
            return CheckQualifiedBlockName(entity);
        }
        public override bool CheckLayerValid(Entity curve)
        {
            return CheckQualifiedLayer(curve);
        }

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }
            return true;
        }
    }
}


