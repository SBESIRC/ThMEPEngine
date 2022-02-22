using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHTee
    {
        private TCHTeeParam teeParam;
        private ThSQLiteHelper sqliteHelper;
        private ThDrawTCHFlanges flangesService;

        public ThDrawTCHTee(ThSQLiteHelper sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
            flangesService = new ThDrawTCHFlanges(sqliteHelper);
        }

        public void Draw(EntityModifyParam info, Matrix3d mat, ref ulong gId)
        {
            sqliteHelper.Conn();
            var points = info.portWidths.Keys.ToList();
            var type = ThDuctPortsShapeService.GetTeeType(info.centerP, points[1], points[2]);
            RecordTeeInfo(type, ref gId);
            GetTeeInfo(info, mat, type, out TeeInfo teeInfo, out Point3d mainP, out Point3d branchP, out Point3d otherP);
            var cP = info.centerP.TransformBy(mat);
            var gap = ThTCHCommonTables.flgThickness * 0.5;
            var v = (mainP - cP).GetNormal();
            var param1 = new TCHInterfaceParam()
            {
                ID = teeParam.mainFaceID,
                sectionType = 0,
                height = 0,
                width = teeInfo.mainWidth,
                normalVector = v,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = mainP - v * gap
            };
            v = (branchP - cP).GetNormal();
            var param2 = new TCHInterfaceParam()
            {
                ID = teeParam.branchFaceID1,
                sectionType = 0,
                height = 0,
                width = teeInfo.branch,
                normalVector = v,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = branchP - v * gap
            };
            v = (otherP - cP).GetNormal();
            var param3 = new TCHInterfaceParam()
            {
                ID = teeParam.branchFaceID2,
                sectionType = 0,
                height = 0,
                width = teeInfo.other,
                normalVector = v,
                heighVector = new Vector3d(0, 0, 1),
                centerPoint = otherP - v * gap
            };
            flangesService.Draw(mainP, param1.normalVector, teeInfo.mainWidth, ref gId);
            flangesService.Draw(branchP, param2.normalVector, teeInfo.branch, ref gId);
            flangesService.Draw(otherP, param3.normalVector, teeInfo.other, ref gId);
            ThTCHService.RecordPortInfo(sqliteHelper, new List<TCHInterfaceParam>() { param1, param2, param3});
            sqliteHelper.db.Close();
        }

        private void GetTeeInfo(EntityModifyParam info, Matrix3d mat, TeeType type, out TeeInfo teeInfo, out Point3d mainP, out Point3d branchP, out Point3d otherP)
        {
            teeInfo = GetTeeInfo(info, type);
            var tee = ThDuctPortsFactory.CreateTee(teeInfo.mainWidth, teeInfo.branch, teeInfo.other, type);
            var transMat = mat * GetTransMat(teeInfo.trans);
            mainP = ThTCHService.GetMidPoint(tee.flg[0] as Line).TransformBy(transMat);
            branchP = ThTCHService.GetMidPoint(tee.flg[1] as Line).TransformBy(transMat);
            otherP = ThTCHService.GetMidPoint(tee.flg[2] as Line).TransformBy(transMat);
        }
        
        private static Matrix3d GetTransMat(TransInfo trans)
        {
            var p = new Point3d(trans.centerPoint.X, trans.centerPoint.Y, 0);
            var flipMat = trans.flip ? Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0))) : Matrix3d.Identity;
            var mat = Matrix3d.Displacement(p.GetAsVector()) *
                      Matrix3d.Rotation(trans.rotateAngle, -Vector3d.ZAxis, Point3d.Origin) * flipMat;
            return mat;
        }

        private TeeInfo GetTeeInfo(EntityModifyParam info, TeeType type)
        {
            var points = info.portWidths.Keys.ToList();
            var inVec = (points[0] - info.centerP).GetNormal();
            double rotateAngle = ThDuctPortsShapeService.GetTeeRotateAngle(inVec);
            double inWidth = info.portWidths[points[0]];
            var vec = (points[1] - info.centerP).GetNormal();
            var flag = (type == TeeType.BRANCH_VERTICAL_WITH_OTTER) ?
                        ThMEPHVACService.IsCollinear(inVec, vec) : inVec.CrossProduct(vec).Z > 0;
            var otherIdx = flag ? 1 : 2;
            var branchIdx = flag ? 2 : 1;
            var branchP = points[branchIdx];
            var otherP = points[otherIdx];
            double other = info.portWidths[otherP];
            double branch = info.portWidths[branchP];
            var branchVec = (points[branchIdx] - info.centerP).GetNormal();
            var judgeVec = (Vector3d.XAxis).RotateBy(rotateAngle, -Vector3d.ZAxis);
            var theta = branchVec.GetAngleTo(judgeVec);
            var tor = 5.0 / 180.0 * Math.PI;// 旁通和主管段夹角在5°内的三通
            var flip = false;
            if (theta > tor)
                flip = true;
            var trans = new TransInfo() { rotateAngle = rotateAngle, centerPoint = info.centerP, flip = flip };
            return new TeeInfo() { mainWidth = inWidth, branch = branch, other = other, trans = trans };
        }
        private void RecordTeeInfo(TeeType type, ref ulong gId)
        {
            teeParam = new TCHTeeParam()
            {
                ID = gId++,
                mainFaceID = gId++,
                branchFaceID1 = gId++,
                branchFaceID2 = gId++,
                subSystemID = 1,
                materialID = 0,
                type = (type == TeeType.BRANCH_VERTICAL_WITH_OTTER) ? 3 : 5,
                radRatio = 0.8
            };
            string recordDuct = (type == TeeType.BRANCH_VERTICAL_WITH_OTTER) ?
                         $"INSERT INTO " + ThTCHCommonTables.teeTableName +
                          " VALUES ('" + teeParam.ID.ToString() + "'," +
                                  "'" + teeParam.mainFaceID.ToString() + "'," +
                                  "'" + teeParam.branchFaceID2.ToString() + "'," +
                                  "'" + teeParam.branchFaceID1.ToString() + "'," +
                                  "'" + teeParam.subSystemID.ToString() + "'," +
                                  "'" + teeParam.materialID.ToString() + "'," +
                                  "'" + teeParam.type.ToString() + "'," +
                                  "'" + teeParam.radRatio.ToString() + "')" :
                         $"INSERT INTO " + ThTCHCommonTables.teeTableName +
                          " VALUES ('" + teeParam.ID.ToString() + "'," +
                                  "'" + teeParam.branchFaceID1.ToString() + "'," +
                                  "'" + teeParam.branchFaceID2.ToString() + "'," +
                                  "'" + teeParam.mainFaceID.ToString() + "'," +
                                  "'" + teeParam.subSystemID.ToString() + "'," +
                                  "'" + teeParam.materialID.ToString() + "'," +
                                  "'" + teeParam.type.ToString() + "'," +
                                  "'" + teeParam.radRatio.ToString() + "')";
            sqliteHelper.Query<TCHTeeParam>(recordDuct);
        }
    }
}