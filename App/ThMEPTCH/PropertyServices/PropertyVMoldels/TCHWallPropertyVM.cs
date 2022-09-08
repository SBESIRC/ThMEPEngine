using HandyControl.Controls;
using System.ComponentModel;
using ThMEPTCH.PropertyServices.PropertyEnums;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.PropertyVMoldels
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    class TCHWallPropertyVM : PropertyVMBase
    {
        private TCHWallProperty property { get; }

        public TCHWallPropertyVM(string typeName, TCHWallProperty tchWallProperty) : base(typeName, tchWallProperty)
        {
            property = tchWallProperty;
        }

        [Browsable(true)]
        [DisplayName("材质")]
        [Editor(typeof(EnumPropertyEditor<EnumTCHWallMaterial>), typeof(PropertyEditorBase))]
        public EnumTCHWallMaterial A10_Material
        {
            get { return property.EnumMaterial; }
            set
            {
                property.EnumMaterial = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("墙高")]
        public double A11_Height
        {
            get { return property.Height; }
            set
            {
                property.Height = value;
                this.RaisePropertyChanged();
            }
        }

        [DisplayName("底高")]
        public double A12_BottomElevation
        {
            get { return property.BottomElevation; }
            set
            {
                property.BottomElevation = value;
                this.RaisePropertyChanged();
            }
        }

        public override object Clone()
        {
            var clone = new TCHWallPropertyVM(this.TypeName, this.property.Clone() as TCHWallProperty);
            clone.A01_ShowTypeName = this.A01_ShowTypeName;
            return clone;
        }
    }
}
