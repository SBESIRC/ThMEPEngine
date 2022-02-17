using System;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHDuct
    {
        public ulong incID;                // 记录入风管表
        public string recordDuct;          // 记录入风管表
        public string recordDuctSrtInfo;   // 记录起始点到接口表
        public string recordDuctEndInfo;   // 记录末尾点到接口表
        private TCHDuctParam ductParam;
        public ThDrawTCHDuct(ulong idIncreaser)
        {
            ductParam = new TCHDuctParam() { 
                ID = idIncreaser++,
                endFaceID = idIncreaser++,
                startFaceID = idIncreaser++,
                subSystemID = 0,
                materialID = 0,
                sectionType = 0,
                ductType = 0,
                Soft = 0,
                Bulge = 0.0,
                AirLoad = 10000.0 };
            recordDuct = $"INSERT INTO " + ThTCHCommonTables.ductTableName +
                          "VALUES ('" + ductParam.ID.ToString() + "'" +
                                  "'" + ductParam.startFaceID.ToString() + "'" +
                                  "'" + ductParam.endFaceID.ToString() + "'" +
                                  "'" + ductParam.subSystemID.ToString() + "'" +
                                  "'" + ductParam.materialID.ToString() + "'" +
                                  "'" + ductParam.sectionType.ToString() + "'" +
                                  "'" + ductParam.ductType.ToString() + "'" +
                                  "'" + ductParam.Soft.ToString() + "'" +
                                  "'" + ductParam.Bulge.ToString() + "'" +
                                  "'" + ductParam.AirLoad.ToString() + "')";
            incID = idIncreaser;
        }
        public void GetInsertStatement(SegInfo segInfo)
        {
            GetWidthAndHeight(segInfo.ductSize, out double width, out double height);
            var l = segInfo.GetShrinkedLine();
            var dirVec = (l.EndPoint - l.StartPoint).GetNormal();
            var sEndParam = new TCHInterfaceParam()
            {
                ID = ductParam.startFaceID,
                sectionType = ductParam.sectionType,
                height = height,
                width = width,
                normalVector = dirVec,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = l.StartPoint };
            var eEndParam = new TCHInterfaceParam()
            {
                ID = ductParam.endFaceID,
                sectionType = ductParam.sectionType,
                height = height,
                width = width,
                normalVector = -dirVec,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = l.EndPoint };
            RecordPortInfo(sEndParam, eEndParam);
        }
        private void RecordPortInfo(TCHInterfaceParam sEndParam, TCHInterfaceParam eEndParam)
        {
            recordDuctSrtInfo = $"INSERT INTO " + ThTCHCommonTables.interfaceTableName +
                                 "VALUES ('" + sEndParam.ID.ToString() + "'" +
                                         "'" + sEndParam.sectionType.ToString() + "'" +
                                         "'" + sEndParam.height.ToString() + "'" +
                                         "'" + sEndParam.width.ToString() + "'" +
                                         "'" + sEndParam.normalVector.ToString() + "'" +
                                         "'" + sEndParam.heighVector.ToString() + "'" +
                                         "'" + sEndParam.centerPoint.ToString() + "')";
            recordDuctEndInfo = $"INSERT INTO " + ThTCHCommonTables.interfaceTableName +
                                 "VALUES ('" + eEndParam.ID.ToString() + "'" +
                                         "'" + eEndParam.sectionType.ToString() + "'" +
                                         "'" + eEndParam.height.ToString() + "'" +
                                         "'" + eEndParam.width.ToString() + "'" +
                                         "'" + eEndParam.normalVector.ToString() + "'" +
                                         "'" + eEndParam.heighVector.ToString() + "'" +
                                         "'" + eEndParam.centerPoint.ToString() + "')";
        }
        private static void GetWidthAndHeight(string size, out double width, out double height)
        {
            string[] s = size.Split('x');
            if (s.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            width = Double.Parse(s[0]);
            height = Double.Parse(s[1]);
        }
    }
}
