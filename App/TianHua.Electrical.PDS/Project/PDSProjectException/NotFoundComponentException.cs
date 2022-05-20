using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.PDSProjectException
{
    public class NotFoundComponentException : Exception
    {
        /// <summary>
        /// 元器件选型库未找到合适元器件异常
        /// </summary>
        public NotFoundComponentException(string errorMsg) : base(errorMsg)
        {
        }
    }
}
