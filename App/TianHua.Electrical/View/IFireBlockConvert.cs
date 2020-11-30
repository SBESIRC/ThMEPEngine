using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical
{
    public interface IFireBlockConvert
    {
        List<ViewFireBlockConvert> m_ListStrongBlockConver { get; set; }


        List<ViewFireBlockConvert> m_ListWeakBlockConver { get; set; }
    }
}
