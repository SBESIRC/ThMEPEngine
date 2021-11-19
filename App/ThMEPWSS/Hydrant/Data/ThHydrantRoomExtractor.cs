using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using ThMEPWSS.OutsideFrameRecognition;
using ThMEPEngineCore.GeojsonExtractor;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThHydrantRoomExtractor : ThRoomExtractor
    {
        public List<Polyline> BuildOutsideFrames()
        {
            var roomBounaries = new List<Polyline>();
            Rooms.ForEach(r =>
            {
                if (r.Boundary is Polyline polyline)
                {
                    roomBounaries.Add(polyline);
                }
                else if (r.Boundary is MPolygon mpolygon)
                {
                    roomBounaries.Add(mpolygon.Shell());
                }
            });
            return ThRecogniseOutsideFrame.GetOutsideFrame(roomBounaries);
        }
        public List<Polyline> Offset(List<Polyline> outSideFrames,double distance)
        {
            var results = new List<Polyline>();
            var bufferService = new ThNTSBufferService();
            outSideFrames.ForEach(o =>
            {
                var newPoly = bufferService.Buffer(o, distance);
                results.Add(newPoly as Polyline);
            });
            return results;
        }
    }
}
