using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 小母排
    /// </summary>
    public class MiniBusbar
    {
        public MiniBusbar()
        {
            Phase = ThPDSPhase.三相;
            PhaseSequence = PhaseSequence.L123;
            DemandFactor = 0.8;
            PowerFactor = 0.85;
        }

        /// <summary>
        /// 功率
        /// </summary>
        public double Power { get; set; }

        /// <summary>
        /// 相序
        /// </summary>
        public PhaseSequence PhaseSequence { get; set; }

        /// <summary>
        /// 相数
        /// </summary>
        public ThPDSPhase Phase { get; set; }

        /// <summary>
        /// 计算电流
        /// </summary>
        public double CalculateCurrent { get; set; }

        /// <summary>
        /// 级联电流额定值
        /// </summary>
        public double CascadeCurrent { get; set; }

        /// <summary>
        /// 需要系数
        /// </summary>
        public double DemandFactor { get; set; }

        /// <summary>
        /// 功率因数
        /// </summary>
        public double PowerFactor { get; set; }

        /// <summary>
        /// 坑位1：预留
        /// </summary>
        public Breaker Breaker { get; set; }

        /// <summary>
        /// 坑位2：预留
        /// </summary>
        public PDSBaseComponent ReservedComponent { get; set; }

        /// <summary>
        /// 修改功率因数
        /// </summary>
        /// <param name="powerFactor"></param>
        public void SetPowerFactor(ThPDSProjectGraphNode node ,double powerFactor)
        {
            if (node.Details.MiniBusbars.ContainsKey(this))
            {
                this.PowerFactor = powerFactor;
                PDSProject.Instance.graphData.UpdateWithMiniBusbar(node ,this, false);
            }
        }

        /// <summary>
        /// 修改需要系数
        /// </summary>
        /// <param name="powerFactor"></param>
        public void SetDemandFactor(ThPDSProjectGraphNode node ,double demandFactor)
        {
            if (node.Details.MiniBusbars.ContainsKey(this))
            {
                this.DemandFactor = demandFactor;
                PDSProject.Instance.graphData.UpdateWithMiniBusbar(node, this, false);
            }
        }
    }
}
