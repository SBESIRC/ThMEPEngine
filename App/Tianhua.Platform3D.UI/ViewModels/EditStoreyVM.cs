using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ThCADExtension;
using TianHua.Platform3D.UI.Model;

namespace TianHua.Platform3D.UI.ViewModels
{
    public class EditStoreyVM
    {
        public ObservableCollection<ThEditStoreyInfo> Source { get; set; }        
        public EditStoreyVM(ObservableCollection<ThEditStoreyInfo> source)
        {
            Source = source;
        }
        public List<string> Storeys
        {
            get
            {
                return Source.OfType<ThEditStoreyInfo>().Select(o => o.StoreyName).ToList();
            }
        }
        public void InitSource(List<string> storeys)
        {
            Source = new ObservableCollection<ThEditStoreyInfo>();
            storeys.ForEach(o => Source.Add(new ThEditStoreyInfo { StoreyName = o }));
        }
        /// <summary>
        /// 地下层
        /// </summary>
        public List<ThEditStoreyInfo> BelowStoreys
        {
            get
            {
                return Source.Where(o => o.StoreyName.StartsWith("B")).ToList();
            }
        }
        /// <summary>
        /// 屋顶层
        /// </summary>
        public List<ThEditStoreyInfo> RoofStoreys
        {
            get
            {
                return Source.Where(o => o.StoreyName.StartsWith("R")).ToList();
            }
        }
        /// <summary>
        /// 地上层
        /// </summary>
        public List<ThEditStoreyInfo> UpperStoreys
        {
            get
            {
                return Source.OfType<ThEditStoreyInfo>().Except(BelowStoreys).Except(RoofStoreys).ToList();
            }
        }

        public void InitSource(List<ThEditStoreyInfo> storeyInfos,bool isSortBtmElevation=true,bool isUpdateStoreyName=true)
        {
            if(storeyInfos.Count == 0)
            {
                return;
            }
            if(isSortBtmElevation)
            {
                // 按层底标高排序
                storeyInfos = storeyInfos.OrderBy(o => o.Bottom_Elevation_Value).ToList();
            }
            if(isUpdateStoreyName)
            {
                // 根据与0值最小的设为首层
                var firstStorey = storeyInfos.OrderBy(o => Math.Abs(o.Bottom_Elevation_Value)).First();
                var firstStoreyIndex = storeyInfos.IndexOf(firstStorey);
                for (int i=0;i< firstStoreyIndex;i++)
                {
                    storeyInfos[i].StoreyName = "B" + (firstStoreyIndex - i).ToString() + "F";
                }
                for (int i = 1; i <=  storeyInfos.Count - firstStoreyIndex; i++)
                {
                    storeyInfos[i].StoreyName = i.ToString() + "F";
                }
            }
            // 设置层名
            Source = new ObservableCollection<ThEditStoreyInfo>(storeyInfos);
        }

        public void UpdateHeight(int index,string height)
        {
            for(int i =0;i<Source.Count;i++)
            {
                if(i == index)
                {
                    var storeyInfo = Source[i];
                    storeyInfo.Height = height;
                }
            }
        }

        public void UpdateRelatePaperName(int index,List<PaperItem> items)
        {
            for (int i = 0; i < Source.Count; i++)
            {
                if (i == index)
                {
                    var storeyInfo = Source[i];
                    storeyInfo.PaperName = string.Join(",",items.Select(o=>o.Name));
                    storeyInfo.PaperFrameHandle = string.Join(",", items.Select(o => o.PaperFrameHandle));
                }
            }
        }

        public void UpdateStoreyName(int index,string storeyName)
        {
            for (int i = 0; i < Source.Count; i++)
            {
                if (i == index)
                {
                    var storeyInfo = Source[i];
                    storeyInfo.StoreyName = storeyName;
                }
            }
        }

        public void ResetStoreyName(List<string> storeyNames)
        {
            if(storeyNames.Count==this.Source.Count)
            {
                for(int i=0;i< storeyNames.Count;i++)
                {
                    this.Source[i].StoreyName = storeyNames[i];
                }
            }
        }

        public void UpdateSource(List<ThEditStoreyInfo> newSources)
        {
            this.Source = new ObservableCollection<ThEditStoreyInfo>();
            this.Source = new ObservableCollection<ThEditStoreyInfo>(newSources);
        }

        public void RemoveStoreyes(List<int> removeIndexes)
        {
            var newSources = new ObservableCollection<ThEditStoreyInfo>();
            for(int i =0;i<Source.Count;i++)
            {
                if(removeIndexes.IndexOf(i) >=0)
                {
                    continue;
                }
                else
                {
                    newSources.Add(Source[i]);
                }
            }
            Source = newSources;
        }

        public void CalculateBuildingStoreyElevation()
        {
            // 根据各层的层高和首层高度计算
            var firstStorey = FindFirstStorey();
            if(firstStorey ==null)
            {
                return;
            }
            int firstStoreyIndex = Source.IndexOf(firstStorey);
            // 调整首层以下的标高
            for (int i = firstStoreyIndex - 1; i >= 0; i--)
            {
                var currentLevel = Source[i];
                var upLevel = Source[i + 1];
                var currentBottomElevation = upLevel.Bottom_Elevation_Value - currentLevel.Height_Value;
                currentLevel.Bottom_Elevation = currentBottomElevation.ToString();
            }
            // 调整首层以上的标高
            for (int i = firstStoreyIndex + 1; i < Source.Count; i++)
            {
                var currentLevel = Source[i];
                var downLevel = Source[i - 1];
                var currentBottomElevation = downLevel.Bottom_Elevation_Value + downLevel.Height_Value;
                currentLevel.Bottom_Elevation = currentBottomElevation.ToString();
            }
        }

        public ThEditStoreyInfo FindFirstStorey()
        {
            foreach(ThEditStoreyInfo storey in Source)
            {
                if(storey.StoreyName.StartsWith("B"))
                {
                    continue;
                }
                else
                {
                    return storey;
                }
            }
            return null;
        }
    }
}
