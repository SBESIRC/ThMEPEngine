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
        public Point3d OriginPt { get; set; }    //原始生成喷淋原始点位
        public Vector3d mainDir { get; set; }    //排布主方向
        public Vector3d otherDir { get; set; }   //排布次方向
        public Line vLine { get; set; }          //所在排布竖线
        public Line tLine { get; set; }          //所在排布横线
        public Line nextVLine { get; set; }      //前一条排布竖线
        public Line nextTLine { get; set; }      //前一条排布横线
        public Line prevVLine { get; set; }      //后一条排布竖线
        public Line prevTLine { get; set; }      //后一条排布横线
        public static SprayLayoutData Create(Point3d pos, Curve radii)
        {
            return new SprayLayoutData()
            {
                Radii = radii,
                Position = pos,
                OriginPt = pos,
            };
        }
    }
}
