using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPEngineCore.Algorithm
{
    //public static class FrameType
    //{
    //    public static string ROOM = "房间框线";
    //    public static string DOOR = "门";
    //    public static string WINDOWS = "窗";
    //    public static string FIRECOMP = "防火分区";
    //}
    public class ThMEPFrameComparer2
    {
        public List<Polyline> ErasedFrame;//可能有重复，需要list
        public HashSet<Polyline> AppendedFrame;// 仅在外参中包含的多段线
        public Dictionary<Polyline, Polyline> unChangedFrame;//外参 -> 本图
        public Dictionary<Polyline, Polyline> ChangedFrame;// 外参 -> 本图
        public Point3d srtP;
        private const float FRAME_AREA_FLOOR = 1000f;
        private const float PL_HEAD_TAIL_LIMIT = 500f;
        private const double Simplify_Tolerance = 5.0;
        private const double PL_Head_Tail_Tol = 1e-3;
        private const double SimilarityTol = 0.995;
        private DBObjectCollection tarFrames;   // 外参框线
        private DBObjectCollection srcFrames;   // 本图框线
        private Dictionary<int, Polyline> dicMp2Polyline;
        private ThCADCoreNTSSpatialIndex srcFrameIndex;
        private ThCADCoreNTSSpatialIndex tarFrameIndex;

        public ThMEPFrameComparer2(DBObjectCollection source, DBObjectCollection target)
        {
            Init(source, target);
            CreateTarIndex();
            CreateSrcIndex();
            DoCompare();
            SearchAppend();
            SearchErase();
            Recovery();
        }

        private void Init(DBObjectCollection source, DBObjectCollection target)
        {
            // tarPl->外参 srcPl->本图 外参映射到本图
            srcFrames = source;
            tarFrames = target;
            // GetSrtPositionAndTrans(source);

            ErasedFrame = new List<Polyline>();
            AppendedFrame = new HashSet<Polyline>();
            unChangedFrame = new Dictionary<Polyline, Polyline>();
            ChangedFrame = new Dictionary<Polyline, Polyline>();
            dicMp2Polyline = new Dictionary<int, Polyline>();
        }
        private void Recovery()
        {
            var mat = Matrix3d.Displacement(srtP.GetAsVector());
            var set = new HashSet<int>();
            foreach (Polyline pl in ErasedFrame)
            {
                if (set.Add(pl.GetHashCode()))
                {
                    pl.TransformBy(mat);
                }
            }

            foreach (Polyline pl in AppendedFrame)
            {
                if (set.Add(pl.GetHashCode()))
                {
                    pl.TransformBy(mat);
                }
            }

            foreach (var pair in unChangedFrame)
            {
                if (set.Add(pair.Key.GetHashCode()))
                {
                    pair.Key.TransformBy(mat);
                }
                if (set.Add(pair.Value.GetHashCode()))
                {
                    pair.Value.TransformBy(mat);
                }
            }

            foreach (var pair in ChangedFrame)
            {
                if (set.Add(pair.Key.GetHashCode()))
                {
                    pair.Key.TransformBy(mat);
                }
                if (set.Add(pair.Value.GetHashCode()))
                {
                    pair.Value.TransformBy(mat);
                }
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
            {
                pl.TransformBy(mat);
            }

            foreach (Polyline pl in tarFrames)
            {
                pl.TransformBy(mat);
            }
        }
        private void SearchErase()
        {
            foreach (Polyline pl in srcFrames)
            {
                bool inUnchanged = unChangedFrame.Where(x => x.Value == pl).Any();
                if (inUnchanged)
                {
                    continue;
                }
                bool inChanged = ChangedFrame.Where(x => x.Value == pl).Any();
                if (inChanged)
                {
                    continue;
                }
                if (ErasedFrame.Contains(pl) == false)
                {
                    ErasedFrame.Add(pl);
                }
            }
        }
        private void SearchAppend()
        {
            foreach (var plDict in dicMp2Polyline)
            {
                //var res = srcFrameIndex.SelectCrossingPolygon(pl);
                //if (res.Count == 0)
                //{
                //    AppendedFrame.Add(pl);
                //}
                var pl = plDict.Value;
                bool inUnchanged = unChangedFrame.ContainsKey(pl);
                if (inUnchanged)
                {
                    continue;
                }
                bool inChanged = ChangedFrame.ContainsKey(pl);
                if (inChanged)
                {
                    continue;
                }
                AppendedFrame.Add(pl);
            }
        }
        private void CreateSrcIndex()
        {
            var frames = new DBObjectCollection();
            foreach (Polyline frame in srcFrames)
            {
                frames.Add(frame);
            }
            srcFrameIndex = new ThCADCoreNTSSpatialIndex(frames);
        }
        private void CreateTarIndex()
        {
            var frames = new DBObjectCollection();
            foreach (Polyline frame in tarFrames)
            {
                var simplyPl = DoProcPl(frame);
                if (simplyPl.Area < FRAME_AREA_FLOOR)
                {
                    continue;
                }
                var mp = CreateMP(simplyPl);
                frames.Add(mp);
                dicMp2Polyline.Add(mp.GetHashCode(), simplyPl);
            }
            tarFrameIndex = new ThCADCoreNTSSpatialIndex(frames);
        }

        /// <summary>
        /// 这一步只找src和ref对应关系。之后会把需要加减的算出来
        /// </summary>
        public void DoCompare()
        {
            foreach (Polyline pl in srcFrames)
            {
                var simplyPl = DoProcPl(pl);
                if (simplyPl.Area < FRAME_AREA_FLOOR)
                {
                    continue;
                }
                var srcFrameMP = CreateMP(simplyPl);

                var res = tarFrameIndex.SelectCrossingPolygon(srcFrameMP);
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

        private Polyline DoProcPl(Polyline pl)
        {
            var simpPl = pl.DPSimplify(Simplify_Tolerance);
            simpPl.Closed = true;
            var vs = simpPl.Vertices();
            if (vs.Count < 1)
            {
                return new Polyline();
            }

            var firstP = vs[0];
            var lastP = vs[vs.Count - 1];
            var dis = firstP.DistanceTo(lastP);
            if (Math.Abs(dis) < PL_Head_Tail_Tol || dis > PL_HEAD_TAIL_LIMIT)
            {
                return simpPl;
            }
            else
            {
                vs.RemoveAt(vs.Count - 1);
                var pl1 = new Polyline();
                pl1.CreatePolyline(vs);
                vs.RemoveAt(0);
                vs.Add(lastP);
                var pl2 = new Polyline();
                pl2.CreatePolyline(vs);
                return Math.Abs(simpPl.Area - pl1.Area) < Math.Abs(simpPl.Area - pl2.Area) ? pl1 : pl2;
            }
        }

        private void AddAppend(Polyline srcPl, DBObjectCollection crossPolygons)
        {
            foreach (MPolygon pl in crossPolygons)
            {
                var realPl = dicMp2Polyline[pl.GetHashCode()];
                var res = srcFrameIndex.SelectCrossingPolygon(realPl);
                res.Remove(srcPl);
                if (res.Count == 0)
                {
                    AppendedFrame.Add(realPl);
                }
            }
        }

        //private void SelectMaxCrossArea(Polyline pl, DBObjectCollection crossPolygons)
        //{
        //    DetectCrossPl(pl, crossPolygons, out Polyline maxCrossPl, out MPolygon maxCrossMPl, out Polyline minBoundPl);
        //    if (maxCrossPl.Area > 0)
        //    {
        //        // 与复数个区域相交，直接将最大的面积并入变化的框线
        //        var simCoef = pl.SimilarityMeasure(maxCrossPl);
        //        if (simCoef < 0.6)
        //        {
        //            return;
        //        }
        //        if (ChangedFrame.ContainsKey(maxCrossPl))
        //        {
        //            if (ChangedFrame[maxCrossPl].Item2 > simCoef)
        //            {
        //                return;
        //            }
        //            else
        //            {
        //                ChangedFrame.Remove(maxCrossPl);
        //            }
        //        }
        //        ChangedFrame.Add(maxCrossPl, new Tuple<Polyline, double>(pl, simCoef));
        //        crossPolygons.Remove(maxCrossMPl);
        //        AddAppend(pl, crossPolygons);
        //    }
        //    else if (minBoundPl.Area > 0) ///改
        //    {
        //        // 被复数个框线包含
        //        CheckSimilarity(pl, minBoundPl);
        //    }
        //    //else 从crossPolygons中找到了unchanged，直接退出
        //}
        //private void DetectCrossPl(Polyline pl,
        //                           DBObjectCollection crossPolygons,
        //                           out Polyline maxCrossPl,
        //                           out MPolygon maxCrossMPl,
        //                           out Polyline minBoundPl)
        //{
        //    maxCrossPl = new Polyline();
        //    maxCrossMPl = new MPolygon();
        //    double maxCrossArea = double.MinValue;
        //    minBoundPl = new Polyline();
        //    double minBoundArea = double.MaxValue;

        //    foreach (MPolygon crossPl in crossPolygons)
        //    {
        //        var realCrossPl = dicMp2Polyline[crossPl.GetHashCode()];// 本图上的相交多段线
        //        var crossArea = CalcCrossArea(pl, realCrossPl);
        //        if (crossArea < 0)
        //        {
        //            continue;// 贴着一个边相交
        //        }
        //        if (pl.SimilarityMeasure(realCrossPl) >= 0.995)
        //        {
        //            AddUnChangedFrame(pl, realCrossPl);// 找到一个完全相同的直接退出
        //            break;
        //        }
        //        if (Math.Abs(crossArea - pl.Area) < 1e-3 && crossPl.Area >= pl.Area)
        //        {
        //            if (crossPl.Area < minBoundArea)
        //            {
        //                minBoundArea = crossPl.Area;
        //                minBoundPl = realCrossPl;
        //            }
        //            continue;
        //        }
        //        if (crossArea > maxCrossArea)
        //        {
        //            maxCrossArea = crossArea;
        //            maxCrossPl = realCrossPl;
        //            maxCrossMPl = crossPl;
        //        }
        //    }
        //}

        private void SelectMaxCrossArea(Polyline pl, DBObjectCollection crossPolygons)
        {
            var maxRefPl = DetectCrossPl(pl, crossPolygons);
            if (maxRefPl != null)
            {
                // 与复数个区域相交，将相交部分ref面积占比最大的并入变化的框线
                var simCoef = pl.SimilarityMeasure(maxRefPl);
                //if (simCoef < 0.6)
                //{
                //    return;
                //}
                AddChangedFrame(pl, maxRefPl, simCoef);
            }
        }

        private Polyline DetectCrossPl(Polyline pl, DBObjectCollection crossPolygons)
        {
            Polyline maxCrossPl = null;
            double maxRatio = double.MinValue;

            foreach (MPolygon crossPl in crossPolygons)
            {
                var realCrossPl = dicMp2Polyline[crossPl.GetHashCode()];// 本图上的相交多段线
                var crossAreaRatio = CalcCrossArea(pl, realCrossPl);
                if (crossAreaRatio <= 0)
                {
                    continue;// 贴着一个边相交
                }
                if (pl.SimilarityMeasure(realCrossPl) >= SimilarityTol)
                {
                    AddUnChangedFrame(realCrossPl, pl);// 找到一个完全相同的直接退出
                    maxCrossPl = null;
                    break;
                }

                if (crossAreaRatio > maxRatio)
                {
                    maxRatio = crossAreaRatio;
                    maxCrossPl = realCrossPl;
                }
            }

            return maxCrossPl;
        }

        /// <summary>
        /// return intersect area/p2 area.
        /// if use source frame ref index, p1= source p2= ref.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double CalcCrossArea(Polyline p1, Polyline p2)
        {
            double areaRatio = 0;

            var crossArea = p1.Intersection(new DBObjectCollection() { p2 }).OfType<Polyline>().Max(x => x.Area);
            areaRatio = crossArea / p2.Area;

            return areaRatio;
        }

        //private void CheckSimilarityOri(Polyline srcPl, Polyline tarPl)
        //{
        //    // 区分是完全重合还是部分重合
        //    // tarPl->外参 srcPl->本图 外参映射到本图
        //    var coef = srcPl.SimilarityMeasure(tarPl);
        //    if (coef >= 0.995)
        //        AddUnChangedFrame(tarPl, srcPl);
        //    else if (coef >= 0.9)
        //        AddChangedFrame(srcPl, tarPl, coef);

        //}

        private void CheckSimilarity(Polyline srcPl, Polyline tarPl)
        {
            // 区分是完全重合还是部分重合
            // tarPl->外参 srcPl->本图 外参映射到本图
            var coef = srcPl.SimilarityMeasure(tarPl);
            if (coef >= SimilarityTol)
            {
                AddUnChangedFrame(tarPl, srcPl);
            }
            else
            {
                AddChangedFrame(srcPl, tarPl, coef);
            }
        }

        private void AddUnChangedFrame(Polyline tarPl, Polyline srcPl)
        {
            if (ChangedFrame.ContainsKey(tarPl))
            {
                ChangedFrame.Remove(tarPl);
            }
            if (unChangedFrame.ContainsKey(tarPl) == false)
            {
                unChangedFrame.Add(tarPl, srcPl);
            }
            else
            {
                unChangedFrame[tarPl] = srcPl;
            }
        }

        //private void AddChangedFrameOri(Polyline srcPl, Polyline tarPl, double coef)
        //{
        //    if (ChangedFrame.ContainsKey(tarPl))
        //    {
        //        if (ChangedFrame[tarPl].Item2 < coef)
        //        {
        //            ChangedFrame.Remove(tarPl);
        //            ChangedFrame.Add(tarPl, new Tuple<Polyline, double>(srcPl, coef));
        //        }
        //        else
        //        {
        //            // 本图与外参相交的框线除了最大的，其他的都放到新增里
        //            AppendedFrame.Add(tarPl);
        //        }
        //    }
        //    else
        //    {
        //        if (coef >= 0.995)
        //            AddUnChangedFrame(tarPl, srcPl);
        //        else if (coef >= 0.9)
        //            ChangedFrame.Add(tarPl, new Tuple<Polyline, double>(srcPl, coef));
        //    }
        //}


        private void AddChangedFrame(Polyline srcPl, Polyline tarPl, double coef)
        {
            if (ChangedFrame.ContainsKey(tarPl))
            {
                var lastSourcePl = ChangedFrame[tarPl];
                var areaLast = CalcCrossArea(lastSourcePl, tarPl);
                var areaCurr = CalcCrossArea(srcPl, tarPl);
                if (areaCurr > areaLast)
                {
                    ChangedFrame.Remove(tarPl);
                    ChangedFrame.Add(tarPl, srcPl);
                }
            }
            else
            {
                ChangedFrame.Add(tarPl, srcPl);
            }
        }

        private MPolygon CreateMP(Polyline pl)
        {
            return pl.ToNTSPolygon().ToDbMPolygon();
        }
    }
}
