using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanPipeAlgorithm
{
    //全局变量
    public class PublicValue
    {
        public static int MAX_LENGTH = 10000000;
        public static int ITER = 50;
        public static int LineStep = 150;
        public static double MIN_DIS = 0.5;
        
        public static int CELL = 150;
        public static int BigCell = 400;
        public static int SmallCell = 150;

        public static int Extension = 1;
        public static int Traversable = 0;

        //mode 0：基础整线
        //mode 1：终点不要附加线条
        public static int Arrange_mode = 0;

        //线条是否在中央
        public static double Deviation = 200;
        public static int Center = 0;

        //方向定义  SSS
        //右(x+1):0 
        //上(y+1):1 
        //左(x-1):2 
        //下(y-1):3 
    }
}
