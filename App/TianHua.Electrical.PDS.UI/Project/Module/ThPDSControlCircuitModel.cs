using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Project.Module
{
    public class ThPDSControlCircuitModel
    {
        private readonly SecondaryCircuit _sc;

        public ThPDSControlCircuitModel(SecondaryCircuit sc)
        {
            _sc = sc;
        }

        public string CircuitID => _sc.CircuitID;
        public string CircuitDescription => _sc.CircuitDescription;
    }
}
