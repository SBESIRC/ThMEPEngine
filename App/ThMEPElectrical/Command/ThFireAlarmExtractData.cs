using System;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using System.Collections.Generic;
using ThMEPEngineCore.IO;
using System.IO;
using AcHelper;

using ThMEPEngineCore.Command;
using ThMEPElectrical.FireAlarmFixLayout.Data;

namespace ThMEPElectrical.Command
{
    public class ThFireAlarmExtractData : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
        }
        public ThFireAlarmExtractData()
        {
            CommandName = "ThFireAlarmExtractData";
            ActionName = "测试数据";
        }

        public override void SubExecute()
        {
          
        }
    }
}
