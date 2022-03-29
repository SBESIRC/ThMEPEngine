using System;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// ATSE 自动转换开关
    /// </summary>
    public class AutomaticTransferSwitch : TransferSwitch
    {
        public AutomaticTransferSwitch(double calculateCurrent, string polesNum)
        {
            this.ComponentType = ComponentType.ATSE;
            var ATSEComponent = ATSEConfiguration.ATSEComponentInfos.FirstOrDefault(o =>
                double.Parse(o.Amps.Split(';').Last()) > calculateCurrent
                && o.Poles.Contains(polesNum));
            if (ATSEComponent.IsNull())
            {
                throw new NotSupportedException();
            }
            Model = ATSEComponent.Model;
            PolesNum = polesNum;
            RatedCurrent = ATSEComponent.Amps.Split(';').Select(o => double.Parse(o)).First(o => o > calculateCurrent).ToString();
        }
    }
}
