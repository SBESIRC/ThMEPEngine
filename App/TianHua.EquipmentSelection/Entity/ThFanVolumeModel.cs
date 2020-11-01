using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace TianHua.FanSelection.Model
{
    [KnownType(typeof(FireFrontModel))]
    [KnownType(typeof(FontroomNaturalModel))]
    [KnownType(typeof(FontroomWindModel))]
    [KnownType(typeof(RefugeFontRoomModel))]
    [KnownType(typeof(RefugeRoomAndCorridorModel))]
    [KnownType(typeof(StaircaseAirModel))]
    [KnownType(typeof(StaircaseNoAirModel))]
    public abstract class ThFanVolumeModel : IFanModel
    {
        public abstract string FireScenario { get; }

        public virtual double TotalVolume { get; }

        public double QueryValue { get; set; }

        public Dictionary<string, List<ThEvacuationDoor>> FrontRoomDoors2 { get; set; }
    }
}
