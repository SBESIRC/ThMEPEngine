using System;
using Linq2Acad;
using DotNetARX;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Colors;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThRacewayGroupService
    {        
        private ObjectIdList ObjIds { get; set; }
        private ThRacewayGroupParameter GroupParameter { get; set; }
        private ThRacewayGroupService(ThRacewayGroupParameter groupParameter)
        {
            ObjIds = new ObjectIdList();
            GroupParameter = groupParameter;
        }
        public static ObjectIdList Create(ThRacewayGroupParameter groupParameter)
        {
            var instance = new ThRacewayGroupService(groupParameter);
            instance.Create();
            return instance.ObjIds;
        }
        private void Create()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //CreateLayer(acadDatabase.Database);
                GroupParameter.Center.Layer = GroupParameter.RacewayParameter.CenterLineParameter.Layer;
                GroupParameter.Sides.ForEach(o => o.Layer = GroupParameter.RacewayParameter.SideLineParameter.Layer);
                GroupParameter.Ports.ForEach(o => o.Layer = GroupParameter.RacewayParameter.PortLineParameter.Layer);

                GroupParameter.Sides.ForEach(o => o.Linetype = "Bylayer");
                GroupParameter.Ports.ForEach(o => o.Linetype = "Bylayer");

                var lines = GroupParameter.GetAll();
                lines.ForEach(o =>
                {
                    var lineId = acadDatabase.ModelSpace.Add(o);
                    ObjIds.Add(lineId);
                    TypedValueList lineValueList = new TypedValueList
                    {
                        { (int)DxfCode.ExtendedDataAsciiString, "CableTray"},
                    };
                    XDataTools.AddXData(lineId, ThGarageLightCommon.ThGarageLightAppName, lineValueList);
                });
                var groupName = Guid.NewGuid().ToString();
                GroupTools.CreateGroup(acadDatabase.Database, groupName, ObjIds);
            }
        }   
    }
}
