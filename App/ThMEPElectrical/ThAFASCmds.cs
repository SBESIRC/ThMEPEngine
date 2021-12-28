using AcHelper;
using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmArea.Command;
using ThMEPElectrical.FireAlarmFixLayout.Command;

#if (ACAD2016 || ACAD2018)
using ThMEPElectrical.FireAlarmDistance.Command;
#endif

namespace ThMEPElectrical
{
    public class ThAFASCmds
    {
        [CommandMethod("TIANHUACAD", "THFASmoke", CommandFlags.Session)]
        public void THFASmoke()
        {
            using (var cmd = new ThAFASSmokeCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFADisplay", CommandFlags.Session)]
        public void THFADisplay()
        {
            using (var cmd = new ThAFASDisplayDeviceLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAMonitor", CommandFlags.Session)]
        public void THFAMonitor()
        {
            using (var cmd = new ThAFASFireProofMonitorLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFATel", CommandFlags.Session)]
        public void THFATel()
        {
            using (var cmd = new ThAFASFireTelLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFAGas", CommandFlags.Session)]
        public void THFAGas()
        {
            using (var cmd = new ThAFASGasCmd())
            {
                cmd.Execute();
            }

        }

        [CommandMethod("TIANHUACAD", "THFABroadcast", CommandFlags.Session)]
        public void THFABroadcast()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASBroadcastCmd())
            {
                cmd.Execute();
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

        [CommandMethod("TIANHUACAD", "THFAManualAlarm", CommandFlags.Session)]
        public void THFAManualAlarm()
        {
#if (ACAD2016 || ACAD2018)
            using (var cmd = new ThAFASManualAlarmCmd())
            {
                cmd.Execute();
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }

       
        [CommandMethod("TIANHUACAD", "THFACleanAllBlk", CommandFlags.Modal)]
        public void THFACleanAllBlk()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var selectPts = ThAFASUtils.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                var transformer = ThAFASUtils.GetTransformer(selectPts);

                ////////导入所有块，图层信息
                var extractBlkList = ThFaCommon.BlkNameList;
                ThFireAlarmInsertBlk.PrepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                ////////清除所选的块
                var cleanBlkList = extractBlkList;
                var previousEquipmentData = new ThAFASBusinessDataSetFactory()
                {
                    BlkNameList = cleanBlkList,
                };
                previousEquipmentData.SetTransformer(transformer);
                var localEquipmentData = previousEquipmentData.Create(acadDatabase.Database, selectPts);
                var cleanEquipment = localEquipmentData.Container;
                ThAFASUtils.CleanPreviousEquipment(cleanEquipment);
            }
        }


    }
}
