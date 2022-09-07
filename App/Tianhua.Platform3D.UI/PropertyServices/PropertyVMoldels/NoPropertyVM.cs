using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tianhua.Platform3D.UI.PropertyServices.PropertyVMoldels
{
    class NoPropertyVM : PropertyVMBase
    {
        public NoPropertyVM(string typeName) : base(typeName,null) 
        {
            
        }
        public override object Clone()
        {
            var clone = new NoPropertyVM(this.TypeName);
            clone.A01_ShowTypeName = this.A01_ShowTypeName;
            return clone;
        }
    }
}
