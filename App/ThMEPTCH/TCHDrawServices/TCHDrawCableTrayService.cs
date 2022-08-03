using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThMEPTCH.Model;
using ThMEPTCH.TCHTables;

namespace ThMEPTCH.TCHDrawServices
{
    public class TCHDrawCableTrayService : TCHDrawServiceBase
    {
        public List<ThTCHCableTray> CableTrays;

        public TCHDrawCableTrayService()
        {
            CableTrays = new List<ThTCHCableTray>();
            TCHDBPath = Path.GetTempPath() + "TG20.db";
            ClearDataTables.Add("TelecObject");
            ClearDataTables.Add("TelecInterface");
            ClearDataTables.Add("TelecClapboard");
            ClearDataTables.Add("TelecCabletry");
        }

        protected override string CmdName => "TImportTg20Hvac";

        protected override void WriteModelToTCHDatabase()
        {
            if (null == CableTrays || CableTrays.Count() < 1)
            {
                return;
            }
            ulong objectId = 1000000;
            ulong startInterfaceId = 1000000;
            ulong endInterfaceId = 1000001;
            foreach (var cableTray in CableTrays)
            {
                var telecObject = CreateTelecObject(cableTray.ObjectId, objectId);
                WriteModelToTCH(telecObject, ThMEPTCHCommon.TCHTableName_TelecObject, ref objectId);

                var startInterface = CreateTelecInterface(cableTray.StartInterfaceId, startInterfaceId);
                WriteModelToTCH(startInterface, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);
                startInterfaceId++;
                var endInterface = CreateTelecInterface(cableTray.EndInterfaceId, endInterfaceId);
                WriteModelToTCH(endInterface, ThMEPTCHCommon.TCHTableName_TelecInterface, ref endInterfaceId);
                endInterfaceId++;

                var telecCableTray = CreateTelecCableTray(cableTray, telecObject.ObjectId, startInterface.InterfaceId, endInterface.InterfaceId);
                WriteModelToTCH(telecCableTray, ThMEPTCHCommon.TCHTableName_TelecCabletry, ref objectId);
                objectId--;
            }
        }

        private TCHTelecObject CreateTelecObject(ThTCHTelecObject obj, ulong id)
        {
            return new TCHTelecObject
            {
                ObjectId = id,
                ObjectType = Convert.ToInt32(obj.Type),
            };
        }

        private TCHTelecInterface CreateTelecInterface(ThTCHTelecInterface thInterface, ulong id)
        {
            return new TCHTelecInterface
            {
                InterfaceId = id,
                Position = DataConvert(thInterface.Position),
                Breadth = thInterface.Breadth,
                Normal = DataConvert(thInterface.Normal),
                Direction = DataConvert(thInterface.Direction),
            };
        }

        private TCHTelecCableTray CreateTelecCableTray(ThTCHCableTray cableTray, ulong objectId, ulong startInterfaceId, ulong endInterfaceId)
        {
            return new TCHTelecCableTray
            {
                ObjectId = objectId,
                Type = cableTray.Type,
                Style = Convert.ToInt32(cableTray.Style),
                CabletraySystem = cableTray.CableTraySystem.GetDescription(),
                Height = cableTray.Height,
                Cover = Convert.ToInt32(cableTray.Cover),
                ClapboardId = null,
                StartInterfaceId = startInterfaceId,
                EndInterfaceId = endInterfaceId,
            };
        }

        private string DataConvert(Point3d position)
        {
            var x = DataConvert(position.X);
            var y = DataConvert(position.Y);
            var z = DataConvert(position.Z);
            return "X:" + x + ";" + "Y:" + y + ";" + "Z:" + z + ";";
        }

        private string DataConvert(Vector3d normal)
        {
            var x = DataConvert(normal.X);
            var y = DataConvert(normal.Y);
            var z = DataConvert(normal.Z);
            return "X:" + x + ";" + "Y:" + y + ";" + "Z:" + z + ";";
        }

        private string DataConvert(double value)
        {
            return Convert.ToInt32(value).ToString();
        }
    }
}
