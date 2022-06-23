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

    /// <summary>
    /// 负荷计算信息
    /// </summary>
    public class LoadCalculationInfo
    {
        /// <summary>
        /// 相序
        /// </summary>
        public PhaseSequence PhaseSequence { get; set; }

        private double KV => PhaseSequence == PhaseSequence.L123 ? 0.38 : 0.22;

        /// <summary>
        /// 是否是双功率
        /// </summary>
        public bool IsDualPower { get; set; }

        /// <summary>
        /// 低功率-只有双功率时才起效
        /// </summary>
        public double LowPower { get; set; }

        /// <summary>
        /// 高功率-只有双功率时才起效 / 单功率的平时功率
        /// </summary>
        public double HighPower { get; set; }

        /// <summary>
        /// 需要系数-只有双功率时才起效
        /// Kx
        /// </summary>
        public double LowDemandFactor { get; set; }

        /// <summary>
        /// 需要系数/消防需要系数-只有双功率时才起效
        /// Kx
        /// </summary>
        public double HighDemandFactor { get; set; }

        /// <summary>
        /// 功率因数
        /// cosφ
        /// </summary>
        public double PowerFactor { get; set; }

        /// <summary>
        /// 有功功率
        /// Pc
        /// </summary>
        public double LowActivePower
        {
            get 
            { 
                return LowDemandFactor * LowPower;
            }
        }

        /// <summary>
        /// 有功功率
        /// Pc
        /// </summary>
        public double HighActivePower
        {
            get
            {
                return HighDemandFactor * HighPower;
            }
        }

        private double φ => Math.Acos(PowerFactor);

        /// <summary>
        /// 无功功率
        /// Qc
        /// </summary>
        public double LowReactivePower
        {
            get
            {
                return Math.Tan(φ) * LowActivePower;
            }
        }

        /// <summary>
        /// 无功功率
        /// Qc
        /// </summary>
        public double HighReactivePower
        {
            get
            {
                return Math.Tan(φ) * HighActivePower;
            }
        }

        /// <summary>
        /// 视在功率
        /// Sc
        /// </summary>
        public double LowApparentPower
        {
            get
            {
                return LowActivePower / PowerFactor;
            }
        }

        /// <summary>
        /// 视在功率
        /// Sc
        /// </summary>
        public double HighApparentPower
        {
            get
            {
                return HighActivePower / PowerFactor;
            }
        }

        /// <summary>
        /// 计算电流
        /// Ic
        /// </summary>
        public double LowCalculateCurrent
        {
            get
            {
                return Math.Round(LowPower * LowDemandFactor / (PowerFactor * Math.Sqrt(3) * KV), 2);
            }
        }

        /// <summary>
        /// 计算电流
        /// Ic
        /// </summary>
        public double HighCalculateCurrent
        {
            get
            {
                return Math.Round(HighPower * HighDemandFactor / (PowerFactor * Math.Sqrt(3) * KV), 2);
            }
        }
    }
}
