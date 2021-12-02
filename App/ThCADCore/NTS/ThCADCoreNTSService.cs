using System;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

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
        public bool PrecisionReduce { get; set; }

        public double AcadGlobalTolerance = 1e-8;
        public double ChordHeightTolerance = 50.0;
        public double ArcTessellationLength { get; set; } = 1000.0;

        private GeometryFactory geometryFactory;
        private GeometryFactory defaultGeometryFactory;
        public GeometryFactory GeometryFactory
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

        private PreparedGeometryFactory preparedGeometryFactory;
        public PreparedGeometryFactory PreparedGeometryFactory
        {
            get
            {
                if (preparedGeometryFactory == null)
                {
                    preparedGeometryFactory = new PreparedGeometryFactory();
                }
                return preparedGeometryFactory;
            }
        }

        private Lazy<PrecisionModel> precisionModel;
        public PrecisionModel PrecisionModel
        {
            get
            {
                if (PrecisionReduce)
                {
                    if (precisionModel == null)
                    {
                        precisionModel = PrecisionModel.Fixed;
                    }
                    return precisionModel.Value;
                }
                else
                {
                    return NtsGeometryServices.Instance.DefaultPrecisionModel;
                }
            }
        }
    }
}
