using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model.WireCircuit
{
    public static class ThWireCircuitConfig
    {
        /// <summary>
        /// 横线
        /// </summary>
        public static List<ThWireCircuit> HorizontalWireCircuits = new List<ThWireCircuit>()
        {
            new ThBroadcastWireCircuit(),
            new ThFireIndicatingWireCircuit(),
            new ThFireTelephoneWireCircuit(),
            new ThAlarmControlWireCircuit(),
            new ThDC24VPowerWireCircuit(),
            new ThTextWireCircuit()
        };

        /// <summary>
        ///竖线
        /// </summary>
        public static List<ThWireCircuit> VerticalWireCircuits = new List<ThWireCircuit>()
        {
            new ThFormHeaderVerticalWireCircuit(),
        };
    }
}
