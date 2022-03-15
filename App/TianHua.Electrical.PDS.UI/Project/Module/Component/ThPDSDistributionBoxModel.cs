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
        [DisplayName("需要系数")]
        public double DemandFactor
        {
            get => vertice.Load.DemandFactor;
            set
            {
                vertice.Load.DemandFactor = value;
                OnPropertyChanged(nameof(DemandFactor));
            }
        }
        [DisplayName("功率因数")]
        public double PowerFactor
        {
            get => vertice.Load.PowerFactor;
            set
            {
                vertice.Load.PowerFactor = value;
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
