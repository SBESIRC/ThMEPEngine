using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.Tools
{
    public static class ListEx
    {
        public static List<T> Slice<T>(this List<T> list,IEnumerable<int> idxs)
        {
            var result = new List<T>();
            foreach(var idx in idxs)
            {
                result.Add(list[idx]);
            }
            return result;  
        }
        public static List<T> SliceExcept<T>(this List<T> list, IEnumerable<int> idxs)
        {
            var excludeSet = idxs.ToHashSet();
            var result = new List<T>();
            for(int i = 0; i < list.Count; i++)
            {
                if(excludeSet.Contains(i)) continue;
                result.Add(list[i]);
            }
            return result;
        }
    }
}
