using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    class PipeDiameter
    {
        private Point3d StartPoint { get; set; }

        private Point3d EndPoint { get; set; }

        private string PipeD { get; set; }

        public PipeDiameter(Point3d startPoint, Point3d endPoint, string pipeD)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            PipeD = pipeD;
        }

        public Point3d GetStartPoint()
        {
            return StartPoint;
        }

        public Point3d GetEndPoint()
        {
            return EndPoint;
        }

        public string GetPipeD()
        {
            return PipeD;
        }
    }
}
