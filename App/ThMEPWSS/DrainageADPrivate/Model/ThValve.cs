using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.DrainageADPrivate.Model
{
    class ThValve
    {
        public Polyline Boundary { get; set; }
        public Point3d InsertPt { get; set; }
        public string Name { get; set; }
        public Vector3d Dir { get; set; } //x轴*角度
        public Point3d TransInsertPt { get; set; }
        public ThDrainageTreeNode ConnectNode { get; set; } //与node => node.parent共线
        public double Scale { get; set; }
    }
}
