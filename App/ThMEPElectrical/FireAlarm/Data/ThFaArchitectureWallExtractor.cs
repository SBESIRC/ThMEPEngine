using Linq2Acad;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace FireAlarm.Data
{
    public class ThFaArchitectureWallExtractor : ThArchitectureExtractor, IPrint, IGroup,IGroup2
    {
        public ThFaArchitectureWallExtractor()
        {
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Walls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, ThFireAlarmUtils.BuildString(GroupOwner, o));
                }
                if (Group2Switch)
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.Group2IdPropertyName, ThFireAlarmUtils.BuildString(Group2Owner, o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }
        public void Group(Dictionary<Entity, string> groupId)
        {
            if(GroupSwitch)
            {
                Walls.ForEach(o => GroupOwner.Add(o, ThFireAlarmUtils.FindCurveGroupIds(groupId, o)));
            }
        }
        public void Group2(Dictionary<Entity, string> groupId)
        {
            if (Group2Switch)
            {
                Walls.ForEach(o => Group2Owner.Add(o, ThFireAlarmUtils.FindCurveGroupIds(groupId, o)));
            }
        }
    }
}
