using System;
using AcHelper;
using NFox.Cad;
using System.IO;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using System.Drawing.Imaging;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcColor = Autodesk.AutoCAD.Colors.Color;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreBlockCmds
    {
        [CommandMethod("TIANHUACAD", "THDUMPBLOCKIMAGE", CommandFlags.Modal)]
        public void THDUMPBLOCKIMAGE()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                var texts = GetTexts();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(texts);
                var searchDistance = 2000.0;
                var imageList = new List<Tuple<System.Drawing.Image, string>>();
                acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .ForEach(o =>
                    {
                        var blkname = o.GetEffectiveName();
                        var obb = o.ToOBB(o.BlockTransform);
                        var rangePts = GetBlockTextRange(obb, searchDistance);
                        var neibours = GetNeibourTexts(rangePts, spatialIndex);
                        var mark  = GetCloestMarkString(neibours, obb.GetCenter());
                        if(string.IsNullOrEmpty(mark))
                        {
                            mark = blkname;
                        }
                        var image = GenerateThumbnail(acadDatabase.Database, blkname);
                        imageList.Add(Tuple.Create(image, mark));
                    });
                Save(imageList, path);
            }
        }

        private void Save(List<Tuple<System.Drawing.Image, string>> images,string path)
        {
            var groups = images.GroupBy(o => o.Item2);
            groups.ForEach(g =>
            {
                int i = 1;
                g.ForEach(o =>
                {
                    var filename = Path.Combine(path, o.Item2+(i++).ToString().PadLeft(3,'0'));
                    filename = Path.ChangeExtension(filename, "jpg");
                    o.Item1.Save(filename, ImageFormat.Jpeg);
                });
            });
        }

        private System.Drawing.Image GenerateThumbnail(Database db, string blkname)
        {
            return ThBlockImageTool.GetBlockImage(blkname, db, 64, 64, AcColor.FromRgb(255, 255, 255));
        }
        private string GetCloestMarkString(DBObjectCollection texts,Point3d pt)
        {
            var infos = new List<Tuple<Point3d, string>>();
            texts.OfType<Entity>().ForEach(e =>
            {
                if (e is DBText dbText)
                {
                    infos.Add(Tuple.Create(dbText.Position, dbText.TextString));
                }
                else if (e is MText mText)
                {
                    var obb = mText.TextOBB();
                    var location = obb.NumberOfVertices > 3 ? obb.GetPoint3dAt(0).GetMidPt(obb.GetPoint3dAt(2)) :
                    mText.Bounds.Value.GetCenter();
                    infos.Add(Tuple.Create(location, mText.Text));
                }
            });
            infos = infos.Where(o => o.Item2.Contains("车")).OrderBy(o => o.Item1.DistanceTo(pt)).ToList();
            return infos.Count > 0 ? infos[0].Item2 : "";
        }
        private DBObjectCollection GetNeibourTexts(Point3dCollection pts, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            return spatialIndex.SelectCrossingPolygon(pts);
        }
        private Point3dCollection GetBlockTextRange(Polyline blkObb,double distance)
        {
            var result = new Point3dCollection();
            var pts = blkObb.Vertices();
            var ucsPts = pts.OfType<Point3d>()
                .Select(p => p.TransformBy(Active.Editor.WCS2UCS())).ToCollection();
            var minX = ucsPts.OfType<Point3d>().OrderBy(p => p.X).FirstOrDefault().X;
            var maxX = ucsPts.OfType<Point3d>().OrderByDescending(p => p.X).FirstOrDefault().X;
            var minY = ucsPts.OfType<Point3d>().OrderBy(p => p.Y).FirstOrDefault().Y;
            var maxY = ucsPts.OfType<Point3d>().OrderByDescending(p => p.Y).FirstOrDefault().Y;
            var width = maxX - minX;
            var height = maxY - minY;
            var midPt = blkObb.GetPoint3dAt(0).GetMidPt(blkObb.GetPoint3dAt(2));
            midPt = midPt.TransformBy(Active.Editor.WCS2UCS());
            var pt1 = new Point3d(midPt.X - width / 2.0, midPt.Y - height / 2.0 - distance, 0);
            var pt2 = new Point3d(midPt.X + width / 2.0, midPt.Y - height / 2.0 - distance, 0);
            var pt3 = new Point3d(midPt.X + width / 2.0, midPt.Y + height / 2.0, 0);
            var pt4 = new Point3d(midPt.X - width / 2.0, midPt.Y + height / 2.0, 0);
            result.Add(pt1.TransformBy(Active.Editor.UCS2WCS()));
            result.Add(pt2.TransformBy(Active.Editor.UCS2WCS()));
            result.Add(pt3.TransformBy(Active.Editor.UCS2WCS()));
            result.Add(pt4.TransformBy(Active.Editor.UCS2WCS()));
            return result;
        }
        private DBObjectCollection GetTexts()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var results = new DBObjectCollection();
                acadDatabase.ModelSpace
                    .OfType<DBText>()
                    .Where(d => IsParkingStallMarkLayer(d.Layer))
                    .ForEach(d => results.Add(d));

                acadDatabase.ModelSpace
                    .OfType<MText>()
                    .Where(d => IsParkingStallMarkLayer(d.Layer))
                    .ForEach(d => results.Add(d));
                return results;
            }
        }
        private bool IsParkingStallMarkLayer(string layer)
        {
            return layer.ToUpper() == "E-UNIV-NOTE";
        }
    }
}
