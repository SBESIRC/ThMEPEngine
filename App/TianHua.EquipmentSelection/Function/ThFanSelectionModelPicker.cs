using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace TianHua.FanSelection.Function
{
    public interface IFanSelectionModelPicker
    {
        /// <summary>
        /// CCCF规格
        /// </summary>
        /// <returns></returns>
        string Model();
        /// <summary>
        /// 全压
        /// </summary>
        /// <returns></returns>
        double Pa();
        /// <summary>
        /// 风量
        /// </summary>
        /// <returns></returns>
        double AirVolume();
        /// <summary>
        /// 是否为最优
        /// </summary>
        /// <returns></returns>
        bool IsOptimalModel();
        /// <summary>
        ///  是否找到
        /// </summary>
        /// <returns></returns>
        bool IsFound();
    }

    public class ThFanSelectionModelPicker : IFanSelectionModelPicker
    { 
        private Point Point { get; set; }
        private Dictionary<Geometry, Point> Models { get; set; }
        public ThFanSelectionModelPicker(List<FanParameters> models,  FanDataModel fanmodel, List<double> point)
        {
            //若是后倾离心风机（目前后倾离心只有单速）
            IEqualityComparer<FanParameters> comparer = null;
            if (ThFanSelectionUtils.IsHTFCBackwardModelStyle(fanmodel.VentStyle))
            {
                comparer = new CCCFRpmComparer();
            }
            else
            {
                comparer = new CCCFComparer();
            }

            var coordinate = new Coordinate(
                     ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point[0]),
                     ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point[1])
                );
            Point = ThCADCoreNTSService.Instance.GeometryFactory.CreatePoint(coordinate);

            //当前项为高速档，需过滤掉gear档位为低的元素，保留高档位元素，反之过滤掉gear档位为高的元素
            if (fanmodel.IsMainModel())
            {
                Models = models.ToGeometries(comparer, "低").ModelPick(Point);
            }
            else
            {
                Models = models.ToGeometries(comparer, "高").ModelPick(Point);
            }
        }

        public string Model()
        {
            if (Models.Count > 0)
            { 
                string moelkeyname = Models.First().Key.UserData as string;
                return moelkeyname.Split('@')[0];
            }
            else
            {
                return string.Empty;
            }
        }

        public double Pa()
        {
            if (Models.Count > 0)
            {
                return Models.First().Value.Y;
            }
            else
            {
                return 0;
            }
        }

        public double AirVolume()
        {
            if (Models.Count > 0)
            {
                return Models.First().Value.X;
            }
            else
            {
                return 0;
            }
        }

        public bool IsOptimalModel()
        {
            return Models.First().Key.IsOptimalModel(Point);
        }

        public bool IsFound()
        {
            return Models.Count > 0;
        }

        public Geometry ModelGeometry()
        {
            if (Models.Count > 0)
            {
                return Models.First().Key;
            }
            else
            {
                return null;
            }
        }

    }

    public class ThFanSelectionAxialModelPicker : IFanSelectionModelPicker
    {
        private Point Point { get; set; }
        private Dictionary<Geometry, Point> Models { get; set; }
        public ThFanSelectionAxialModelPicker(List<AxialFanParameters> models, FanDataModel fanmodel, List<double> point)
        {
            IEqualityComparer<AxialFanParameters> comparer = new AxialModelNumberComparer();
            var coordinate = new Coordinate(
                     ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point[0]),
                     ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point[1])
                );
            Point = ThCADCoreNTSService.Instance.GeometryFactory.CreatePoint(coordinate);

            //当前项为高速档，需过滤掉gear档位为低的元素，保留高档位元素，反之过滤掉gear档位为高的元素
            if (fanmodel.IsMainModel())
            {
                Models = models.ToGeometries(comparer, "低").ModelPick(Point);
            }
            else
            {
                Models = models.ToGeometries(comparer, "高").ModelPick(Point);
            }
        }

        public string Model()
        {
            if (Models.Count > 0)
            {
                return Models.First().Key.UserData as string;
            }
            else
            {
                return string.Empty;
            }
        }

        public double Pa()
        {
            if (Models.Count > 0)
            {
                return Models.First().Value.Y;
            }
            else
            {
                return 0;
            }
        }

        public double AirVolume()
        {
            if (Models.Count > 0)
            {
                return Models.First().Value.X;
            }
            else
            {
                return 0;
            }
        }

        public bool IsOptimalModel()
        {
            return Models.First().Key.IsOptimalModel(Point);
        }

        public bool IsFound()
        {
            return Models.Count > 0;
        }

        public Geometry ModelGeometry()
        {
            if (Models.Count > 0)
            {
                return Models.First().Key;
            }
            else
            {
                return null;
            }
        }

    }
}
