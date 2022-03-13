using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHDuct
    {
        private TCHDuctParam ductParam;
        private TCHDuctDimensionsParam ductDimensionParam;
        private TCHDuctDimContentsParam ductContentsParam;
        private ulong subSysId;
        private ThSQLiteHelper sqliteHelper;
        private const double lineLimition = 100;
        public ThDrawTCHDuct(ThSQLiteHelper sqliteHelper, ulong subSysId)
        {
            this.subSysId = subSysId;
            this.sqliteHelper = sqliteHelper;
        }
        public void DrawVerticalPipe(List<SegInfo> segInfos, Matrix3d mat, ref ulong gId)
        {
            sqliteHelper.Conn();
            foreach (var seg in segInfos)
            {
                RecordDuctInfo(seg.airVolume, ref gId);
                GetWidthAndHeight(seg.ductSize, out double width, out double height);
                var centerEleDisVec = seg.l.StartPoint.Z * Vector3d.ZAxis;
                var sp = new Point3d(seg.l.StartPoint.X, seg.l.StartPoint.Y, 0);
                var ep = new Point3d(seg.l.EndPoint.X, seg.l.EndPoint.Y, 0);
                // 低点向上指，高点向下指
                var sEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.startFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = new Vector3d(0, 0, 1),
                    heighVector = seg.horizontalVec,
                    centerPoint = sp.TransformBy(mat) + centerEleDisVec
                };
                centerEleDisVec =  + seg.l.EndPoint.Z * Vector3d.ZAxis;
                var eEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.endFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = new Vector3d(0, 0, -1),
                    heighVector = seg.horizontalVec,
                    centerPoint = ep.TransformBy(mat) + centerEleDisVec
                };
                ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { sEndParam, eEndParam });
            }
            sqliteHelper.db.Close();
        }
        public void DrawPortVerticalPipe(List<SegInfo> segInfos, Matrix3d mat, ThMEPHVACParam param, ref ulong gId)
        {
            sqliteHelper.Conn();
            var mmElevation = param.elevation * 1000;
            var mainHeight = ThMEPHVACService.GetHeight(param.inDuctSize);
            foreach (var seg in segInfos)
            {
                RecordDuctInfo(seg.airVolume, ref gId);
                GetWidthAndHeight(seg.ductSize, out double width, out double height);
                var centerEleDisVec = ThMEPHVACService.GetEleDis(mmElevation, mainHeight, seg.l.Length) + seg.l.StartPoint.Z * Vector3d.ZAxis;
                var selfEleOftVec = seg.l.Length * Vector3d.ZAxis;
                var sp = new Point3d(seg.l.StartPoint.X, seg.l.StartPoint.Y, 0);
                var ep = new Point3d(seg.l.EndPoint.X, seg.l.EndPoint.Y, 0);
                var dirVec = ThMEPHVACService.GetLeftVerticalVec(seg.horizontalVec);// 管子的方向转90°
                // 低点向上指，高点向下指
                var sEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.startFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = new Vector3d(0, 0, 1),
                    heighVector = dirVec,
                    centerPoint = sp.TransformBy(mat) + seg.l.StartPoint.Z * Vector3d.ZAxis
            };
                centerEleDisVec = ThMEPHVACService.GetEleDis(mmElevation, mainHeight, seg.l.Length) + seg.l.EndPoint.Z * Vector3d.ZAxis;
                var eEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.endFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = new Vector3d(0, 0, -1),
                    heighVector = dirVec,
                    centerPoint = ep.TransformBy(mat) + seg.l.EndPoint.Z * Vector3d.ZAxis
                };
                ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { sEndParam, eEndParam });
            }
            sqliteHelper.db.Close(); 
        }
        public void DrawVTDuct(List<SegInfo> segInfos, Matrix3d mat, bool isTextSide, ThMEPHVACParam param, ref ulong gId)
        {
            sqliteHelper.Conn();
            var gap = ThTCHCommonTables.flgThickness * 0.5;
            foreach (var seg in segInfos)
            {
                var l = seg.GetShrinkedLine();
                if (l.Length < lineLimition)
                    continue;
                RecordDuctInfo(seg.airVolume, ref gId);
                RecordDuctDimContents(ref gId);
                RecordDuctDimensions(mat, seg, param, isTextSide, ref gId);
                GetWidthAndHeight(seg.ductSize, out double width, out double height);
                var dirVec = (l.EndPoint - l.StartPoint).GetNormal();
                var sEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.startFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = (l.StartPoint.TransformBy(mat) + (gap * dirVec)),
                };
                var eEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.endFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = -dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = (l.EndPoint.TransformBy(mat) - (gap * dirVec))
                };
                ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { sEndParam, eEndParam });
            }
            sqliteHelper.db.Close();
        }
        public void DrawDuct(List<SegInfo> segInfos, Matrix3d mat, bool isTextSide, ThMEPHVACParam param, ref ulong gId)
        {
            sqliteHelper.Conn();
            var gap = ThTCHCommonTables.flgThickness * 0.5;
            var mmElevation = param.elevation * 1000;
            var mainHeight = ThMEPHVACService.GetHeight(param.inDuctSize);
            foreach (var seg in segInfos)
            {
                var l = seg.GetShrinkedLine();
                if (l.Length < lineLimition)
                    continue;
                RecordDuctInfo(seg.airVolume, ref gId);
                RecordDuctDimContents(ref gId);
                RecordDuctDimensions(mat, seg, param, isTextSide, ref gId);
                GetWidthAndHeight(seg.ductSize, out double width, out double height);
                var dirVec = (l.EndPoint - l.StartPoint).GetNormal();
                var centerEleDisVec = ThMEPHVACService.GetEleDis(mmElevation, mainHeight, height);
                var sEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.startFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = (l.StartPoint.TransformBy(mat) + (gap * dirVec)) + centerEleDisVec,
                };
                var eEndParam = new TCHInterfaceParam()
                {
                    ID = ductParam.endFaceID,
                    sectionType = ductParam.sectionType,
                    height = height,
                    width = width,
                    normalVector = -dirVec,
                    heighVector = new Vector3d(0, 0, 1),
                    centerPoint = (l.EndPoint.TransformBy(mat) - (gap * dirVec)) + centerEleDisVec
                };
                ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { sEndParam, eEndParam });
            }
            sqliteHelper.db.Close();
        }

        private void GetTextInfo(SegInfo seg, bool isTextSide, out double angle, out Point3d p)
        {
            ThMEPHVACService.GetLinePosInfo(seg.l, out angle, out Point3d midP);
            var dirVec = ThMEPHVACService.GetEdgeDirection(seg.l);
            var verticalVec = ThMEPHVACService.GetVerticalVec(dirVec);
            var ductWidth = ThMEPHVACService.GetWidth(seg.ductSize);
            var leaveDuctMat = verticalVec * (ductWidth * 0.5 + 250);
            p = isTextSide ? midP + leaveDuctMat : midP;
        }

        private void RecordDuctDimensions(Matrix3d mat, SegInfo seg, ThMEPHVACParam param, bool isTextSide, ref ulong gId)
        {
            var elevation = param.elevation;
            double ductHeight = ThMEPHVACService.GetHeight(seg.ductSize);
            double num = (elevation * 1000 + param.mainHeight - ductHeight) / 1000;
            var strElevation = num > 0 ? ("+" + num.ToString()) : num.ToString();
            GetTextInfo(seg, isTextSide, out double angle, out Point3d p);
            var baseP = p.TransformBy(mat);
            ductDimensionParam = new TCHDuctDimensionsParam()
            {
                ID = gId++,
                ductID = ductParam.ID,
                dimContentID = ductContentsParam.ID,
                subSystemID = ductParam.subSystemID,
                text = seg.ductSize + " h " + strElevation + "m |||",
                basePoint = baseP,
                leadPoint = baseP + new Vector3d(100, 0, 0),
                type = 1,
                eleType = 2,
                textAngle = angle,
                sysKey = 1,
            };
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.ductDimensions +
                          " VALUES ('" + ductDimensionParam.ID.ToString() + "'," +
                                  "'" + ductDimensionParam.ductID.ToString() + "'," +
                                  "'" + ductDimensionParam.dimContentID.ToString() + "'," +
                                  "'" + ductDimensionParam.subSystemID.ToString() + "'," +
                                  "'" + ductDimensionParam.text.ToString() + "'," +
                                  "'" + ThTCHService.CovertPoint(ductDimensionParam.basePoint) + "'," +
                                  "'" + ThTCHService.CovertPoint(ductDimensionParam.leadPoint) + "'," +
                                  "'" + ductDimensionParam.type.ToString() + "'," +
                                  "'" + ductDimensionParam.eleType.ToString() + "'," +
                                  "'" + ductDimensionParam.textAngle.ToString() + "'," +
                                  "'" + ductDimensionParam.sysKey.ToString() + "')";
            sqliteHelper.Query<TCHDuctDimensionsParam>(recordDuct);
        }

        private void RecordDuctInfo(double airVolume, ref ulong gId)
        {
            ductParam = new TCHDuctParam()
            {
                ID = gId++,
                startFaceID = gId++,
                endFaceID = gId++,
                subSystemID = subSysId,
                materialID = 0,
                sectionType = 0,
                ductType = 1,
                Soft = 0,
                Bulge = 0.0,
                AirLoad = airVolume
            };
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.ductTableName +
                          " VALUES ('" + ductParam.ID.ToString() + "'," +
                                  "'" + ductParam.startFaceID.ToString() + "'," +
                                  "'" + ductParam.endFaceID.ToString() + "'," +
                                  "'" + ductParam.subSystemID.ToString() + "'," +
                                  "'" + ductParam.materialID.ToString() + "'," +
                                  "'" + ductParam.sectionType.ToString() + "'," +
                                  "'" + ductParam.ductType.ToString() + "'," +
                                  "'" + ductParam.Soft.ToString() + "'," +
                                  "'" + ductParam.Bulge.ToString() + "'," +
                                  "'" + ductParam.AirLoad.ToString() + "')";
            sqliteHelper.Query<TCHDuctParam>(recordDuct);
        }
        private void RecordDuctDimContents(ref ulong gId)
        {
            ductContentsParam = new TCHDuctDimContentsParam()
            {
                ID = gId++,
                section = 1,
                haveElevation = 1,
                haveAirVolume = 0,
                haveVelocity = 0,
                wayResis = 0 
            };
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.ductDimContents +
                          " VALUES ('" + ductContentsParam.ID.ToString() + "'," +
                                  "'" + ductContentsParam.section.ToString() + "'," +
                                  "'" + ductContentsParam.haveElevation.ToString() + "'," +
                                  "'" + ductContentsParam.haveAirVolume.ToString() + "'," +
                                  "'" + ductContentsParam.haveVelocity.ToString() + "'," +
                                  "'" + ductContentsParam.wayResis.ToString() + "')";
            sqliteHelper.Query<TCHDuctDimContentsParam>(recordDuct);
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
