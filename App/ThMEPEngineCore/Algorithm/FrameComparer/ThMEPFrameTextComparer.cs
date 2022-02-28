using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Algorithm.FrameComparer
{
    public class ThMEPFrameTextComparer
    {
        private Point3d srtP;
        private ThCADCoreNTSSpatialIndex orgTextIndex;
        private ThCADCoreNTSSpatialIndex refTextIndex;
        private Dictionary<int, string> dicFrameText;   // 外包框到文字参数的映射
        
        public ThMEPFrameTextComparer(ThMEPFrameComparer frameComp, ThFrameTextExactor textExactor)
        {
            // 检查UnChanged的功能是否变化
            // UnChanged中框线与各自文字外包框求交后，对比结果文字是否一致
            Init(frameComp, textExactor);
            CheckUnChangedFrame(frameComp);
            Recover(textExactor);
        }

        private void Recover(ThFrameTextExactor textExactor)
        {
            var mat = Matrix3d.Displacement(srtP.GetAsVector());
            foreach (var text in textExactor.curTexts)
                text.Geometry.TransformBy(mat);
            foreach (var text in textExactor.refTexts)
                text.Geometry.TransformBy(mat);
        }

        private void Init(ThMEPFrameComparer frameComp, ThFrameTextExactor textExactor)
        {
            srtP = frameComp.srtP;
            dicFrameText = new Dictionary<int, string>();
            var mat = Matrix3d.Displacement(-srtP.GetAsVector());
            // 框线大不需要用MPolygon
            var orgTextBounds = new DBObjectCollection();
            foreach (var text in textExactor.curTexts)
            {
                text.Geometry.TransformBy(mat);
                orgTextBounds.Add(text.Geometry);
                dicFrameText.Add(text.Geometry.GetHashCode(), text.Text);
            }
            orgTextIndex = new ThCADCoreNTSSpatialIndex(orgTextBounds);
            var refTextBounds = new DBObjectCollection();
            foreach (var text in textExactor.refTexts)
            {
                text.Geometry.TransformBy(mat);
                refTextBounds.Add(text.Geometry);
                dicFrameText.Add(text.Geometry.GetHashCode(), text.Text);
            }
            refTextIndex = new ThCADCoreNTSSpatialIndex(refTextBounds);
        }

        private void CheckUnChangedFrame(ThMEPFrameComparer frameComp)
        {
            var mat = Matrix3d.Displacement(-srtP.GetAsVector());
            var revMat = Matrix3d.Displacement(srtP.GetAsVector());
            var tFrames = new Dictionary<Polyline, Polyline>();
            foreach (var pair in frameComp.unChangedFrame)
            {
                pair.Key.TransformBy(mat);
                var orgTexts = orgTextIndex.SelectCrossingPolygon(pair.Key);
                if (orgTexts.Count > 0)
                {
                    // unchanged可以用本图框线与底图文字求交
                    var refTexts = refTextIndex.SelectCrossingPolygon(pair.Key);
                    if (!IsSameTexts(orgTexts, refTexts))
                    {
                        if (!frameComp.ChangedFrame.ContainsKey(pair.Key))
                        {
                            pair.Key.TransformBy(revMat);
                            frameComp.ChangedFrame.Add(pair.Key, new Tuple<Polyline, double>(pair.Value, 1.1));
                        }
                    }
                    else
                    {
                        pair.Key.TransformBy(revMat);
                        tFrames.Add(pair.Key, pair.Value);
                    }
                }
            }
            frameComp.unChangedFrame.Clear();
            frameComp.unChangedFrame = tFrames;
        }
        private bool IsSameTexts(DBObjectCollection texts1, DBObjectCollection texts2)
        {
            if (texts1.Count != texts2.Count)
                return false;
            var set = new HashSet<string>();
            foreach (Polyline pl in texts1)
                set.Add(dicFrameText[pl.GetHashCode()]);
            foreach (Polyline pl in texts2)
                if (!set.Contains(dicFrameText[pl.GetHashCode()]))
                    return false;
            return true;
        }
    }
}
