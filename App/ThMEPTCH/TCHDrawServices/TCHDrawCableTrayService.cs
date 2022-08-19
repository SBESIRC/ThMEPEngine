using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;

using ThMEPTCH.Model;
using ThCADExtension;
using ThMEPTCH.TCHTables;

namespace ThMEPTCH.TCHDrawServices
{
    public class TCHDrawCableTrayService : TCHDrawServiceBase
    {
        public List<ThTCHCableTray> CableTrays;
        public List<ThTCHElbow> Elbows;
        public List<ThTCHTee> Tees;
        public List<ThTCHCross> Crosses;

        public TCHDrawCableTrayService()
        {
            CableTrays = new List<ThTCHCableTray>();
            Elbows = new List<ThTCHElbow>();
            Tees = new List<ThTCHTee>();
            Crosses = new List<ThTCHCross>();
            TCHDBPath = Path.GetTempPath() + "TG20E.db";
            TCHTemplateDBPath = ThCADCommon.TCHELECDBPath();
            ClearDataTables.Add("TelecObject");
            ClearDataTables.Add("TelecInterface");
            ClearDataTables.Add("TelecClapboard");
            ClearDataTables.Add("TelecCabletry");
            ClearDataTables.Add("TelecElbow");
            ClearDataTables.Add("TelecTee");
            ClearDataTables.Add("TelecCross");
        }

        protected override string CmdName => "TH2T20";

        protected override void WriteModelToTCHDatabase()
        {
            if (null == CableTrays || CableTrays.Count() < 1)
            {
                return;
            }
            ulong objectId = 1000000;
            ulong startInterfaceId = 1000000;
            ulong clapboardId = 1000000;
            // 导入桥架
            foreach (var cableTray in CableTrays)
            {
                var telecObject = CreateTelecObject(cableTray.ObjectId, objectId);
                WriteModelToTCH(telecObject, ThMEPTCHCommon.TCHTableName_TelecObject, ref objectId);

                var startInterface = CreateTelecInterface(cableTray.StartInterface, startInterfaceId);
                WriteModelToTCH(startInterface, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);
                var endInterface = CreateTelecInterface(cableTray.EndInterface, startInterfaceId);
                WriteModelToTCH(endInterface, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);

                var clapboard = CreateTelecClapboard(cableTray.Clapboard, clapboardId);
                WriteModelToTCH(clapboard, ThMEPTCHCommon.TCHTableName_TelecClapboard, ref clapboardId);

                var telecCableTray = CreateTelecCableTray(cableTray, telecObject.ObjectId, clapboard.ClapboardId, startInterface.InterfaceId, endInterface.InterfaceId);
                WriteModelToTCH(telecCableTray, ThMEPTCHCommon.TCHTableName_TelecCabletry, ref objectId);
                objectId--;
            }
            // 导入弯头
            foreach (var elbow in Elbows)
            {
                var telecObject = CreateTelecObject(elbow.ObjectId, objectId);
                WriteModelToTCH(telecObject, ThMEPTCHCommon.TCHTableName_TelecObject, ref objectId);

                var startInterface = CreateTelecInterface(elbow.MajInterfaceId, startInterfaceId);
                WriteModelToTCH(startInterface, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);
                var endInterface = CreateTelecInterface(elbow.MinInterfaceId, startInterfaceId);
                WriteModelToTCH(endInterface, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);

                var clapboard = CreateTelecClapboard(elbow.Clapboard, clapboardId);
                WriteModelToTCH(clapboard, ThMEPTCHCommon.TCHTableName_TelecClapboard, ref clapboardId);

                var telecElbow = CreateTelecElbow(elbow, telecObject.ObjectId, clapboard.ClapboardId, startInterface.InterfaceId, endInterface.InterfaceId);
                WriteModelToTCH(telecElbow, ThMEPTCHCommon.TCHTableName_TelecElbow, ref objectId);
                objectId--;
            }
            // 导入三通
            foreach (var tee in Tees)
            {
                var telecObject = CreateTelecObject(tee.ObjectId, objectId);
                WriteModelToTCH(telecObject, ThMEPTCHCommon.TCHTableName_TelecObject, ref objectId);

                var startInterface = CreateTelecInterface(tee.MajInterfaceId, startInterfaceId);
                WriteModelToTCH(startInterface, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);
                var endInterface = CreateTelecInterface(tee.MinInterfaceId, startInterfaceId);
                WriteModelToTCH(endInterface, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);
                var endInterface2 = CreateTelecInterface(tee.Min2InterfaceId, startInterfaceId);
                WriteModelToTCH(endInterface2, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);

                var clapboard = CreateTelecClapboard(tee.Clapboard, clapboardId);
                WriteModelToTCH(clapboard, ThMEPTCHCommon.TCHTableName_TelecClapboard, ref clapboardId);

                var telecElbow = CreateTelecTee(tee, telecObject.ObjectId, clapboard.ClapboardId, startInterface.InterfaceId,
                    endInterface.InterfaceId, endInterface2.InterfaceId);
                WriteModelToTCH(telecElbow, ThMEPTCHCommon.TCHTableName_TelecTee, ref objectId);
                objectId--;
            }
            // 导入四通
            foreach (var cross in Crosses)
            {
                var telecObject = CreateTelecObject(cross.ObjectId, objectId);
                WriteModelToTCH(telecObject, ThMEPTCHCommon.TCHTableName_TelecObject, ref objectId);

                var startInterface = CreateTelecInterface(cross.MajInterfaceId, startInterfaceId);
                WriteModelToTCH(startInterface, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);
                var endInterface = CreateTelecInterface(cross.MinInterfaceId, startInterfaceId);
                WriteModelToTCH(endInterface, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);
                var endInterface2 = CreateTelecInterface(cross.Min2InterfaceId, startInterfaceId);
                WriteModelToTCH(endInterface2, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);
                var endInterface3 = CreateTelecInterface(cross.Min3InterfaceId, startInterfaceId);
                WriteModelToTCH(endInterface3, ThMEPTCHCommon.TCHTableName_TelecInterface, ref startInterfaceId);

                var clapboard = CreateTelecClapboard(cross.Clapboard, clapboardId);
                WriteModelToTCH(clapboard, ThMEPTCHCommon.TCHTableName_TelecClapboard, ref clapboardId);

                var telecCross = CreateTelecCross(cross, telecObject.ObjectId, clapboard.ClapboardId, startInterface.InterfaceId,
                    endInterface.InterfaceId, endInterface2.InterfaceId, endInterface3.InterfaceId);
                WriteModelToTCH(telecCross, ThMEPTCHCommon.TCHTableName_TelecCross, ref objectId);
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

        private TCHTelecClapboard CreateTelecClapboard(ThTCHTelecClapboard clapboard, ulong id)
        {
            return new TCHTelecClapboard
            {
                ClapboardId = id,
                HaveClapboard = Convert.ToInt32(clapboard.HaveClapboard),
            };
        }

        private TCHTelecCableTray CreateTelecCableTray(ThTCHCableTray cableTray, ulong objectId, ulong clapboardId, ulong startInterfaceId, ulong endInterfaceId)
        {
            return new TCHTelecCableTray
            {
                ObjectId = objectId,
                Type = cableTray.Type,
                Style = Convert.ToInt32(cableTray.Style),
                CabletraySystem = cableTray.CableTraySystem.ToString(),
                Height = cableTray.Height,
                Cover = Convert.ToInt32(cableTray.Cover),
                ClapboardId = clapboardId,
                StartInterfaceId = startInterfaceId,
                EndInterfaceId = endInterfaceId,
            };
        }

        private TCHTelecElbow CreateTelecElbow(ThTCHElbow elbow, ulong objectId, ulong clapboardId, ulong majInterfaceId, ulong minInterfaceId)
        {
            return new TCHTelecElbow
            {
                ObjectId = objectId,
                Type = elbow.Type,
                ElbowStyle = elbow.ElbowStyle,
                Style = Convert.ToInt32(elbow.Style),
                CabletraySystem = elbow.CableTraySystem.ToString(),
                Height = elbow.Height,
                Length = elbow.Length,
                Cover = Convert.ToInt32(elbow.Cover),
                ClapboardId = clapboardId,
                MidPosition = DataConvert(elbow.MidPosition),
                MajInterfaceId = majInterfaceId,
                MinInterfaceId = minInterfaceId,
            };
        }

        private TCHTelecTee CreateTelecTee(ThTCHTee tee, ulong objectId, ulong clapboardId, ulong majInterfaceId, ulong minInterfaceId, ulong min2InterfaceId)
        {
            return new TCHTelecTee
            {
                ObjectId = objectId,
                Type = tee.Type,
                TeeStyle = tee.TeeStyle,
                Style = Convert.ToInt32(tee.Style),
                CabletraySystem = tee.CableTraySystem.ToString(),
                Height = tee.Height,
                Length = tee.Length,
                Length2 = tee.Length2,
                Cover = Convert.ToInt32(tee.Cover),
                ClapboardId = clapboardId,
                MidPosition = DataConvert(tee.MidPosition),
                MajInterfaceId = majInterfaceId,
                MinInterfaceId = minInterfaceId,
                Min2InterfaceId = min2InterfaceId,
            };
        }

        private TCHTelecCross CreateTelecCross(ThTCHCross cross, ulong objectId, ulong clapboardId, ulong majInterfaceId, ulong minInterfaceId, ulong min2InterfaceId, ulong min3InterfaceId)
        {
            return new TCHTelecCross
            {
                ObjectId = objectId,
                Type = cross.Type,
                CrossStyle = cross.CrossStyle,
                Style = Convert.ToInt32(cross.Style),
                CabletraySystem = cross.CableTraySystem.ToString(),
                Height = cross.Height,
                Length = cross.Length,
                Cover = Convert.ToInt32(cross.Cover),
                ClapboardId = clapboardId,
                MidPosition = DataConvert(cross.MidPosition),
                InclineFit = Convert.ToInt32(cross.InclineFit),
                MajInterfaceId = majInterfaceId,
                MinInterfaceId = minInterfaceId,
                InterfaceId3 = min2InterfaceId,
                InterfaceId4 = min3InterfaceId,
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
            return Convert.ToDouble(value).ToString();
        }
    }
}
