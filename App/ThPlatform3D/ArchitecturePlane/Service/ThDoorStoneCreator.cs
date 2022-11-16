using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;

namespace ThPlatform3D.ArchitecturePlane.Service
{
    internal class ThDoorStoneCreator
    {
        public ThDoorStoneCreator()
        {
        }
        public DBObjectCollection Create(List<ThComponentInfo> doors) 
        {
            var results = new DBObjectCollection();
            doors.ForEach(o =>
            {
                Create(o).OfType<Polyline>().ForEach(p =>
                {
                    results.Add(p);
                });
            });
            return results;
        }
        
        private DBObjectCollection Create(ThComponentInfo door)
        {
            var results = new DBObjectCollection();
            var sp = door.Start;
            var ep = door.End;
            if(sp.HasValue && ep.HasValue)
            {
                if(sp.Value.DistanceTo(ep.Value)<=10.0)
                {
                    return results;
                }
                var dir = sp.Value.GetVectorTo(ep.Value);
                if (ThGeometryTool.IsParallelToEx(dir, Vector3d.ZAxis))
                {
                    return results; //和Z轴平行
                }
                var wallThick = door.Thickness.GetWallThick(ThArchitecturePlaneCommon.Instance.WallWindowThickRatio);
                if(wallThick<=1e-6)
                {
                    return results;
                }
                var dsLength = GetDoorStoneLength(wallThick);
                if(sp.Value.DistanceTo(ep.Value)< dsLength*2.0)
                {
                    return results;
                }
                results.Add(CreateDoorStone(sp.Value, dir, dsLength, dsLength));
                results.Add(CreateDoorStone(ep.Value, dir.Negate(), dsLength, dsLength));
            }
            return results;
        }

        private Polyline CreateDoorStone(Point3d pt,Vector3d dir,double length,double height)
        {
            var pendVec = dir.GetPerpendicularVector().GetNormal();
            var pt1 = pt + pendVec.MultiplyBy(height/2.0);
            var pt4 = pt - pendVec.MultiplyBy(height/2.0);
            var pt2 = pt1 + dir.GetNormal().MultiplyBy(length);
            var pt3 = pt4 + dir.GetNormal().MultiplyBy(length);

            var pts = new Point3dCollection() { pt1, pt2,pt3,pt4};
            return pts.CreatePolyline();
        }

        private double GetDoorStoneLength(double wallThick)
        {
            return wallThick / 100.0 * 30.0;
        }
    }
}
