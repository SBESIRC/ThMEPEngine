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
    public class ThTCHAxisLineExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public const string CategoryValue = "轴网线";
        public ThTCHAxisLineExtractionVisitor()
        {
        }

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            DoExtract(elements, dbObj, matrix, new List<object>(), 0);
        }

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix, List<object> containers, int uid)
        {
            if (dbObj is Line line)
            {
                elements.AddRange(HandleLine(line, matrix, containers,uid));
            }
            else if(dbObj is Arc arc)
            {
                elements.AddRange(HandleArc(arc, matrix, containers, uid));
            }
            else if (dbObj is Polyline polyline)
            {
                elements.AddRange(HandlePolyline(polyline, matrix, containers, uid));
            }
            else
            {
                //
            }
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
            return entity is Line || entity is Polyline || entity is Arc;
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

        private List<ThRawIfcDistributionElementData> HandleLine(Line line, Matrix3d matrix, List<object> containers, int uid)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(line) && CheckLayerValid(line))
            {
                if(HasContainer(containers))
                {
                    results.Add(new ThRawIfcDistributionElementData()
                    {
                        Data = CreateFloorCurveEntity(line, matrix, uid),
                    });
                }
            }
            return results;
        }

        private List<ThRawIfcDistributionElementData> HandleArc(Arc arc, Matrix3d matrix, List<object> containers, int uid)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(arc) && CheckLayerValid(arc) && HasContainer(containers))
            {
                results.Add(new ThRawIfcDistributionElementData()
                {
                    Data = CreateFloorCurveEntity(arc, matrix, uid),
                });
            }
            return results;
        }

        private List<ThRawIfcDistributionElementData> HandlePolyline(Polyline polyline, Matrix3d matrix, List<object> containers, int uid)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(polyline) && CheckLayerValid(polyline) && HasContainer(containers))
            {
                results.Add(new ThRawIfcDistributionElementData()
                {
                    Data = CreateFloorCurveEntity(polyline, matrix, uid),
                });           
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
