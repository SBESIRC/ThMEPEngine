using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil.PassageWay
{
    public class PipeInput
    {
        public Point3d pin;
        public Point3d pout = new Point3d(0, 0, 0);
        public double in_buffer;
        public double out_buffer = -1;

        public double length = 120000;
        PipeInput() { }
        public PipeInput(Point3d pin,double in_buffer)
        {
            this.pin = pin;
            this.in_buffer = in_buffer;
        }
        public PipeInput(Point3d pin, Point3d pout, double in_buffer, double out_buffer, double length = 120000)
        {
            this.pin = pin;
            this.pout = pout;
            this.in_buffer = in_buffer;
            this.out_buffer = out_buffer;
            this.length = length;
        }
    }
}
