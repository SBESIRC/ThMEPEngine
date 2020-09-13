using System;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Algorithm
{
    [Flags]
    public enum BuildElement
    {
        Beam = 0b_0000_0000,
        Column = 0b_0000_0001,
        ShearWall = 0b_0000_0010,
        All = Beam | Column | ShearWall,
    }

    public class ThMEPModelManager : IDisposable
    {
        public ThMEPModelManager(Database database)
        {
            HostDb = database;
            BeamEngine = null;
            ColumnEngine = null;
            ShearWallEngine = null;
        }

        public void Dispose()
        {
        }

        public Database HostDb { get; private set; }
        public ThBeamRecognitionEngine BeamEngine { get; private set; }
        public ThColumnRecognitionEngine ColumnEngine { get; private set; }
        public ThShearWallRecognitionEngine ShearWallEngine { get; private set; }

        public void CreateSpatialIndex()
        {
            if (BeamEngine != null)
            {
                ThSpatialIndexManager.Instance.CreateBeamSpaticalIndex(BeamEngine.Collect());
            }
            if (ColumnEngine != null)
            {
                ThSpatialIndexManager.Instance.CreateColumnSpaticalIndex(ColumnEngine.Collect());
            }
            if (ShearWallEngine != null)
            {
                ThSpatialIndexManager.Instance.CreateWallSpaticalIndex(ShearWallEngine.Collect());
            }
        }

        public void Acquire(BuildElement elements)
        {
            Acquire(elements, new Point3dCollection());
        }

        public void Acquire(BuildElement elements, Point3dCollection polygon)
        {
            if ((elements & BuildElement.Beam) == BuildElement.Beam)
            {
                BeamEngine = new ThBeamRecognitionEngine();
                BeamEngine.Recognize(HostDb, polygon);
            }
            if ((elements & BuildElement.Column) == BuildElement.Column)
            {
                ColumnEngine = new ThColumnRecognitionEngine();
                ColumnEngine.Recognize(HostDb, polygon);
            }
            if ((elements & BuildElement.ShearWall) == BuildElement.ShearWall)
            {
                ShearWallEngine = new ThShearWallRecognitionEngine();
                ShearWallEngine.Recognize(HostDb, polygon);
            }
        }
    }
}
