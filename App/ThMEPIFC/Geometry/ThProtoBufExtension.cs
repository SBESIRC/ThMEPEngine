using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPIFC.Ifc2x3;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc2x3.ProfileResource;

namespace ThMEPIFC.Geometry
{
    public static class ThProtoBufExtension
    {
        public static ThTCHMatrix3d ToTCHMatrix3d(this XbimMatrix3D m)
        {
            return new ThTCHMatrix3d
            {
                Data11 = m.M11,
                Data12 = m.M12,
                Data13 = m.M13,
                Data14 = m.M14,
                Data21 = m.M21,
                Data22 = m.M22,
                Data23 = m.M23,
                Data24 = m.M24,
                Data31 = m.M31,
                Data32 = m.M32,
                Data33 = m.M33,
                Data34 = m.M34,
                Data41 = m.OffsetX,
                Data42 = m.OffsetY,
                Data43 = m.OffsetZ,
                Data44 = m.M44,
            };
        }

        public static XbimVector3D ToXbimVector3D(this ThTCHVector3d v)
        {
            return new XbimVector3D(v.X, v.Y, v.Z);
        }

        public static XbimPoint3D ToXbimPoint3D(this ThTCHPoint3d p)
        {
            return new XbimPoint3D(p.X, p.Y, p.Z);
        }

        public static IfcArbitraryProfileDefWithVoids ToIfcArbitraryProfileDefWithVoids(this IfcStore model, ThTCHMPolygon e)
        {
            return model.Instances.New<IfcArbitraryProfileDefWithVoids>(d =>
            {
                d.ProfileType = IfcProfileTypeEnum.AREA;
                d.OuterCurve = model.ToIfcCompositeCurve(e.Shell);
                foreach (var hole in e.Holes)
                {
                    var innerCurve = model.ToIfcCompositeCurve(hole);
                    d.InnerCurves.Add(innerCurve);
                }
            });
        }

        public static IfcArbitraryClosedProfileDef ToIfcArbitraryClosedProfileDef(this IfcStore model, ThTCHMPolygon e)
        {
            return model.Instances.New<IfcArbitraryClosedProfileDef>(d =>
            {
                d.ProfileType = IfcProfileTypeEnum.AREA;
                d.OuterCurve = model.ToIfcCompositeCurve(e.Shell);
            });
        }
    }
}
