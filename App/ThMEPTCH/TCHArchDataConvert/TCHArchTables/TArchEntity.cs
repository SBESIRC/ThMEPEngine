using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    public abstract class TArchEntity
    {
        public ulong Id { get; set; }
        public string StyleID { get; set; }
        public string LineType { get; set; }
        public string Layer { get; set; }
        public virtual bool IsValid() => true;
    }
}
