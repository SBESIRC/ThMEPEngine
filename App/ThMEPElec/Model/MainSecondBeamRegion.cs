using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ThMEPElectrical.Model
{
    /// <summary>
    /// 记录主次梁的有效区域
    /// </summary>
    public class MainSecondBeamRegion
    {
        //可布置区域
        public List<Polyline> ValidRegions
        {
            get;
            private set;
        } = new List<Polyline>();

        // 原始的没有经过调整的插入点
        public List<Point3d> PlacePoints
        {
            get;
            private set;
        } = new List<Point3d>();

        public MainSecondBeamRegion(List<Polyline> validPolys, List<Point3d> pts)
        {
            ValidRegions = validPolys;
            PlacePoints = pts;
        }
    }
}
