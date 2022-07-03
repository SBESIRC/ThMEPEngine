using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil.PassageWay
{
    class BufferPoly:IDisposable
    {
        public Polyline poly;
        public List<double> buff;
        public BufferPoly(Polyline poly, List<double> buff)
        {
            this.poly = poly;
            this.buff = buff;
        }
        public void Dispose()
        {
            poly.Dispose();
            buff.Clear();
            buff = null;
        }
        public Polyline Buffer(int multiple=1)
        {
            var list_buffer = new List<Polyline>();
            for(int i = 0; i < buff.Count; ++i)
            {
                var p0 = poly.GetPoint3dAt(i);
                var p1 = poly.GetPoint3dAt(i + 1);
                if (i > 0)
                    p0 += (p0 - p1).GetNormal() * buff[i - 1] * multiple;
                if (i < buff.Count - 1)
                    p1 += (p1 - p0).GetNormal() * buff[i + 1] * multiple;
                var line = new Line(p0, p1);
                list_buffer.Add(line.Buffer(buff[i] * multiple));
            }
            return list_buffer.ToArray().ToCollection().UnionPolygons().Cast<Polyline>().First();
        }
    }
}
