using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Engine
{
    public class ThColumnRecognitionEngine : ThModelRecognitionEngine, IDisposable
    {
        public override List<ThIfcElement> Elements { get; set ; }
        public ThColumnRecognitionEngine()
        {
        }

        public void Dispose()
        {
            //ToDo
        }

        public override void Recognize()
        {
           
        }
    }
}
