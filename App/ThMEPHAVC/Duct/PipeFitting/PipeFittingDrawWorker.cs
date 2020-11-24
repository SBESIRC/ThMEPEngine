using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHAVC.Duct.PipeFitting
{
    class PipeFittingDrawWorker
    {
        public void DrawPipeFitting(IPipeFitting pipefitting, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (Entity geometrie in pipefitting.Geometries)
                {
                    geometrie.TransformBy(matrix);
                    acadDatabase.ModelSpace.Add(geometrie);
                }
            }
        }
    }
}
