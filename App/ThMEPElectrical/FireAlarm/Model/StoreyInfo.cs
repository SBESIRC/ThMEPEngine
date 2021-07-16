using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Model.Common;

namespace ThMEPElectrical.FireAlarm.Model
{
    public class StoreyInfo
    {
        public string Id { get; set; }
        public Polyline Boundary { get; set; }
        /// <summary>
        /// 楼层编号原始值
        /// </summary>
        public string OriginFloorNumber { get; set; }
        /// <summary>
        /// 解析过的楼层编号
        /// </summary>
        public string StoreyNumber { get; set; }
        /// <summary>
        /// 楼层范围
        /// </summary>
        public string StoreyRange { get; set; }
        /// <summary>
        /// 楼层类型
        /// </summary>
        public string StoreyType { get; set; }

        public string BasePoint { get; set; }

        private ThStoreys Storey { get; set; }

        public StoreyInfo(ThStoreys storey)
        {
            Storey = storey;
            Id = Guid.NewGuid().ToString();
            Parse();
        }
        public StoreyInfo()
        {
            Id = Guid.NewGuid().ToString();
        }
        private void Parse()
        {
            StoreyRange = GetFloorRange();
            OriginFloorNumber = GetFloorNumber();
            StoreyType = Storey.StoreyTypeString;
            StoreyNumber = ParseStoreyNumber();
            Boundary = GetBoundary();
            BasePoint = GetBasePoint();
        }
        private string GetFloorNumber()
        {
            var attributeDic = Storey.ObjectId.GetAttributesInBlockReference(true);
            foreach (var item in attributeDic)
            {
                if (item.Key.Contains("编号"))  // 标准层编号、非标准层编号、大屋面、小面...
                {
                    return item.Value;
                }
            }
            return "";
        }
        private string GetFloorRange()
        {
            var attributeDic = Storey.ObjectId.GetAttributesInBlockReference(true);
            foreach (var item in attributeDic)
            {
                if (item.Key == "楼层范围")
                {
                    return item.Value;
                }
            }
            return "";
        }
        private string ParseStoreyNumber()
        {
            // 通过OriginFloorNumber和楼层范围解析
            OriginFloorNumber = OriginFloorNumber.Replace('，', ',');
            string[] values = OriginFloorNumber.Split(',');
            string pattern1 = @"\d+\s*[-]\s*\d+";
            string pattern2 = @"\d+";

            var rg1 = new Regex(pattern1);
            var rg2 = new Regex(pattern2);
            var integers = new List<int>();
            foreach (string value in values)
            {
                if (rg1.IsMatch(value)) // 1 - 10
                {
                    var numbers = new List<int>();
                    foreach (Match match in rg2.Matches(value))
                    {
                        numbers.Add(int.Parse(match.Value));
                    }
                    numbers = numbers.OrderBy(o => o).ToList();
                    if (numbers.Count == 2)
                    {
                        for (int i = numbers[0]; i <= numbers[1]; i++)
                        {
                            integers.Add(i);
                        }
                    }
                }
                else if (rg2.IsMatch(value))
                {
                    integers.Add(int.Parse(value.Trim()));
                }
            }
            integers = integers.OrderBy(o => o).ToList();
            if (StoreyRange.Contains("奇数"))
            {
                return string.Join(",", integers.Where(i => i % 2 != 0).ToArray());
            }
            else if (StoreyRange.Contains("偶数"))
            {
                return string.Join(",", integers.Where(i => i % 2 == 0).ToArray());
            }
            else
            {
                return OriginFloorNumber;
            }
        }
        private Polyline GetBoundary()
        {
            if (Storey.ObjectId.IsErased || Storey.ObjectId.IsNull || !Storey.ObjectId.IsValid)
            {
                return new Polyline();
            }
            using (var acadDb = AcadDatabase.Use(Storey.ObjectId.Database))
            {
                var br = acadDb.Element<BlockReference>(Storey.ObjectId);
                return br.ToOBB(br.BlockTransform.PreMultiplyBy(Matrix3d.Identity));
            }
        }
        private string GetBasePoint()
        {
            using (var acadDb = AcadDatabase.Use(Storey.ObjectId.Database))
            {
                var br = acadDb.Element<BlockReference>(Storey.ObjectId);
                double xOffset = 0.0, yOffset = 0.0;
                foreach (DynamicBlockReferenceProperty item in br.DynamicBlockReferencePropertyCollection)
                {
                    if (item.PropertyName.Trim() == "基点X" || item.PropertyName.Trim() == "基点 X")
                    {
                        xOffset = (double)item.Value;
                    }
                    if (item.PropertyName.Trim() == "基点Y" || item.PropertyName.Trim() == "基点 Y")
                    {
                        yOffset = (double)item.Value;
                    }
                }
                return (br.Position.X + xOffset).ToString() + "," + (br.Position.Y + yOffset).ToString();
            }
        }
    }
}
