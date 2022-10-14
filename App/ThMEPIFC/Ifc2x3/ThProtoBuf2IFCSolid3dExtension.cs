using Xbim.Ifc;
using Xbim.Common.Geometry;
using System.Collections.Generic;
using Xbim.Ifc2x3.ProfileResource;

namespace ThMEPIFC.Ifc2x3
{
    public static class ThProtoBuf2IFCSolid3dExtension
    {
        public static readonly XbimVector3D XAxis = new XbimVector3D(1, 0, 0);
        public static readonly XbimVector3D YAxis = new XbimVector3D(0, 1, 0);
        public static readonly XbimVector3D ZAxis = new XbimVector3D(0, 0, 1);
        public static readonly XbimMatrix3D WordMatrix = new XbimMatrix3D(XbimVector3D.Zero);

        public static List<IXbimSolid> GetSlabSolid(this ThTCHSlabData slab, ThXbimSlabEngine slabxbimEngine)
        {
            var geometryParam = slab.BuildElement;
            var slabDes = slab.Descendings;
            XbimVector3D moveVector = XbimVector3D.Zero;
            IXbimSolidSet solidSet = slabxbimEngine.Engine.CreateSolidSet();
            var slabSolid = GetXBimSolid(geometryParam, moveVector, slabxbimEngine);
            foreach (var item in slabSolid)
                solidSet.Add(item);
            var openings = new List<IXbimSolid>();
            using (var txn = slabxbimEngine.Model.BeginTransaction("Create solid"))
            {
                var thisMove = moveVector;
                foreach (var item in slabDes)
                {
                    if (item.IsDescending)
                    {
                        var outLine = item.OutlineBuffer;
                        IXbimSolid opening = null;
                        IXbimSolid geoSolid = null;
                        geoSolid = GetXBimSolid2x3(outLine.Shell, thisMove, ZAxis.Negated(), item.DescendingThickness + item.DescendingHeight, slabxbimEngine);
                        opening = GetXBimSolid2x3(item.Outline.Shell, thisMove, ZAxis.Negated(), item.DescendingHeight, slabxbimEngine);
                        if (null == geoSolid || geoSolid.SurfaceArea < 10)
                            continue;
                        solidSet = solidSet.Union(geoSolid, 1);
                        openings.Add(opening);
                    }
                    else
                    {
                        geometryParam.Outline.Holes.Add(item.Outline.Shell);
                    }
                }
                foreach (var item in geometryParam.Outline.Holes)
                {
                    IXbimSolid opening = null;
                    if (item.Points == null || item.Points.Count < 1)
                        continue;
                    opening = GetXBimSolid2x3(item, thisMove, ZAxis.Negated(), geometryParam.Height + 0, slabxbimEngine);//geometryStretch.Outline.HolesMaxHeight
                    if (null == opening || opening.SurfaceArea < 10)
                        continue;
                    openings.Add(opening);
                }
                foreach (var item in openings)
                {
                    solidSet = solidSet.Cut(item, 1);
                }
                txn.Commit();
            }
            List<IXbimSolid> solids = new List<IXbimSolid>();
            foreach (var item in solidSet)
                solids.Add(item);
            return solids;
        }

        public static List<IXbimSolid> GetXBimSolid(ThTCHBuiltElementData geometryParam, XbimVector3D moveVector, ThXbimSlabEngine slabxbimEngine)
        {
            var resList = new List<IXbimSolid>();
            IXbimSolid geoSolid = null;
            using (var txn = slabxbimEngine.Model.BeginTransaction("Create solid"))
            {
                geoSolid = GetXBimSolid2x3(geometryParam, moveVector, slabxbimEngine.Model, slabxbimEngine);
                txn.Commit();
            }
            if (null != geoSolid)
                resList.Add(geoSolid);
            return resList;
        }

        private static IXbimSolid GetXBimSolid2x3(ThTCHBuiltElementData geometryStretch, XbimVector3D moveVector, IfcStore memoryModel, ThXbimSlabEngine slabxbimEngine)
        {
            IfcProfileDef profile = null;
            if (geometryStretch.Outline != null && geometryStretch.Outline.Shell != null && geometryStretch.Outline.Shell.Points.Count > 0)
            {
                profile = memoryModel.ToIfcArbitraryClosedProfileDef(geometryStretch.Outline.Shell);
            }
            if (profile == null)
                return null;
            var solid = memoryModel.ToIfcExtrudedAreaSolid(profile, ZAxis.Negated(), geometryStretch.Height);
            var geoSolid = slabxbimEngine.Engine.CreateSolid(solid);
            var realMove = moveVector;// + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
            var trans = XbimMatrix3D.CreateTranslation(realMove.X, realMove.Y, realMove.Z);
            geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            return geoSolid;
        }

        private static IXbimSolid GetXBimSolid2x3(ThTCHPolyline polyline, XbimVector3D moveVector, XbimVector3D zAxis, double zHeight, ThXbimSlabEngine slabxbimEngine)
        {
            IfcProfileDef profile = slabxbimEngine.Model.ToIfcArbitraryClosedProfileDef(polyline);
            if (profile == null)
                return null;
            var solid = slabxbimEngine.Model.ToIfcExtrudedAreaSolid(profile, zAxis, zHeight);
            var geoSolid = slabxbimEngine.Engine.CreateSolid(solid);
            var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
            geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            return geoSolid;
        }
    }
}
