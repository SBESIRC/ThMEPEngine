using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPStructure.Reinforcement.Service;

namespace ThMEPStructure.Reinforcement.Data.YJK
{
    /// <summary>
    /// 提取墙
    /// </summary>
    public class ThExtractLeaderMarkService
    {
        public Dictionary<string, DBObjectCollection> MarkLines { get; set; }
        public Dictionary<string, DBObjectCollection> MarkTexts { get; set; }
        private List<string> TextLayers { get; set; }
        public ThExtractLeaderMarkService(List<string> textLayers)
        {
            TextLayers = textLayers;
            MarkLines = new Dictionary<string, DBObjectCollection>();
            MarkTexts = new Dictionary<string, DBObjectCollection>();
        }
        public void Extract(Database db,Point3dCollection pts)
        {
            // 获取指定图层的对象
            var objs = GetLeaderMarks(db, TextLayers);
            objs = objs.OfType<Entity>().Where(e => e is DBText || e is Line).ToCollection();

            // 按图层对Objs分类
            var markLines = Classify(objs.OfType<Line>().ToCollection());
            var markTexts = Classify(objs.OfType<DBText>().ToCollection());

            // 
            markLines.ForEach(o =>
            {
                var clones = o.Value.CloneEx();
                var transformer = new ThMEPOriginTransformer(clones);
                transformer.Transform(clones);
                var newPts = transformer.Transform(pts);
                var results = clones.SelectCrossPolygon(newPts);
                var restObjs = clones.Difference(results);
                restObjs.DisposeEx();
                transformer.Reset(results);
                MarkLines.Add(o.Key,results);
            });

            //
            markTexts.ForEach(o =>
            {
                var clones = o.Value.CloneEx();
                var transformer = new ThMEPOriginTransformer(clones);
                transformer.Transform(clones);
                var newPts = transformer.Transform(pts);
                var results = clones.SelectCrossPolygon(newPts);
                var restObjs = clones.Difference(results);
                restObjs.DisposeEx();
                transformer.Reset(results);
                MarkTexts.Add(o.Key, results);
            });
        }

        private DBObjectCollection GetLeaderMarks(Database database,List<string> layerFilter)
        {
            var visitor = new ThLeaderMarkExtractionVisitor()
            {
                LayerFilter = layerFilter,
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(visitor);
            extractor.Extract(database);
            extractor.ExtractFromMS(database);
            return visitor.Results.Select(o => o.Geometry).ToCollection();
        }

        private Dictionary<string, DBObjectCollection> Classify(DBObjectCollection objs)
        {
            var results = new Dictionary<string, DBObjectCollection>();
            objs.OfType<Entity>().ForEach(e =>
            {
                if (results.ContainsKey(e.Layer))
                {
                    results[e.Layer].Add(e);
                }
                else
                {
                    results.Add(e.Layer,new DBObjectCollection() { e});
                }
            });
            return results;
        }
    }
}
