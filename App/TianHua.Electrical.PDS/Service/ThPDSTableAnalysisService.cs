using System.Linq;
using System.Collections.Generic;

using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSTableAnalysisService
    {
        /// <summary>
        /// 配电箱序列
        /// </summary>
        public List<int> DistBoxFilter = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        public void Analysis(List<ThPDSBlockInfo> tableInfo, ref List<string> nameFilter, ref List<string> propertyFilter,
            ref List<string> distBoxKey)
        {
            foreach (var o in tableInfo)
            {
                if (string.IsNullOrEmpty(o.Properties))
                {
                    nameFilter.Add(o.BlockName);
                }
                else
                {
                    propertyFilter.Add(o.Properties);
                }
            }
            for (var i = 0; i < propertyFilter.Count; i++)
            {
                if (DistBoxFilter.Contains(i))
                {
                    distBoxKey.Add(propertyFilter[i]);
                }
            }

            // 调整数组顺序
            distBoxKey = distBoxKey.OrderByDescending(key => key.Length).ToList();
        }
    }
}
