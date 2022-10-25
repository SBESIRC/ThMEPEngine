using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model.Common;

namespace ThMEPEngineCore.Engine
{
    public class ThWallExtractionVisitor : ThBuildingElementExtractionVisitor,ISetContainer
    {
        private List<ThContainerInfo> _containers = new List<ThContainerInfo>();
        public List<ThContainerInfo> Containers => _containers;

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间
            if (blockTableRecord.IsLayout)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Polyline || dbObj is Arc || dbObj is Line)
            {
                elements.AddRange(HandleCurve(dbObj as Curve, matrix));
            }
            else if (dbObj is Mline mLine)
            {
                elements.AddRange(HandleMLine(mLine, matrix));
            }
        }
        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                if(xclip.Inverted)
                {
                    elements.RemoveAll(o => xclip.Contains(o.Geometry as Curve));
                }
                else
                {
                    elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
                }
            }
        }
        public override bool IsBuildElement(Entity entity)
        {
            return base.IsBuildElement(entity) && entity.Visible;
        }
        public override bool CheckLayerValid(Entity curve)
        {
            if(base.CheckLayerValid(curve))
            {
                return true;
            }
            else
            {
                if(curve.Layer =="0")
                {
                    return CheckParentLayer();
                }
                else
                {
                    return false;
                }
            }
        }

        public void SetContainers(List<ThContainerInfo> containers)
        {
            this._containers = containers;
        }

        private bool CheckParentLayer()
        {
            if(_containers.Count>0)
            {
                var parentLayer = _containers.Last().Layer;
                if(!string.IsNullOrEmpty(parentLayer))
                {
                    return LayerFilter.Contains(parentLayer);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private List<ThRawIfcBuildingElementData> HandleCurve(Curve curve, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(curve) && CheckLayerValid(curve))
            {
                try
                {
                    var clone = curve.WashClone();
                    if (clone != null)
                    {
                        clone.TransformBy(matrix);                       
                        results.Add(new ThRawIfcBuildingElementData()
                        {
                            Geometry = clone,
                        });
                    }
                }
                catch
                {
                    // 由于传入的矩阵是NonUniform Scale,导致Transform失败
                    // 暂时这样跳过去
                }
            }
            return results;
        }
        private List<ThRawIfcBuildingElementData> HandleMLine(Mline mline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(mline) && CheckLayerValid(mline))
            {
                try
                {
                    var curves = new DBObjectCollection();
                    mline.Explode(curves);
                    foreach(Curve curve in curves)
                    {
                        var clone = curve.WashClone();
                        if (clone != null)
                        {
                            clone.TransformBy(matrix);
                            results.Add(new ThRawIfcBuildingElementData()
                            {
                                Geometry = clone,
                            });
                        }
                    }
                }
                catch
                {
                    // 由于传入的矩阵是NonUniform Scale,导致Transform失败
                    // 暂时这样跳过去
                }
            }
            return results;
        }
    }
}
