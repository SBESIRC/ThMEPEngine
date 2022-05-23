using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Model;

namespace ThMEPWSS.Service
{
    public class ThWSSUIService
    { 
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThWSSUIService instance = new ThWSSUIService() {  };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThWSSUIService() { }
        internal ThWSSUIService() { }
        public static ThWSSUIService Instance { get { return instance; } }
        //-------------SINGLETON-----------------


        public ThWSSParameter Parameter = new ThWSSParameter();

        public ThPSPMParameter PSPMParameter = new ThPSPMParameter();
    }
}
