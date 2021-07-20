using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Common;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPEngineCore.Temp
{
    public class ThEStoreyExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<EStoreyInfo> Storeys { get; private set; }
        private const string FloorNumberPropertyName = "FloorNumber";
        private const string FloorTypePropertyName = "FloorType";
        private const string BasePointPropertyName = "BasePoint";
        public ThEStoreyExtractor()
        {
            UseDb3Engine = true;
            Storeys = new List<EStoreyInfo>();
            Category = BuiltInCategory.StoreyBorder.ToString();
            TesslateLength = 200.0;
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                var engine = new ThEStoreysRecognitionEngine();
                engine.Recognize(database, pts);
                Storeys = engine.Elements.Cast<ThEStoreys>().Select(o=>new EStoreyInfo(o)).ToList();                
            }
            else
            {
                //
            }
            Storeys.ForEach(o =>
            {
                var curve = ThTesslateService.Tesslate(o.Boundary, TesslateLength);
                o.Boundary = curve as Polyline;
            });
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Storeys.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o.Boundary));
                }
                if (Group2Switch)
                {
                    geometry.Properties.Add(Group2IdPropertyName, BuildString(Group2Owner, o.Boundary));
                }
                geometry.Properties.Add(FloorTypePropertyName, o.StoreyType);
                geometry.Properties.Add(FloorNumberPropertyName, o.StoreyNumber);                
                geometry.Properties.Add(IdPropertyName, o.Id);
                geometry.Properties.Add(BasePointPropertyName, o.BasePoint);
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
    public class EStoreyInfo:StoreyInfo
    {        
        private ThEStoreys Storey { get; set; }

        public EStoreyInfo(ThEStoreys storey)
        {
            Storey = storey;
            Id = Guid.NewGuid().ToString();
            Parse();
        }
        private void Parse()
        {
            StoreyRange = GetFloorRange();
            OriginFloorNumber = GetFloorNumber();
            StoreyNumber = string.Join(",",Storey.Storeys);
            Boundary = GetBoundary();
            BasePoint = GetBasePoint();
            StoreyType = Storey.StoreyTypeString;
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
        private string GetBasePoint()
        {
            using (var acadDb = AcadDatabase.Use(Storey.ObjectId.Database))
            {
                var br = acadDb.Element<BlockReference>(Storey.ObjectId);
                double xOffset = 0.0,yOffset = 0.0;
                foreach(DynamicBlockReferenceProperty item in br.DynamicBlockReferencePropertyCollection)
                {
                    if(item.PropertyName.Trim() == "基点X" || item.PropertyName.Trim() == "基点 X")
                    {
                        xOffset = (double)item.Value;
                    }
                    if(item.PropertyName.Trim() == "基点Y" || item.PropertyName.Trim() == "基点 Y")
                    {
                        yOffset = (double)item.Value;
                    }
                }
                return (br.Position.X + xOffset).ToString() + "," +(br.Position.Y + yOffset).ToString();
            }
        }
    }
}
