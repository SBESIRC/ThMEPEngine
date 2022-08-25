using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using ThCADCore.NTS;
using AcHelper;
using Dreambuild.AutoCAD;
using ThCADExtension;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;

using ThMEPHVAC.FloorHeatingCoil.Cmd;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Service;
using ThMEPHVAC.FloorHeatingCoil.Model;
using ThMEPHVAC.FloorHeatingCoil.Heating;
using Autodesk.AutoCAD.EditorInput;
using System.Text.RegularExpressions;

namespace ThMEPHVAC.FloorHeatingCoil.Service
{
    internal class ThFloorHeatingCoilUtilServices
    {
        public static ThFloorHeatingDataProcessService GetData(AcadDatabase acadDatabase, List<Polyline> selectFrames, ThMEPOriginTransformer transformer)
        {
            var dataFactory = new ThFloorHeatingDataFactory()
            {
                Transformer = transformer,
            };
            dataFactory.GetElements(acadDatabase.Database, new Point3dCollection());

            var dataQuery = new ThFloorHeatingDataProcessService()
            {
                WithUI = ThFloorHeatingCoilSetting.Instance.WithUI,
                InputExtractors = dataFactory.Extractors,
                FurnitureObstacleData = dataFactory.SanitaryTerminal,
                RoomSeparateLine = dataFactory.RoomSeparateLine,
                //RoomSuggestDist = dataFactory.RoomSuggestDist,
                WaterSeparatorData = dataFactory.WaterSeparator,
                BathRadiatorData = dataFactory.BathRadiator,
                FurnitureObstacleDataTemp = dataFactory.SenitaryTerminalOBBTemp,

            };
            dataQuery.ProcessDataWithRoom(selectFrames);

            return dataQuery;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        public static double GetNumberFromString(string power)
        {
            double resDouble = -1;
            if (power != null)
            {
                var reg = new Regex(@"[0-9]*[.]?[0-9]+");

                var str = reg.Match(power);
                if (str.Success)
                {
                    resDouble = double.Parse(str.Value);
                }
            }
            return resDouble;
        }

        public static void PassUserParameter(ThFloorHeatingCoilViewModel vm)
        {
            Parameter.PublicRegionConstraint = Convert.ToBoolean(vm.PublicRegionConstraint);
            Parameter.IndependentRoomConstraint = Convert.ToBoolean(vm.IndependentRoomConstraint);
            Parameter.AuxiliaryRoomConstraint = Convert.ToBoolean(vm.AuxiliaryRoomConstraint);
            Parameter.PrivatePublicMode = vm.PrivatePublicMode;
            Parameter.TotalLength = vm.TotalLenthConstraint * 1000;

        }
    }
}
