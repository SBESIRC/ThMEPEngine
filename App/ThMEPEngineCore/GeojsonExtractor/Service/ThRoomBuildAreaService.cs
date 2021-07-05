using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.GeojsonExtractor.Service
{
    public class ThRoomBuildAreaService
    {
        public static List<Entity> BuildArea(List<Polyline> roomOutlines)
        {
            var outlines = new DBObjectCollection();
            roomOutlines.ForEach(o => outlines.Add(o));
            return outlines.BuildArea().Cast<Entity>().ToList();
        }
    }
}
