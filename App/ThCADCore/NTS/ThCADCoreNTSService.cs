using GeoAPI.Geometries;
using NetTopologySuite;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThCADCoreNTSService instance = new ThCADCoreNTSService() { PrecisionReduce = false };
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThCADCoreNTSService() { }
        internal ThCADCoreNTSService() { }
        public static ThCADCoreNTSService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public double Scale { get; set; }
        public bool PrecisionReduce { get; set; }


        private IGeometryFactory geometryFactory;
        private IGeometryFactory defaultGeometryFactory;
        public IGeometryFactory GeometryFactory
        {
            get
            {
                if (PrecisionReduce)
                {
                    if (geometryFactory == null)
                    {
                        geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(PrecisionModel);
                    }
                    return geometryFactory;

                }
                else
                {
                    if (defaultGeometryFactory == null)
                    {
                        defaultGeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
                    }
                    return defaultGeometryFactory;
                }
            }
        }

        private IPrecisionModel precisionModel;
        public IPrecisionModel PrecisionModel
        {
            get
            {
                if (PrecisionReduce)
                {
                    if (precisionModel == null)
                    {
                        if (Scale == 0.0)
                        {
                            precisionModel = NtsGeometryServices.Instance.CreatePrecisionModel(PrecisionModels.FloatingSingle);
                        }
                        else
                        {
                            precisionModel = NtsGeometryServices.Instance.CreatePrecisionModel(Scale);
                        }
                    }
                    return precisionModel;
                }
                else
                {
                    return NtsGeometryServices.Instance.DefaultPrecisionModel;
                }
            }
        }
    }
}
