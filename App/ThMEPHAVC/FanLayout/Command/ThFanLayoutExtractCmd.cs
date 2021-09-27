using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.DrawService;
using ThMEPHVAC.FanLayout.Model;
using ThMEPHVAC.FanLayout.Service;
using ThMEPHVAC.FanLayout.ViewModel;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.FanLayout.Command
{
    public class ThFanLayoutExtractCmd : IAcadCommand, IDisposable
    {
        public ThFanLayoutConfigInfo thFanLayoutConfigInfo { set; get; }
        public ThFanLayoutExtractCmd()
        {
        }
        public void Dispose()
        {
        }
        public static void FocusMainWindow()
        {
#if ACAD_ABOVE_2014
            Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Focus();
#else
FocusToCAD();
#endif
        }
        public static void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
        public void Execute()
        {
            FocusMainWindow();
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                switch (thFanLayoutConfigInfo.FanType)
                {
                    case 0://壁式轴流风机
                        InsertWAFFan(database, thFanLayoutConfigInfo.WAFConfigInfo, thFanLayoutConfigInfo.MapScale, thFanLayoutConfigInfo.IsInsertHole);
                        break;
                    case 1://壁式排气扇
                        InsertWEXHFan(database, thFanLayoutConfigInfo.WEXHConfigInfo, thFanLayoutConfigInfo.MapScale, thFanLayoutConfigInfo.IsInsertHole);
                        break;
                    case 2://吊顶式排气扇
                        InsertCEXHFan(database, thFanLayoutConfigInfo.CEXHConfigInfo, thFanLayoutConfigInfo.MapScale, thFanLayoutConfigInfo.IsInsertHole);
                        break;
                    default:
                        break;
                }
            }
        }

        public void InsertDuct()
        {
            var point1 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point1.Status != PromptStatus.OK)
            {
                return;
            }
            var point2 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point2.Status != PromptStatus.OK)
            {
                return;
            }
            Vector3d vector = point1.Value.GetVectorTo(point2.Value).GetNormal();
            ThMEPHVACDrawService drawService = new ThMEPHVACDrawService("平时排风","1:100", point1.Value, vector);
            Duct_modify_param param = new Duct_modify_param("120x120",100,2.5, point1.Value.ToPoint2D(), point2.Value.ToPoint2D());
            drawService.Draw_duct(param, Matrix3d.Identity);
        }

        /// <summary>
        /// 壁式轴流风机
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <param name="info"></param>
        private void InsertWAFFan(AcadDatabase acadDatabase, ThFanWAFConfigInfo info,string mapScale,bool isInsertHole)
        {
            var point1 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point1.Status != PromptStatus.OK)
            {
                return;
            }
            var point2 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point2.Status != PromptStatus.OK)
            {
                return;
            }
            Vector3d basVector = new Vector3d(1, 0, 0);
            Vector3d refVector = new Vector3d(0, 0, 1);
            Vector3d vector = point2.Value.GetVectorTo(point1.Value).GetNormal();
            double fanAngle = basVector.GetAngleTo(vector, refVector) - Math.PI/2.0;
            double fontScale = ThFanLayoutDealService.GetFontHeight(1, mapScale);
            double fontHeight = ThFanLayoutDealService.GetFontHeight(0, mapScale);
            string strFanVolume = ThFanLayoutDealService.GetFanVolume(info.FanConfigInfo.FanVolume);
            string strFanPower = ThFanLayoutDealService.GetFanPower(info.FanConfigInfo.FanPower);
            string strFanWeight = ThFanLayoutDealService.GetFanWeight(info.FanConfigInfo.FanWeight);
            string strFanNoise = ThFanLayoutDealService.GetFanNoise(info.FanConfigInfo.FanNoise);
            string strFanMark = ThFanLayoutDealService.GetFanHoleMark(info.FanMarkHeigthType,info.FanMarkHeight);
            //插入风机
            InsertWAFFan(acadDatabase, point1.Value,fanAngle, fontHeight, info.FanConfigInfo.FanDepth
                , info.FanConfigInfo.FanWidth, info.FanConfigInfo.FanLength, info.FanConfigInfo.FanNumber
                , strFanVolume, strFanPower, strFanWeight, strFanNoise, strFanMark);
            //插入墙洞
            if (isInsertHole)
            {
                string strFanHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.FanConfigInfo.FanWidth, info.FanConfigInfo.FanLength);
                string strFanHoleMark = ThFanLayoutDealService.GetFanHoleMark(info.FanMarkHeigthType, info.FanMarkHeight - 0.05);
                InsertFanHole(acadDatabase, point1.Value, fanAngle, fontHeight, info.FanConfigInfo.FanWidth + 100.0, strFanHoleSize, strFanHoleMark);
            }
            //插入防火阀
            Point3d pt = point1.Value - vector * (info.FanConfigInfo.FanDepth - 10);//沿着vector反方向平移电机深度
            Vector3d tmpV = new Vector3d(Math.Cos(fanAngle + Math.PI), Math.Sin(fanAngle + Math.PI), 0.0);//沿着vector垂直方向平移电机宽度
            pt = pt + (tmpV * info.FanConfigInfo.FanWidth / 2.0);
            InsertFireValve(acadDatabase, pt, fanAngle, fontHeight, info.FanConfigInfo.FanWidth, "70度防火阀FD");

            if(!info.IsInsertAirPort)
            {
                return;
            }

            var point3 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point3.Status != PromptStatus.OK)
            {
                return;
            }
            var point4= Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point4.Status != PromptStatus.OK)
            {
                return;
            }

            //插入风口
            Vector3d vector1 = point4.Value.GetVectorTo(point3.Value).GetNormal();
            double airPortAngle = basVector.GetAngleTo(vector1, refVector) + Math.PI / 2.0;
            InsertAirPort(acadDatabase, point3.Value, airPortAngle, info.AirPortLength, info.AirPortDeepth, "侧送风口", 0);
            //插入墙洞
            if (isInsertHole)
            {
                string strAirPortHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.AirPortLength, info.AirPortHeight);
                string strAirPortHoleMark = ThFanLayoutDealService.GetFanHoleMark(info.AirPortMarkHeigthType, info.AirPortMarkHeight + 0.05);
                InsertFanHole(acadDatabase, point3.Value, airPortAngle - Math.PI, fontHeight, info.AirPortLength + 100, strAirPortHoleSize, strAirPortHoleMark);
            }
            //插入防火阀
            if (info.IsInsertValve)
            {
                Point3d pt1 = point3.Value - vector1 * info.AirPortDeepth;//沿着vector1反方向平移百叶深度
                Vector3d tmpV1 = new Vector3d(Math.Cos(airPortAngle), Math.Sin(airPortAngle), 0.0);//沿着vector1垂直方向平移百叶长度
                pt1 = pt1 + (tmpV1 * info.AirPortLength / 2.0);
                InsertFireValve(acadDatabase, pt1, airPortAngle - Math.PI, fontHeight, info.AirPortLength, "70度防火阀FD");
            }

            //插入风口标记
            Vector3d vector2 = new Vector3d(400, 1900, 0);
            Point3d p2 = point3.Value + vector2;
            string strAirPortMark = ThFanLayoutDealService.GetAirPortMarkSize(info.AirPortLength, info.AirPortHeight);
            string strAirPortMarkVolume = ThFanLayoutDealService.GetAirPortMarkVolume(info.FanConfigInfo.FanVolume);
            InsertAirPortMark(acadDatabase, point3.Value, p2, fontScale, "AH", strAirPortMark, "1", strAirPortMarkVolume);
        }
        /// <summary>
        /// 插入壁式排气扇
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <param name="info"></param>
        private void InsertWEXHFan(AcadDatabase acadDatabase, ThFanWEXHConfigInfo info, string mapScale, bool isInsertHole)
        {
            var point1 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point1.Status != PromptStatus.OK)
            {
                return;
            }
            var point2 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point2.Status != PromptStatus.OK)
            {
                return;
            }
            Vector3d basVector = new Vector3d(1, 0, 0);
            Vector3d refVector = new Vector3d(0, 0, 1);
            Vector3d vector = point2.Value.GetVectorTo(point1.Value).GetNormal();
            double fanAngle = basVector.GetAngleTo(vector, refVector) - Math.PI / 2.0;
            double fontScale = ThFanLayoutDealService.GetFontHeight(1, mapScale);
            double fontHeight = ThFanLayoutDealService.GetFontHeight(0, mapScale);
            string strFanVolume = ThFanLayoutDealService.GetFanVolume(info.FanConfigInfo.FanVolume);
            string strFanPower = ThFanLayoutDealService.GetFanPower(info.FanConfigInfo.FanPower);
            string strFanWeight = ThFanLayoutDealService.GetFanWeight(info.FanConfigInfo.FanWeight);
            string strFanNoise = ThFanLayoutDealService.GetFanNoise(info.FanConfigInfo.FanNoise);
            string strFanMark = ThFanLayoutDealService.GetFanHoleMark(info.FanMarkHeigthType, info.FanMarkHeight);
            //插入风机
            InsertWEXHFan(acadDatabase, point1.Value, fanAngle, fontHeight, info.FanConfigInfo.FanDepth
                , info.FanConfigInfo.FanWidth, info.FanConfigInfo.FanLength, info.FanConfigInfo.FanNumber
                , strFanVolume, strFanPower, strFanWeight, strFanNoise, strFanMark);
            //插入墙洞
            if (isInsertHole)
            {
                string strFanHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.FanConfigInfo.FanWidth, info.FanConfigInfo.FanLength);
                string strFanHoleMark = ThFanLayoutDealService.GetFanHoleMark(info.FanMarkHeigthType, info.FanMarkHeight - 0.05);
                InsertFanHole(acadDatabase, point1.Value, fanAngle, fontHeight, info.FanConfigInfo.FanWidth + 100.0, strFanHoleSize, strFanHoleMark);
            }
            //插入防火阀
            Point3d pt = point1.Value - vector * (info.FanConfigInfo.FanDepth - 10);//沿着vector反方向平移电机深度
            Vector3d tmpV = new Vector3d(Math.Cos(fanAngle + Math.PI), Math.Sin(fanAngle + Math.PI), 0.0);//沿着vector垂直方向平移电机宽度
            pt = pt + (tmpV * info.FanConfigInfo.FanWidth / 2.0);
            InsertFireValve(acadDatabase, pt, fanAngle, fontHeight, info.FanConfigInfo.FanWidth, "70度防火阀FD");

            if (!info.IsInsertAirPort)
            {
                return;
            }

            var point3 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point3.Status != PromptStatus.OK)
            {
                return;
            }
            var point4 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point4.Status != PromptStatus.OK)
            {
                return;
            }

            //插入风口
            Vector3d vector1 = point4.Value.GetVectorTo(point3.Value).GetNormal();
            double airPortAngle = basVector.GetAngleTo(vector1, refVector) + Math.PI / 2.0;
            InsertAirPort(acadDatabase, point3.Value, airPortAngle, info.AirPortLength, info.AirPortDeepth, "侧送风口", 0);
            //插入墙洞
            if (isInsertHole)
            {
                string strAirPortHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.AirPortLength, info.AirPortHeight);
                string strAirPortHoleMark = ThFanLayoutDealService.GetFanHoleMark(info.AirPortMarkHeigthType, info.AirPortMarkHeight + 0.05);
                InsertFanHole(acadDatabase, point3.Value, airPortAngle - Math.PI, fontHeight, info.AirPortLength + 100, strAirPortHoleSize, strAirPortHoleMark);
            }
            //插入防火阀
            if (info.IsInsertValve)
            {
                Point3d pt1 = point3.Value - vector1 * info.AirPortDeepth;//沿着vector1反方向平移百叶深度
                Vector3d tmpV1 = new Vector3d(Math.Cos(airPortAngle), Math.Sin(airPortAngle), 0.0);//沿着vector1垂直方向平移百叶长度
                pt1 = pt1 + (tmpV1 * info.AirPortLength / 2.0);
                InsertFireValve(acadDatabase, pt1, airPortAngle - Math.PI, fontHeight, info.AirPortLength, "70度防火阀FD");
            }

            //插入风口标记
            Vector3d vector2 = new Vector3d(400, 1900, 0);
            Point3d p2 = point3.Value + vector2;
            string strAirPortMark = ThFanLayoutDealService.GetAirPortMarkSize(info.AirPortLength, info.AirPortHeight);
            string strAirPortMarkVolume = ThFanLayoutDealService.GetAirPortMarkVolume(info.FanConfigInfo.FanVolume);
            InsertAirPortMark(acadDatabase, point3.Value, p2, fontScale, "AH", strAirPortMark, "1", strAirPortMarkVolume);
        }
        /// <summary>
        /// 吊顶式排气扇
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <param name="info"></param>
        private void InsertCEXHFan(AcadDatabase acadDatabase, ThFanCEXHConfigInfo info, string mapScale, bool isInsertHole)
        {
            var point1 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point1.Status != PromptStatus.OK)
            {
                return;
            }
            var point2 = Active.Editor.GetPoint("\n选择要插入的基点位置");
            if (point2.Status != PromptStatus.OK)
            {
                return;
            }
            //插入风机
            Vector3d basVector = new Vector3d(1, 0, 0);
            Vector3d refVector = new Vector3d(0, 0, 1);
            Vector3d vector = point2.Value.GetVectorTo(point1.Value).GetNormal();
            double fanAngle = basVector.GetAngleTo(vector, refVector) - Math.PI / 2.0;
            double fontHeight = ThFanLayoutDealService.GetFontHeight(0, mapScale);
            double fontScale = ThFanLayoutDealService.GetFontHeight(1, mapScale);
            string strFanVolume = ThFanLayoutDealService.GetFanVolume(info.FanConfigInfo.FanVolume);
            string strFanPower = ThFanLayoutDealService.GetFanPower(info.FanConfigInfo.FanPower);
            string strFanWeight = ThFanLayoutDealService.GetFanWeight(info.FanConfigInfo.FanWeight);
            string strFanNoise = ThFanLayoutDealService.GetFanNoise(info.FanConfigInfo.FanNoise);
            InsertCEXHFan(acadDatabase, point1.Value, fanAngle, fontHeight, info.FanConfigInfo.FanDepth
                , info.FanConfigInfo.FanWidth, info.FanConfigInfo.FanLength, info.FanConfigInfo.FanNumber
                , strFanVolume, strFanPower, strFanWeight, strFanNoise);

            if(info.IsInsertAirPortAndPipe)//插入风管及排风口
            {
                //插入洞口
                if(isInsertHole)
                {
                    string strFanHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.FanConfigInfo.FanWidth, info.FanConfigInfo.FanLength);
                    string strFanHoleMark = ThFanLayoutDealService.GetFanHoleMark(0);
                    InsertFanHole(acadDatabase, point2.Value, fanAngle + Math.PI, fontHeight, info.FanConfigInfo.FanWidth + 100.0, strFanHoleSize, strFanHoleMark);
                }
                //插入风口
                InsertAirPort(acadDatabase, point2.Value, fanAngle, info.FanConfigInfo.FanWidth, info.FanConfigInfo.FanDepth, "外墙防雨百叶", 1);
                //插入防火阀
                Point3d pt = point2.Value + vector * 200;//沿着vector方向平移200
                Vector3d tmpV = new Vector3d(Math.Cos(fanAngle + Math.PI), Math.Sin(fanAngle + Math.PI), 0.0);//沿着vector垂直方向平移电机宽度
                pt = pt - (tmpV * info.FanConfigInfo.FanWidth / 2.0);
                InsertFireValve(acadDatabase, pt, fanAngle + Math.PI, fontHeight, info.FanConfigInfo.FanWidth, "70度防火阀FD");
                //插入排风管
                Vector3d vector0 = new Vector3d(Math.Cos(fanAngle), Math.Sin(fanAngle), 0.0); ;

                Point3d pt0 = point1.Value - (vector * (info.FanConfigInfo.FanWidth/2.0 + 100));//沿着vector反方向平移
                Point3d pt01 = pt0 - (vector0 * 75.0);//沿着vector垂线平移
                Point3d pt02 = pt0 + (vector0 * 75.0);//沿着vector垂线平移
                Point3d pt1 = point1.Value - (vector * (info.FanConfigInfo.FanWidth/2.0 + 100 + 200));//沿着vector反方向平移
                Point3d pt11 = pt1 - (vector0 * 60.0);//沿着vector垂线平移
                Point3d pt12 = pt1 + (vector0 * 60.0);//沿着vector垂线平移
                Point3d pt2 = point2.Value + (vector * (520 + 150));//沿着vector方向平移200
                Point3d pt21 = pt2 - (vector0 * 60.0);//沿着vector垂线平移
                Point3d pt22 = pt2 + (vector0 * 60.0);//沿着vector垂线平移
                Point3d pt3 = point2.Value + (vector * 520);//沿着vector方向平移200
                Point3d pt31 = pt3 - (vector0 * info.FanConfigInfo.FanWidth/2.0);//沿着vector垂线平移
                Point3d pt32 = pt3 + (vector0 * info.FanConfigInfo.FanWidth/2.0);//沿着vector垂线平移

                ThMEPHVACDrawService drawService = new ThMEPHVACDrawService("平时排风", mapScale, pt1, vector);
                Duct_modify_param param = new Duct_modify_param("120x120", 100, 2.5, pt1.ToPoint2D(), pt2.ToPoint2D());
                drawService.Draw_duct(param, Matrix3d.Identity);

                Line l1 = new Line(pt0, pt11);
                Line l2 = new Line(pt0, pt12);

                Line l3 = new Line(pt01, pt11);
                Line l4 = new Line(pt02, pt12);

                Line l5 = new Line(pt21, pt31);
                Line l6 = new Line(pt22, pt32);
                l1.Layer = "H-DUCT-VENT";
                l2.Layer = "H-DUCT-VENT";
                l3.Layer = "H-DUCT-VENT";
                l4.Layer = "H-DUCT-VENT";
                l5.Layer = "H-DUCT-VENT";
                l6.Layer = "H-DUCT-VENT";
                acadDatabase.ModelSpace.Add(l1);
                acadDatabase.ModelSpace.Add(l2);
                acadDatabase.ModelSpace.Add(l3);
                acadDatabase.ModelSpace.Add(l4);
                acadDatabase.ModelSpace.Add(l5);
                acadDatabase.ModelSpace.Add(l6);
            }
        }
        private void InsertWAFFan(AcadDatabase acadDatabase, Point3d pt, double angle, double fontHeight, double depth, double width, double length
                            , string strNumber, string strVolume, string strPower, string strWeight, string strNoise, string strMark)
        {
            var WAFFan = new ThFanWAFModel();
            WAFFan.FanPosition = pt;
            WAFFan.FanAngle = angle;
            WAFFan.FontHeight = fontHeight;
            WAFFan.FanDepth = depth;
            WAFFan.FanWidth = width;
            WAFFan.FanLength = length;
            WAFFan.FanNumber = strNumber;
            WAFFan.FanVolume = strVolume;
            WAFFan.FanPower = strPower;
            WAFFan.FanWeight = strWeight;
            WAFFan.FanNoise = strNoise;
            WAFFan.FanMark = strMark;
            WAFFan.InsertWAFFan(acadDatabase);
        }
        private void InsertWEXHFan(AcadDatabase acadDatabase, Point3d pt, double angle,double fontHeight, double depth, double width, double length
                            , string strNumber, string srtVolume, string strPower, string strWeight, string strNoise, string strMark)
        {
            var WEXFan = new ThFanWEXHModel();
            WEXFan.FanPosition = pt;
            WEXFan.FanAngle = angle;
            WEXFan.FontHeight = fontHeight;
            WEXFan.FanDepth = depth;
            WEXFan.FanWidth = width;
            WEXFan.FanLength = length;
            WEXFan.FanNumber = strNumber;
            WEXFan.FanVolume = srtVolume;
            WEXFan.FanPower = strPower;
            WEXFan.FanWeight = strWeight;
            WEXFan.FanNoise = strNoise;
            WEXFan.FanMark = strMark;
            WEXFan.InsertWEXHFan(acadDatabase);
        }
        private void InsertCEXHFan(AcadDatabase acadDatabase,Point3d pt, double angle,double fontHeigth,double depth,double width,double length
            , string strNumber,string srtVolume,string strPower,string strWeight,string strNoise)
        {
            var CEXHFan = new ThFanCEXHModel();
            CEXHFan.FanPosition = pt;
            CEXHFan.FanAngle = angle;
            CEXHFan.FontHeight = fontHeigth;
            CEXHFan.FanDepth = depth;
            CEXHFan.FanWidth = width;
            CEXHFan.FanLength = length;
            CEXHFan.FanNumber = strNumber;
            CEXHFan.FanVolume = srtVolume;
            CEXHFan.FanPower = strPower;
            CEXHFan.FanWeight = strWeight;
            CEXHFan.FanNoise = strNoise;
            CEXHFan.InsertCEXHFan(acadDatabase);
        }
        private void InsertFanHole(AcadDatabase acadDatabase, Point3d pt, double angle,double fontHeight , double width,string strSize,string mark)
        {
            var fanHole = new ThFanHoleModel();
            fanHole.FanHolePosition = pt;
            fanHole.FontHeight = fontHeight;
            fanHole.FanHoleWidth = width;
            fanHole.FanHoleAngle = angle;
            fanHole.FanHoleSize = strSize;
            fanHole.FanHoleMark = mark;
            fanHole.InsertFanHole(acadDatabase);
        }
        private void InsertFireValve(AcadDatabase acadDatabase, Point3d valvePt, double angle, double fontHeight,double width,string mark)
        {
            var fireValve = new ThFanFireValveModel();
            fireValve.FontHeight = fontHeight;
            fireValve.FireValvePosition = valvePt;
            fireValve.FireValveAngle = angle;
            fireValve.FireValveWidth = width;
            fireValve.FireValveMark = mark;
            fireValve.InsertFireValve(acadDatabase);
        }
        private void InsertAirPortMark(AcadDatabase acadDatabase,Point3d fanPt, Point3d markPt,
            double fontHeight,string markName,string markSize,string markCount,string markVolume)
        {
            var airPortMark = new ThFanAirPortMarkModel();
            airPortMark.FanPosition = fanPt;
            airPortMark.AirPortMarkPosition = markPt;
            airPortMark.FontHeight = fontHeight;
            airPortMark.AirPortMarkName = markName;
            airPortMark.AirPortMarkSize = markSize;
            airPortMark.AirPortMarkCount = markCount;
            airPortMark.AirPortMarkVolume = markVolume;
            airPortMark.InsertAirPortMark(acadDatabase);
        }
        private void InsertAirPort(AcadDatabase acadDatabase, Point3d portPt,double angle,double length,double depth,string type,short strDirection)
        {
            var airPort = new ThFanAirPortModel();
            airPort.AirPortPosition = portPt;
            airPort.AirPortAngle = angle;
            airPort.AirPortType = type;
            airPort.AirPortLength = length;
            airPort.AirPortDepth = depth;
            airPort.AirPortDirection = strDirection;
            airPort.InsertAirPort(acadDatabase);
        }
    }
}
