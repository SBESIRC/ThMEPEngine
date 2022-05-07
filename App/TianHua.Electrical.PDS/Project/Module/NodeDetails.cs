using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 节点附加信息
    /// </summary>
    [Serializable]
    public class NodeDetails
    {
        //public CircuitFormInType CircuitFormType { get; set; }
        public PDSBaseInCircuit CircuitFormType { get; set; }
        public PDSProjectErrorType ErrorType { get; set; }

        /// <summary>
        /// 是否已统计
        /// </summary>
        public bool IsStatistical { get; set; }


        /// <summary>
        /// 是否是双功率
        /// </summary>
        public bool IsDualPower { get; set; }
        
        public double LowPower { get; set; }
        public double HighPower { get; set; }

        /// <summary>
        /// 级联电流额定值
        /// </summary>
        public double CascadeCurrent { get; set; }

        public bool IsOnlyLoad { get; set; }
        public bool FirePowerMonitoring { get; set; }
        public bool ElectricalFireMonitoring { get; set; }

        /// <summary>
        /// 允许断路器切换成隔离开关
        /// </summary>
        public bool AllowBreakerSwitch { get; set; }

        /// <summary>
        /// 相序
        /// </summary>
        public PhaseSequence PhaseSequence { get; set; }

        public SurgeProtectionDeviceType SurgeProtection { get; set; }

        /// <summary>
        /// 箱体尺寸
        /// </summary>
        public BoxSize BoxSize { get; set; }

        /// <summary>
        /// 配电箱安装方式
        /// </summary>
        public BoxInstallationType BoxInstallationType { get; set; }

        /// <summary>
        /// 小母排
        /// </summary>
        public Dictionary<MiniBusbar,List<ThPDSProjectGraphEdge>> MiniBusbars { get; set;}

        /// <summary>
        /// 控制回路 
        /// </summary>
        public Dictionary<SecondaryCircuit, List<ThPDSProjectGraphEdge>> SecondaryCircuits { get; set; }

        public NodeDetails()
        {
            CircuitFormType = new OneWayInCircuit();
            CascadeCurrent = 0;
            PhaseSequence = PhaseSequence.L123;
            SurgeProtection = SurgeProtectionDeviceType.None;
            BoxSize = BoxSize.Non_Standard;
            AllowBreakerSwitch = false;
            MiniBusbars = new Dictionary<MiniBusbar, List<ThPDSProjectGraphEdge>>();
            SecondaryCircuits = new Dictionary<SecondaryCircuit, List<ThPDSProjectGraphEdge>>();
            if (Convert.ToUInt32(BoxSize) > 6)
            {
                BoxInstallationType = BoxInstallationType.落地安装;
            }
            else
            {
                BoxInstallationType = BoxInstallationType.挂墙明装;
            }
        }
    }
}
