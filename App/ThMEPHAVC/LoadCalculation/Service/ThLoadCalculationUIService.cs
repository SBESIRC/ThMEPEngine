using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.LoadCalculation.Model;

namespace ThMEPHVAC.LoadCalculation.Service
{
    public class ThLoadCalculationUIService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThLoadCalculationUIService instance = new ThLoadCalculationUIService() { };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThLoadCalculationUIService() { }
        internal ThLoadCalculationUIService() { }
        public static ThLoadCalculationUIService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public LoadCalculationParameterFromUI Parameter = new LoadCalculationParameterFromUI();
    }
}
