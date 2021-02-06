using System;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    public class ThCreateLightToDatabaseService
    {
        private ObjectIdList ObjIds { get; set; }
        private ThRegionBorder RegionBorder { get; set; }
        private ThRacewayParameter RacewayParameter { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        private ThCreateLightToDatabaseService(
            ThRegionBorder regionBorder, 
            ThRacewayParameter racewayParameter,
            ThLightArrangeParameter arrangeParameter)
        {
            ObjIds = new ObjectIdList();
            RegionBorder = regionBorder;
            ArrangeParameter = arrangeParameter;
            RacewayParameter = racewayParameter;
        }
        public static void Create(
            ThRegionBorder regionBorder,
            ThRacewayParameter racewayParameter,
            ThLightArrangeParameter arrangeParameter)
        {
            var instance = new ThCreateLightToDatabaseService(
                regionBorder, racewayParameter, arrangeParameter);
            instance.Create();
        }
        private void Create()
        {
            ObjIds.AddRange(CreateGroup());
            ObjIds.AddRange(CreateLightAndNumber());
        }
        private ObjectIdList CreateGroup()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var results = new ObjectIdList();
                //先将Buffer后的边线提交到Database
                RegionBorder.CableTrayCenters.ForEach(o => o.Layer = RacewayParameter.CenterLineParameter.Layer);
                RegionBorder.CableTrayCenters.ForEach(o => results.Add(acadDb.ModelSpace.Add(o)));

                RegionBorder.CableTraySides.ForEach(o => o.Layer = RacewayParameter.SideLineParameter.Layer);
                RegionBorder.CableTraySides.ForEach(o => results.Add(acadDb.ModelSpace.Add(o)));

                RegionBorder.CableTrayGroups.ForEach(o =>
                {
                    var groupIds = new ObjectIdList();
                    o.Value.ForEach(v => groupIds.Add(v.Id));
                    var ports = FindPorts(o.Key, RegionBorder.CableTrayPorts);
                    ports.ForEach(p => groupIds.Add(p.Id));
                    var groupName = Guid.NewGuid().ToString();
                    GroupTools.CreateGroup(acadDb.Database, groupName, groupIds);
                });
                return results;
            }
        }
        protected ObjectIdList CreateLightAndNumber()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objIds = new ObjectIdList();
                RegionBorder.LightEdges.Where(o => o.IsDX).ForEach(m =>
                {
                    var normalLine = ThGarageLightUtils.NormalizeLaneLine(m.Edge);
                    m.LightNodes.ForEach(n =>
                    {
                        if (!string.IsNullOrEmpty(n.Number))
                        {
                            DBText code = new DBText();
                            code.TextString = n.Number;
                            var alignPt = n.Position + normalLine.StartPoint.GetVectorTo(normalLine.EndPoint)
                              .GetPerpendicularVector()
                              .GetNormal()
                              .MultiplyBy(ArrangeParameter.Width / 2.0 + 100 + ArrangeParameter.LightNumberTextHeight / 2.0);
                            code.Height = ArrangeParameter.LightNumberTextHeight;
                            code.WidthFactor = ArrangeParameter.LightNumberTextWidthFactor;
                            code.Position = alignPt;
                            double angle = normalLine.Angle / Math.PI * 180.0;
                            angle = ThGarageLightUtils.LightNumberAngle(angle);
                            angle = angle / 180.0 * Math.PI;
                            code.Rotation = angle;
                            code.HorizontalMode = TextHorizontalMode.TextCenter;
                            code.VerticalMode = TextVerticalMode.TextVerticalMid;
                            code.AlignmentPoint = code.Position;
                            code.ColorIndex = RacewayParameter.NumberTextParameter.ColorIndex;
                            code.Layer = RacewayParameter.NumberTextParameter.Layer;
                            code.TextStyleId = acadDatabase.TextStyles.Element(ArrangeParameter.LightNumberTextStyle).Id;
                            code.SetDatabaseDefaults(acadDatabase.Database);
                            var codeId = acadDatabase.ModelSpace.Add(code);
                            objIds.Add(codeId);

                            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                  RacewayParameter.LaneLineBlockParameter.Layer,
                                  ThGarageLightCommon.LaneLineLightBlockName,
                                  n.Position,
                                  new Scale3d(ArrangeParameter.PaperRatio),
                                  normalLine.Angle);
                            objIds.Add(blkId);
                        }
                    });
                });
                return objIds;
            }
        }

        public static void SetDatabaseDefaults(
            ThRacewayParameter racewayParameter, 
            ThLightArrangeParameter arrangeParameter)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.LaneLineLightDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThGarageLightCommon.LaneLineLightBlockName));
                acadDatabase.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(arrangeParameter.LightNumberTextStyle), false);
                var centerLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(racewayParameter.CenterLineParameter.LineType));
                var laneLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(racewayParameter.LaneLineBlockParameter.LineType));
                var numberTextLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(racewayParameter.NumberTextParameter.LineType));
                var portLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(racewayParameter.PortLineParameter.LineType));
                var sideLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(racewayParameter.SideLineParameter.LineType));

                var centerLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(racewayParameter.CenterLineParameter.Layer));
                var centerLineLayerLTR = centerLineLayer.Item as LayerTableRecord;
                centerLineLayerLTR.UpgradeOpen();
                centerLineLayerLTR.LinetypeObjectId = centerLineLT.Item.Id;
                centerLineLayerLTR.DowngradeOpen();

                var laneLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(racewayParameter.LaneLineBlockParameter.Layer));
                var laneLineLTR = laneLineLayer.Item as LayerTableRecord;
                laneLineLTR.UpgradeOpen();
                laneLineLTR.LinetypeObjectId = laneLineLT.Item.Id;
                laneLineLTR.DowngradeOpen();

                var numberTextLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(racewayParameter.NumberTextParameter.Layer));
                var numberTextLTR = numberTextLayer.Item as LayerTableRecord;
                numberTextLTR.UpgradeOpen();
                numberTextLTR.LinetypeObjectId = numberTextLT.Item.Id;
                numberTextLTR.DowngradeOpen();

                var portLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(racewayParameter.PortLineParameter.Layer));
                var portLineLTR = portLineLayer.Item as LayerTableRecord;
                portLineLTR.UpgradeOpen();
                portLineLTR.LinetypeObjectId = portLineLT.Item.Id;
                portLineLTR.DowngradeOpen();

                var sideLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(racewayParameter.SideLineParameter.Layer));
                var sideLineLTR = sideLineLayer.Item as LayerTableRecord;
                sideLineLTR.UpgradeOpen();
                sideLineLTR.LinetypeObjectId = sideLineLT.Item.Id;
                sideLineLTR.DowngradeOpen();
            }
        }

        private List<Line> FindPorts(Line center, Dictionary<Line, List<Line>> centerPorts)
        {
            if (centerPorts.ContainsKey(center))
            {
                return centerPorts[center];
            }
            else
            {
                foreach (var item in centerPorts)
                {
                    if (center.IsCoincide(item.Key, 1.0))
                    {
                        return item.Value;
                    }
                }
            }
            return new List<Line>();
        }
    }
}
