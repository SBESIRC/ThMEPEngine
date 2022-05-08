using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPTCH.Engine
{
    public class ThTCHArchWallExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            elements.AddRange(HandleTCHArchWall(dbObj, matrix));
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            // 暂时不支持对三维图元的裁剪
        }

        public override bool IsBuildElement(Entity entity)
        {
            return entity.IsTCHArchWall();
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }

        private List<ThRawIfcBuildingElementData> HandleTCHArchWall(Entity wall, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(wall) && CheckLayerValid(wall))
            {
                var solid3d = Explode2Solid3d(wall);
                if (solid3d != null)
                {
                    solid3d.TransformBy(matrix);
                    results.Add(new ThRawIfcBuildingElementData()
                    {
                        Geometry = solid3d,
                    });
                }
            }
            return results;
        }

        private Solid3d Explode2Solid3d(Entity wall)
        {
            return wall.ExplodeTCHElement()
                .OfType<Solid3d>()
                .FirstOrDefault();
        }
    }
}
