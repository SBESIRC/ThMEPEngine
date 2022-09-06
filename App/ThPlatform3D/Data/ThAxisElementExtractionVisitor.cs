using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThPlatform3D.Data
{
    public class ThAxisElementExtractionVisitor : ThDistributionElementExtractionVisitor, ISetContainer
    {
        private List<string> _containers;
        public List<string> Containers => _containers;
        public ThAxisElementExtractionVisitor()
        {
            _containers = new List<string>();
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Line line)
            {
                elements.AddRange(HandleLine(line, matrix));
            }
            else if (dbObj is Polyline polyline)
            {
                elements.AddRange(HandlePolyline(polyline, matrix));
            }
            else if (dbObj is Mline mline)
            {
                elements.AddRange(HandleMline(mline, matrix));
            }
            else if (dbObj is Arc arc)
            {
                elements.AddRange(HandleArc(arc, matrix));
            }
            else if (dbObj is Circle circle)
            {
                elements.AddRange(HandleCircle(circle, matrix));
            }
            else if (dbObj is DBText dbText)
            {
                elements.AddRange(HandleDBText(dbText, matrix));
            }
            else if (dbObj is MText mText)
            {
                elements.AddRange(HandleMText(mText, matrix));
            }
            else if (dbObj is Dimension dim)
            {
                elements.AddRange(HandleDimension(dim, matrix));
            }
            else if(dbObj.IsTCHElement())
            {
                elements.AddRange(HandleTCHElement(dbObj, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }


        public override bool IsDistributionElement(Entity entity)
        {
            if(entity is BlockReference br)
            {
                return false;
            }
            else
            {
                return HasContainer();
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

        public override bool CheckLayerValid(Entity entity)
        {
            return true;
        }

        private List<ThRawIfcDistributionElementData> HandleLine(Line line, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(line) && CheckLayerValid(line))
            {
                results.Add(CreateDistributionElementData(line.GetTransformedCopy(matrix)));
            }
            return results;
        }

        private List<ThRawIfcDistributionElementData> HandlePolyline(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(polyline) && CheckLayerValid(polyline))
            {
                var entitySet = new DBObjectCollection();
                polyline.Explode(entitySet);
                entitySet.Cast<Entity>().ForEach(o => results.Add(CreateDistributionElementData(o.GetTransformedCopy(matrix))));
            }
            return results;
        }

        private List<ThRawIfcDistributionElementData> HandleMline(Mline mline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(mline) && CheckLayerValid(mline))
            {
                var entitySet = new DBObjectCollection();
                mline.Explode(entitySet);
                entitySet.Cast<Entity>().ForEach(o => results.Add(CreateDistributionElementData(o.GetTransformedCopy(matrix))));
            }
            return results;
        }

        private List<ThRawIfcDistributionElementData> HandleArc(Arc arc, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(arc) && CheckLayerValid(arc))
            {
                results.Add(CreateDistributionElementData(arc.GetTransformedCopy(matrix)));
            }
            return results;
        }

        private List<ThRawIfcDistributionElementData> HandleCircle(Circle circle, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(circle) && CheckLayerValid(circle))
            {
                results.Add(CreateDistributionElementData(circle.GetTransformedCopy(matrix)));
            }
            return results;
        }

        private List<ThRawIfcDistributionElementData> HandleDBText(DBText dBText, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(dBText) && CheckLayerValid(dBText))
            {
                results.Add(CreateDistributionElementData(dBText.GetTransformedCopy(matrix)));
            }
            return results;
        }

        private List<ThRawIfcDistributionElementData> HandleMText(MText mtext, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(mtext) && CheckLayerValid(mtext))
            {
                results.Add(CreateDistributionElementData(mtext.GetTransformedCopy(matrix)));
            }
            return results;
        }

        private List<ThRawIfcDistributionElementData> HandleDimension(Dimension dimension, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(dimension) && CheckLayerValid(dimension))
            {
                results.Add(CreateDistributionElementData(dimension.GetTransformedCopy(matrix)));
            }
            return results;
        }
        private List<ThRawIfcDistributionElementData> HandleTCHElement(Entity tchElement, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(tchElement) && CheckLayerValid(tchElement))
            {
                results.Add(CreateDistributionElementData(tchElement.GetTransformedCopy(matrix)));
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

        public void SetContainers(List<string> containers)
        {
            _containers = containers;
        }
    }
}
