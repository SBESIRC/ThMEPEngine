using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Model
{
    public class SprayLayoutData
    {
        public Curve Radii { get; set; }
        public Point3d Position { get; set; }
        public Vector3d mainDir { get; set; }    //排布主方向
        public Vector3d otherDir { get; set; }   //排布次方向
        public Polyline vLine { get; set; }          //所在排布竖线
        public Polyline tLine { get; set; }          //所在排布横线
        public Polyline nextVLine { get; set; }      //前一条排布竖线
        public Polyline nextTLine { get; set; }      //前一条排布横线
        public Polyline prevVLine { get; set; }      //后一条排布竖线
        public Polyline prevTLine { get; set; }      //后一条排布横线
        public static SprayLayoutData Create(Point3d pos, Curve radii)
        {
            return new SprayLayoutData()
            {
                Radii = radii,
                Position = pos,
            };
        }
    }
}
