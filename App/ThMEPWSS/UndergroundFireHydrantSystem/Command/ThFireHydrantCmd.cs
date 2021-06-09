using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Pipe.Engine;
using ThMEPEngineCore.Engine;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using Linq2Acad;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Command
{
    class ThFireHydrantCmd : IAcadCommand, IDisposable
    {
        public ThFireHydrantCmd()
        {

        }
        public void Dispose()
        {
        }

        public void Execute()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tuplePoint = ThMEPWSS.Common.Utils.SelectPoints();
                var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);
                //var engine = new ThSpatialElementRecognitionEngine();
                //engine.RecognizeMS(acadDatabase.Database, selectArea);
                ;

            }
        }
    }
}
