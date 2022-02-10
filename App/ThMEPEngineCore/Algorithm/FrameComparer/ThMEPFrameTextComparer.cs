using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Algorithm.FrameComparer
{
    public class ThMEPFrameTextComparer
    {
        private Point3d srtP;
        private Dictionary<int, string> dicTexts;         // 文字外包框到文字参数的映射
        private ThCADCoreNTSSpatialIndex orgTextBounds;
        private Dictionary<int, HashSet<string>> dicFrameTexts;   // 外包框到文字参数的映射
        
        public ThMEPFrameTextComparer(ThMEPFrameComparer frameComp)
        {
            // 检查UnChanged 和 Changed 的功能是否变化
            Init(frameComp);
        }

        private void Init(ThMEPFrameComparer frameComp)
        {
            srtP = frameComp.srtP;
            dicTexts = new Dictionary<int, string>();
            dicFrameTexts = new Dictionary<int, HashSet<string>>();
        }

        private void AttachFrameText(ThMEPFrameComparer frameComp)
        {
            var mat = Matrix3d.Displacement(-srtP.GetAsVector());
            GetTextBounds(mat);
            foreach (Polyline pl in frameComp.unChangedFrame)
            {
                var mp = CreateMP(pl);
                mp.TransformBy(mat);
                var res = orgTextBounds.SelectCrossingPolygon(mp);
                if (res.Count > 0)
                {
                    int key = pl.GetHashCode();
                    dicFrameTexts.Add(key, new HashSet<string>());
                    foreach (MPolygon p in res)
                        dicFrameTexts[key].Add(dicTexts[p.GetHashCode()]);
                }
                // 找到了本图上的字，怎么找底图上的字？？？
            }
        }
        private void GetTextBounds(Matrix3d mat)
        {
            var texts = ReadDuctTexts();
            var bounds = new DBObjectCollection();
            foreach (var t in texts)
            {
                var l = new Line(t.Bounds.Value.MinPoint, t.Bounds.Value.MaxPoint);
                var mp = CreateMP(l.Buffer(1));
                mp.TransformBy(mat);
                dicTexts.Add(mp.GetHashCode(), t.TextString);
                bounds.Add(mp);
            }
            orgTextBounds = new ThCADCoreNTSSpatialIndex(bounds);
        }
        private List<DBText> ReadDuctTexts()
        {
            var texts = new List<DBText>();
            using (var db = AcadDatabase.Active())
            {
                var dbTexts = db.ModelSpace.OfType<DBText>();
                foreach (var t in dbTexts)
                    if (IsRoom(t.TextString) && t.Layer == "AI-房间名称")
                        texts.Add(t);
            }
            return texts;
        }
        private bool IsSameTexts(HashSet<string> texts1, HashSet<string> texts2)
        {
            if (texts1.Count != texts2.Count)
                return false;
            foreach (var t in texts1)
            {
                if (!texts2.Contains(t))
                    return false;
            }
            return true;
        }
        private bool IsRoom(string s)
        {
            return s.Contains("房") ||
                   s.Contains("道") ||
                   s.Contains("室") ||
                   s.Contains("间") ||
                   s.Contains("梯") ||
                   s.Contains("堂") ||
                   s.Contains("电") ||
                   s.Contains("厅") ||
                   s == "车库" ||
                   s == "水";
        }
        private MPolygon CreateMP(Polyline pl)
        {
            return pl.ToNTSPolygon().ToDbMPolygon();
        }
    }
}
