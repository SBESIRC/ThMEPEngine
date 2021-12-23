using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanPipeAlgorithm
{
    public class edge
    {
        //变量
        public double rx1, ry1, rx2, ry2;  //端点真实坐标
        public int x1, y1, x2, y2;      //端点网格坐标
        public int angle = 0;
        public int area_id = -1;
        public bool fix_first_stage = false;
        public bool fix_second_stage = false;

        public edge(int x1, int y1, int x2, int y2)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;

            if (x1 == x2)
            {
                this.angle = 90;
            }
            else
            {
                this.angle = 0;
            }

            fix_first_stage = false;
            fix_second_stage = false;
            //attached = false;

            //area_id = "None";

        }

        public edge(double x1, double y1, double x2, double y2)
        {
            this.rx1 = x1;
            this.ry1 = y1;
            this.rx2 = x2;
            this.ry2 = y2;

            if (rx1 == rx2)
            {
                this.angle = 90;
            }
            else
            {
                this.angle = 0;
            }

            fix_first_stage = false;
            fix_second_stage = false;
            //attached = false;

            //area_id = "None";

        }


        public void fix_stage_one(double coord)
        {
            if (angle == 0)
            {
                ry1 = coord;
                ry2 = coord;
            }
            else
            {
                rx1 = coord;
                rx2 = coord;
            }
            fix_first_stage = true;
        }


        public void fix_stage_two(double coord1, double coord2)
        {
            if (angle == 0)
            {
                rx1 = coord1;
                rx2 = coord2;
            }
            else
            {
                ry1 = coord1;
                ry2 = coord2;
            }
            fix_second_stage = false;
        }

    }
}
