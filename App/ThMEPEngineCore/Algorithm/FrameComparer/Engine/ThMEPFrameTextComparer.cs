using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Algorithm.FrameComparer
{
    public class ThMEPFrameTextComparer
    {
        private Point3d srtP;
        private ThCADCoreNTSSpatialIndex srcTextIndex;//本图
        private ThCADCoreNTSSpatialIndex refTextIndex;//外参
        private List<Polyline> srcTextOBB;
        private List<Polyline> refTextOBB;
        private const double AreaRatio = 0.4;
        public Dictionary<int, ThIfcTextNodeData> dicFrameText;   //文字外包框，文字源数据的映射
        public Dictionary<Polyline, List<Polyline>> ErasedText;//src polyline(or src text boundary when it is not contained by any room), src contained text polyline (需要删的文字）
        public Dictionary<Polyline, Tuple<List<Polyline>, List<Polyline>>> ChangedText;//src polyline, (ref contained text,src contained text）(tuple:需要增加的,需要删掉的）
        private Dictionary<Polyline, Tuple<List<Polyline>, List<Polyline>>> UnchangedText;//搜寻earsed append时候用
        public Dictionary<Polyline, List<Polyline>> AppendText;//ref polyline(or ref text boundary when it is not contained by any room), ref contained text polyline

        public ThMEPFrameTextComparer(ThMEPFrameComparer frameComp, ThFrameTextExactor textExactor)
        {
            Init();
            InitIndex(frameComp, textExactor);

            CheckChangedFrame(frameComp.unChangedFrame);
            CheckChangedFrame(frameComp.ChangedFrame);//这俩必须放在前面，因为后面增加和清除会检查房间框不出来的选项
            CheckErasedFrame(frameComp.ErasedFrame);
            CheckAppendFrame(frameComp.AppendedFrame);


            //Recover(textExactor);
        }

        private void Recover(ThFrameTextExactor textExactor)
        {
            var mat = Matrix3d.Displacement(srtP.GetAsVector());
            foreach (var text in textExactor.curTexts)
                text.Geometry.TransformBy(mat);
            foreach (var text in textExactor.refTexts)
                text.Geometry.TransformBy(mat);
        }
        private void Init()
        {
            srcTextOBB = new List<Polyline>();
            refTextOBB = new List<Polyline>();
            dicFrameText = new Dictionary<int, ThIfcTextNodeData>();
            ErasedText = new Dictionary<Polyline, List<Polyline>>();
            ChangedText = new Dictionary<Polyline, Tuple<List<Polyline>, List<Polyline>>>();
            UnchangedText = new Dictionary<Polyline, Tuple<List<Polyline>, List<Polyline>>>();
            AppendText = new Dictionary<Polyline, List<Polyline>>();
        }

        private void InitIndex(ThMEPFrameComparer frameComp, ThFrameTextExactor textExactor)
        {
            srtP = frameComp.srtP;
            var mat = Matrix3d.Displacement(-srtP.GetAsVector());
            // 框线大不需要用MPolygon
            var srcTextBounds = new DBObjectCollection();
            foreach (var text in textExactor.curTexts)
            {
                text.Geometry.TransformBy(mat);
                srcTextBounds.Add(text.Geometry);
                Diagnostics.DrawUtils.ShowGeometry(text.Geometry, "l0curTextBound", 1);
                dicFrameText.Add(text.Geometry.GetHashCode(), text);
                srcTextOBB.Add(text.Geometry);
            }
            srcTextIndex = new ThCADCoreNTSSpatialIndex(srcTextBounds);

            var refTextBounds = new DBObjectCollection();
            foreach (var text in textExactor.refTexts)
            {
                text.Geometry.TransformBy(mat);
                refTextBounds.Add(text.Geometry);
                Diagnostics.DrawUtils.ShowGeometry(text.Geometry, "l0refTextBound", 3);
                dicFrameText.Add(text.Geometry.GetHashCode(), text);
                refTextOBB.Add(text.Geometry);
            }
            refTextIndex = new ThCADCoreNTSSpatialIndex(refTextBounds);
        }

        private List<Polyline> ContainedText(ThCADCoreNTSSpatialIndex textIndex, Polyline pl)
        {
            var containText = new List<Polyline>();
            var containTextsBoundary = textIndex.SelectCrossingPolygon(pl); //有些卡在线上的，或者蹭到一个角的。面积比例占到 60以上的算

            foreach (Polyline textpl in containTextsBoundary)
            {
                var areaRatio = ThMEPFrameComparer.CalcCrossArea(pl, textpl);
                if (areaRatio >= AreaRatio)
                {
                    containText.Add(textpl);
                }
            }
            return containText;
        }

        private void CheckChangedFrame(Dictionary<Polyline, Polyline> changedFrame)
        {
            foreach (var pair in changedFrame)
            {
                var refContainText = ContainedText(refTextIndex, pair.Key);
                var srcContainText = ContainedText(srcTextIndex, pair.Value);
                if (IsSameTexts(refContainText, srcContainText) == false)
                {
                    ChangedText.Add(pair.Value, new Tuple<List<Polyline>, List<Polyline>>(refContainText, srcContainText));
                }
                else
                {
                    UnchangedText.Add(pair.Value, new Tuple<List<Polyline>, List<Polyline>>(refContainText, srcContainText));
                }
            }
        }
        private void CheckErasedFrame(List<Polyline> EraseList)
        {
            foreach (var pl in EraseList)
            {
                var containText = ContainedText(srcTextIndex, pl);
                if (containText.Count == 0)
                {
                    continue;
                }
                if (ErasedText.ContainsKey(pl) == false)
                {
                    ErasedText.Add(pl, containText);
                }
                else
                {
                    ErasedText[pl].AddRange(containText);
                }
            }
            foreach (var pl in srcTextOBB)
            {
                bool inErase = ErasedText.Where(x => x.Value.Contains(pl)).Any();
                if (inErase)
                {
                    continue;
                }
                bool inChanged = ChangedText.Where(x => x.Value.Item2.Contains(pl)).Any();
                if (inChanged)
                {
                    continue;
                }
                bool inUnChanged = UnchangedText.Where(x => x.Value.Item2.Contains(pl)).Any();
                if (inUnChanged)
                {
                    continue;
                }
                ErasedText.Add(pl, new List<Polyline> { pl });

            }

        }

        private void CheckAppendFrame(HashSet<Polyline> appendFrame)
        {
            foreach (var pl in appendFrame)
            {
                var refContainText = ContainedText(refTextIndex, pl);
                AppendText.Add(pl, refContainText);
            }

            foreach (var pl in refTextOBB)
            {
                bool isAppend = AppendText.Where(x => x.Value.Contains(pl)).Any();
                if (isAppend)
                {
                    continue;
                }
                bool inChanged = ChangedText.Where(x => x.Value.Item1.Contains(pl)).Any();
                if (inChanged)
                {
                    continue;
                }
                bool inUnChanged = UnchangedText.Where(x => x.Value.Item1.Contains(pl)).Any();
                if (inUnChanged)
                {
                    continue;
                }
                AppendText.Add(pl, new List<Polyline> { pl });
            }
        }

        private bool IsSameTexts(List<Polyline> texts1, List<Polyline> texts2)
        {
            var bReturn = true;
            var set1 = new HashSet<string>();
            foreach (var pl in texts1)
            {
                set1.Add(dicFrameText[pl.GetHashCode()].Text);
            }

            var set2 = new HashSet<string>();
            foreach (var pl in texts2)
            {
                set2.Add(dicFrameText[pl.GetHashCode()].Text);
            }

            foreach (var st in set1)
            {
                if (set2.Contains(st) == false)
                {
                    bReturn = false;
                    break;
                }
            }

            foreach (var st in set2)
            {
                if (bReturn == true && set1.Contains(st) == false)
                {
                    bReturn = false;
                    break;
                }
            }

            return bReturn;
        }
    }
}
