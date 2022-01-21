using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using DotNetARX;
using Linq2Acad;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPFrameComparer
    {
        public DBObjectCollection ErasedFrame;
        public HashSet<Polyline> AppendedFrame;// 仅在外参中包含的多段线
        public HashSet<Polyline> unChangedFrame;
        public Dictionary<Polyline, Tuple<Polyline, double>> ChangedFrame;// 外参 -> (本土, 相似度)
        private Point3d srtP;
        private DBObjectCollection tarFrames;   // 外参框线
        private DBObjectCollection srcFrames;   // 本图框线
        private Dictionary<int, Polyline> dicMp2Polyline;
        private ThCADCoreNTSSpatialIndex tarFrameIndex;
        
        public ThMEPFrameComparer(DBObjectCollection source, DBObjectCollection target)
        {
            Init(source, target);
            CreateTarIndex();
            DoCompare();
            SearchAppend();
            Recovery();
        }

        private void Init(DBObjectCollection source, DBObjectCollection target)
        {
            // tarPl->外参 srcPl->本图 外参映射到本图
            srcFrames = source;
            tarFrames = target;
            GetSrtPositionAndTrans(source);
            
            ErasedFrame = new DBObjectCollection();
            AppendedFrame = new HashSet<Polyline>();
            unChangedFrame = new HashSet<Polyline>();
            ChangedFrame = new Dictionary<Polyline, Tuple<Polyline, double>>();
            dicMp2Polyline = new Dictionary<int, Polyline>();
        }
        private void Recovery()
        {
            var mat = Matrix3d.Displacement(srtP.GetAsVector());
            var set = new HashSet<int>();
            foreach (Polyline pl in ErasedFrame)
                if (set.Add(pl.GetHashCode()))
                    pl.TransformBy(mat);
            foreach (Polyline pl in AppendedFrame)
                if (set.Add(pl.GetHashCode()))
                    pl.TransformBy(mat);
            foreach (Polyline pl in unChangedFrame)
                if (set.Add(pl.GetHashCode()))
                    pl.TransformBy(mat);
            foreach (var pair in ChangedFrame)
            {
                if (set.Add(pair.Key.GetHashCode()))
                    pair.Key.TransformBy(mat);
                if (set.Add(pair.Value.Item1.GetHashCode()))
                    pair.Value.Item1.TransformBy(mat);
            }
        }
        private void GetSrtPositionAndTrans(DBObjectCollection source)
        {
            foreach (Polyline pl in source)
            {
                srtP = pl.GetCenter();
                break;
            }
            var mat = Matrix3d.Displacement(-srtP.GetAsVector());
            foreach (Polyline pl in srcFrames)
                pl.TransformBy(mat);
            foreach (Polyline pl in tarFrames)
                pl.TransformBy(mat);
        }
        private void SearchAppend()
        {
            var srcFrameIndex = CreateSrcIndex();
            foreach (Polyline pl in tarFrames)
            {
                if (!unChangedFrame.Contains(pl) && !ChangedFrame.ContainsKey(pl))
                {
                    var mp = CreateMP(pl);
                    var res = srcFrameIndex.SelectCrossingPolygon(mp);
                    if (res.Count == 0)
                        AppendedFrame.Add(pl);
                }
            }
        }
        private ThCADCoreNTSSpatialIndex CreateSrcIndex()
        {
            var frames = new DBObjectCollection();
            foreach (Polyline frame in srcFrames)
            {
                var mp = CreateMP(frame);
                frames.Add(mp);
            }
            return new ThCADCoreNTSSpatialIndex(frames);
        }
        private void CreateTarIndex()
        {
            var frames = new DBObjectCollection();
            foreach (Polyline frame in tarFrames)
            {
                var mp = frame.ToNTSPolygon().ToDbMPolygon();
                frames.Add(mp);
                dicMp2Polyline.Add(mp.GetHashCode(), frame);
            }
            tarFrameIndex = new ThCADCoreNTSSpatialIndex(frames);
        }
        public void DoCompare()
        {
            foreach (Polyline pl in srcFrames)
            {
                var simpPl = pl.DPSimplify(5);
                var mp = CreateMP(simpPl);
                var res = tarFrameIndex.SelectCrossingPolygon(mp);
                if (res.Count == 0)
                {
                    ErasedFrame.Add(pl);// 仅在本图中
                }
                else if (res.Count == 1)
                {
                    CheckSimilarity(pl, dicMp2Polyline[(res[0] as MPolygon).GetHashCode()]);
                }
                else
                {
                    // 交一个面积最大的区域，区分是完全重合还是部分重合
                    SelectMaxCrossArea(pl, res);
                }
            }
        }
        private void SelectMaxCrossArea(Polyline pl, DBObjectCollection crossPolygons)
        {
            var maxPl = new Polyline();
            double maxArea = double.MinValue;
            foreach (MPolygon crossPl in crossPolygons)
            {
                var realCrossPl = dicMp2Polyline[crossPl.GetHashCode()];
                var crossArea = CalcCrossArea(pl, realCrossPl);
                if (crossArea > maxArea)
                {
                    maxArea = crossArea;
                    maxPl = realCrossPl;
                }
            }
            if (maxPl.Area > 10)
                CheckSimilarity(pl, maxPl);
        }
        private double CalcCrossArea(Polyline p1, Polyline p2)
        {
            double maxArea = double.MinValue;
            var cross = p1.Intersection(new DBObjectCollection() { p2 }).OfType<Polyline>();
            foreach (Polyline p in cross)
            {
                if (p.Area > maxArea)
                {
                    maxArea = p.Area;
                }
            }
            return maxArea;
        }
        private void CheckSimilarity(Polyline srcPl, Polyline tarPl)
        {
            // 区分是完全重合还是部分重合
            // tarPl->外参 srcPl->本图 外参映射到本图
            var coef = srcPl.SimilarityMeasure(tarPl);
            if (coef >= 0.995)
                unChangedFrame.Add(tarPl);
            else if(coef >= 0.9)
            {
                if (ChangedFrame.ContainsKey(tarPl))
                {
                    if (ChangedFrame[tarPl].Item2 < coef)
                    {
                        ChangedFrame.Remove(tarPl);
                        ChangedFrame.Add(tarPl, new Tuple<Polyline, double>(srcPl, coef));
                    }
                    else
                    {
                        // 本图与外参相交的框线除了最大的，其他的都放到新增里
                        AppendedFrame.Add(tarPl);
                    }
                }
                else
                {
                    ChangedFrame.Add(tarPl, new Tuple<Polyline, double>(srcPl, coef));
                }
            }
        }
        private MPolygon CreateMP(Polyline pl)
        {
            return pl.ToNTSPolygon().ToDbMPolygon();
        }
        private ThCADCoreNTSSpatialIndex CreatePl2Text(out Dictionary<int, string> dic)
        {
            var curText = ReadCurGraphTexts();
            var bounds = new DBObjectCollection();
            dic = new Dictionary<int, string>();
            foreach (var t in curText)
            {
                var cp = t.Bounds.GetValueOrDefault().CenterPoint();
                var pl = new Polyline();
                pl.CreatePolygon(cp.ToPoint2D(), 4, 10);
                var mp = pl.ToNTSPolygon().ToDbMPolygon();
                dic.Add(mp.GetHashCode(), t.TextString);
                bounds.Add(mp);
            }
            return new ThCADCoreNTSSpatialIndex(bounds);
        }
        public static List<DBText> ReadCurGraphTexts()
        {
            using (var db = AcadDatabase.Active())
            {
                var texts = new List<DBText>();
                var dbTexts = db.ModelSpace.OfType<DBText>();
                foreach (var t in dbTexts)
                    if (t.Layer == "AI-房间名称")
                        texts.Add(t);
                return texts;
            }
        }
    }
}
