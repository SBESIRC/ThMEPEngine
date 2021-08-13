using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThConcaveBuilder
    {
        private DBObjectCollection Elements { get; set; }
        private double TesslateLength { get; set; }
        public double Hershold { get; set; }
        public ThConcaveBuilder(DBObjectCollection objs, double tesslateLength)
        {
            Elements = objs;
            Hershold = tesslateLength*1.2; //建议值
            TesslateLength = tesslateLength;
        }
        public DBObjectCollection Build()
        {
            // 移动
            var transform = new ThMEPOriginTransformer(Elements);
            transform.Transform(Elements);

            // 打散
            var polys = Tesslate(Elements);

            // 炸线
            var lines = Expode(polys);

            // 构建
            var concaveHull = new ThCADCoreNTSConcaveHull(lines.ToMultiLineString(), Hershold);
            var results = concaveHull.getConcaveHull().ToDbCollection();

            // 还原
            transform.Reset(results);
            transform.Reset(Elements);
            return results;
        }
        private DBObjectCollection Tesslate(DBObjectCollection objs)
        {
            var tesslateService = new ThTesslateService();
            return objs
                .Cast<Entity>()
                .Select(o => ThTesslateService.Tesslate(o, TesslateLength, true))
                .ToCollection();
        }
        private DBObjectCollection Expode(DBObjectCollection polys)
        {
            var lines = new DBObjectCollection();
            polys.Cast<Polyline>().ForEach(e =>
            {
                var lineObjs = new DBObjectCollection();
                e.Explode(lineObjs);
                lineObjs.Cast<Line>().ForEach(l => lines.Add(l));
            });
            return lines;
        }
    }
}
