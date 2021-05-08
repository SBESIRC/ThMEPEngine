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
            int FLOOR_HEIGHT = 2900;  //楼层线间距 mm
            //立管对应的最低、最高层
            int[] LOWESTSTOREY = { 1, 3, 13, 24 };
            int[] HIGHESTSTOREY = { 1, 12, 23, 31 };
            int[] FLUSHFAUCET = { 1, 6, 11, 21, 26, 31 }; //冲洗龙头层
            int[] PRESSUREREDUCINGVALVE = { 12, 23, 31 }; //无减压阀层
            var LAYINGMETHOD = (int)LayingMethod.Piercing;  //敷设方式为穿梁

            



            using (var acadDatabase = AcadDatabase.Active())
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

                double[] PipeDiameter = {15,10,5};
                //int FloorNumber = 5;
                int[] loweststorey = {0,13,21};
                int[] higheststorey = {12,20,32};
                double[] offset_X = { 1500,1000,500};
                //var PipeList = new ThWSSDPipeRun(PipeDiameter, loweststorey, higheststorey);

                //var PipeLine = PipeList.CreatePipeLine();


                //acadDatabase.CurrentSpace.Add(PipeLine);
                var PipeSystem = new List<ThWSuplySystemDiagram>();
                for(int i = 0; i< loweststorey.Length; i++)
                {
                    PipeSystem.Add(new ThWSuplySystemDiagram(loweststorey[i], higheststorey[i], offset_X[i]));
                    for (int j = 0; j < PipeDiameter.Length; j++)
                    {
                        PipeSystem[i].PipeRuns.Add(new ThWSSDPipeRun(PipeDiameter[i], loweststorey[i], higheststorey[i]));
                    }
                }
                for (int i = 0; i < PipeDiameter.Length; i++)
                {
                    var PipeLine = PipeSystem[i].CreatePipeLine();
                    acadDatabase.CurrentSpace.Add(PipeLine);
                }
                //var PipeList = new ThWSuplySystemDiagram(loweststorey[0], higheststorey[0],offset_X[0]);
                //for(int i = 0; i< PipeDiameter.Length; i++)
                //{
                //    PipeList.PipeRuns.Add(new ThWSSDPipeRun(PipeDiameter[i], loweststorey[i], higheststorey[i]));
                //}
                //var PipeLine = PipeList.CreatePipeLine();

                    //for (int i = 0; i < PipeDiameter.Length; i++)
                    //{
                    //    var PipeLine = PipeList.PipeRuns[i].CreatePipeLine();
                    //    acadDatabase.CurrentSpace.Add(PipeLine);
                    //}




            }
            
            
            //throw new NotImplementedException();
        }
    }
}
