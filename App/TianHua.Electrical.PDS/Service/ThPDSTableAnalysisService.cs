using System.Collections.Generic;

using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSTableAnalysisService
    {
        public void Analysis(List<ThPDSBlockInfo> tableInfo, List<int> distBoxFilter, ref List<string> nameFilter,
            ref List<string> propertyFilter, ref List<string> distBoxKey)
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
                if (distBoxFilter.Contains(i))
                {
                    distBoxKey.Add(propertyFilter[i]);
                }
            }
        }
    }
}
