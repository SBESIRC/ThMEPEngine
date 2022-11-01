using System;
using AcHelper;
using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThMEPEngineCore;

namespace ThMEPWSS.BlockNameConfig
{
    public class Json2Cad
    {
        public int paper_index = 0; // 当前打印的图纸尺寸index[0,5]
        public double measure_scale = 4;   // 缩放比例
        public class JsonBoxAnnoItem
        {
            // anno in .json：从json文件中读取每个标注的信息
            public int class_index { get; set; }
            public String class_name { get; set; }
            public List<int> bbox { get; set; }
        }

        public void DrawRect(PicInfo picInfo)
        {
            using (var docLock = Active.Document.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                try
                {
                    var layerNames = "BlockCoinfig";
                    if (!acadDatabase.Layers.Contains(layerNames))
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                    }
                    var height_img = picInfo.HeightImg;
                    var origin1 = picInfo.Origin1;
                    paper_index = picInfo.PaperIndex;
                    var imgFileNum = picInfo.ImgFileNum;

                    String[] classes = { "坐便器", "小便器", "蹲便器", "洗脸盆", "洗涤槽", "拖把池", "洗衣机", "地漏", "淋浴房", "淋浴房-转角型", "浴缸", "淋浴器" };
                    System.IO.StreamReader file = System.IO.File.OpenText(@"D:\THdetection\label\" + Convert.ToString(imgFileNum) + ".json");
                    JsonTextReader reader = new JsonTextReader(file);
                    JArray array = (JArray)JToken.ReadFrom(reader);
                    List<JsonBoxAnnoItem> boxlist = array.ToObject<List<JsonBoxAnnoItem>>();
                    foreach (JsonBoxAnnoItem box_anno in boxlist)
                    {
                        String boxstring = "";
                        String label = classes[box_anno.class_index];
                        Point2dCollection point2Ds = new Point2dCollection();
                        double[] textLoc = { 0, 0 };
                        for (int i = 0; i < 8; i += 2)
                        {
                            int pt_X = box_anno.bbox[i];
                            int pt_Y = (int)(height_img) - box_anno.bbox[i + 1];
                            Point2d tmp_pt2d = new Point2d(pt_X * measure_scale + origin1.X, pt_Y * measure_scale + origin1.Y);
                            textLoc[0] += tmp_pt2d.X;
                            textLoc[1] += tmp_pt2d.Y;
                            point2Ds.Add(tmp_pt2d);
                        }
                        int pt_X0 = box_anno.bbox[0];
                        int pt_Y0 = (int)(height_img) - box_anno.bbox[1];
                        Point2d tmp_pt2d0 = new Point2d(pt_X0 * measure_scale + origin1.X, pt_Y0 * measure_scale + origin1.Y);
                        point2Ds.Add(tmp_pt2d0);

                        Active.Editor.WriteLine(label);
                        Polyline poly = new Polyline(); // Draw Polyline
                        PolylineTools.CreatePolyline(poly, point2Ds);
                        poly.ColorIndex = 4;
                        poly.Layer = layerNames;
                        acadDatabase.CurrentSpace.Add(poly);

                        var dBText = new MText();
                        dBText.Contents = label;
                        dBText.Location = new Point3d(textLoc[0] / 4, textLoc[1] / 4, 0);
                        dBText.Height = 40;
                        dBText.TextHeight = 40;
                        dBText.ColorIndex = 4;
                        acadDatabase.CurrentSpace.Add(dBText);

                    }
                }
                catch (System.Exception ex)
                {
                    ;
                }
            }
        }
    }
}
