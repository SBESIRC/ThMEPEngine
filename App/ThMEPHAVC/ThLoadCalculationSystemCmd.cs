﻿using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.LoadCalculation.Command;

namespace ThMEPHVAC
{
    public class ThLoadCalculationSystemCmd
    {
        [CommandMethod("TIANHUACAD", "THFJBHCR", CommandFlags.Modal)]
        //天华暖通房间块布置
        public void THFJBHCR()
        {
            using (var cmd = new ThInsertRoomFunctionCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFJGN", CommandFlags.Modal)]
        //天华提取房间功能
        public void THFJGN()
        {
            using (var cmd = new ThRoomFunctionExtractCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THSCFH", CommandFlags.Modal)]
        //天华生成负荷通风计算表
        public void THSCFH()
        {
            using (var cmd = new ThCreatLoadCalculationTableCmd())
            {
                cmd.Execute();
            }
        }
    }
}
