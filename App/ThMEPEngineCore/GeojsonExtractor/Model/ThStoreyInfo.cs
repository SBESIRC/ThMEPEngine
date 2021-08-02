using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore.Model.Common;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPEngineCore.GeojsonExtractor.Model
{
    public class ThStoreyInfo
    {
        public string Id { get; set; }
        public Polyline Boundary { get; set; }
        /// <summary>
        /// 楼层编号原始值
        /// </summary>
        public string OriginFloorNumber { get;  set; }
        /// <summary>
        /// 解析过的楼层编号
        /// </summary>
        public string StoreyNumber { get;  set; }
        /// <summary>
        /// 楼层范围
        /// </summary>
        public string StoreyRange { get; set; }
        /// <summary>
        /// 楼层类型
        /// </summary>
        public string StoreyType { get;  set; }

        public string BasePoint { get;  set; }

        private ThStoreys Storey { get; set; }

        public ThStoreyInfo(ThStoreys storey)
        {
            Storey = storey;
            Id = Guid.NewGuid().ToString();
            Parse();
        }
        public ThStoreyInfo()
        {
            //
        }
        private void Parse()
        {
            StoreyRange = GetFloorRange(Storey.ObjectId);
            OriginFloorNumber = GetFloorNumber(Storey.ObjectId);
            StoreyType = Storey.StoreyTypeString;
            StoreyNumber = ParseStoreyNumber();
            Boundary = GetBoundary(Storey.ObjectId);
            BasePoint = GetBasePoint(Storey.ObjectId);
        }
        protected virtual string GetFloorNumber(ObjectId storeyId)
        {
            var attributeDic = storeyId.GetAttributesInBlockReference(true);
            foreach (var item in attributeDic)
            {
                if (item.Key.Contains("编号"))  // 标准层编号、非标准层编号、大屋面、小面...
                {
                    return item.Value;
                }
            }
            return "";
        }
        protected virtual string GetFloorRange(ObjectId storeyId)
        {
            var attributeDic = storeyId.GetAttributesInBlockReference(true);
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
        protected virtual Polyline GetBoundary(ObjectId storeyId)
        {
            if (storeyId.IsErased || storeyId.IsNull || !storeyId.IsValid)
            {
                return new Polyline();
            }
            using (var acadDb = AcadDatabase.Use(storeyId.Database))
            {
                var br = acadDb.Element<BlockReference>(storeyId);
                return br.ToOBB(br.BlockTransform.PreMultiplyBy(Matrix3d.Identity));
            }
        }
        protected virtual string GetBasePoint(ObjectId storeyId)
        {
            using (var acadDb = AcadDatabase.Use(storeyId.Database))
            {
                var br = acadDb.Element<BlockReference>(storeyId);
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
