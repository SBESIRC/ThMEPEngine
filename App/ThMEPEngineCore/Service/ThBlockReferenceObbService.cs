using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Service
{
    public class ThBlockReferenceObbService
    {
        /// <summary>
        /// 椭圆打散的弦高
        /// </summary>
        public double ChordHeight { get; set; }
        /// <summary>
        /// 圆弧打散的长度
        /// </summary>
        public double ArcTesslateLength { get; set; }
        public ThBlockReferenceObbService()
        {            
        }

        /// <summary>
        /// 获取块中的Curve对象，生成OBB
        /// </summary>
        /// <param name="br"></param>
        /// <returns>obb有可能为null</returns>
        public Polyline ToObb(BlockReference br)
        {            
            CheckTesslateLength();            
            var objs = Explode(br);            
            var lines = ExplodeToLines(objs.OfType<Curve>().ToCollection());
            var obb = CalculateObb(lines);
            var garbages = new DBObjectCollection();
            garbages = garbages.Union(objs);
            garbages = garbages.Union(lines);
            garbages.MDispose();
            return obb;
        }

        private void CheckTesslateLength()
        {
            if (ChordHeight <= 0.0)
            {
                ChordHeight = 5.0;
            }
            if (ArcTesslateLength<= 0.0)
            {
                ArcTesslateLength = 5.0;
            }
        }

        private DBObjectCollection ExplodeToLines(DBObjectCollection curves)
        {
            return curves.ExplodeToLines(ArcTesslateLength,ChordHeight);
        }

        private DBObjectCollection Explode(BlockReference br)
        {
            if(br!=null)
            {
                return ThDrawTool.Explode(br);
            }
            else
            {
                return new DBObjectCollection();
            }
        }

        private Polyline CalculateObb(DBObjectCollection lines)
        {
            if (lines.Count > 0)
            {
                var transformer = new ThMEPOriginTransformer(lines);
                transformer.Transform(lines);
                var obb = lines.GetMinimumRectangle();
                transformer.Reset(obb);
                transformer.Reset(lines);
                return obb;
            }
            return null;
        }
    }
}
