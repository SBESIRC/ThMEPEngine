using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    public class PolygonInfo
    {
        public Polyline ExternalProfile;
        public List<Polyline> InnerProfiles;

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
