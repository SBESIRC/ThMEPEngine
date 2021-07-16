using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Electrical;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPElectrical.FireAlarm.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace FireAlarm.Data
{
    public class ThEStoreyExtractor : ThExtractorBase,IPrint,IGroup
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
        }

        public override void Extract(Database database, Point3dCollection pts)
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
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Storeys.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, ThFireAlarmUtils.BuildString(GroupOwner, o.Boundary));
                }
                if (Group2Switch)
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.Group2IdPropertyName, ThFireAlarmUtils.BuildString(Group2Owner, o.Boundary));
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.FloorTypePropertyName, o.StoreyType);
                geometry.Properties.Add(ThExtractorPropertyNameManager.FloorNumberPropertyName, o.StoreyNumber);                
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, o.Id);
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
}
