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
        private void MirrorDimension(ref double angle, ref Point3d firstDimPos, EndlineSegInfo seg)
        {
            // 天正数据库，将定位标注按管道走向做镜像
            angle += 180;
            firstDimPos = seg.portsInfo.FirstOrDefault().position;
        }
        private void InsertDirDimension(EndlineSegInfo seg, Point3d startPos, ref ulong gId)
        {
            if (!seg.dirAlignPoint.IsEqualTo(Point3d.Origin, new Tolerance(1e-3, 1e-3)))
                InsertDirWallPoint(seg);
            if (seg.portsInfo.Count() == 0)
                return;
            var disVec = startPos.GetAsVector();
            var dirVec = ThMEPHVACService.GetEdgeDirection(seg.seg.l);// 与x正方向求夹角
            var angle = Vector3d.XAxis.GetAngleTo(dirVec);
            angle = (angle < Math.PI && dirVec.Y < 0) ? 2 * Math.PI * 2 - angle : angle;
            angle = ThMEPHVACService.Radian2Angle(angle);
            var firstDimPos = seg.portsInfo.LastOrDefault().position;
            if (angle < 1e-3 || (angle - 90) < 1e-3 || dirVec.X > 0)
                MirrorDimension(ref angle, ref firstDimPos, seg);
            firstDimPos += disVec;
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
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.dimSegments +
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
                dimStyle = "TH-DIM150",
                location = firstDimPos,
                rotation = Math.Round(angle, 6),
                dist2DimLine = 1800,
                scale = 100,
            };
            gId = dimensionParam.segmentID + 1;// 把ID更新到Dimension的段数并为下一个ID准备
            string recordDuct = $"INSERT INTO " + ThTCHCommonTables.dimPt2Pts +
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
