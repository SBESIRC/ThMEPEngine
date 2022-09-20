using System.ComponentModel;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.PropertyVMoldels
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    class RailingPropertyVM : PropertyVMBase
    {
        private RailingProperty property { get; }

        public RailingPropertyVM(string typeName, RailingProperty railingProperty) : base(typeName, railingProperty)
        {
            property = railingProperty;
        }

        [DisplayName("底高")]
        public double A10_BottomHeight
        {
            get { return property.BottomElevation; }
            set
            {
                property.BottomElevation = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("栏杆高")]
        public double A11_Height
        {
            get { return property.Height; }
            set
            {
                property.Height = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("厚度")]
        public double A12_Thickness
        {
            get { return property.Thickness; }
            set
            {
                property.Thickness = value;
                this.RaisePropertyChanged();
            }
        }

        public override object Clone()
        {
            var clone = new RailingPropertyVM(this.TypeName, this.property.Clone() as RailingProperty);
            clone.A01_ShowTypeName = this.A01_ShowTypeName;
            return clone;
        }
    }
}
