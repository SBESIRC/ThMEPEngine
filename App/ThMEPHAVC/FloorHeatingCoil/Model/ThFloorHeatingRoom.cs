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
        public Polyline RoomBoundary { get; private set; }
        public Polyline OriginalBoundary { get; private set; }
        public List<string> Name { get; private set; }
        public double SuggestDist { get; private set; }

        public ThFloorHeatingRoom(Polyline room)
        {
            RoomBoundary = room;
            Name = new List<string>();
            SuggestDist = ThFloorHeatingCommon.DefaultValue_SuggestDist;
            //SuggestDist = 300;
        }

        public void SetName(List<string> name)
        {
            Name.Clear();
            Name.AddRange(name);
        }

        public void SetSuggestDist(double d)
        {
            if (SuggestDist != 0 || SuggestDist != -1)
            {
                SuggestDist = d;
            }
        }

        public void SetOriginalBoundary(Polyline originalBoundary)
        {
            OriginalBoundary = originalBoundary;
        }


    }
}
