using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class PipeOutput
    {
        public List<Polyline> skeleton = null;
        public Polyline shape = null;
        public int pipe_id = -1;
        public PipeOutput() { }
        public PipeOutput(int pipe_id,BufferPoly buffer_poly)
        {
            this.pipe_id = pipe_id;
            this.skeleton = new List<Polyline>();
            this.skeleton.Add(PassageWayUtils.BuildPolyline(buffer_poly.poly));
            this.shape = buffer_poly.Buffer();

            SetColor();
        }
        public PipeOutput(int pipe_id,List<Polyline> skeletons,Polyline shape)
        {
            this.pipe_id = pipe_id;
            this.skeleton = skeletons;
            this.shape = shape;

            SetColor();
        }

        public PipeOutput(int pipe_id,Polyline skeleton,Polyline shape)
        {
            this.pipe_id = pipe_id;
            this.skeleton = new List<Polyline>();
            this.skeleton.Add(skeleton);
            this.shape = shape;

            SetColor();
        }

        void SetColor()
        {
            foreach (var poly in skeleton)
                poly.ColorIndex = pipe_id % 7 + 1;
            shape.ColorIndex = pipe_id % 7 + 1;
        }
    }
}
