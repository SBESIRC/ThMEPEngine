using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.FanLayout.Model;
using ThMEPHVAC.FanLayout.Service;
using ThMEPHVAC.FanLayout.ViewModel;
using ThMEPHVAC.Model;
using ThMEPHVAC.Service;

namespace ThMEPHVAC.FanLayout.Command
{
    public class ThFanLayoutExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public ThFanLayoutConfigInfo thFanLayoutConfigInfo { set; get; }
        public ThFanLayoutExtractCmd()
        {
            CommandName = "THXFJ";
            ActionName = "插入";
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
        public void ImportBlockFile()
        {
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))//引用模块的位置
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (blockDb.Blocks.Contains("AI-壁式轴流风机"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-壁式轴流风机"));
                }
                if (blockDb.Blocks.Contains("AI-壁式排风扇"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-壁式排风扇"));
                }
                if (blockDb.Blocks.Contains("AI-吊顶式排风扇"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-吊顶式排风扇"));
                }
                if (blockDb.Blocks.Contains("AI-吊顶式排风扇"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-吊顶式排风扇"));
                }
                if (blockDb.Blocks.Contains("AI-风口标注1"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-风口标注1"));
                }
                if (blockDb.Blocks.Contains("AI-风口"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-风口"));
                }
                if (blockDb.Blocks.Contains("防火阀"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("防火阀"));
                }
                if (blockDb.Blocks.Contains("AI-洞口"))
                {
                    acadDb.Blocks.Import(blockDb.Blocks.ElementOrDefault("AI-洞口"));
                }
                if (blockDb.Layers.Contains("H-EQUP-FANS"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-EQUP-FANS"));
                }
                if (blockDb.Layers.Contains("H-DIMS-DUCT"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-DIMS-DUCT"));
                }
                if (blockDb.Layers.Contains("H-DAPP-GRIL"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-DAPP-GRIL"));
                }
                if (blockDb.Layers.Contains("H-DAPP-ADAMP"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-DAPP-ADAMP"));
                }
                if (blockDb.Layers.Contains("H-HOLE"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-HOLE"));
                }
                if (blockDb.Layers.Contains("H-DUCT-VENT"))
                {
                    acadDb.Layers.Import(blockDb.Layers.ElementOrDefault("H-DUCT-VENT"));
                }
            }
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                DbHelper.EnsureLayerOn("H-EQUP-FANS");
                DbHelper.EnsureLayerOn("H-DIMS-DUCT");
                DbHelper.EnsureLayerOn("H-DAPP-GRIL");
                DbHelper.EnsureLayerOn("H-DAPP-ADAMP");
                DbHelper.EnsureLayerOn("H-HOLE");
                DbHelper.EnsureLayerOn("H-DUCT-VENT");
            }
        }
        public bool GetTuplePoints(out Tuple<Point3d, Point3d> pts, string tips1, string tips2)
        {
            var point1 = Active.Editor.GetPoint(tips1);
            if (point1.Status != PromptStatus.OK)
            {
                pts = Tuple.Create(new Point3d(0, 0, 0), new Point3d(0, 0, 0));
                return false;
            }
            var ppo = new PromptPointOptions(tips2);
            ppo.UseBasePoint = true;
            ppo.BasePoint = point1.Value;

            var point2 = Active.Editor.GetPoint(ppo);
            if (point2.Status != PromptStatus.OK)
            {
                pts = Tuple.Create(new Point3d(0, 0, 0), new Point3d(0, 0, 0));
                return false;
            }

            pts = Tuple.Create(point1.Value.TransformBy(Active.Editor.UCS2WCS()), point2.Value.TransformBy(Active.Editor.UCS2WCS()));
            return true;
        }
        public override void SubExecute()
        {
            try
            {
                FocusMainWindow();
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    ImportBlockFile();
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
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
        /// <summary>
        /// 壁式轴流风机
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <param name="info"></param>
        private void InsertWAFFan(AcadDatabase acadDatabase, ThFanWAFConfigInfo info,string mapScale,bool isInsertHole)
        {
            
            if(info.FanSideConfigInfo.FanConfigInfo == null)
            {
                return;
            }
            Tuple<Point3d, Point3d> tuplePts1;
            if(!GetTuplePoints(out tuplePts1, "\n请选择风机插入的基点位置：", "\n请选择风机插入的第二点（方向）："))
            {
                return;
            }
            var point1 = tuplePts1.Item1;
            var point2 = tuplePts1.Item2;

            Vector3d basVector = new Vector3d(1, 0, 0);
            Vector3d refVector = new Vector3d(0, 0, 1);
            Vector3d vector = point1.GetVectorTo(point2).GetNormal();
            double fanAngle = basVector.GetAngleTo(vector, refVector) - Math.PI/2.0;
            double fontScale = ThFanLayoutDealService.GetFontHeight(1, mapScale);
            double fontHeight = ThFanLayoutDealService.GetFontHeight(0, mapScale);
            string strFanVolume = ThFanLayoutDealService.GetFanVolume(info.FanSideConfigInfo.FanConfigInfo.FanVolume);
            string strFanPower = ThFanLayoutDealService.GetFanPower(info.FanSideConfigInfo.FanConfigInfo.FanPower);
            string strFanWeight = ThFanLayoutDealService.GetFanWeight(info.FanSideConfigInfo.FanConfigInfo.FanWeight);
            string strFanNoise = ThFanLayoutDealService.GetFanNoise(info.FanSideConfigInfo.FanConfigInfo.FanNoise);
            string strFanMark = ThFanLayoutDealService.GetFanHoleMark(info.FanSideConfigInfo.MarkHeigthType, info.FanSideConfigInfo.FanMarkHeight);
            //插入风机侧元素
            //插入风机
            InsertWAFFan(acadDatabase , info.FanSideConfigInfo.FanConfigInfo, point2, fanAngle, fontHeight, info.FanSideConfigInfo.FanConfigInfo.FanDepth
                , info.FanSideConfigInfo.FanConfigInfo.FanWidth, info.FanSideConfigInfo.FanConfigInfo.FanLength, info.FanSideConfigInfo.FanConfigInfo.FanNumber
                , strFanVolume, strFanPower, strFanWeight, strFanNoise, strFanMark);
            //插入墙洞
            if (isInsertHole)
            {
                string strFanHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.FanSideConfigInfo.FanConfigInfo.FanWidth, info.FanSideConfigInfo.FanConfigInfo.FanLength);
                string strFanHoleMark = ThFanLayoutDealService.GetFanHoleMark(info.FanSideConfigInfo.MarkHeigthType, info.FanSideConfigInfo.FanMarkHeight - 0.05);
                InsertFanHole(acadDatabase, point2, fanAngle, fontHeight, info.FanSideConfigInfo.FanConfigInfo.FanWidth + 100.0, strFanHoleSize, strFanHoleMark);
            }
            //插入防火阀
            Point3d pt = point2 - vector * (info.FanSideConfigInfo.FanConfigInfo.FanDepth - 10);//沿着vector反方向平移电机深度
            Vector3d tmpV = new Vector3d(Math.Cos(fanAngle + Math.PI), Math.Sin(fanAngle + Math.PI), 0.0);//沿着vector垂直方向平移电机宽度
            pt = pt + (tmpV * info.FanSideConfigInfo.FanConfigInfo.FanWidth / 2.0);
            InsertFireValve(acadDatabase, pt, fanAngle, fontHeight, info.FanSideConfigInfo.FanConfigInfo.FanWidth, "70度防火阀FD");
            
            if(!info.AirPortSideConfigInfo.IsInsertAirPort)
            {
                return;
            }

            Tuple<Point3d, Point3d> tuplePts2;
            if (!GetTuplePoints(out tuplePts2, "\n请选择补风口插入的基点位置：", "\n请选择补风口插入的第二点（方向）："))
            {
                return;
            }
            var point3 = tuplePts2.Item1;
            var point4 = tuplePts2.Item2;

            //插入补风侧元素
            //插入风口
            Vector3d vector1 = point4.GetVectorTo(point3).GetNormal();
            double airPortAngle = basVector.GetAngleTo(vector1, refVector) + Math.PI / 2.0;
            InsertAirPort(acadDatabase, point3, airPortAngle, info.AirPortSideConfigInfo.AirPortLength, info.AirPortSideConfigInfo.AirPortDeepth, "侧回风口", 0);
            //插入墙洞
            if (isInsertHole)
            {
                string strAirPortHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.AirPortSideConfigInfo.AirPortLength, info.AirPortSideConfigInfo.AirPortHeight);
                string strAirPortHoleMark = ThFanLayoutDealService.GetFanHoleMark(info.AirPortSideConfigInfo.MarkHeigthType, info.AirPortSideConfigInfo.AirPortMarkHeight - 0.05);
                InsertFanHole(acadDatabase, point3, airPortAngle - Math.PI, fontHeight, info.AirPortSideConfigInfo.AirPortLength + 100, strAirPortHoleSize, strAirPortHoleMark);
            }
            //插入防火阀
            if (info.AirPortSideConfigInfo.IsInsertValve)
            {
                Point3d pt1 = point3 - vector1 * info.AirPortSideConfigInfo.AirPortDeepth;//沿着vector1反方向平移百叶深度
                Vector3d tmpV1 = new Vector3d(Math.Cos(airPortAngle), Math.Sin(airPortAngle), 0.0);//沿着vector1垂直方向平移百叶长度
                pt1 = pt1 + (tmpV1 * info.AirPortSideConfigInfo.AirPortLength / 2.0);
                InsertFireValve(acadDatabase, pt1, airPortAngle - Math.PI, fontHeight, info.AirPortSideConfigInfo.AirPortLength, "70度防火阀FD");
            }

            //插入风口标记
            string strAirPortMark = ThFanLayoutDealService.GetAirPortMarkSize(info.AirPortSideConfigInfo.AirPortLength, info.AirPortSideConfigInfo.AirPortHeight);
            string strAirPortMarkVolume = ThFanLayoutDealService.GetAirPortMarkVolume(info.FanSideConfigInfo.FanConfigInfo.FanVolume);
            string strAirPortHeightMark = ThFanLayoutDealService.GetAirPortHeightMark(info.AirPortSideConfigInfo.MarkHeigthType, info.AirPortSideConfigInfo.AirPortMarkHeight);
            InsertAirPortMark(acadDatabase, point3, point3, fontScale, "AH", strAirPortMark, "1", strAirPortMarkVolume, strAirPortHeightMark);
        }
        /// <summary>
        /// 插入壁式排气扇
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <param name="info"></param>
        private void InsertWEXHFan(AcadDatabase acadDatabase, ThFanWEXHConfigInfo info, string mapScale, bool isInsertHole)
        {
            if (info.FanSideConfigInfo.FanConfigInfo == null)
            {
                return;
            }
            Tuple<Point3d, Point3d> tuplePts1;
            if (!GetTuplePoints(out tuplePts1, "\n请选择风机插入的基点位置：", "\n请选择风机插入的第二点（方向）："))
            {
                return;
            }
            var point1 = tuplePts1.Item1;
            var point2 = tuplePts1.Item2;
            Vector3d basVector = new Vector3d(1, 0, 0);
            Vector3d refVector = new Vector3d(0, 0, 1);
            Vector3d vector = point1.GetVectorTo(point2).GetNormal();
            double fanAngle = basVector.GetAngleTo(vector, refVector) - Math.PI / 2.0;
            double fontScale = ThFanLayoutDealService.GetFontHeight(1, mapScale);
            double fontHeight = ThFanLayoutDealService.GetFontHeight(0, mapScale);
            string strFanVolume = ThFanLayoutDealService.GetFanVolume(info.FanSideConfigInfo.FanConfigInfo.FanVolume);
            string strFanPower = ThFanLayoutDealService.GetFanPower(info.FanSideConfigInfo.FanConfigInfo.FanPower);
            string strFanWeight = ThFanLayoutDealService.GetFanWeight(info.FanSideConfigInfo.FanConfigInfo.FanWeight);
            string strFanNoise = ThFanLayoutDealService.GetFanNoise(info.FanSideConfigInfo.FanConfigInfo.FanNoise);
            string strFanMark = ThFanLayoutDealService.GetFanHoleMark(info.FanSideConfigInfo.MarkHeigthType, info.FanSideConfigInfo.FanMarkHeight);
            //插入风机侧元素
            //插入风机
            InsertWEXHFan(acadDatabase, info.FanSideConfigInfo.FanConfigInfo, point2, fanAngle, fontHeight, info.FanSideConfigInfo.FanConfigInfo.FanDepth
                , info.FanSideConfigInfo.FanConfigInfo.FanWidth, info.FanSideConfigInfo.FanConfigInfo.FanLength, info.FanSideConfigInfo.FanConfigInfo.FanNumber
                , strFanVolume, strFanPower, strFanWeight, strFanNoise, strFanMark);
            //插入墙洞
            if (isInsertHole)
            {
                string strFanHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.FanSideConfigInfo.FanConfigInfo.FanWidth, info.FanSideConfigInfo.FanConfigInfo.FanLength);
                string strFanHoleMark = ThFanLayoutDealService.GetFanHoleMark(info.FanSideConfigInfo.MarkHeigthType, info.FanSideConfigInfo.FanMarkHeight - 0.05);
                InsertFanHole(acadDatabase, point2, fanAngle, fontHeight, info.FanSideConfigInfo.FanConfigInfo.FanWidth + 100.0, strFanHoleSize, strFanHoleMark);
            }
            //插入防火阀
            Point3d pt = point2 - vector * (info.FanSideConfigInfo.FanConfigInfo.FanDepth - 10);//沿着vector反方向平移电机深度
            Vector3d tmpV = new Vector3d(Math.Cos(fanAngle + Math.PI), Math.Sin(fanAngle + Math.PI), 0.0);//沿着vector垂直方向平移电机宽度
            pt = pt + (tmpV * info.FanSideConfigInfo.FanConfigInfo.FanWidth / 2.0);
            InsertFireValve(acadDatabase, pt, fanAngle, fontHeight, info.FanSideConfigInfo.FanConfigInfo.FanWidth, "70度防火阀FD");

            if (!info.AirPortSideConfigInfo.IsInsertAirPort)
            {
                return;
            }

            Tuple<Point3d, Point3d> tuplePts2;
            if (!GetTuplePoints(out tuplePts2, "\n请选择补风口插入的基点位置：", "\n请选择补风口插入的第二点（方向）："))
            {
                return;
            }
            var point3 = tuplePts2.Item1;
            var point4 = tuplePts2.Item2;
            //插入补风侧元素
            //插入风口
            Vector3d vector1 = point4.GetVectorTo(point3).GetNormal();
            double airPortAngle = basVector.GetAngleTo(vector1, refVector) + Math.PI / 2.0;
            InsertAirPort(acadDatabase, point3, airPortAngle, info.AirPortSideConfigInfo.AirPortLength, info.AirPortSideConfigInfo.AirPortDeepth, "侧回风口", 0);
            //插入墙洞
            if (isInsertHole)
            {
                string strAirPortHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.AirPortSideConfigInfo.AirPortLength, info.AirPortSideConfigInfo.AirPortHeight);
                string strAirPortHoleMark = ThFanLayoutDealService.GetFanHoleMark(info.AirPortSideConfigInfo.MarkHeigthType, info.AirPortSideConfigInfo.AirPortMarkHeight - 0.05);
                InsertFanHole(acadDatabase, point3, airPortAngle - Math.PI, fontHeight, info.AirPortSideConfigInfo.AirPortLength + 100, strAirPortHoleSize, strAirPortHoleMark);
            }
            //插入防火阀
            if (info.AirPortSideConfigInfo.IsInsertValve)
            {
                Point3d pt1 = point3 - vector1 * info.AirPortSideConfigInfo.AirPortDeepth;//沿着vector1反方向平移百叶深度
                Vector3d tmpV1 = new Vector3d(Math.Cos(airPortAngle), Math.Sin(airPortAngle), 0.0);//沿着vector1垂直方向平移百叶长度
                pt1 = pt1 + (tmpV1 * info.AirPortSideConfigInfo.AirPortLength / 2.0);
                InsertFireValve(acadDatabase, pt1, airPortAngle - Math.PI, fontHeight, info.AirPortSideConfigInfo.AirPortLength, "70度防火阀FD");
            }

            //插入风口标记
            string strAirPortMark = ThFanLayoutDealService.GetAirPortMarkSize(info.AirPortSideConfigInfo.AirPortLength, info.AirPortSideConfigInfo.AirPortHeight);
            string strAirPortMarkVolume = ThFanLayoutDealService.GetAirPortMarkVolume(info.FanSideConfigInfo.FanConfigInfo.FanVolume);
            string strAirPortHeightMark = ThFanLayoutDealService.GetAirPortHeightMark(info.AirPortSideConfigInfo.MarkHeigthType, info.AirPortSideConfigInfo.AirPortMarkHeight);
            InsertAirPortMark(acadDatabase, point3, point3, fontScale, "AH", strAirPortMark, "1", strAirPortMarkVolume, strAirPortHeightMark);
        }
        /// <summary>
        /// 吊顶式排气扇
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <param name="info"></param>
        private void InsertCEXHFan(AcadDatabase acadDatabase, ThFanCEXHConfigInfo info, string mapScale, bool isInsertHole)
        {
            if (info.FanSideConfigInfo.FanConfigInfo == null)
            {
                return;
            }
            Tuple<Point3d, Point3d> tuplePts1;
            if (!GetTuplePoints(out tuplePts1, "\n请选择风机插入的基点位置：", "\n请选择风机插入的第二点（方向）："))
            {
                return;
            }
            var point1 = tuplePts1.Item1;
            var point2 = tuplePts1.Item2;
            //插入风机
            Vector3d basVector = new Vector3d(1, 0, 0);
            Vector3d refVector = new Vector3d(0, 0, 1);
            Vector3d vector = point2.GetVectorTo(point1).GetNormal();
            double fanAngle = basVector.GetAngleTo(vector, refVector) - Math.PI / 2.0;
            double fontHeight = ThFanLayoutDealService.GetFontHeight(0, mapScale);
            double fontScale = ThFanLayoutDealService.GetFontHeight(1, mapScale);
            string strFanVolume = ThFanLayoutDealService.GetFanVolume(info.FanSideConfigInfo.FanConfigInfo.FanVolume);
            string strFanPower = ThFanLayoutDealService.GetFanPower(info.FanSideConfigInfo.FanConfigInfo.FanPower);
            string strFanWeight = ThFanLayoutDealService.GetFanWeight(info.FanSideConfigInfo.FanConfigInfo.FanWeight);
            string strFanNoise = ThFanLayoutDealService.GetFanNoise(info.FanSideConfigInfo.FanConfigInfo.FanNoise);
            InsertCEXHFan(acadDatabase, info.FanSideConfigInfo.FanConfigInfo, point1, fanAngle, fontHeight, info.FanSideConfigInfo.FanConfigInfo.FanDepth
                , info.FanSideConfigInfo.FanConfigInfo.FanWidth, info.FanSideConfigInfo.FanConfigInfo.FanLength, info.FanSideConfigInfo.FanConfigInfo.FanNumber
                , strFanVolume, strFanPower, strFanWeight, strFanNoise);
            
            if(info.AirPipeConfigInfo.IsInsertPipe)//插入风管及排风口
            {
                //插入洞口
                if(isInsertHole)
                {
                    string strFanHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.AirPipeConfigInfo.AirPortLength, info.AirPipeConfigInfo.AirPortHeight,100);
                    string strFanHoleMark = ThFanLayoutDealService.GetFanHoleMark(1, info.AirPipeConfigInfo.AirPortMarkHeight);
                    InsertFanHole(acadDatabase, point2, fanAngle + Math.PI, fontHeight, info.AirPipeConfigInfo.AirPortLength + 50.0, strFanHoleSize, strFanHoleMark);
                }
                //插入风口
                InsertAirPort(acadDatabase, point2, fanAngle, info.AirPipeConfigInfo.AirPortLength, info.AirPipeConfigInfo.AirPortDeepth, "外墙防雨百叶", 1);

                //插入防火阀
                Point3d pt = point2 + vector * 200;//沿着vector方向平移200
                Vector3d tmpV = new Vector3d(Math.Cos(fanAngle + Math.PI), Math.Sin(fanAngle + Math.PI), 0.0);
                pt = pt - (tmpV * info.AirPipeConfigInfo.AirPortLength / 2.0);//沿着vector垂直方向平移风口宽度
                InsertFireValve(acadDatabase, pt, fanAngle + Math.PI, fontHeight, info.AirPipeConfigInfo.AirPortLength, "70度防火阀FD");

                //插入排风管
                Point3d pt0 = point1 - (vector * (info.FanSideConfigInfo.FanConfigInfo.FanWidth / 2.0 + 100));
                Point3d pt1 = point1 - (vector * (info.FanSideConfigInfo.FanConfigInfo.FanWidth / 2.0 + 100 + 200));
                Point3d pt2 = point2 + (vector * (200 + 320 + 150));
                Point3d pt3 = point2 + (vector * (200 + 320));
                string pipeSize = ThFanLayoutDealService.GetAirPortMarkSize(info.AirPipeConfigInfo.AirPipeLength, info.AirPipeConfigInfo.AirPipeHeight);
                ThMEPHVACDrawService drawService = new ThMEPHVACDrawService("平时排风", mapScale, pt1);
                DuctModifyParam param = new DuctModifyParam(pipeSize, 100, info.AirPipeConfigInfo.AirPipeMarkHeight, pt1, pt2);
                drawService.DrawDuct(param, Matrix3d.Identity);
                //
                Line centerLine1 = new Line(pt0, pt1);
                drawService.DrawReducing(centerLine1,150, info.AirPipeConfigInfo.AirPipeLength, true, Matrix3d.Identity);
                //
                Line centerLine2 = new Line(pt2, pt3);
                drawService.DrawReducing(centerLine2, info.AirPipeConfigInfo.AirPipeLength, info.AirPipeConfigInfo.AirPortLength, false, Matrix3d.Identity);
            }

            if (!info.AirPortSideConfigInfo.IsInsertAirPort)
            {
                return;
            }

            Tuple<Point3d, Point3d> tuplePts2;
            if (!GetTuplePoints(out tuplePts2, "\n请选择补风口插入的基点位置：", "\n请选择补风口插入的第二点（方向）："))
            {
                return;
            }
            var point3 = tuplePts2.Item1;
            var point4 = tuplePts2.Item2;
            //插入补风侧元素
            //插入风口
            Vector3d vector1 = point4.GetVectorTo(point3).GetNormal();
            double airPortAngle = basVector.GetAngleTo(vector1, refVector) + Math.PI / 2.0;
            InsertAirPort(acadDatabase, point3, airPortAngle, info.AirPortSideConfigInfo.AirPortLength, info.AirPortSideConfigInfo.AirPortDeepth, "外墙防雨百叶", 0);
            //插入墙洞
            if (isInsertHole)
            {
                string strAirPortHoleSize = ThFanLayoutDealService.GetFanHoleSize(info.AirPortSideConfigInfo.AirPortLength, info.AirPortSideConfigInfo.AirPortHeight);
                string strAirPortHoleMark = ThFanLayoutDealService.GetFanHoleMark(info.AirPortSideConfigInfo.MarkHeigthType, info.AirPortSideConfigInfo.AirPortMarkHeight - 0.05);
                InsertFanHole(acadDatabase, point3, airPortAngle - Math.PI, fontHeight, info.AirPortSideConfigInfo.AirPortLength + 100, strAirPortHoleSize, strAirPortHoleMark);
            }
            //插入防火阀
            if (info.AirPortSideConfigInfo.IsInsertValve)
            {
                Point3d pt1 = point3 - vector1 * info.AirPortSideConfigInfo.AirPortDeepth;//沿着vector1反方向平移百叶深度
                Vector3d tmpV1 = new Vector3d(Math.Cos(airPortAngle), Math.Sin(airPortAngle), 0.0);//沿着vector1垂直方向平移百叶长度
                pt1 = pt1 + (tmpV1 * info.AirPortSideConfigInfo.AirPortLength / 2.0);
                InsertFireValve(acadDatabase, pt1, airPortAngle - Math.PI, fontHeight, info.AirPortSideConfigInfo.AirPortLength, "70度防火阀FD");
            }

            //插入风口标记
            string strAirPortMark = ThFanLayoutDealService.GetAirPortMarkSize(info.AirPortSideConfigInfo.AirPortLength, info.AirPortSideConfigInfo.AirPortHeight);
            string strAirPortMarkVolume = ThFanLayoutDealService.GetAirPortMarkVolume(info.FanSideConfigInfo.FanConfigInfo.FanVolume);
            string strAirPortHeightMark = ThFanLayoutDealService.GetAirPortHeightMark(info.AirPortSideConfigInfo.MarkHeigthType, info.AirPortSideConfigInfo.AirPortMarkHeight);
            InsertAirPortMark(acadDatabase, point3, point3, fontScale, "AH", strAirPortMark, "1", strAirPortMarkVolume, strAirPortHeightMark);

        }
        private void InsertWAFFan(AcadDatabase acadDatabase, ThFanConfigInfo info, Point3d pt, double angle, double fontHeight, double depth, double width, double length
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
            ThFanToDBServiece toDbServiece = new ThFanToDBServiece();
            toDbServiece.InsertWAFFan(acadDatabase, WAFFan, info);
        }
        private void InsertWEXHFan(AcadDatabase acadDatabase, ThFanConfigInfo info, Point3d pt, double angle,double fontHeight, double depth, double width, double length
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
            ThFanToDBServiece toDbServiece = new ThFanToDBServiece();
            toDbServiece.InsertWEXHFan(acadDatabase, WEXFan, info);
        }
        private void InsertCEXHFan(AcadDatabase acadDatabase, ThFanConfigInfo info, Point3d pt, double angle,double fontHeigth,double depth,double width,double length
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
            ThFanToDBServiece toDbServiece = new ThFanToDBServiece();
            toDbServiece.InsertCEXHFan(acadDatabase, CEXHFan, info);
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
            ThFanToDBServiece toDbServiece = new ThFanToDBServiece();
            toDbServiece.InsertFanHole(acadDatabase, fanHole);
        }
        private void InsertFireValve(AcadDatabase acadDatabase, Point3d valvePt, double angle, double fontHeight,double width,string mark)
        {
            var fireValve = new ThFanFireValveModel();
            fireValve.FontHeight = fontHeight;
            fireValve.FireValvePosition = valvePt;
            fireValve.FireValveAngle = angle;
            fireValve.FireValveWidth = width;
            fireValve.FireValveMark = mark;
            ThFanToDBServiece toDbServiece = new ThFanToDBServiece();
            toDbServiece.InsertFireValve(acadDatabase, fireValve);
        }
        private void InsertAirPortMark(AcadDatabase acadDatabase,Point3d fanPt, Point3d markPt,
            double fontHeight,string markName,string markSize,string markCount,string markVolume,string heightMark)
        {
            var airPortMark = new ThFanAirPortMarkModel();
            airPortMark.FanPosition = fanPt;
            airPortMark.AirPortMarkPosition = markPt;
            airPortMark.FontHeight = fontHeight;
            airPortMark.AirPortMarkName = markName;
            airPortMark.AirPortMarkSize = markSize;
            airPortMark.AirPortMarkCount = markCount;
            airPortMark.AirPortMarkVolume = markVolume;
            airPortMark.AirPortHeightMark = heightMark;
            ThFanToDBServiece toDbServiece = new ThFanToDBServiece();
            toDbServiece.InsertAirPortMark(acadDatabase, airPortMark);
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
            ThFanToDBServiece toDbServiece = new ThFanToDBServiece();
            toDbServiece.InsertAirPort(acadDatabase, airPort);
        }
    }
}
