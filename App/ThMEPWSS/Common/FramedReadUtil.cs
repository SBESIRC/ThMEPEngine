using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Common;
using ThMEPWSS.Model;

namespace ThMEPWSS.Common
{
    public class FramedReadUtil
    {
        static readonly string floorBlockName = "楼层框定";
        public static List<FloorFramed> ReadAllFloorFramed()
        {
            //楼层框定是动态块，目前的提取引擎不支持动态块的提取
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
                    resFloors.Add(new FloorFramed(block, block.Id));
                }
            }
            return resFloors;
        }

        public static bool SelectFloorFramed(out List<FloorFramed> resFloors, Action cb = null)
        {
            if (cb != null)
            {
                cb(); resFloors = null; return false;
            }
            bool selectSucceed = false;
            resFloors = new List<FloorFramed>();
            using (Active.Document.LockDocument())
            {
                Utils.FocusToCAD();
                using (AcadDatabase acdb = AcadDatabase.Active())
                {
                    var selectedArea = Utils.SelectAreas();
                    if (selectedArea.Count == 0)
                        return false;
                    var rect = new Rectangle3d(selectedArea[0], selectedArea[1], selectedArea[2], selectedArea[3]);
                    var storeysRecEngine = new ThStoreysRecognitionEngine();//创建楼板识别引擎R
                    storeysRecEngine.Recognize(acdb.Database, selectedArea);
                    if (storeysRecEngine.Elements.Count < 1)
                        return true;
                    var selectIds = storeysRecEngine.Elements.Where(c => c is ThStoreys).Cast<ThStoreys>().Select(c => c.ObjectId).ToList();
                    if (selectIds == null || selectIds.Count < 1)
                        return true;
                    foreach (var sId in selectIds)
                    {
                        var br = acdb.Element<BlockReference>(sId);
                        if (null == br || !br.GetEffectiveName().Equals(floorBlockName))
                            continue;
                        resFloors.Add(new FloorFramed(br, sId));
                    }
                    selectSucceed = true;
                }
            }
            return selectSucceed;
        }

        public static List<FloorFramed> FloorFramedOrder(List<FloorFramed> orderFloors, bool isDes)
        {
            var resFloors = new List<FloorFramed>();
            var tempFramed = orderFloors.OrderBy(c => c.endFloorOrder).ToList();
            if (isDes)
                tempFramed = orderFloors.OrderByDescending(c => c.endFloorOrder).ToList();
            resFloors.AddRange(tempFramed.Where(c => c.floorType.Equals("小屋面")).ToList());
            resFloors.AddRange(tempFramed.Where(c => c.floorType.Equals("大屋面")).ToList());
            foreach (var item in tempFramed)
            {
                if (item.floorType.Contains("屋面"))
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
