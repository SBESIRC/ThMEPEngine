using System.ComponentModel;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.PropertyVMoldels
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    class DescendingPropertyVM : PropertyVMBase
    {
        private DescendingProperty property { get; }

        public DescendingPropertyVM(string typeName, DescendingProperty descendingProperty) : base(typeName, descendingProperty)
        {
            property = descendingProperty;
        }

        [DisplayName("结构降板厚度")]
        public double A10_Thickness
        {
            get { return property.Thickness; }
            set
            {
                property.Thickness = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("结构包围厚度")]
        public double A11_WrapThickness
        {
            get { return property.WrapThickness; }
            set
            {
                property.WrapThickness = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("建筑面层厚度")]
        public double A12_SurfaceThickness
        {
            get { return property.SurfaceThickness; }
            set
            {
                property.SurfaceThickness = value;
                this.RaisePropertyChanged();
            }
        }

        public override object Clone()
        {
            var clone = new DescendingPropertyVM(this.TypeName, this.property.Clone() as DescendingProperty);
            clone.A01_ShowTypeName = this.A01_ShowTypeName;
            return clone;
        }
    }
}
