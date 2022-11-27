using System.Collections.Generic;

namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class ListTool
    {
        public static void AddItems<T>(this List<T> list1,List<T> list2,List<T> checkList=null)
        {
            if(checkList==null)
            {
                foreach (var item in list2)
                {
                    if (!list1.Contains(item))
                    {
                        list1.Add(item);
                    }
                }
            }
            else
            {
                foreach (var item in list2)
                {
                    if (!checkList.Contains(item))
                    {
                        list1.Add(item);
                    }
                }
            }
        }
    }
}
