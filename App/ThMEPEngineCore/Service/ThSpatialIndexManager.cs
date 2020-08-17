using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThSpatialIndexManager
    {
        private static readonly ThSpatialIndexManager instance = new ThSpatialIndexManager();
        static ThSpatialIndexManager() { }
        internal ThSpatialIndexManager() { }
        public static ThSpatialIndexManager Instance { get { return instance; } }
        public ThCADCoreNTSSpatialIndex WallSpatialIndex { get; private set; }
        public ThCADCoreNTSSpatialIndex ColumnSpatialIndex { get; private set; }
        public ThCADCoreNTSSpatialIndex BeamSpatialIndex { get; private set; }

        public void CreateWallSpaticalIndex(DBObjectCollection wallObjs)
        {
            WallSpatialIndex = ThSpatialIndexService.CreatWallSpatialIndex(wallObjs);
        }
        public void CreateBeamSpaticalIndex(DBObjectCollection beamObjs)
        {
            BeamSpatialIndex = ThSpatialIndexService.CreateBeamSpatialIndex(beamObjs);
        }
        public void CreateColumnSpaticalIndex(DBObjectCollection beamObjs)
        {
            ColumnSpatialIndex = ThSpatialIndexService.CreateBeamSpatialIndex(beamObjs);
        }
    }
}
