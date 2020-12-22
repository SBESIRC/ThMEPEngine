using System;
using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    public class ThRacewayGroupService
    {        
        private ThRacewayGroupParameter GroupParameter { get; set; }
        private ThRacewayGroupService(ThRacewayGroupParameter groupParameter)
        {
            GroupParameter = groupParameter;
        }
        public static void Create(ThRacewayGroupParameter groupParameter)
        {
            var instance = new ThRacewayGroupService(groupParameter);
            instance.Create();
        }
        private void Create()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                CreateLayer(acadDatabase.Database);
                GroupParameter.Center.Layer = GroupParameter.RacewayParameter.CenterLineParameter.Layer;
                GroupParameter.Sides.ForEach(o => o.Layer = GroupParameter.RacewayParameter.SideLineParameter.Layer);
                GroupParameter.Ports.ForEach(o => o.Layer = GroupParameter.RacewayParameter.PortLineParameter.Layer);
                var lines = GroupParameter.GetAll();
                var objids = new ObjectIdList();
                lines.ForEach(o => objids.Add(acadDatabase.ModelSpace.Add(o)));
                var groupName = Guid.NewGuid().ToString();
                GroupTools.CreateGroup(acadDatabase.Database, groupName, objids);
            }
        }
        private void CreateLayer(Database db)
        {
            LoadLinetype(db);
            CreateLayer(db,
            GroupParameter.RacewayParameter.SideLineParameter.Layer,
            GroupParameter.RacewayParameter.SideLineParameter.LineType,
            GroupParameter.RacewayParameter.SideLineParameter.ColorIndex);
            CreateLayer(db,
                GroupParameter.RacewayParameter.PortLineParameter.Layer,
                GroupParameter.RacewayParameter.PortLineParameter.LineType,
                GroupParameter.RacewayParameter.PortLineParameter.ColorIndex);
            CreateLayer(db,
                GroupParameter.RacewayParameter.CenterLineParameter.Layer,
                GroupParameter.RacewayParameter.CenterLineParameter.LineType,
                GroupParameter.RacewayParameter.CenterLineParameter.ColorIndex);
            CreateLayer(db,
                GroupParameter.RacewayParameter.NumberTextParameter.Layer,
                GroupParameter.RacewayParameter.NumberTextParameter.LineType,
                GroupParameter.RacewayParameter.NumberTextParameter.ColorIndex);
            CreateLayer(db,
               GroupParameter.RacewayParameter.LaneLineBlockParameter.Layer,
               GroupParameter.RacewayParameter.LaneLineBlockParameter.LineType,
               GroupParameter.RacewayParameter.LaneLineBlockParameter.ColorIndex);
        }
        private void CreateLayer(Database db,string layerName,string linetype,short colorIndex)
        {
            using (AcadDatabase acadDatabase= AcadDatabase.Use(db))
            {
                var layerId=ThGarageLightUtils.AddLayer(acadDatabase.Database, layerName);               
                var ltr = acadDatabase.Element<LayerTableRecord>(layerId);
                ltr.UpgradeOpen();
                ltr.LinetypeObjectId = ThGarageLightUtils.AddLineType(acadDatabase.Database, linetype);
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
                ltr.DowngradeOpen();
            }
        }
        private void LoadLinetype(Database db)
        {
            ThGarageLightUtils.LoadLineType(db,
                    GroupParameter.RacewayParameter.SideLineParameter.LineType);
            ThGarageLightUtils.LoadLineType(db,
                GroupParameter.RacewayParameter.PortLineParameter.LineType);
            ThGarageLightUtils.LoadLineType(db,
                GroupParameter.RacewayParameter.CenterLineParameter.LineType);
            ThGarageLightUtils.LoadLineType(db,
                GroupParameter.RacewayParameter.NumberTextParameter.LineType);
            ThGarageLightUtils.LoadLineType(db,
                GroupParameter.RacewayParameter.LaneLineBlockParameter.LineType);
        }
    }
}
