using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSBlockInfo
    {
        public string BlockName { get; set; }
        public ThPDSLoadType Cat_1 { get; set; }
        public string Cat_2 { get; set; }
        public string Properties { get; set; }
    }
}
