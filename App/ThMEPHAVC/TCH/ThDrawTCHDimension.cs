using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHDimension : ThDrawDimension
    {
        private ThSQLiteHelper sqliteHelper;
        private TCHDimensionParam dimensionParam;
        private TCHDimSegmentParam dimensionSegParam;
        public ThDrawTCHDimension(ThSQLiteHelper sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
        }

        public void DrawDimension(List<EndlineInfo> segInfos, PortParam portParam, ref ulong gId)
        {
            sqliteHelper.Conn();
            foreach (var endline in segInfos)
            {
                foreach (var seg in endline.endlines.Values)
                {
                    InsertDirDimension(seg, portParam.srtPoint, ref gId);
                }
            }
            sqliteHelper.db.Close();
        }

        private void InsertDirDimension(EndlineSegInfo seg, Point3d startPos, ref ulong gId)
        {
            if (!seg.dirAlignPoint.IsEqualTo(Point3d.Origin, new Tolerance(1e-3, 1e-3)))
                InsertDirWallPoint(seg);
            var disVec = startPos.GetAsVector();
            var dirVec = ThMEPHVACService.GetEdgeDirection(seg.seg.l);// 与x正方向求夹角
            var z = Vector3d.XAxis.CrossProduct(dirVec).Z;
            var angle = Vector3d.XAxis.GetAngleTo(dirVec);
            angle = z > 0 ? angle + Math.PI : angle;
            angle = ThMEPHVACService.Radian2Angle(angle);
            var firstDimPos = seg.portsInfo.FirstOrDefault().position + disVec;
            var segmentID = (ulong)seg.portsInfo.Count() - 1;
            RecordDimensionInfo(ref gId, segmentID, firstDimPos, angle);
            RecordDimensionSegInfo(seg, gId);
        }

        private void RecordDimensionSegInfo(EndlineSegInfo seg, ulong gId)
        {
            for (int i = 1; i < seg.portsInfo.Count(); ++i)
            {
                var portsInfo = seg.portsInfo;
                var srtP = portsInfo[i - 1].position;
                var endP = portsInfo[i].position;
                gId--;
                RecordDimensionInfo(gId, srtP, endP);
            }
        }
        private void RecordDimensionInfo(ulong gId, Point3d srtP, Point3d endP)
        {
            dimensionSegParam = new TCHDimSegmentParam()
            {
                ID = gId--,
                nextSegmentID = (long)gId,
                dimLength = srtP.DistanceTo(endP),
            };
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.dimPt2Pts +
                                 " VALUES ('" + dimensionSegParam.ID.ToString() + "'," +
                                 "'" + dimensionSegParam.nextSegmentID.ToString() + "'," +
                                 "'" + dimensionSegParam.dimLength.ToString() + "')";
            sqliteHelper.Query<TCHDimensionParam>(recordDuct);
        }
        private void RecordDimensionInfo(ref ulong gId, ulong segmentID, Point3d firstDimPos, double angle)
        {
            // 插入一条水平Dimension
            dimensionParam = new TCHDimensionParam()
            {
                ID = gId,
                segmentID = gId + segmentID,
                dimStyle = "_TCH_ARCH",
                location = firstDimPos,
                rotation = angle,
                dist2DimLine = 1800,
                scale = 100,
            };
            gId = dimensionParam.segmentID + 1;// 把ID更新到Dimension的段数并为下一个ID准备
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.dimSegments +
                          " VALUES ('" + dimensionParam.ID.ToString() + "'," +
                                  "'" +  dimensionParam.segmentID.ToString() + "'," +
                                  "'" +  dimensionParam.dimStyle.ToString() + "'," +
                                  "'" + ThTCHService.CovertPoint(dimensionParam.location) + "'," +
                                  "'" + dimensionParam.rotation.ToString() + "'," +
                                  "'" +  dimensionParam.dist2DimLine.ToString() + "'," +
                                  "'" +  dimensionParam.scale.ToString() + "')";
            sqliteHelper.Query<TCHDimensionParam>(recordDuct);
        }
    }
}
