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
             A-Toilet-8  ->  阳台洗手盆
             */
            return name.ToLower().Contains("A-Toilet-5".ToLower()) 
                || name.ToLower().Contains("A-Toilet-1".ToLower()) 
                || name.ToLower().Contains("A-Kitchen-4".ToLower()) 
                || name.ToLower().Contains("A-Toilet-6".ToLower())
                || name.ToLower().Contains("A-Toilet-9".ToLower())
                || name.ToLower().Contains("A-Kitchen-9".ToLower());
        }
        public static bool IsToilet5(string name)
        {
            return name.ToLower().Contains("A-Toilet-5".ToLower());
        }
    }
}
