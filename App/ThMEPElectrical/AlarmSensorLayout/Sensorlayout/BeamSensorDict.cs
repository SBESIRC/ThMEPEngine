using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.AlarmSensorLayout.Sensorlayout
{
    public class BeamSensorDict
    {
        List<LineSegment> hLine { get; set; }//横线
        List<LineSegment> vLine { get; set; }//竖线
    }
}
