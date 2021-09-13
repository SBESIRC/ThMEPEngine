using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Data;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public abstract class ThSprinklerChecker
    {
        public ThMEPDataSet DataSet { get; set; }

        public abstract void Check();

        public abstract void Present();
    }
}
