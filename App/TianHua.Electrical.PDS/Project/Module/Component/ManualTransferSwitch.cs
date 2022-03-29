using System;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// MTSE 手动转换开关
    /// </summary>
    public class ManualTransferSwitch : TransferSwitch
    {
        public ManualTransferSwitch(double calculateCurrent, string polesNum)
        {
            this.ComponentType = ComponentType.MTSE;
            var MTSEComponent = MTSEConfiguration.MTSEComponentInfos.FirstOrDefault(o =>
                double.Parse(o.Amps.Split(';').Last()) > calculateCurrent
                && o.Poles.Contains(polesNum));
            if (MTSEComponent.IsNull())
            {
                throw new NotSupportedException();
            }
            Model = MTSEComponent.Model;
            PolesNum = polesNum;
            RatedCurrent = MTSEComponent.Amps.Split(';').Select(o => double.Parse(o)).First(o => o > calculateCurrent).ToString();
        }
    }
}
