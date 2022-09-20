using System.Collections.Generic;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThPlatform3D.Data
{
    public class ThAxisBasePointExtractionVisitor : ThDistributionElementExtractionVisitor,ISetContainer
    {
        private List<string> _containers;
        public List<string> Containers => _containers;

        public ThAxisBasePointExtractionVisitor()
        {
            _containers = new List<string>();
        }

        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if(dbObj is BlockReference br)
            {
                elements.AddRange(HandleBlockReference(br, matrix));
            }            
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains((o.Geometry as DBPoint).Position));
            }
        }

        public void SetContainers(List<string> containers)
        {
            _containers=containers;
        }

        public override bool IsDistributionElement(Entity e)
        {
            if (e is BlockReference br)
            {
                var name = br.GetEffectiveName().ToUpper();
                return name.Contains("BASEPOINT") && HasContainer();
            }
            else
            {
                return false;
            }
        }

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool HasContainer()
        {
            foreach (string name in _containers)
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

        private List<ThRawIfcDistributionElementData> HandleBlockReference(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(br) && CheckLayerValid(br))
            {
                var dbPoint = new DBPoint(br.Position);
                dbPoint.TransformBy(matrix);
                results.Add(CreateDistributionElementData(dbPoint));
            }
            return results;
        }

        private ThRawIfcDistributionElementData CreateDistributionElementData(Entity entity)
        {
            return new ThRawIfcDistributionElementData()
            {
                Geometry = entity,
            };
        }
    }
}
