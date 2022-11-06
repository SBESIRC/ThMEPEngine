using System.ComponentModel;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.PropertyVMoldels
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    class TCHDoorPropertyVM : PropertyVMBase
    {
        private TCHDoorProperty property { get; }

        public TCHDoorPropertyVM(string typeName, TCHDoorProperty tchDoorProperty) : base(typeName, tchDoorProperty)
        {
            property = tchDoorProperty;
        }


        [DisplayName("是否门窗统计")]
        public bool A10_Statistics
        {
            get { return property.Statistics; }
            set
            {
                property.Statistics = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("底高")]
        public double A11_BottomHeight
        {
            get { return property.BottomHeight; }
            set
            {
                property.BottomHeight = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("编号前缀")]
        public string A12_NumberPrefix
        {
            get { return property.NumberPrefix; }
            set
            {
                property.NumberPrefix = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("编号后缀")]
        public string A13_NumberPostfix
        {
            get { return property.NumberPostfix; }
            set
            {
                property.NumberPostfix = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("安全出口")]
        public bool A14_Entrance
        {
            get { return property.Entrance; }
            set
            {
                property.Entrance = value;
                this.RaisePropertyChanged();
            }
        }

        public override object Clone()
        {
            var clone = new TCHDoorPropertyVM(this.TypeName, this.property.Clone() as TCHDoorProperty);
            clone.A01_ShowTypeName = this.A01_ShowTypeName;
            return clone;
        }
    }
}
