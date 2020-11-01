using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection
{
    public class FanDesignDataModel
    {
        public string ID { get; set; }

        public string Name { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime LastOperationDate { get; set; }

        public string LastOperationName { get; set; }

        public string Path { get; set; }

        public string Status { get; set; }
    }
}
