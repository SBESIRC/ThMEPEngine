using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;

namespace ThMEPStructure.StructPlane.Command
{
    internal class ThStructurePlaneGeneratorCmd : ThMEPBaseCommand, IDisposable
    {
        public ThStructurePlaneGeneratorCmd()
        {
            ActionName = "结构平面图";
            CommandName = "THSMUTSC";
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void SubExecute()
        {
            throw new NotImplementedException();
        }
    }
}
