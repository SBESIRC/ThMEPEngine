using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class HandleConfluenceService
    {
        Polyline mainPoly;
        List<VerticalPipeModel> verticalPipes;
        SewageWasteWaterEnum sewageWasteWaterEnum;
        List<Dictionary<Polyline, int>> deepRooms;
        public HandleConfluenceService(SewageWasteWaterEnum _sewageWasteWaterEnum, List<VerticalPipeModel> _verticalPipes, List<Dictionary<Polyline, int>> _deepRooms)
        {
            sewageWasteWaterEnum = _sewageWasteWaterEnum;
            verticalPipes = _verticalPipes;
            deepRooms = _deepRooms;
        }

        public void GetMainPolyVerticalPipe()
        {
            
        }
    }
}
