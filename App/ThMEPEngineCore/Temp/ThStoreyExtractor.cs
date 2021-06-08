using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Common;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThStoreyExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<StoreyInfo> Storeys { get; private set; }
        private const string FloorNumberPropertyName = "FloorNumber";
        private const string FloorTypePropertyName = "FloorType";
        public ThStoreyExtractor()
        {
            Storeys = new List<StoreyInfo>();
            Category = "StoreyBorder";
            UseDb3Engine = true;
            ElementLayer = "AD-FLOOR-AREA";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                var engine = new ThStoreysRecognitionEngine();
                engine.Recognize(database, pts);
                Storeys = engine.Elements.Cast<ThStoreys>().Select(o=>new StoreyInfo(o)).ToList();                
            }
            else
            {
                //
            }
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Storeys.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(FloorTypePropertyName, o.StoreyType);
                geometry.Properties.Add(FloorNumberPropertyName, o.StoreyNumber);                
                geometry.Properties.Add(IdPropertyName, o.Id);
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }
        public void Print(Database database)
        {
            using (var acadDb= AcadDatabase.Use(database))
            {
                Storeys.Select(o=>o.Boundary)                    
                    .Cast<Entity>()
                    .ToList()
                    .CreateGroup(database, ColorIndex);
            }   
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            //
        }

        public Dictionary<Entity, string> StoreyIds
        {
            get
            {
                var result = new Dictionary<Entity, string>();
                Storeys.ForEach(o => result.Add(o.Boundary, o.Id));
                return result;
            }
        }

    }
    public class StoreyInfo
    {        
        public string Id { get; set; }
        public Polyline Boundary { get; private set; }
        /// <summary>
        /// 楼层编号原始值
        /// </summary>
        public string OriginFloorNumber { get; private set; }
        /// <summary>
        /// 解析过的楼层编号
        /// </summary>
        public string StoreyNumber { get; private set; }
        /// <summary>
        /// 楼层范围
        /// </summary>
        public string StoreyRange { get; private set; }
        /// <summary>
        /// 楼层类型
        /// </summary>
        public string StoreyType { get; private set; }

        private ThStoreys Storey { get; set; }

        public StoreyInfo(ThStoreys storey)
        {
            Storey = storey;
            Id = Guid.NewGuid().ToString();
            Parse();
        }
        private void Parse()
        {
            StoreyRange = GetFloorRange();
            OriginFloorNumber = GetFloorNumber();
            StoreyType = Storey.StoreyTypeString;
            StoreyNumber = ParseStoreyNumber();
            Boundary = GetBoundary(); 
        }
        private string GetFloorNumber()
        {
            var attributeDic = Storey.ObjectId.GetAttributesInBlockReference(true);
            foreach (var item in attributeDic)
            {
                if (item.Key == "楼层编号" ||
                    item.Key == "非标层编号" ||
                    item.Key == "标准层编号")
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
                    if(numbers.Count==2)
                    {
                        for(int i = numbers[0];i<=numbers[1];i++)
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
            else if(StoreyRange.Contains("偶数")) 
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
            if(Storey.ObjectId.IsErased || Storey.ObjectId.IsNull || !Storey.ObjectId.IsValid)
            {
                return new Polyline();
            }
            using (var acadDb = AcadDatabase.Use(Storey.ObjectId.Database))
            {
                var br = acadDb.Element<BlockReference>(Storey.ObjectId);
                return br.ToOBB(br.BlockTransform.PreMultiplyBy(Matrix3d.Identity));
            }
        }
    }
}
