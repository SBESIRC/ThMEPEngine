using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class BufferPoly:IDisposable
    {
        public List<Point3d> poly { get; set; } = null;
        public List<double> buff { get; set; } = null;
        public BufferPoly() { }
        public BufferPoly(List<Point3d> poly) { this.poly = poly; }
        public BufferPoly(List<Point3d> poly, List<double> buff)
        {
            this.poly = poly;
            this.buff = buff;
        }
        public BufferPoly(Line line,double buffer)
        {
            poly = new List<Point3d>();
            poly.Add(line.StartPoint);
            poly.Add(line.EndPoint);
            buff = new List<double>();
            buff.Add(buffer);
        }
        public void Dispose()
        {
            poly.Clear();
            poly = null;
            buff.Clear();
            buff = null;
        }
        public Polyline Buffer(int multiple = 1)
        {
            var list_buffer = new List<Polyline>();
            for(int i = 0; i < buff.Count; ++i)
            {
                var p0 = poly[i];
                var p1 = poly[i + 1];
                if (i > 0)
                    p0 = poly[i] + (poly[i] - poly[i + 1]).GetNormal() * buff[i - 1] * multiple;
                if (i < buff.Count - 1)
                    p1 = poly[i+1] + (poly[i+1] - poly[i]).GetNormal() * buff[i + 1] * multiple;
                var line = new Line(p0, p1);
                list_buffer.Add(line.Buffer(buff[i] * multiple));
                line.Dispose();
            }
            var ret = list_buffer.ToArray().ToCollection().UnionPolygons().Cast<Polyline>().First();
            foreach (var polyline in list_buffer)
                polyline.Dispose();
            return ret;
        }
        public MPolygon BufferWithHole(int multiple = 1)
        {
            var list_buffer = new List<Polyline>();
            for (int i = 0; i < buff.Count; ++i)
            {
                var p0 = poly[i];
                var p1 = poly[i + 1];
                if (i > 0)
                    p0 = poly[i] + (poly[i] - poly[i + 1]).GetNormal() * buff[i - 1] * multiple;
                if (i < buff.Count - 1)
                    p1 = poly[i + 1] + (poly[i + 1] - poly[i]).GetNormal() * buff[i + 1] * multiple;
                var line = new Line(p0, p1);
                list_buffer.Add(line.Buffer(buff[i] * multiple));
                line.Dispose();
            }
            var ret = list_buffer.ToArray().ToCollection().UnionPolygons(true).Cast<MPolygon>().First();
            foreach (var polyline in list_buffer)
                polyline.Dispose();
            return ret;
        }
    }
}
