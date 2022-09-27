using System.Collections.Generic;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPTCH.Services;
using ThMEPTCH.PropertyServices.PropertyModels;

namespace ThMEPTCH.CAD
{
    public class ThTCHAxisContinuousDimensionExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public const string CategoryValue = "连续标注";
        public ThTCHAxisContinuousDimensionExtractionVisitor()
        {
        }

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            DoExtract(elements, dbObj, matrix, new List<object>(), 0);
        }

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix, List<object> containers, int uid)
        {
            elements.AddRange(HandleTCHDimension(dbObj, matrix, containers, uid));
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //var xclip = blockReference.XClipInfo();
            //if (xclip.IsValid)
            //{
            //    xclip.TransformBy(matrix);
            //    elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            //}
        }

        public override bool IsDistributionElement(Entity entity)
        {
            if(entity.IsTCHElement())
            {
                //var propertyDict = ThOPMTools.GetOPMProperties(tchDimension.Id);
                var dxfName = entity.GetRXClass().DxfName.ToUpper();
                return dxfName == "TCH_DIMENSION2";
            }
            else
            {
                return false;
            }
        }

        private bool HasContainer(List<object> containers)
        {
            foreach (string name in containers)
            {
                if (!name.Contains("轴网"))
                {
                    continue;
                }
                else
                {
                    if (name.Contains("A9") || name.Contains("A10"))
                    {
                        return true;
                    }
                    if (name.Contains("a9") || name.Contains("a10"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private List<ThRawIfcDistributionElementData> HandleTCHDimension(Entity tchDimension, Matrix3d matrix, List<object> containers, int uid)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(tchDimension) && CheckLayerValid(tchDimension))
            {     
                if(HasContainer(containers))
                {
                    results.Add(new ThRawIfcDistributionElementData()
                    {
                        Data = CreateFloorCurveEntity(tchDimension, matrix, uid),
                    });
                }
            }
            return results;
        }

        private ulong GetUniqueId(Entity e, Matrix3d matrix, int uid)
        {
            return (ulong)ThMEPDbUniqueIdService.UniqueId(e.ObjectId, uid, matrix);
        }

        private FloorCurveEntity CreateFloorCurveEntity(Entity e, Matrix3d matrix, int uid)
        {
            var prop = new TCHAxisProperty(e.ObjectId)
            {
                Category = CategoryValue,
            };
            return new FloorCurveEntity(GetUniqueId(e, matrix, uid), GetOutline(e, matrix), "轴网", prop);
        }

        private Entity GetOutline(Entity e, Matrix3d matrix)
        {
            return e.GetTransformedCopy(matrix);
        }
    }
}
