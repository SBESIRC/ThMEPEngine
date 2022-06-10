using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHVAC.FloorHeatingCoil.Model
{
    public class ThFloorHeatingRoom
    {
        public MPolygon RoomBoundary { get; private set; }
        public List<string> Name { get; private set; }
        public double SuggestDist { get; private set; }

        public ThFloorHeatingRoom(MPolygon room)
        {
            RoomBoundary = room;
            Name = new List<string>();
            SuggestDist = 200;
        }

        public void SetName(List<string> name)
        {
            Name.Clear();
            Name.AddRange(name);
        }

        public void SetSuggestDist(double d)
        {
            SuggestDist = d;
        }




    }
}
