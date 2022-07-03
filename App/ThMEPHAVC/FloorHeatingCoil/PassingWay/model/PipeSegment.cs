using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil.PassageWay
{
    class PipeSegment
    {
        // for calulate shortest way
        public int dir;                 // right,up,left,down
        public double start, end;       // end range
        public double min, max;
        public bool side = true;        // true：turn left / false:turn right
        public bool close_to = true;    // true:close to last end / false:close to last start
        // for intersect with buffer
        public double offset;           // offset buffer layer
        public bool buffer_turn;        // true:choose the left buffer / false:choose the right buffer
        public double dw;               // buffer distance
        public PipeSegment() { }
    }
}
