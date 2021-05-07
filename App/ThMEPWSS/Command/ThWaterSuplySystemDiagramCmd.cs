using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Command
{
    class ThWaterSuplySystemDiagramCmd : IAcadCommand, IDisposable
    {

        public void Dispose()
        {
        }

        public void Execute()
        {
            using(var acadDatabase = AcadDatabase.Active())
            {
                var LineList = new List<Line>();
                int FloorNumbers = 32;
                for (int i = 0; i < FloorNumbers; i++)
                {
                    var storey = new ThWSSDStorey(i);
                    var line1 = storey.CreateLine();
                    LineList.Add(line1);
                }
                for(int i = 0; i < FloorNumbers; i++)
                {
                    acadDatabase.CurrentSpace.Add(LineList[i]);
                }
                
            }
            
            
            //throw new NotImplementedException();
        }
    }
}
