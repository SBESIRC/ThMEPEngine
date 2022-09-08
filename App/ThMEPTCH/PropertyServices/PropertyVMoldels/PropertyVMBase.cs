using System;
using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.PropertyServices.PropertyVMoldels
{
    public abstract class PropertyVMBase : NotifyPropertyChangedBase,ICloneable
    {
        [Browsable(false)]
        public string TypeName { get; }
        [Browsable(false)]
        public PropertyBase Property { get; protected set; }
        private string showTypeName { get; set; }
        /*
         用数字开头是PropertyGrid字段排序问题
         */
        [ReadOnly(true)]
        [Browsable(true)]
        [DisplayName("构件名")]
        public string A01_ShowTypeName
        {
            get { return showTypeName; }
            set
            {
                showTypeName = value;
                this.RaisePropertyChanged();
            }
        }
        public PropertyVMBase(string typeName, PropertyBase property) 
        {
            TypeName = typeName;
            Property = property;
            A01_ShowTypeName = string.Format("{0}({1})", TypeName, 1);
        }
        [Browsable(false)]
        public abstract object Clone();
    }
}
