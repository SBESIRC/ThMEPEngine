using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.CAD;
using ThMEPElectrical.Business;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Core
{
    public class PackageManager
    {
        public PackageManager()
        {
            ;
        }

        public List<Polyline> DoMainBeamProfiles()
        {
            var dataExtract = new DBExtract();
            dataExtract.GetCurves();

            var calculateDect = new CalculateDetection(dataExtract.Walls.First(), dataExtract.SubtractCurves);

            var mainBeamProfiles = calculateDect.CalculateMainBeamProfiles();
            return mainBeamProfiles;
        }
    }
}
