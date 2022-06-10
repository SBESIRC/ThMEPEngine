using System.Linq;
using System.Text.RegularExpressions;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public static class Check
    {
        public static bool IsCurrentFloor(this string floor)
        {
            if(floor is null)
            {
                return true;
            }
            if(floor == "")
            {
                return true;
            }
            var f =  floor?.Trim().ToUpper();
            if(!f.Contains('#'))//不包含'#'必然不是3号类型
            {
                return true;
            }
            var f1 = f.Split('#').First();//f1必须是纯数字
            var f2 = f.Split('#').Last();//f2以"-X"起头
            Regex regex = new Regex("^(-?[0-9]*[.]*[0-9]{0,3})$");
            if (regex.IsMatch(f1))
            {
                if(f2.Count() > 2)
                {
                    if(f2[0] == '-' && f2[1] == 'X')
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
