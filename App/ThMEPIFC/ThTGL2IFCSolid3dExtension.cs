using System;
using ThMEPTCH.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace ThMEPIFC
{
    public static class ThTGL2IFCSolid3dExtension
    {
        public static Solid3d CreateSolid3d(this ThTCHSlab slab)
        {
            // 先取出楼板Profile
            var profile = slab.Outline as MPolygon;

            // 创建楼板三维实体
            var regions = Region.CreateFromCurves(new DBObjectCollection() { profile.Shell() });
            var solid = CreateExtrudedSolid(regions[0] as Region, slab.Thickness, 0.0);


            //
            throw new NotImplementedException();
        }

        public static Solid3d CreateSolid3d(this ThTCHWall wall)
        {
            throw new NotImplementedException();
        }

        private static Solid3d CreateExtrudedSolid(Region region, double height,double taperAngle)
        {
            Solid3d ent = new Solid3d();
            ent.Extrude(region, height, taperAngle);
            return ent;
        }
    }
}