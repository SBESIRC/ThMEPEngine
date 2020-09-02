using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPModelManager
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThMEPModelManager instance = new ThMEPModelManager() { };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThMEPModelManager() { }
        internal ThMEPModelManager() { }
        public static ThMEPModelManager Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public ThBeamRecognitionEngine BeamEngine { get; set; }
        public ThColumnRecognitionEngine ColumnEngine { get; set; }
        public ThShearWallRecognitionEngine ShearWallEngine { get; set; }
        private ThCADCoreNTSSpatialIndex SpaialIndex { get; set; }

        public void Initialize()
        {
            BeamEngine = new ThBeamRecognitionEngine();
            ColumnEngine = new ThColumnRecognitionEngine();
            ShearWallEngine = new ThShearWallRecognitionEngine();
        }

        public void LoadFromDatabase(Database database)
        {
            BeamEngine.Recognize(database);
            ColumnEngine.Recognize(database);
            ShearWallEngine.Recognize(database);
        }

        public void CreateSpatialIndex()
        {
            ThSpatialIndexManager.Instance.CreateBeamSpaticalIndex(BeamEngine.Collect()); 
            ThSpatialIndexManager.Instance.CreateColumnSpaticalIndex(ColumnEngine.Collect());
            ThSpatialIndexManager.Instance.CreateWallSpaticalIndex(ShearWallEngine.Collect());
        }
    }
}
