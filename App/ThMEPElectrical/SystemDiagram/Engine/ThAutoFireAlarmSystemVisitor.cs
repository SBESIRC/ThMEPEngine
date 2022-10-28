using System;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.SystemDiagram.Model;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public class ThAutoFireAlarmSystemVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                HandleBlockReference(elements, blkref, matrix);
            }
        }
        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        public override bool IsDistributionElement(Entity entity)
        {
            return true;
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (IsDistributionElement(blkref) && CheckLayerValid(blkref) && IsDistributeElementBlock(blkref))
            {
                var dic = blkref.Id.GetAttributesInBlockReferenceEx();
                if(dic.Any(o=>o.Value=="APEa"))
                {
                    ThBlockConfigModel.SetGlobleAPEa();
                }
                if (dic.Any(o => o.Value == "APEf"))
                {
                    ThBlockConfigModel.SetGlobleAPEf();
                }
                if (IsRequiredElement(blkref, dic))
                {
                    var info = new ElementInfo()
                    {
                        Layer = blkref.Layer,
                        Name = blkref.Name,
                        AttNames = dic
                    };
                    elements.Add(new ThRawIfcDistributionElementData()
                    {
                        Data = info,
                        Geometry = blkref.GetTransformedCopy(matrix),
                    });
                }
            }
        }

        private bool IsRequiredElement(BlockReference blkref, List<KeyValuePair<string, string>> dic)
        {
            bool IsRequired = false;
            ThBlockConfigModel.BlockConfig.ForEach(o =>
            {
                switch (o.StatisticMode)
                {
                    case StatisticType.BlockName:
                        {
                            if (o.BlockName == blkref.Name || (o.HasAlias && o.AliasList.Contains(blkref.Name)))
                            {
                                IsRequired = true;
                                return;
                            }
                            break;
                        }
                    case StatisticType.Attributes:
                        {
                            if(o.HasAlias && o.AliasList.Contains(blkref.Name))
                            {
                                IsRequired = true;
                                return;
                            }
                            dic.ForEach(keyvaluepair =>
                            {
                                if (o.StatisticAttNameValues.ContainsKey(keyvaluepair.Key))
                                {
                                    var atts = o.StatisticAttNameValues[keyvaluepair.Key];
                                    if(atts.Any(att => att[0] == '~'))
                                    {

                                    }
                                    if(atts.Any(att => att[0] == '~' && att.Substring(1) == keyvaluepair.Value))
                                    {
                                        IsRequired = false;
                                        return;
                                    }
                                    if (atts.Contains(keyvaluepair.Value))
                                    {
                                        IsRequired = true;
                                    }
                                }
                            });
                            return;
                        }
                    case StatisticType.NeedSpecialTreatment:
                        {
                            if (o.BlockName == blkref.Name)
                            {
                                IsRequired = true;
                                return;
                            }
                            break;
                        }
                    case StatisticType.NoStatisticsRequired:
                        {
                            dic.ForEach(keyvaluepair =>
                            {
                                if (o.StatisticAttNameValues.ContainsKey(keyvaluepair.Key) && o.StatisticAttNameValues[keyvaluepair.Key].Contains(keyvaluepair.Value))
                                {
                                    IsRequired = true;
                                    return;
                                }
                            });
                            break;
                        }
                    default:
                        break;
                }
            });
            return IsRequired;
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                //TODO: 获取块的OBB
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }

        private bool IsDistributeElementBlock(BlockReference blkref)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkref.Database))
            {
                var blockTableRecord = acadDatabase.Blocks.Element(blkref.BlockTableRecord);
                return base.IsBuildElementBlock(blockTableRecord);
            }
        }
    }
    public class ElementInfo
    {
        public string Layer { get; set; }
        public string Name { get; set; }
        public List<KeyValuePair<string, string>> AttNames { get; set; }
    }
}
