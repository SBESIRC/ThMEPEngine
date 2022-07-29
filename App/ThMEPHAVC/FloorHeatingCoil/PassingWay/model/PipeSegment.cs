using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class PipeSegment
    {
        // for group dirs
        public int dir;                 // right,up,left,down
        public double start, end;       // end range
        public double min, max;
        // for calculate shortest way
        public bool side = true;        // true：turn left / false:turn right
        public bool close_to = true;    // true:close to last end / false:close to last start
        public double offset;           // offset buffer layer
        public double pw;               // pipe_width
        // for intersect with buffer
        public bool equispaced;         // true:evenly distribute / false:fixed distance to room boundary
        public PipeSegment() { }
        public PipeSegment(int dir,double pw)
        {
            this.dir = dir;
            this.pw = pw;
            this.equispaced = false;
        }
    }
}
