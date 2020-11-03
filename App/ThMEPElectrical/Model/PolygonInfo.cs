using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using System.Collections.Generic;


namespace ThMEPElectrical.Model
{
    public class PolygonInfo
    {
        public Polyline ExternalProfile;
        public List<Polyline> InnerProfiles;

        public Matrix3d UserSys = Active.Editor.WCS2UCS(); // 被最小的拥有
        public Vector3d BlockXAxis;

        public Matrix3d OriginMatrix;
        public double rotateAngle; // 块的旋转角度
        public bool IsUsed = false;

        public PolygonInfo(Polyline externalPro)
        {
            ExternalProfile = externalPro;
            InnerProfiles = new List<Polyline>();
        }

        public PolygonInfo(Polyline externalPro, List<Polyline> innerProfiles)
        {
            ExternalProfile = externalPro;
            InnerProfiles = innerProfiles;
        }
    }
}
