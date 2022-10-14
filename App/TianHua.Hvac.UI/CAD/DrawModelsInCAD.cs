using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ThCADCore.NTS;
using TianHua.Publics.BaseCode;
using NetTopologySuite.Geometries;
using ThMEPHVAC.EQPMFanModelEnums;
using ThMEPHVAC.EQPMFanSelect;

namespace TianHua.Hvac.UI.CAD
{
    public class DrawHtfcModelsInCAD : DrawModels
    {
        private readonly string HTFC_Parameters_Single = "离心-后倾-单速.json";
        private readonly string HTFC_Parameters_Double = "离心-前倾-双速.json";

        public DrawHtfcModelsInCAD(string jsonpath)
        {
            var JsonFanParameters = ReadTxt(jsonpath);
            var ListFanParameters = FuncJson.Deserialize<List<FanParameters>>(JsonFanParameters);

            IEqualityComparer<FanParameters> comparer = new CCCFComparer();
            if (jsonpath.Contains(HTFC_Parameters_Single))
            {
                comparer = new CCCFRpmComparer();
            }
            HighModels = ListFanParameters.ToGeometries(comparer, "低").Cast<LineString>().ToList();
            if (jsonpath.Contains(HTFC_Parameters_Double))
            {
                LowModels = ListFanParameters.ToGeometries(comparer, "高").Cast<LineString>().ToList();
            }

        }

        public override void DrawInCAD()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptPointResult pr = Active.Editor.GetPoint("\n请选择绘图坐标原点: ");
                if (pr.Status != PromptStatus.OK)
                    return;
                Matrix3d displacement = Matrix3d.Displacement(pr.Value.GetAsVector());

                DrawILineString(acadDatabase.Database, HighModels, displacement, "高");
                if (!LowModels.IsNull() && LowModels.Count != 0)
                {
                    DrawILineString(acadDatabase.Database, LowModels, displacement, "低");
                }
            }

        }
    }

    public class DrawAxialModelsInCAD : DrawModels
    {
        private readonly string AXIAL_Parameters_Double = "轴流-双速.json";

        public DrawAxialModelsInCAD(string jsonpath)
        {
            var JsonAxialFanParameters = ReadTxt(jsonpath);
            var ListAxialFanParameters = FuncJson.Deserialize<List<AxialFanParameters>>(JsonAxialFanParameters);

            IEqualityComparer<AxialFanParameters> comparer = new AxialModelNumberComparer();
            HighModels = ListAxialFanParameters.ToGeometries(comparer, "低").Cast<LineString>().ToList();
            if (jsonpath.Contains(AXIAL_Parameters_Double))
            {
                LowModels = ListAxialFanParameters.ToGeometries(comparer, "高").Cast<LineString>().ToList();
            }
        }

        public override void DrawInCAD()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptPointResult pr = Active.Editor.GetPoint("\n请选择绘图坐标原点: ");
                if (pr.Status != PromptStatus.OK)
                    return;
                Matrix3d displacement = Matrix3d.Displacement(pr.Value.GetAsVector());

                DrawILineString(acadDatabase.Database, HighModels, displacement, "高");
                if (!LowModels.IsNull() && LowModels.Count != 0)
                {
                    DrawILineString(acadDatabase.Database, LowModels, displacement, "低");
                }
            }

        }

    }

    public class DrawModels
    {
        public List<LineString> HighModels { get; set; }
        public List<LineString> LowModels { get; set; }

        public string ReadTxt(string Path)
        {
            try
            {
                using (StreamReader _StreamReader = File.OpenText(Path))
                {
                    return _StreamReader.ReadToEnd();
                }
            }
            catch
            {
                MessageBox.Show("数据文件读取时发生错误！");
                return string.Empty;
            }
        }

        public void DrawILineString(Database database, List<LineString> linestrings, Matrix3d originalmatrix, string geartype)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                Dictionary<string, Polyline> newpolys = new Dictionary<string, Polyline>();
                foreach (var item in linestrings)
                {
                    var poly = item.ToDbPolyline();
                    int colorindex = geartype == "高" ? 7 : 1;
                    poly.ColorIndex = colorindex;
                    //ObjectId polyid = acadDatabase.ModelSpace.Add(poly.GetTransformedCopy(originalmatrix));
                    newpolys.Add(item.UserData.ToString(), poly.GetTransformedCopy(originalmatrix) as Polyline);
                    //ObjectId polyid = acadDatabase.ModelSpace.Add(poly);
                    //acadDatabase.Element<Polyline>(polyid, true).Hyperlinks.Add(new HyperLink()
                    //{
                    //    Description = (item.UserData + geartype) as string,
                    //    Name = (item.UserData + geartype) as string,
                    //});
                }
                foreach (var newpoly in newpolys)
                {
                    ObjectId polyid = acadDatabase.ModelSpace.Add(newpoly.Value);
                    acadDatabase.Element<Polyline>(polyid, true).Hyperlinks.Add(new HyperLink()
                    {
                        Description = (newpoly.Key + geartype) as string,
                        Name = (newpoly.Key + geartype) as string,
                    });
                }
            }
        }

        public virtual void DrawInCAD()
        {
            //
        }

    }
}
