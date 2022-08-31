using System.Linq;
using System.Collections.Generic;
using ThMEPStructure.Common;
using ThMEPEngineCore.Model;

namespace ThMEPStructure.StructPlane
{
    public class ThDrawingParameterConfig
    {
        private static readonly ThDrawingParameterConfig instance = new ThDrawingParameterConfig() { };
        public static ThDrawingParameterConfig Instance { get { return instance; } }       
        internal ThDrawingParameterConfig()
        {
            IsAllStorey = true;
            DrawingScale = "1:100";
            FileFormatOption = "IFC";
            Storeies = new List<ThIfcStoreyInfo>();
            DefaultSlabThick = 100.0;
            FloorSpacing = 100000;
            DrawingType = ThStructurePlaneCommon.StructurePlanName;
            DrawingScales = new List<string> { "1:100", "1:150" };
        }
        static ThDrawingParameterConfig()
        {
        }
        public List<string> DrawingScales { get; private set; }
        public List<ThIfcStoreyInfo> Storeies { get; set; }

        /// <summary>
        /// 图纸比例
        /// </summary>
        public string DrawingScale { get; set; } = "";
        /// <summary>
        /// 默认板厚
        /// </summary>
        public double DefaultSlabThick { get; set; }
        /// <summary>
        /// 楼层间距
        /// </summary>
        public double FloorSpacing { get; set; }
        /// <summary>
        /// 楼层
        /// 显示在UI的楼层名称
        /// </summary>
        public string Storey { get; set; } = "";
        /// <summary>
        /// 全部楼层
        /// </summary>
        public bool IsAllStorey { get; set; }
        /// <summary>
        /// 文件格式
        /// </summary>
        public string FileFormatOption { get; set; } = "";
        /// <summary>
        /// 文件格式
        /// </summary>
        public string DrawingType { get; set; } = "";

        /// <summary>
        /// 获取
        /// </summary>
        public Dictionary<string, string> StdFlrNoDisplayName
        {
            get
            {
                var sortStories = SortByStdFlrNo(Storeies);
                var groupDict = GroupByStdFlrNo(sortStories);
                return GetStdFloorNoDisplayName(groupDict);
            }
        }
        /// <summary>
        /// 标准楼层号
        /// </summary>
        public string StdFlrNo
        {
            get
            {
                return GetStdFlrNo(Storey);
            }
        }

        public string GetStdFlrNo(string stdFlrDisplayName)
        {
            foreach (var item in StdFlrNoDisplayName)
            {
                if (item.Value == stdFlrDisplayName)
                {
                    return item.Key;
                }
            }
            return "";
        }

        private List<ThIfcStoreyInfo> SortByStdFlrNo(List<ThIfcStoreyInfo> storeys)
        {
            return storeys.OrderBy(o =>
            {
                var integerValues = o.StdFlrNo.GetIntegers();
                if(integerValues.Count>0)
                {
                    return integerValues[0];
                }
                else
                {
                    return int.MinValue;
                }
            }).ToList();
        }

        /// <summary>
        /// 根据标准层分组
        /// </summary>
        /// <param name="storyes"></param>
        /// <returns></returns>
        private Dictionary<string, List<ThIfcStoreyInfo>> GroupByStdFlrNo(List<ThIfcStoreyInfo> storyes)
        {
            var groupDict = new Dictionary<string, List<ThIfcStoreyInfo>>();
            var groups = storyes
                .Where(o => !string.IsNullOrEmpty(o.StdFlrNo))
                .GroupBy(o => o.StdFlrNo);
            foreach (var group in groups)
            {
                groupDict.Add(group.Key, group.ToList());
            }
            return groupDict;
        }
        private Dictionary<string, string> GetStdFloorNoDisplayName(Dictionary<string, List<ThIfcStoreyInfo>> stdFlrNoGroup)
        {
            var result = new Dictionary<string, string>();
            foreach (var item in stdFlrNoGroup)
            {
                var stdFlrName = GetStandardFloorName(item.Key);
                var floorNos = item.Value
                    .Select(o => o.FloorNo)
                    .SelectMany(o => o.GetIntegers())
                    .ToList();
                var naturalFloor = GetNaturalFloor(floorNos);
                var displayName = stdFlrName + " (" + naturalFloor + ")";
                result.Add(item.Key, displayName);
            }
            return result;
        }

        private string GetNaturalFloor(List<int> floorNos)
        {
            var nos = new List<string>();
            floorNos = floorNos.OrderBy(o => o).ToList();
            for (int i = 0; i < floorNos.Count; i++)
            {
                var values = new List<int> { floorNos[i] };
                for (int j = i + 1; j < floorNos.Count; j++)
                {
                    if (floorNos[j] - values.Last() == 1)
                    {
                        values.Add(floorNos[j]);
                    }
                    else
                    {
                        break;
                    }
                }
                if (values.Count == 1)
                {
                    nos.Add(values[0].ToString());
                }
                else
                {
                    nos.Add(values.First().ToString() + "~" + values.Last().ToString());
                }
                i += values.Count-1;
            }
            return string.Join(",", nos);
        }

        private string GetStandardFloorName(string stdFldNo)
        {
            var chineseNumber = stdFldNo.NumToChinese();
            return "标准层" + chineseNumber;
        }
    }
}
