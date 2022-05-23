using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;



using ThMEPEngineCore.Algorithm.FrameComparer.Model;


namespace ThMEPEngineCore.Algorithm.FrameComparer
{
    internal class ThFrameCompareModel
    {
        public ThFrameChangedCommon.CompareFrameType FrameType;
        private string Layer;
        private string LayerText;
        private bool WithText;
        private ThMEPFrameComparer FrameComp;
        private ThMEPFrameTextComparer TextComp;
        private ThFrameExactor FrameExtractor;
        private Dictionary<int, ObjectId> DictOriginalEntity;//srcPoly.Hash, originalSrc objectId
        public List<ThFrameChangeItem> ResultList;

        public ThFrameCompareModel(ThFrameChangedCommon.CompareFrameType type, bool withText)
        {
            FrameType = type;
            WithText = withText;
            if (FrameType == ThFrameChangedCommon.CompareFrameType.ROOM)
            {
                Layer = ThMEPEngineCoreLayerUtils.ROOMOUTLINE;
                LayerText = ThMEPEngineCoreLayerUtils.ROOMMARK;
            }
            else if (FrameType == ThFrameChangedCommon.CompareFrameType.DOOR)
            {
                Layer = ThMEPEngineCoreLayerUtils.DOOR;
            }
            else if (FrameType == ThFrameChangedCommon.CompareFrameType.WINDOW)
            {
                Layer = ThMEPEngineCoreLayerUtils.WINDOW;
            }
            else if (FrameType == ThFrameChangedCommon.CompareFrameType.FIRECOMPONENT)
            {
                Layer = ThMEPEngineCoreLayerUtils.FIRECOMPARTMENT;
            }
            DictOriginalEntity = new Dictionary<int, ObjectId>();
            ResultList = new List<ThFrameChangeItem>();
        }

        #region Compare
        public void DoCompare(Point3dCollection fence)
        {
            if (WithText == false)
            {
                DoCompareFrame(fence);
            }
            else
            {
                DoCompareFrameWithText(fence);
            }

            FrameExtractor.curGraph.OfType<Entity>().ToList().ForEach(x => DrawUtils.ShowGeometry(x, "l0curGraph", 1));
            FrameExtractor.reference.OfType<Entity>().ToList().ForEach(x => DrawUtils.ShowGeometry(x, "l0refGraph", 2));
        }
        private void DoCompareFrameWithText(Point3dCollection fence)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                FrameExtractor = new ThFrameExactor(FrameType, fence);
                FrameComp = new ThMEPFrameComparer(FrameExtractor.curGraph, FrameExtractor.reference);

                var textExactor = new ThFrameTextExactor(fence);
                TextComp = new ThMEPFrameTextComparer(FrameComp, textExactor);// 对房间框线需要对文本再进行比对

                foreach (var dict in FrameExtractor.dicCode2Id)
                {
                    DictOriginalEntity.Add(dict.Key, dict.Value);
                }
            }
        }
        private void DoCompareFrame(Point3dCollection fence)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                FrameExtractor = new ThFrameExactor(FrameType, fence);
                FrameComp = new ThMEPFrameComparer(FrameExtractor.curGraph, FrameExtractor.reference);
                foreach (var dict in FrameExtractor.dicCode2Id)
                {
                    DictOriginalEntity.Add(dict.Key, dict.Value);
                }
            }
        }
        #endregion

        #region GenerateResult
        public void GenerateResult()
        {
            var resultList = GenerateFrame();
            if (WithText == true)
            {
                GenerateFrameWithText(ref resultList);
            }
            ResultList.AddRange(resultList);
        }
        private List<ThFrameChangeItem> GenerateFrame()
        {
            var changeItem = new List<ThFrameChangeItem>();

            foreach (var pair in FrameComp.ChangedFrame)
            {
                var item = new ThFrameChangeItem(FrameType, ThFrameChangedCommon.ChangeType_Change, pair.Value);
                item.RemovePoly = pair.Value;
                item.AddPoly = pair.Key;
                changeItem.Add(item);
            }
            foreach (var pl in FrameComp.AppendedFrame)
            {
                var item = new ThFrameChangeItem(FrameType, ThFrameChangedCommon.ChangeType_Append, pl);
                item.AddPoly = pl;
                changeItem.Add(item);
            }
            foreach (var pl in FrameComp.ErasedFrame)
            {
                var item = new ThFrameChangeItem(FrameType, ThFrameChangedCommon.ChangeType_Delete, pl);
                item.RemovePoly = pl;
                changeItem.Add(item);
            }
            return changeItem;
        }

        private void GenerateFrameWithText(ref List<ThFrameChangeItem> frameResult)
        {
            if (TextComp == null)
            {
                return;
            }
            //frameComp不变的如果在textComp变的，则马克“功能变化”
            foreach (var unchangeFrame in FrameComp.unChangedFrame)
            {
                if (TextComp.ChangedText.ContainsKey(unchangeFrame.Value))
                {
                    var item = new ThFrameChangeItem(FrameType, ThFrameChangedCommon.ChangeType_ChangeText, unchangeFrame.Value);
                    var removeTextData = TextComp.ChangedText[unchangeFrame.Value].Item2.Select(y => TextComp.dicFrameText[y.GetHashCode()]).Select(x => (DBText)x.Data).ToList();
                    var addTextData = TextComp.ChangedText[unchangeFrame.Value].Item1.Select(y => TextComp.dicFrameText[y.GetHashCode()]).Select(x => (DBText)x.Data).ToList();
                    item.RemoveText.AddRange(removeTextData);
                    item.AddText.AddRange(addTextData);
                    frameResult.Add(item);
                }
            }

            foreach (var item in TextComp.ChangedText)
            {
                var changeCase = frameResult.Where(x => x.FocusPoly == item.Key).FirstOrDefault();
                if (changeCase != null)
                {
                    var addTextData = item.Value.Item1.Select(y => TextComp.dicFrameText[y.GetHashCode()]).Select(x => (DBText)x.Data).ToList();
                    var removeTextData = item.Value.Item2.Select(y => TextComp.dicFrameText[y.GetHashCode()]).Select(x => (DBText)x.Data).ToList();
                    changeCase.AddText.AddRange(addTextData);
                    changeCase.RemoveText.AddRange(removeTextData);
                }
            }
            foreach (var item in TextComp.AppendText)
            {
                var changeCase = frameResult.Where(x => x.FocusPoly == item.Key).FirstOrDefault();
                if (changeCase != null)
                {
                    var addTextData = item.Value.Select(y => TextComp.dicFrameText[y.GetHashCode()]).Select(x => (DBText)x.Data).ToList();
                    changeCase.AddText.AddRange(addTextData);
                }
                else
                {
                    //不在room里的文字
                    var newCase = new ThFrameChangeItem(FrameType, ThFrameChangedCommon.ChangeType_ChangeText, item.Key);
                    var addTextData = item.Value.Select(y => TextComp.dicFrameText[y.GetHashCode()]).Select(x => (DBText)x.Data).ToList();
                    newCase.AddText.AddRange(addTextData);
                    frameResult.Add(newCase);
                }
            }
            foreach (var item in TextComp.ErasedText)
            {
                var changeCase = frameResult.Where(x => x.FocusPoly == item.Key).FirstOrDefault();
                if (changeCase != null)
                {
                    var rmoveTextData = item.Value.Select(y => TextComp.dicFrameText[y.GetHashCode()]).Select(x => (DBText)x.Data).ToList();
                    changeCase.RemoveText.AddRange(rmoveTextData);
                }
                else
                {
                    //不在room里的文字
                    var newCase = new ThFrameChangeItem(FrameType, ThFrameChangedCommon.ChangeType_ChangeText, item.Key);
                    var removeTextData = item.Value.Select(y => TextComp.dicFrameText[y.GetHashCode()]).Select(x => (DBText)x.Data).ToList();
                    newCase.RemoveText.AddRange(removeTextData);
                    frameResult.Add(newCase);
                }
            }
        }

        #endregion

        #region Update
        public void UpdateFrame(ThFrameChangeItem changeItem)
        {
            if (changeItem.RemovePoly != null)
            {
                var removeEntityId = DictOriginalEntity[changeItem.RemovePoly.GetHashCode()];
                ThFramePainter.EraseEntity(removeEntityId);
            }
            if (changeItem.AddPoly != null)
            {
                ThFramePainter.AddEntity(changeItem.AddPoly, Layer);
            }
            changeItem.RemoveText.ForEach(x => ThFramePainter.EraseEntity(x.ObjectId));
            changeItem.AddText.ForEach(x => ThFramePainter.AddEntity(x, LayerText));
        }

        /// <summary>
        /// 之后决定要不要这一步。即update结果的时候把最原始的compare里面的数据也删除掉
        /// </summary>
        /// <param name="changeItem"></param>
        public void RemoveResult(ThFrameChangeItem changeItem)
        {
            //remove result from compare
            if (changeItem.AddPoly != null)
            {
                FrameComp.AppendedFrame.Remove(changeItem.AddPoly);
                FrameComp.ChangedFrame.Remove(changeItem.AddPoly);
            }
            if (changeItem.RemovePoly != null)
            {
                FrameComp.ErasedFrame.Remove(changeItem.RemovePoly);
            }

            if (changeItem.AddText.Count() != 0)
            {

            }
            if (changeItem.RemoveText.Count() != 0)
            {

            }

            ResultList.Remove(changeItem);
        }

        #endregion
    }
}
