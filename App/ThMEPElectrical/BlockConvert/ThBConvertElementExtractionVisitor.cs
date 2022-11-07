using System.Collections.Generic;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertElementExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        /// <summary>
        /// 块名
        /// </summary>
        public List<string> NameFilter { get; set; }

        /// <summary>
        /// 专业
        /// </summary>
        public ConvertCategory Category { get; set; }

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                elements.AddRange(Handle(br, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid && elements.Count != 0)
            {
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is Curve curve)
            {
                return xclip.Contains(curve);
            }
            else
            {
                return false;
            }
        }

        private List<ThRawIfcDistributionElementData> Handle(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(br) && CheckLayerValid(br))
            {
                results.Add(new ThRawIfcDistributionElementData()
                {
                    Data = new ThBlockReferenceData(br.ObjectId, matrix),
                    Geometry = br.ToOBB(br.BlockTransform.PreMultiplyBy(matrix)),
                });
            }
            return results;
        }

        public override bool IsDistributionElement(Entity entity)
        {
            try
            {
                if (entity is BlockReference br)
                {
                    return NameFilter.Contains(ThMEPXRefService.OriginalFromXref(br.GetEffectiveName()));
                }
                return false;
            }
            catch
            {
                // BlockReference.IsDynamicBlock可能会抛出异常
                // 这里可以忽略掉这些有异常情况的动态块
                return false;
            }
        }

        public override bool CheckLayerValid(Entity curve)
        {
            if (curve.LayerId.IsValid)
            {
                var layer = curve.LayerId.GetObject(OpenMode.ForRead) as LayerTableRecord;
                return !layer.IsFrozen && !layer.IsOff && !layer.IsHidden;
            }
            else
            {
                return false;
            }
        }

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsFromExternalReference || blockTableRecord.IsFromOverlayReference)
            {
                // 根据外参前缀进行过滤
                using (var CurrentDb = Linq2Acad.AcadDatabase.Active())
                {
                    var xrg = CurrentDb.Database.GetHostDwgXrefGraph(false);
                    var name = blockTableRecord.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        var r = new Regex(@"([a-zA-Z])");
                        var m = r.Match(name.ToUpper());
                        if (!m.Success)
                        {
                            return false;
                        }
                        var c = m.Value[0];
                        var flag = false;
                        switch (Category)
                        {
                            case ConvertCategory.WSS:
                                flag = c.Equals('W');
                                break;
                            case ConvertCategory.HVAC:
                                flag = c.Equals('H');
                                break;
                            case ConvertCategory.ALL:
                                flag = c.Equals('W') || c.Equals('H');
                                break;
                        }
                        if (!flag)
                        {
                            return flag;
                        }
                    }
                }
            }

            // 不支持图纸空间
            if (blockTableRecord.IsLayout)
            {
                return false;
            }
            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }
            return true;
        }
    }
}