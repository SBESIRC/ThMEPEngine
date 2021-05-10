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

        public ThWSSDStorey(int FloorNumber)
        {
            this.FloorNumber = FloorNumber;
        }

        public Line CreateLine()
        {
            var pt1 = new Point3d(INDEX_START_X, INDEX_START_Y + FloorNumber * FLOOR_HEIGHT, 0);
            var pt2 = new Point3d(INDEX_START_X + FLOOR_LENGTH, INDEX_START_Y + FloorNumber * FLOOR_HEIGHT, 0);

            var line1 = new Line(pt1, pt2);

            return line1;
        }


    }


    public class ThWSuplySystemDiagram //: ThWSSDPipeRun  //竖管系统类
    {
        public int Loweststorey;  //竖管最低层
        public int Higheststorey; //竖管最高层
        public List<ThWSSDPipeRun> PipeRuns;//PipeRun的数组
        public const int FLOOR_HEIGHT = 2900;  //楼层线间距

        public const double INDEX_START_X = 1700;
        public const double INDEX_START_Y = 1000;
        public const double FLOOR_LENGTH = 20000;
        //public const double OFFSET_X = 1000;
        public double Offset_X;  

        public ThWSuplySystemDiagram(int loweststorey, int higheststorey, double offset_X)
        {
            this.Loweststorey = loweststorey;
            this.Higheststorey = higheststorey;
            this.Offset_X = offset_X;
            this.PipeRuns = new List<ThWSSDPipeRun>();
        }

        public Line CreatePipeLine()
        {
            //var pt1 = new Point3d(INDEX_START_X + Offset_X, INDEX_START_Y + (Loweststorey - 1) * FLOOR_HEIGHT, 0);
            var pt1 = new Point3d(INDEX_START_X + Offset_X, 0, 0);
            var pt2 = new Point3d(INDEX_START_X + Offset_X, INDEX_START_Y + Higheststorey * FLOOR_HEIGHT, 0);

            var line1 = new Line(pt1, pt2);
            return line1;
        }
    }


    public class ThWSSDPipeRun
    {
        public double PipeDiameter { get; set; }//管径
        public int Loweststorey;  //最低楼层
        public int Higheststorey; //最高楼层
        public List<ThWSSDPipeUnit> PipeUnits;//PipeUnit的数组

        public const int FLOOR_HEIGHT = 2900;  //楼层线间距
        public const double INDEX_START_X = 1700;
        public const double INDEX_START_Y = 1000;
        public const double FLOOR_LENGTH = 20000;
        public const double OFFSET_X = 1000;

        public ThWSSDPipeRun(double pipeDiameter, int loweststorey, int higheststorey)
        { 
            this.PipeDiameter = pipeDiameter;
            this.Loweststorey = loweststorey;
            this.Higheststorey = higheststorey;
            this.PipeUnits = new List<ThWSSDPipeUnit>();
            for(int i = 0; i <= higheststorey - loweststorey; ++i)
            {
                PipeUnits.Add(new ThWSSDPipeUnit(pipeDiameter, loweststorey + i));
            }
        }

        public Line CreatePipeLine()
        {
            var pt1 = new Point3d(INDEX_START_X + OFFSET_X, INDEX_START_Y + (Loweststorey - 1) * FLOOR_HEIGHT, 0);
            var pt2 = new Point3d(INDEX_START_X + OFFSET_X, INDEX_START_Y + Higheststorey * FLOOR_HEIGHT, 0);

            var line1 = new Line(pt1, pt2);
            return line1;
        }
    }

    public class ThWSSDPipeUnit  //竖管单元
    {
        public double PipeDiameter;  //管径
        public int FloorNumber;  //楼层号


        //管径计算参数
        private int QL = 250;  //最高日用水定额 QL
        private double Kh = 2.5;  //最高日小时变化系数  Kh
        private double m = 3.5;   //每户人数  m

        private double Ng = 6.7;  //用水总当量/分区内住户总数
        private double T = 24; //用水时数，24

        
        //public double PipeDiameterCompute(double PipeDiameter)
        public double PipeDiameterCompute()
        {
            //计算各管段的流量
            double U0 = 100 * QL * m * Kh / (0.2 * Ng * T * 3600);

            double alphaC = 0.01097;  //对应于 U0 的系数，线性插入

            double U = (1 + alphaC * Math.Pow((Ng - 1), 0.45)) / (Math.Sqrt(Ng));

            double qg = 0.2 * U * Ng;  //管段的设计秒流量


            //管径列表
            Dictionary<string, double> pipeDList = new Dictionary<string, double> 
            { {"DN15",0.0157 },  {"DN20",0.0213 }, {"DN25",0.0273 },  {"DN32",0.0354 }, 
              {"DN40",0.0413 },  {"DN50",0.0527 }, {"DN65",0.0681 },  {"DN80",0.0809 },
              {"DN100",0.1063 }, {"DN125",0.131 }, {"DN150",0.1593 }, {"DN200",0.2071 } };


            //double[] pipeDList = {0.0157, 0.0213, 0.0273, 0.0354, 0.0413, 0.0527, 0.0681, 0.0809, 0.1063, 0.131, 0.1593, 0.2071};

            double FlowRate = qg * 4 / (Math.PI * PipeDiameter * PipeDiameter * 1000);  // 不同管径下的流速

            return FlowRate;
        }
        


        public ThWSSDPipeUnit(double PipeDiameter, int FloorNumber)  //构造函数
        {
            this.PipeDiameter = PipeDiameter;
            this.FloorNumber = FloorNumber;
        }
    }
    

    public class ThWSSDBranchPipe   //给用户供水的支管类
    {
        public int FloorNumber;  //楼层号
        
    }

}

