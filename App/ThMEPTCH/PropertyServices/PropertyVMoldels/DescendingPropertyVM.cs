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
        public double A10_StructureThickness
        {
            get { return property.StructureThickness; }
            set
            {
                property.StructureThickness = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("降板面层厚度")]
        public double A11_SurfaceThickness
        {
            get { return property.SurfaceThickness; }
            set
            {
                property.SurfaceThickness = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("结构包围厚度")]
        public double A12_StructureWrapThickness
        {
            get { return property.StructureWrapThickness; }
            set
            {
                property.StructureWrapThickness = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("包围面层厚度")]
        public double A13_WrapSurfaceThickness
        {
            get { return property.WrapSurfaceThickness; }
            set
            {
                property.WrapSurfaceThickness = value;
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
