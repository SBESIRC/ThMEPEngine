using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DU = ThMEPWSS.Assistant.DrawUtils;

namespace ThMEPWSS.Pipe.Model
{
    public enum LayingMethod //敷设方式
    {
        Piercing,  //穿梁
        Buried     //埋地
    }

    /// <summary>
    /// 楼层类
    /// </summary>
    public class ThWSSDStorey  //楼层类  Th Water Suply System Diagram Storey
    {
        public int FloorNumber;  //楼层号
        public const int FLOOR_HEIGHT = 2900;  //楼层线间距
        public const double INDEX_START_X = 1700;
        public const double INDEX_START_Y = 1000;
        public const double FLOOR_LENGTH = 20000;
        public bool FlushFaucet; //冲洗龙头
        public bool PressureReducingValve; //减压阀

        public ThWSSDStorey(int num)
        {
            FloorNumber = num;
        }

        public Line CreateLine()
        {
            var pt1 = new Point3d(INDEX_START_X, INDEX_START_Y + FloorNumber * FLOOR_HEIGHT, 0);
            var pt2 = new Point3d(INDEX_START_X + FLOOR_LENGTH, INDEX_START_Y + FloorNumber * FLOOR_HEIGHT, 0);

            var line1 = new Line(pt1, pt2);

            return line1;
        }


    }


    public class ThWSuplySystemDiagram : ThWSSDPipeRun  //竖管系统类
    {

        

    }
    public class ThWSSDPipeRun : ThWSSDPipeUnit  //
    {
        public int loweststorey;
        public int higheststorey;




    }

    public class ThWSSDPipeUnit  //竖管单元
    {
        public double PipeDiameter;



    }
}
