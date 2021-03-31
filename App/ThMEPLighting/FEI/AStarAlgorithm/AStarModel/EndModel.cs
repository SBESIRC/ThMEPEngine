using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.AStarAlgorithm.AStarModel
{
    //记录终点信息,可能是点或者线或者其他图元
    public class EndModel
    {
        public EndInfoType type;

        public Point3d endPoint { get; set; }

        public Point mapEndPoint { get; set; }

        public Line endLine { get; set; }

        public AStarLine mapEndLine { get; set; }
    }

    public enum EndInfoType
    {
        point,

        line,
    }
}
