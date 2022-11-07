using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
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
        private Dictionary<string, List<List<string>>> _blockNameConfigList;
        public Dictionary<string, List<List<string>>> BlockNameConfigList
        {
            get { return _blockNameConfigList; }
            set
            {
                _blockNameConfigList = value;
                this.RaisePropertyChanged();
            }
        }
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
            BlockNameConfigList.Add("侧入式雨水斗", new List<List<string>>() {
                new List<string>() { "W-drain-2", "W-drain-5" }, new List<string>() });

            Blocks.Add("重力流雨水斗");
            BlockNameConfigList.Add("重力流雨水斗", new List<List<string>>() {
                new List<string>() { "W-drain-1" }, new List<string>() });

            Blocks.Add("房屋雨水立管");
            BlockNameConfigList.Add("房屋雨水立管", new List<List<string>>() {
                new List<string>() { "W-pipe-1" }, new List<string>() });

            Blocks.Add("阳台立管");
            BlockNameConfigList.Add("阳台立管", new List<List<string>>() {
                new List<string>() { "W-pipe-2" }, new List<string>() });

            Blocks.Add("冷凝立管");
            BlockNameConfigList.Add("冷凝立管", new List<List<string>>() { 
                new List<string>() { "W-pipe-3" }, new List<string>() });

            Blocks.Add("地漏");
            BlockNameConfigList.Add("地漏", new List<List<string>>() {
                new List<string>() { "W-drain-3", "W-drain-4" }, new List<string>() });

            Blocks.Add("拖把池");
            BlockNameConfigList.Add("拖把池", new List<List<string>>() { 
                new List<string>() { "A-Kitchen-9" }, new List<string>() });

            Blocks.Add("洗衣机");
            BlockNameConfigList.Add("洗衣机", new List<List<string>>() {
                new List<string>() { "A-Toilet-9" }, new List<string>() });

            Blocks.Add("厨房洗涤盆");
            BlockNameConfigList.Add("厨房洗涤盆", new List<List<string>>() {
                new List<string>() { "A-Kitchen-4" }, new List<string>() });

            Blocks.Add("坐便器");
            BlockNameConfigList.Add("坐便器", new List<List<string>>() {
                new List<string>() { "A-Toilet-5" }, new List<string>() });

            Blocks.Add("单盆洗手台");
            BlockNameConfigList.Add("单盆洗手台", new List<List<string>>() {
                new List<string>() { "A-Toilet-1", "A-Toilet-3", "A-Toilet-4" }, new List<string>() });

            Blocks.Add("双盆洗手台");
            BlockNameConfigList.Add("双盆洗手台", new List<List<string>>() { 
                new List<string>() { "A-Toilet-2" }, new List<string>() });

            Blocks.Add("淋浴器");
            BlockNameConfigList.Add("淋浴器", new List<List<string>>() { 
                new List<string>() { "A-Toilet-6", "A-Toilet-7" }, new List<string>() });

            Blocks.Add("浴缸");
            BlockNameConfigList.Add("浴缸", new List<List<string>>() { 
                new List<string>() { "A-Toilet-8" }, new List<string>() });

            Blocks.Add("集水井");
            BlockNameConfigList.Add("集水井", new List<List<string>>() { 
                new List<string>(), new List<string>() });

            Blocks.Add("非机械车位");
            BlockNameConfigList.Add("非机械车位", new List<List<string>>() { 
                new List<string>(), new List<string>() });

            Blocks.Add("机械车位");
            BlockNameConfigList.Add("机械车位", new List<List<string>>() { 
                new List<string>(), new List<string>() });

            Blocks.Add("空调内机--挂机");
            BlockNameConfigList.Add("空调内机--挂机", new List<List<string>>() { 
                new List<string>(), new List<string>() });

            Blocks.Add("空调内机--柜机");
            BlockNameConfigList.Add("空调内机--柜机", new List<List<string>>() { 
                new List<string>(), new List<string>() });

            Blocks.Add("门块");
            BlockNameConfigList.Add("门块", new List<List<string>>() { 
                new List<string>(), new List<string>() });
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


