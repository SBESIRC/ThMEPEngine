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
        /// <summary>
        /// 带有宽度信息的多段线外扩，外扩之后只保留外壳
        /// </summary>
        /// <param name="multiple">外扩倍数</param>
        /// <returns>外扩后的多段线轮廓</returns>
        public Polyline Buffer(double multiple = 1)
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
        /// <summary>
        /// 带有宽度信息的多段线外扩，外扩之后保留外壳和洞
        /// </summary>
        /// <param name="multiple">外扩倍数</param>
        /// <returns>外扩后的多段线轮廓</returns>
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
        public Polyline Buffer4()
        {
            var list_buffer = new List<Polyline>();
            var dirs = new List<int>();
            for (int i = 0; i < buff.Count; ++i)
                dirs.Add(PassageWayUtils.GetDirBetweenTwoPoint(poly[i], poly[i + 1]));
            for (int i = 0; i < buff.Count; ++i)
            {
                List<Point3d> cur_buffer = new List<Point3d>();
                var dir = (poly[i + 1] - poly[i]).GetNormal();
                if (i == 0)
                {
                    cur_buffer.Add(poly[i] + dir.RotateBy(-Math.PI / 2, Vector3d.ZAxis) * buff[i] * 4);
                    cur_buffer.Add(poly[i] + dir.RotateBy(Math.PI / 2, Vector3d.ZAxis) * buff[i] * 4);
                }
                else
                {
                    if ((dirs[i - 1] + 1) % 4 == dirs[i]) 
                    {
                        cur_buffer.Add(poly[i] + dir.RotateBy(-Math.PI / 2, Vector3d.ZAxis) * buff[i] * 4 - dir * buff[i - 1] * 4);
                        cur_buffer.Add(poly[i] - dir * buff[i - 1] * 4);
                        cur_buffer.Add(poly[i]);
                        cur_buffer.Add(poly[i] + dir.RotateBy(Math.PI / 2, Vector3d.ZAxis) * buff[i] * 4);
                    }
                    else
                    {
                        cur_buffer.Add(poly[i] + dir.RotateBy(-Math.PI / 2, Vector3d.ZAxis) * buff[i] * 4);
                        cur_buffer.Add(poly[i]);
                        cur_buffer.Add(poly[i] - dir * buff[i - 1] * 4);
                        cur_buffer.Add(poly[i] + dir.RotateBy(Math.PI / 2, Vector3d.ZAxis) * buff[i] * 4 - dir * buff[i - 1] * 4);
                    }
                }
                cur_buffer.Add(poly[i + 1] + dir.RotateBy(Math.PI / 2, Vector3d.ZAxis) * buff[i] * 4);
                cur_buffer.Add(poly[i + 1] + dir.RotateBy(-Math.PI / 2, Vector3d.ZAxis) * buff[i] * 4);
                cur_buffer.Add(cur_buffer.First());
                list_buffer.Add(PassageWayUtils.BuildPolyline(cur_buffer));
            }
            var ret = list_buffer.ToArray().ToCollection().UnionPolygons().Cast<Polyline>().First();
            foreach (var polyline in list_buffer)
                polyline.Dispose();
            return ret;
        }
    }
}
