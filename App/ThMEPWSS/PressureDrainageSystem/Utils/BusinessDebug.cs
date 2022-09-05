using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.PressureDrainageSystem.Model;
using ThMEPWSS.Uitl;
using ThMEPWSS.WaterSupplyPipeSystem;
using static ThMEPWSS.PressureDrainageSystem.DebugTools;

namespace ThMEPWSS.PressureDrainageSystem.Utils
{
    public static class BusinessDebug
    {
        public static string ShowPipeLineSystemUnits(List<PipeLineSystemUnitClass> units)
        {
            var str = "";
            int count = 0;
            foreach (var unit in units)
            {
                count++;
                var unitstr = "PipeLineSystemUnitClass单元" + count.ToString() + ":{";
                unitstr += ShowPipeLineSystemUnit(unit);
                unitstr += "}";
                str += unitstr;
            }
            return str;
        }
        public static string ShowGroupHorizontals(List<List<Horizontal>> groupLines)
        {
            var str = "";
            for (int i = 0; i < groupLines.Count; i++)
            {
                str += "组" + i.ToString() + ":";
                str += ShowHorizontals(groupLines[i]);
            }
            return str;
        }
        public static string ShowPipeLineSystemUnit(PipeLineSystemUnitClass unit)
        {
            var str = "PipeLineSystemUnitClass:{";
            str += "PipeLineSystemUnitClass_List<Point3d>FloorLocPoints" + ":{" + ShowPoints(unit.FloorLocPoints) + "}";
            str += "PipeLineSystemUnitClass_int_LayerNumbers" + ":{" + unit.LayerNumbers.ToString() + "}";
            str += "PipeLineSystemUnitClass_List<PipeLineUnit>PipeLineUnits" + ":{" + ShowPipeLineUnits(unit.PipeLineUnits) + "}";
            str += "PipeLineSystemUnitClass_List<int[,]>CrossLayerConnectedArrs" + ":{" + ShowArrays(unit.CrossLayerConnectedArrs) + "}";
            str += "PipeLineSystemUnitClass_int_DrainageMode" + ":{" + unit.DrainageMode.ToString() + "}";
            str += "PipeLineSystemUnitClass_int_DrainWellPipeIndex" + ":{" + unit.DrainWellPipeIndex.ToString() + "}";
            str += "PipeLineSystemUnitClass_DrainWellClass_DrainWell" + ":{" + ShowDrainWellClass(unit.DrainWell) + "}";
            str += "PipeLineSystemUnitClass_List<int>verticalPipeId" + ":{" + ShowListInt(unit.verticalPipeId) + "}";
            str += "PipeLineSystemUnitClass_List<Point3d>SameUnitsStartPt " + ":{" + ShowPoints(unit.SameUnitsStartPt) + "}";
            str += "PipeLineSystemUnitClass_string_InitialLayer " + ":{" + unit.InitialLayer + "}";
            return str;
        }
        public static string ShowListInt(List<int> nums)
        {
            var str = "";
            foreach (var num in nums)
                str += num.ToString() + ",";
            if (str.Length > 0)
                str.Remove(str.Length - 1);
            return str;
        }
        public static string ShowDrainWellClass(DrainWellClass drainWell)
        {
            var str = "DrainWellClass:{";
            if (drainWell != null)
            {
                str += "DrainWellClass_Polyline_Extents" + ":{" + ShowPolyline(drainWell.Extents) + "}";
                str += "DrainWellClass_string_Label" + ":{" + drainWell.Label + "}";
                str += "DrainWellClass_string_WellTypeName" + ":{" + drainWell.WellTypeName + "}";
                str += "}";
            }
            return str;
        }
        public static string ShowArrays(List<int[,]> arrays)
        {
            var str = "";
            int count = 0;
            foreach (var unit in arrays)
            {
                count++;
                var unitstr = "<int[,]>单元" + count.ToString() + ":{";
                unitstr += ShowArray(unit);
                unitstr += "}";      
                str += unitstr;
            }
            return str;
        }
        public static string ShowArray(int[,] array)
        {
            var str = "int[,]:{";
            var count_a = array.GetLongLength(0);
            var count_b = array.GetLongLength(1);
            str += count_a.ToString() + "," + count_b.ToString() + ":";
            foreach (var e in array.Cast<int>())
                str += e.ToString() + ",";
            if (array.Cast<int>().Count() > 0)
                str.Remove(str.Length - 1);
            str += "}";
            return str;
        }
        public static string ShowPipeLineUnit(PipeLineUnit unit)
        {
            var str = "PipeLineUnit:{";
            if (unit != null)
            {
                str += "PipeLineUnit_int_DrainMode" + ":{" + unit.DrainMode.ToString() + "}";
                str += "PipeLineUnit_int_DrainWellPipeIndex" + ":{" + unit.DrainWellPipeIndex.ToString() + "}";
                str += "PipeLineUnit_int[,]_VertPipeConnectedArr" + ":{" + ShowArray(unit.VertPipeConnectedArr) + "}";
                str += "PipeLineUnit_List<Polyline>WrapPipes" + ":{" + ShowPolylineList(unit.WrapPipes) + "}";
                str += "PipeLineUnit_List<Horizontal>OriginalHorizontalPipes" + ":{" + ShowHorizontals(unit.OriginalHorizontalPipes) + "}";
                str += "PipeLineUnit_List<Horizontal>HorizontalPipes" + ":{" + ShowHorizontals(unit.HorizontalPipes) + "}";
                str += "PipeLineUnit_List<VerticalPipeClass>VerticalPipes" + ":{" + ShowVerticalPipeClasses(unit.VerticalPipes) + "}";
            }
            str += "}";
            return str;
        }
        public static string ShowVerticalPipeClass(VerticalPipeClass pipe)
        {
            var str = "VerticalPipeClass:{";
            if (pipe != null)
            {
                str += "VerticalPipeClass_Circle_Circle" + ":{" + ShowCircle(pipe.Circle) + "}";
                str += "VerticalPipeClass_string_Identifier" + ":{" + pipe.Identifier + "}";
                str += "VerticalPipeClass_string_Label" + ":{" + pipe.Label + "}";
                str += "VerticalPipeClass_bool_IsPumpVerticalPipe" + ":{" + (pipe.IsPumpVerticalPipe ? "1" : "0") + "}";
                str += "VerticalPipeClass_SubmergedPumpClass_AppendedSubmergedPump" + ":{" + ShowSubmergedPumpClass(pipe.AppendedSubmergedPump) + "}";
                str += "VerticalPipeClass_bool_IsNexttoDainWell" + ":{" + (pipe.IsNexttoDainWell ? "1" : "0") + "}";
                str += "VerticalPipeClass_DrainWellClass_AppendedDrainWell" + ":{" + ShowDrainWellClass(pipe.AppendedDrainWell) + "}";
                str += "VerticalPipeClass_int_Id" + ":{" + pipe.Id.ToString() + "}";
                str += "VerticalPipeClass_bool_isUnitStart" + ":{" + (pipe.isUnitStart ? "1" : "0") + "}";
                str += "VerticalPipeClass_bool_HasChildPipe" + ":{" + (pipe.HasChildPipe ? "1" : "0") + "}";
                str += "VerticalPipeClass_List<string>_SameTypeIdentifiers" + ":{" + ShowListString(pipe.SameTypeIdentifiers) + "}";
                str += "VerticalPipeClass_int_Diameter" + ":{" + pipe.Diameter.ToString() + "}";
                str += "VerticalPipeClass_double_totalQ" + ":{" + pipe.totalQ.ToString() + "}";
                str += "VerticalPipeClass_int_totalUsedPump" + ":{" + pipe.totalUsedPump.ToString() + "}";
                str += "VerticalPipeClass_int_AppendusedpumpCount" + ":{" + pipe.AppendusedpumpCount.ToString() + "}";
                str += "VerticalPipeClass_int_IsBridgePipe" + ":{" + pipe.IsBridgePipe.ToString() + "}";
                str += "VerticalPipeClass_bool_IsInitialDrainWell" + ":{" + (pipe.IsInitialDrainWell ? "1" : "0") + "}";
                str += "VerticalPipeClass_bool_IsAdditionPipe" + ":{" + (pipe.IsAdditionPipe ? "1" : "0") + "}";
                str += "VerticalPipeClass_bool_CanUsedToJudgeCrossLayer" + ":{" + (pipe.CanUsedToJudgeCrossLayer ? "1" : "0") + "}";
                str += "VerticalPipeClass_bool_IsGenerated" + ":{" + (pipe.IsGenerated ? "1" : "0") + "}";
            }
            str += "}";
            return str;
        }
        public static string ShowListString(List<string> strs)
        {
            var str = "";
            foreach (var item in strs)
                str += item + ",";
            if (strs.Count > 0)
                str.Remove(str.Length - 1);
            return str;
        }
        public static string ShowSubmergedPumpClass(SubmergedPumpClass submerged)
        {
            var str = "SubmergedPumpClass:{";
            if (submerged != null)
            {
                str += "SubmergedPumpClass_Polyline_Extents" + ":{" + ShowPolyline(submerged.Extents) + "}";
                str += "SubmergedPumpClass_string_Serial" + ":{" + submerged.Serial + "}";
                str += "SubmergedPumpClass_string_Visibility" + ":{" + submerged.Visibility + "}";
                str += "SubmergedPumpClass_string_Location" + ":{" + submerged.Location + "}";
                str += "SubmergedPumpClass_string_Allocation" + ":{" + submerged.Allocation + "}";
                str += "SubmergedPumpClass_string_Length" + ":{" + submerged.Length + "}";
                str += "SubmergedPumpClass_string_Width" + ":{" + submerged.Width + "}";
                str += "SubmergedPumpClass_double_paraQ" + ":{" + submerged.paraQ.ToString() + "}";
                str += "SubmergedPumpClass_double_paraH" + ":{" + submerged.paraH.ToString() + "}";
                str += "SubmergedPumpClass_double_paraN" + ":{" + submerged.paraN.ToString() + "}";
                str += "SubmergedPumpClass_double_Depth" + ":{" + submerged.Depth.ToString() + "}";
                str += "SubmergedPumpClass_int_PumpCount" + ":{" + submerged.PumpCount.ToString() + "}";
                str += "SubmergedPumpClass_BlockReference_Block" + ":{" + ShowBlock(submerged.Block) + "}";
            }
            str += "}";
            return str;
        }
        public static string ShowBlock(BlockReference block)
        {
            var str = "BlockReference:{";
            str += "BlockReference_Position" + ":{" + ShowPoint(block.Position) + "}";
            str += "BlockReference_Extent" + ":{" + ShowPolyline(((Extents3d)block.Bounds).ToRectangle()) + "}";
            str += "BlockReference_Name" + ":{" + block.Name + "}";
            str += "}";
            return str;
        }
        public static string ShowCircle(Circle circle)
        {
            var str = "";
            str += ShowPoint(circle.Center) + ",";
            str += circle.Diameter.ToString();
            return str;
        }
        public static string ShowVerticalPipeClasses(List<VerticalPipeClass> pipes)
        {
            var str = "";
            int count = 0;
            foreach (var unit in pipes)
            {
                count++;
                var unitstr = "VerticalPipeClass单元" + count.ToString() + ":{";
                unitstr += ShowVerticalPipeClass(unit);
                unitstr += "}";
                str += unitstr;
            }
            return str;
        }
        public static string ShowHorizontal(Horizontal horizontal)
        {
            var str = "Horizontal:{";
            if (horizontal != null)
            {
                str += "Horizontal_Line_Line" + ":{" + ShowLine(horizontal.Line) + "}";
                str += "Horizontal_bool_IsInitialLine" + ":{" + (horizontal.IsInitialLine ? "1" : "0") + "}";
            }
            str += "}";
            return str;
        }
        public static string ShowHorizontals(List<Horizontal> horizontals)
        {
            if (horizontals == null)
            {
                return "Horizontal单元:null";
            }
            var str = "";
            int count = 0;
            foreach (var unit in horizontals)
            {
                count++;
                var unitstr = "Horizontal单元" + count.ToString() + ":{";
                unitstr += ShowHorizontal(unit);
                unitstr += "}";
                str += unitstr;
            }
            return str;
        }
        public static string ShowPipeLineUnits(List<PipeLineUnit> units)
        {
            var str = "";
            int count = 0;
            foreach (var unit in units)
            {
                count++;
                var unitstr = "PipeLineUnit单元" + count.ToString() + ":{";
                unitstr += ShowPipeLineUnit(unit);
                unitstr += "}";
                str += unitstr;
            }
            return str;
        }
    }
}

