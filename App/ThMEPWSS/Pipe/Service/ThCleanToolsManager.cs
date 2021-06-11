using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Pipe.Service
{
    public class ThCleanToolsManager
    {
        public static bool IsCleanToolBlockName(string name)
        {
            /*
             A-Toilet-5  ->  座便器
             A-Toilet-1  ->  洗手台
             A-Kitchen-4 ->  洗涤盆
             A-Toilet-6  ->  淋浴器
             A-Toilet-9  ->  洗衣机
             A-Kitchen-9  ->  阳台洗手盆
             */

            return name.ToLower().Contains("A-Toilet-5".ToLower())
                || name.ToLower().Contains("A-Toilet-1".ToLower())
                || name.ToLower().Contains("A-Kitchen-4".ToLower())
                || name.ToLower().Contains("A-Toilet-6".ToLower())
                || name.ToLower().Contains("A-Toilet-9".ToLower())
                || name.ToLower().Contains("A-Kitchen-9".ToLower());
        }

        public static int CleanToolIndex(string name)
        {
            return Convert.ToInt32(name.ToLower().Contains("A-Toilet-5".ToLower())) * 0
                 + Convert.ToInt32(name.ToLower().Contains("A-Toilet-1".ToLower())) * 1
                 + Convert.ToInt32(name.ToLower().Contains("A-Kitchen-4".ToLower())) * 2
                 + Convert.ToInt32(name.ToLower().Contains("A-Toilet-6".ToLower())) * 3
                 + Convert.ToInt32(name.ToLower().Contains("A-Toilet-9".ToLower())) * 4
                 + Convert.ToInt32(name.ToLower().Contains("A-Kitchen-9".ToLower())) * 5
                 + Convert.ToInt32(name.ToLower().Contains("拖把池".ToLower())) * 6
                 + Convert.ToInt32(name.ToLower().Contains("浴缸".ToLower())) * 7;
        }

        //public static bool IsToilet(string name) //坐便器
        //{
        //    return name.ToLower().Contains("A-Toilet-5".ToLower());
        //}

        //public static bool IsWashBasin(string name)//洗手台
        //{
        //    return name.ToLower().Contains("A-Toilet-1".ToLower());
        //}

        //public static bool IsSink(string name)//洗涤盆
        //{
        //    return name.ToLower().Contains("A-Kitchen-4".ToLower());
        //}

        //public static bool IsShower(string name)//沐浴器
        //{
        //    return name.ToLower().Contains("A-Toilet-6".ToLower());
        //}

        //public static bool IsWashingMachine(string name)//洗衣机
        //{
        //    return name.ToLower().Contains("A-Toilet-9".ToLower());
        //}

        //public static bool IsBalconyWashBasin(string name)//阳台洗手盆
        //{
        //    return name.ToLower().Contains("A-Kitchen-9".ToLower());
        //}
    }
}
