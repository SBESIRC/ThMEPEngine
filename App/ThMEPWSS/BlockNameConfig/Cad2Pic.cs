using System;
using System.IO;
using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.ApplicationServices;
using System.Diagnostics;


namespace ThMEPWSS.BlockNameConfig
{
    delegate string PrintJPG_dele(Point3d objStart, Point3d objEnd, string strPrintName, string strStyleName, string strImgName, int PaperSizeIndex);
    public class Cad2Pic
    {
        public Point3d anchor1; // 打印选取的锚点1：左下角
        public Point3d anchor2; // 打印选取的锚点2:右上角
        public double measure_scale = 4;   // 缩放比例
        public int paper_index = 0; // 当前打印的图纸尺寸index[0,5]
        public int imgFileNum = 0;  // 当前打印的图纸名称

        public string THDETECT(PicInfo picInfo)
        {
            // 选择矩形区域
            // 打印指定区域jpg
            // 提取标注：得到所有识别图层的矩形
            // 提取标注：每一个矩形寻找内部文字
            using (var docLock = Active.Document.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                double[] papersize_array_w = { 4000, 6000, 8000, 10000, 12000, 14000, 16000, 18000, 20000, 24000, 28000, 32000, 40000, 50000, 60000 };
                double[] papersize_array_h = { 3000, 4500, 6000, 7500, 9000, 10500, 12000, 13500, 15000, 18000, 21000, 24000, 30000, 40000, 45000 };

                // select a rectangle
                Point3d pt1 = Active.Editor.GetPoint("select left down point of rotated region: ").Value;
                Point3d pt2 = Active.Editor.GetPoint("select right up point of rotated region: ").Value;
                anchor1 = pt1;
                anchor2 = pt2;
                Point2d origin1 = new Point2d(Math.Min(pt1.X, pt2.X), Math.Min(pt1.Y, pt2.Y));// 左下角
                Point2d origin2 = new Point2d(Math.Max(pt1.X, pt2.X), Math.Max(pt1.Y, pt2.Y));// 右上角
                picInfo.Origin1 = origin1;
                double width_window = Math.Abs(origin2.X - origin1.X);
                double height_window = Math.Abs(origin2.Y - origin1.Y);

                double ratio = height_window / width_window;
                double width_img = width_window / measure_scale;
                double height_img = height_window / measure_scale;

                Active.Editor.WriteLine(width_img);
                Active.Editor.WriteLine(height_img);
                bool find_paper = false;
                for (int i = 0; i < 15; i++)
                {   // 确定打印的图纸尺寸:实际图片范围长宽需要都小于图纸长宽
                    if (width_img <= papersize_array_w[i] && height_img <= papersize_array_h[i])
                    {
                        paper_index = i;
                        width_img = papersize_array_w[i];
                        height_img = papersize_array_h[i];
                        find_paper = true;
                        break;
                    }
                }
                if (!find_paper)
                {
                    paper_index = 14;
                    width_img = papersize_array_w[paper_index];
                    height_img = papersize_array_h[paper_index];
                }
                picInfo.HeightImg = height_img;
                picInfo.PaperIndex = paper_index;
                var pr = Active.Editor.GetInteger("Input a number as img name:");

                String RootFolder = "d:\\THdetection";
                String ImgFolder = "d:\\THdetection\\image";
                String LabelFolder = "d:\\THdetection\\label";
                if (Directory.Exists(RootFolder) == false)//如果不存在就创建file文件夹
                {
                    Directory.CreateDirectory(RootFolder);
                }
                if (Directory.Exists(ImgFolder) == false)//如果不存在就创建file文件夹
                {
                    Directory.CreateDirectory(ImgFolder);
                }
                if (Directory.Exists(LabelFolder) == false)//如果不存在就创建file文件夹
                {
                    Directory.CreateDirectory(LabelFolder);
                }

                String strFileName = ImgFolder;
                if (pr.Status != PromptStatus.OK)
                {
                    return "";
                }
                imgFileNum = pr.Value;
                picInfo.ImgFileNum = imgFileNum;
                strFileName = ImgFolder + "\\" + Convert.ToString(pr.Value);
                String csvName = LabelFolder + "\\" + Convert.ToString(pr.Value);
                if (File.Exists(csvName + ".csv"))
                {
                    File.Delete(csvName + ".csv");
                }
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                // -- PRINT -- //
                Active.Editor.WriteLine("启动plot");
                Active.Editor.WriteLine(DateTime.Now.ToString("yyyyMMdd HH:mm:ss"));
                PrintJPG(pt1, pt2, "PublishToWeb JPG.pc3", "monochrome.ctb", strFileName, paper_index);

                Active.Editor.WriteLine("plot返回");
                Active.Editor.WriteLine(DateTime.Now.ToString("yyyyMMdd HH:mm:ss"));

                while (!File.Exists(strFileName + ".jpg")) { }
                Active.Editor.WriteLine("文件出现：");
                Active.Editor.WriteLine(DateTime.Now.ToString("yyyyMMdd HH:mm:ss"));
                /*
                while (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting){}
                */
                stopwatch.Stop();
                TimeSpan timespan = stopwatch.Elapsed;
                Active.Editor.WriteLine("not plotting");
                Active.Editor.WriteLine(DateTime.Now.ToString("yyyyMMdd HH:mm:ss"));
                Active.Editor.WriteLine("Print Finished!");
                Active.Editor.WriteLine(timespan.TotalSeconds);
                Console.WriteLine("Print Finished!");
                PrintJPG_dele printerDele = new PrintJPG_dele(PrintJPG);
                printerDele.Invoke(pt1, pt2, "PublishToWeb JPG.pc3", "monochrome.ctb", strFileName, paper_index);

                return Convert.ToString(pr.Value);
            }
        }

        private Extents2d Ucs2Dcs(Point3d objStart, Point3d objEnd)
        {

            ResultBuffer rbFrom = new ResultBuffer(new TypedValue(5003, 1)),
                rbTo = new ResultBuffer(new TypedValue(5003, 2));

            Point3d pt1 = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(objStart, false);
            Point3d pt2 = Autodesk.AutoCAD.Internal.Utils.UcsToDisplay(objEnd, false);
            Point2d pStart = new Point2d(pt1.X, pt1.Y);
            Point2d pEnd = new Point2d(pt2.X, pt2.Y);
            //设置打印范围
            Extents2d exWin = new Extents2d(pStart, pEnd);
            return exWin;
        }

        public string PrintJPG(Point3d objStart, Point3d objEnd, string strPrintName, string strStyleName, string strImgName, int PaperSizeIndex)
        {
            // 打开文档数据库
            Document acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Extents2d printAreaextent = Ucs2Dcs(objStart, objEnd);//获取打印范围
            string strFileName = string.Empty;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr =
                          (BlockTableRecord)acTrans.GetObject(
                            acCurDb.CurrentSpaceId,
                            OpenMode.ForRead
                          );

                Layout acLayout =
                    (Layout)acTrans.GetObject(
                        btr.LayoutId,
                    OpenMode.ForRead
                    );

                // Get the PlotInfo from the layout
                PlotInfo acPlInfo = new PlotInfo();
                acPlInfo.Layout = btr.LayoutId;

                // Get a copy of the PlotSettings from the layout
                PlotSettings acPlSet = new PlotSettings(acLayout.ModelType);
                acPlSet.CopyFrom(acLayout);

                // Update the PlotSettings object
                PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;

                acPlSetVdr.SetPlotWindowArea(acPlSet, printAreaextent); //设置打印范围
                // Set the plot type
                acPlSetVdr.SetPlotType(acPlSet,
                                       Autodesk.AutoCAD.DatabaseServices.PlotType.Window);

                // FIXME: Set the plot scale：待修正
                acPlSetVdr.SetUseStandardScale(acPlSet, true);
                StdScaleType stype = (StdScaleType)18;  // 1To4

                acPlSetVdr.SetStdScaleType(acPlSet, stype);

                // Center the plot
                acPlSetVdr.SetPlotCentered(acPlSet, false);
                Point2d temp_origin = new Point2d(0, 0);
                acPlSetVdr.SetPlotOrigin(acPlSet, temp_origin);

                // Set the plot device to use
                // acPlSetVdr.SetPlotConfigurationName(acPlSet, strPrintName);

                var devicelist = acPlSetVdr.GetPlotDeviceList();

                acPlSetVdr.SetPlotConfigurationName(acPlSet, strPrintName, null);
                acPlSetVdr.RefreshLists(acPlSet);

                var medialist = acPlSetVdr.GetCanonicalMediaNameList(acPlSet);
                foreach (var canonmedia in medialist)
                {
                    Active.Editor.WriteLine(canonmedia);
                    Console.WriteLine(canonmedia);
                }
                double[] papersize_array_w = { 4000, 6000, 8000, 10000, 12000, 14000, 16000, 18000, 20000, 24000, 28000, 32000, 40000, 50000, 60000 };
                double[] papersize_array_h = { 3000, 4500, 6000, 7500, 9000, 10500, 12000, 13500, 15000, 18000, 21000, 24000, 30000, 40000, 45000 };
                String[] media_array =
                {
                    "UserDefinedRaster (4000.00 x 3000.00像素)",  //0
                    "UserDefinedRaster (6000.00 x 4500.00像素)",  //0
                    "UserDefinedRaster (8000.00 x 6000.00像素)",      //1 
                    "UserDefinedRaster (10000.00 x 7500.00像素)",      //new
                    "UserDefinedRaster (12000.00 x 9000.00像素)",     //2
                    "UserDefinedRaster (14000.00 x 10500.00像素)",      //new 
                    "UserDefinedRaster (16000.00 x 12000.00像素)",    //3
                    "UserDefinedRaster (18000.00 x 13500.00像素)",      //new
                    "UserDefinedRaster (20000.00 x 15000.00像素)",    //4
                    "UserDefinedRaster (24000.00 x 18000.00像素)",    //5
                    "UserDefinedRaster (28000.00 x 21000.00像素)",      //new
                    "UserDefinedRaster (32000.00 x 24000.00像素)",    //6
                    "UserDefinedRaster (40000.00 x 30000.00像素)",    //7
                    "UserDefinedRaster (50000.00 x 40000.00像素)",     //8
                    "UserDefinedRaster (60000.00 x 45000.00像素)"      //new
                };
                Active.Editor.WriteLine(paper_index);
                String localmedia = media_array[PaperSizeIndex];
                Active.Editor.WriteLine(localmedia);
                acPlSetVdr.SetPlotConfigurationName(acPlSet, strPrintName, localmedia);

                acPlSetVdr.SetCurrentStyleSheet(acPlSet, strStyleName);

                acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees000);

                // Set the plot info as an override since it will
                // not be saved back to the layout
                acPlInfo.OverrideSettings = acPlSet;

                // Validate the plot info
                PlotInfoValidator acPlInfoVdr = new PlotInfoValidator();
                acPlInfoVdr.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                acPlInfoVdr.Validate(acPlInfo);

                // Check to see if a plot is already in progress
                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                {
                    using (PlotEngine acPlEng = PlotFactory.CreatePublishEngine())
                    {
                        // Track the plot progress with a Progress dialog
                        PlotProgressDialog acPlProgDlg = new PlotProgressDialog(false, 1, false);

                        using (acPlProgDlg)
                        {
                            // Define the status messages to display when plotting starts
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.DialogTitle, "");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "正在生成" + acDoc.Name);

                            // Set the plot progress range
                            acPlProgDlg.LowerPlotProgressRange = 0;
                            acPlProgDlg.UpperPlotProgressRange = 100;
                            acPlProgDlg.PlotProgressPos = 0;
                            Active.Editor.WriteLine(PlotFactory.ProcessPlotState);
                            // Display the Progress dialog
                            acPlProgDlg.OnBeginPlot();
                            acPlProgDlg.IsVisible = true;
                            Active.Editor.WriteLine(PlotFactory.ProcessPlotState);
                            // Start to plot the layout
                            acPlEng.BeginPlot(acPlProgDlg, null);

                            string strTempPath = "d:";
                            // strFileName = Path.Combine(strTempPath,acDoc.Name.Substring(acDoc.Name.LastIndexOf("\\") + 1).Replace("dwg", "")+ DateTime.Now.ToString("yyyyMMddhhmmssfff") + "Compare" + ".jpg");
                            strFileName = strImgName + ".jpg";

                            // Define the plot output
                            acPlEng.BeginDocument(acPlInfo, acDoc.Name, null, 1, true, strFileName);

                            // Display information about the current plot
                            acPlProgDlg.set_PlotMsgString(PlotMessageIndex.Status, "Plotting: " + acDoc.Name + " - " + acLayout.LayoutName);

                            // Set the sheet progress range
                            acPlProgDlg.OnBeginSheet();
                            acPlProgDlg.LowerSheetProgressRange = 0;
                            acPlProgDlg.UpperSheetProgressRange = 100;
                            acPlProgDlg.SheetProgressPos = 0;

                            // Plot the first sheet/layout
                            PlotPageInfo acPlPageInfo = new PlotPageInfo();
                            acPlEng.BeginPage(acPlPageInfo, acPlInfo, true, null);

                            acPlEng.BeginGenerateGraphics(null);
                            Active.Editor.WriteLine(PlotFactory.ProcessPlotState);
                            acPlEng.EndGenerateGraphics(null);

                            // Finish plotting the sheet/layout
                            acPlEng.EndPage(null);
                            acPlProgDlg.SheetProgressPos = 100;
                            acPlProgDlg.OnEndSheet();

                            // Finish plotting the document
                            acPlEng.EndDocument(null);
                            Active.Editor.WriteLine("end doc");
                            Active.Editor.WriteLine(PlotFactory.ProcessPlotState);
                            // Finish the plot
                            acPlProgDlg.PlotProgressPos = 100;
                            acPlProgDlg.OnEndPlot();
                            acPlEng.EndPlot(null);

                            Active.Editor.WriteLine(PlotFactory.ProcessPlotState);
                            acPlEng.Destroy();
                            acPlProgDlg.Destroy();
                        }
                    }
                }
            }
            return strFileName;
        }
    }
}