using AcHelper;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using ThMEPEngineCore.Command;
using ThMEPWSS.UndergroundSpraySystem.Method;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundSpraySystem.Service;
using ThMEPWSS.UndergroundSpraySystem.ViewModel;

namespace ThMEPWSS.UndergroundSpraySystem.Command
{
    public class ThSpraySystemCmd : ThMEPBaseCommand, IDisposable
    {
        readonly SprayVM _UiConfigs;
        public ThSpraySystemCmd(SprayVM viewModel)
        {
            _UiConfigs = viewModel;
            CommandName = "THDXPLXTT";
            ActionName = "生成";
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    if(_UiConfigs.IsAlarmValveSys)
                    {
                        CreateAlarmValveSystem(currentDb);
                    }
                    else
                    {
                        CreateSpraySystem(currentDb);
                    }
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }

        public override void AfterExecute()
        {
            base.AfterExecute();
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
        }

        public void CreateAlarmValveSystem(AcadDatabase curDb)
        {
            var startPts = SpraySys.GetStartPts();
            if (startPts.Count == 0) return;
            var rstGetInsertPt = SpraySys.GetInsertPoint(out Point3d insertPt);
            if (!rstGetInsertPt) return;
            var selectArea = _UiConfigs.SelectedArea;
            var sprayOut = new SprayOut(insertPt);//输出参数
            var sprayIn = new SprayIn(_UiConfigs, startPts);//输入参数
            var spraySystem = new SpraySystem();//系统参数
            AlarmValveSystem.GetInput(curDb, sprayIn, selectArea);
            AlarmValveSystem.Processing(curDb, sprayIn, spraySystem, sprayOut);
            AlarmValveSystem.GetOutput(sprayIn, spraySystem, sprayOut);
            sprayOut.Draw(curDb);
        }

        public void CreateSpraySystem(AcadDatabase curDb)
        {
            var rstPipeMarkPt = SpraySys.GetPipeMarkPt(curDb, out Point3d startPt);
            if (!rstPipeMarkPt) return;
            var selectArea = _UiConfigs.SelectedArea;

            var rstGetInsertPt = SpraySys.GetInsertPoint(out Point3d insertPt);
            if (!rstGetInsertPt) return;
            var sprayOut = new SprayOut(insertPt);//输出参数
            var sprayIn = new SprayIn(_UiConfigs);//输入参数
            var spraySystem = new SpraySystem();//系统参数

            var alarmValve = new AlarmValveTCH();
            var alarmPts = alarmValve.Extract(curDb.Database, selectArea);
            var sprayType = CheckSprayType.IsAcrossFloor(sprayIn, alarmPts);

            var rstGetInput = SpraySysWithAcrossFloor.GetInput(curDb, sprayIn, selectArea, startPt);//提取输入参数
            if (!rstGetInput) return;
            if (sprayType == 0)
            {
                CmdWithoutAcrossLayers(curDb, sprayIn, spraySystem, sprayOut);
            }
            else
            {
                CmdWithAcrossLayers(curDb, sprayIn, spraySystem, sprayOut);
            }

            sprayOut.Draw(curDb);
        }

        /// <summary>
        /// 不存在跨楼层报警阀间
        /// </summary>
        public static void CmdWithoutAcrossLayers(AcadDatabase curDb, SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            var loopFlag = SpraySys.Processing(curDb, sprayIn, spraySystem);

            if (loopFlag == 1)
            {
                SpraySys.GetOutput(sprayIn, spraySystem, sprayOut);
            }
            else if(loopFlag == 2)
            {
                SpraySys.GetOutput2(sprayIn, spraySystem, sprayOut);
            }
            else
            {
                SpraySys.GetOutput3(sprayIn, spraySystem, sprayOut);
            }
        }

        /// <summary>
        /// 存在跨楼层报警阀间
        /// </summary>
        public static void CmdWithAcrossLayers(AcadDatabase curDb, SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            var rstMainLoopsInOtherFloor = SpraySysWithAcrossFloor.AcrossFloorTypeCheck(curDb, sprayIn, spraySystem);

            if(rstMainLoopsInOtherFloor)//存在跨楼层主环
            {
                //环管跨楼层时，1.dfs起点所在层；2.dfs其它层；3.将其它层的连接到起点所在层
                SpraySysWithMainLoopAcrossFloor.Processing(curDb, sprayIn, spraySystem);
                SpraySysWithMainLoopAcrossFloor.GetOutput(sprayIn, spraySystem, sprayOut);
                var acrossMainLoop = spraySystem.MainLoopsInOtherFloor[0];
                SpraySysWithMainLoopAcrossFloor.ProcessingInOtherFloor(curDb, acrossMainLoop, sprayIn, spraySystem);
                SpraySysWithMainLoopAcrossFloor.GetOutputInOtherFloor(sprayIn, spraySystem, sprayOut);
                if(spraySystem.MainLoopsInOtherFloor.Count> 1)
                {
                    acrossMainLoop = spraySystem.MainLoopsInOtherFloor[1];
                    SpraySysWithMainLoopAcrossFloor.ProcessingInOtherFloor(curDb, acrossMainLoop, sprayIn, spraySystem);
                    SpraySysWithMainLoopAcrossFloor.GetOutputInOtherFloor(sprayIn, spraySystem, sprayOut, 2);
                }
            }
            else
            {
                //暂时用楼层数作为判断条件
                if(sprayIn.FloorRectDic.Count > 3)
                {
                    SpraySysWithAcrossFloor.Processing2(curDb, sprayIn, spraySystem);
                    SpraySysWithAcrossFloor.GetOutput2(sprayIn, spraySystem, sprayOut);
                }
                else
                {
                    SpraySysWithAcrossFloor.Processing(curDb, sprayIn, spraySystem);
                    SpraySysWithAcrossFloor.GetOutput(sprayIn, spraySystem, sprayOut);
                }
            }
        }
    }
}
