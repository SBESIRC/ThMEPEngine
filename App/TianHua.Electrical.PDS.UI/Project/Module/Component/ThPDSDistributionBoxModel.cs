using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSDistributionBoxModel : NotifyPropertyChangedBase
    {
        ThPDSProjectGraphNode vertice;

        public ThPDSDistributionBoxModel(ThPDSProjectGraphNode vertice)
        {
            this.vertice = vertice;
        }
        [DisplayName("配电箱编号")]
        public string ID
        {
            get => vertice.Load.ID.LoadID;
            //set => vertice.Load.ID.LoadID = value;
        }
        [DisplayName("功率")]
        public double InstallCapacity
        {
            get => vertice.Details.LowPower;
            set => vertice.Details.LowPower = value;
        }
        [DisplayName("相数")]
        public Model.ThPDSPhase Phase
        {
            get => vertice.Load.Phase;
            set => vertice.Load.Phase = value;
        }
        string lastDemandFactorValue = "1.00";
        [DisplayName("需要系数")]
        [Range(typeof(double), "0.01", "1.00")]
        public string DemandFactor
        {
            get
            {
                var v = vertice.Load.DemandFactor;
                if (v > 0 && v <= 1)
                {
                    var s = $"{v:F2}";
                    if (s == "0.00") s = "0.01";
                    lastDemandFactorValue = s;
                    return s;
                }
                vertice.Load.DemandFactor = double.Parse(lastDemandFactorValue);
                return lastDemandFactorValue;
            }
            set
            {
                if (double.TryParse(value, out var v))
                {
                    if (v > 0 && v <= 1)
                    {
                        vertice.Load.DemandFactor = v;
                        lastDemandFactorValue = $"{v:F2}";
                    }
                }
                OnPropertyChanged(nameof(DemandFactor));
            }
        }
        string lastPowerFactorValue = "1.00";
        [DisplayName("功率因数")]
        [Range(typeof(double), "0.01", "1.00")]
        public string PowerFactor
        {
            get
            {
                var v = vertice.Load.PowerFactor;
                if (v > 0 && v <= 1)
                {
                    var s = $"{v:F2}";
                    if (s == "0.00") s = "0.01";
                    lastPowerFactorValue = s;
                    return s;
                }
                vertice.Load.PowerFactor = double.Parse(lastPowerFactorValue);
                return lastPowerFactorValue;
            }
            set
            {
                if (double.TryParse(value, out var v))
                {
                    if (v > 0 && v <= 1)
                    {
                        vertice.Load.PowerFactor = v;
                        lastPowerFactorValue = $"{v:F2}";
                    }
                }
                OnPropertyChanged(nameof(PowerFactor));
            }
        }
        [DisplayName("计算电流")]
        public double CalculateCurrent
        {
            get => vertice.Load.CalculateCurrent;
        }
        [DisplayName("用途描述")]
        public string Description
        {
            get => vertice.Load.ID.Description;
            set
            {
                vertice.Load.ID.Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }
        [DisplayName("箱体尺寸")]
        public string OverallDimensions
        {
            get => "";
        }
        [DisplayName("安装方式")]
        public string NstalledMethod
        {
            get => "";
        }
    }
}
