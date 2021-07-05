using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Model;

namespace ThMEPWSS.Common
{
    public class FramedReadUtil
    {
        static readonly string floorBlockName = "楼层框定";
        public static List<FloorFramed> ReadAllFloorFramed()
        {
            //楼层框定是动态块，目前的提取引擎不支持动态块的提前
            //这里楼层框定一般不在其它块中，这里就不继续遍历块去找
            List<FloorFramed> resFloors = new List<FloorFramed>();
            Active.Document.LockDocument();
            //获取图中的屋面框线信息
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var allBlockReference = acdb.ModelSpace.OfType<BlockReference>();
                   
                foreach (var block in allBlockReference)
                {
                    if (!block.GetEffectiveName().Equals(floorBlockName))
                        continue;
                    resFloors.Add(new FloorFramed(block,block.Id));
                }
            }
            return resFloors;
        }

        public static bool SelectFloorFramed(out List<FloorFramed> resFloors)
        {
            bool selectSucceed = false;
            resFloors = new List<FloorFramed>();
            using (Active.Document.LockDocument())
            {
                Utils.FocusToCAD();
                //Active.Document.SendStringToExecute("\x03\x03", true, false, true);
                using (AcadDatabase acdb = AcadDatabase.Active())
                {
                    PromptSelectionOptions options = new PromptSelectionOptions()
                    {
                        AllowDuplicates = false,
                        MessageForAdding = "请选择楼层框线",
                        RejectObjectsOnLockedLayers = true,
                    };
                    var dxfNames = new string[]
                    {
                        RXClass.GetClass(typeof(BlockReference)).DxfName,
                    };
                    var filter = ThSelectionFilterTool.Build(dxfNames);
                    var result = Active.Editor.GetSelection(options, filter);

                    if (result.Status == PromptStatus.OK)//框选择成功
                    {
                        var selectedIds = result.Value.GetObjectIds();
                        foreach (var sId in selectedIds)
                        {
                            var br = acdb.Element<BlockReference>(sId);
                            if (null == br || !br.GetEffectiveName().Equals(floorBlockName))
                                continue;
                            resFloors.Add(new FloorFramed(br, sId));
                        }
                        selectSucceed = true;
                    }
                }
            }
            return selectSucceed;
        }

        public static List<FloorFramed> FloorFramedOrder(List<FloorFramed> orderFloors,bool isDes)
        {
            var resFloors = new List<FloorFramed>();
            var tempFramed = orderFloors.OrderBy(c => c.startFloorOrder).ToList();
            if(isDes)
                tempFramed = orderFloors.OrderByDescending(c => c.startFloorOrder).ToList();
            resFloors.AddRange(tempFramed.Where(c => c.floorType.Equals("小屋面")).ToList());
            resFloors.AddRange(tempFramed.Where(c => c.floorType.Equals("大屋面")).ToList());
            foreach (var item in tempFramed)
            {
                if (item.floorType.Contains("屋面"))
                    continue;
                if (item.floorType.Contains("非标"))
                    continue;
                resFloors.Add(item);
            }
            foreach (var item in tempFramed)
            {
                if (!item.floorType.Contains("非标"))
                    continue;
                resFloors.Add(item);
            }
            return resFloors;
        }
        public static List<Line> FloorFrameSpliteLines(FloorFramed floorFramed) 
        {
            var spt = floorFramed.floorBlock.Position;
            var spliteLines = new List<Line>();
            var allSpliteInts = new List<int>();
            var spliteXValues = new List<double>();
            Active.Document.LockDocument();
            //获取图中的屋面框线信息
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var block = acdb.Element<BlockReference>(floorFramed.blockId);
                var dynColls = block.DynamicBlockReferencePropertyCollection;
                var str = "距离";
                var strSplite = "分割";
                for (int i = 0; i < dynColls.Count; i++)
                {
                    DynamicBlockReferenceProperty property = dynColls[i];
                    if (property.PropertyName.Contains(str))
                    {
                        
                        var allNums = StringAllNumber(property.PropertyName);
                        if (allNums == null || allNums.Count < 1)
                            continue;
                        int index = allNums.FirstOrDefault();
                        if (!allSpliteInts.Any(c => c == index))
                            allSpliteInts.Add(index);
                    }
                }
                allSpliteInts = allSpliteInts.OrderBy(c => c).ToList();
                for (int i = 0; i < allSpliteInts.Count; i++) 
                {
                    var disSplite = BlockTools.GetDynBlockValue(floorFramed.blockId, string.Format("{0}{1}", str, allSpliteInts[i]));
                    var spliteX = BlockTools.GetDynBlockValue(floorFramed.blockId, string.Format("{0}{1} {2}", strSplite, allSpliteInts[i], "X"));
                    var spliteY = BlockTools.GetDynBlockValue(floorFramed.blockId, string.Format("{0}{1} {2}", strSplite, allSpliteInts[i], "Y"));
                    var lineSp = spt + Vector3d.XAxis.MultiplyBy(Convert.ToDouble(spliteX));
                    lineSp = lineSp + Vector3d.YAxis.MultiplyBy(Convert.ToDouble(spliteY));
                    spliteXValues.Add(lineSp.X);
                    var lineEp = lineSp + Vector3d.YAxis.MultiplyBy(Convert.ToDouble(disSplite));
                    spliteLines.Add(new Line(lineSp, lineEp));
                }
            }
            return spliteLines;
        }

        public static List<int> StringAllNumber(string str) 
        {
            List<int> intNums = new List<int>();
            if (string.IsNullOrEmpty(str))
                return intNums;
            var chars = str.ToCharArray();
            string num = "";
            for (int i = 0; i < chars.Count(); i++)
            {
                if (chars[i] >= 48 && chars[i] <= 57)
                {
                    num += chars[i];
                }
                else if (!string.IsNullOrEmpty(num))
                {
                    var intNum = Convert.ToInt32(num);
                    if (!intNums.Any(c => c == intNum))
                        intNums.Add(intNum);
                    num = "";
                }
            }
            if (!string.IsNullOrEmpty(num))
            {
                var intNum = Convert.ToInt32(num);
                if (!intNums.Any(c => c == intNum))
                    intNums.Add(intNum);
                num = "";
            }
            return intNums;
        }

    }
}
