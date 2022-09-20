using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class PipeSegment
    {
        // 计算方向分组所需信息
        public int dir;                 // 0~3:右上左下
        public double start, end;       // 末段坐标范围 同时为x或者同时为y  与下方变量垂直
        public double min, max;         // min < max
        // 计算均匀分布路径所需信息 
        public bool side = true;        // T:下一段左转/F:下一段右转
        public bool close_to = true;    // T:端点靠近末端范围终点/F:端点靠近末端范围起点
        public double offset;           // 偏移条数
        public double pw;               // 该段管道宽度
        public double max_pw = -1;      // 该管道最大均匀分布宽度
        // 计算导向路径所需信息
        public bool equispaced;         // T:均匀间距分布/F:推荐间距分布
        public PipeSegment() { }
        public PipeSegment(int dir,double pw)
        {
            this.dir = dir;
            this.pw = pw;
            this.equispaced = false;
        }
    }
}
