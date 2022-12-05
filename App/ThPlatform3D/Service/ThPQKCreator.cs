using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThPlatform3D.Model;

namespace ThPlatform3D.Service
{
    public class ThPQKCreator
    {
        public ThPQKCreator()
        {
            //
        }

        public ObjectId Create(Database database, ThPQKParameter parameter)
        {
            // 创建
            var midPt = parameter.Start.GetMidPt(parameter.End);
            var uprightDir = GetUprightEyeDirection(parameter.Start, parameter.End, parameter.SectionDirection);
            if (uprightDir.Length == 0)
            {
                return ObjectId.Null;
            }
            var objs = CreateSectionPath(parameter.Start, parameter.End, uprightDir, parameter.Depth);
            if (objs.Count == 0)
            {
                return ObjectId.Null;
            }

            DBText mark = null;
            if (!string.IsNullOrEmpty(parameter.Mark))
            {
                mark = CreatePQKMark(parameter.Mark, parameter.MarkTextHeight, parameter.MarkTextWidthFactor);
                // 调整文字位置
                var textWidth = mark.GeometricExtents.MaxPoint.X - mark.GeometricExtents.MinPoint.X;
                var mt1 = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(midPt));
                mark.TransformBy(mt1);

                var angle = Vector3d.XAxis.GetAngleTo(uprightDir, Vector3d.ZAxis);
                var mt2 = Matrix3d.Rotation(angle, Vector3d.ZAxis, midPt);
                mark.TransformBy(mt2);

                var newCenter = midPt + uprightDir.GetNormal().MultiplyBy(textWidth / 2.0 + parameter.MarkInterval);
                var mt3 = Matrix3d.Displacement(midPt.GetVectorTo(newCenter));
                mark.TransformBy(mt3);
            }

            // 导入
            var textStyles = new List<string> { parameter.MarkTextStyle };
            var layers = new List<string> { parameter.LineLayer, parameter.BlockLayer, parameter.MarkTextLayer };
            layers = layers.Distinct().ToList();
            textStyles = textStyles.Distinct().ToList();
            ImportTemplate(database, layers, textStyles);

            using (var acadDb = AcadDatabase.Use(database))
            {
                // 设置
                objs.OfType<Line>().ForEach(l =>
                {
                    l.Layer = parameter.LineLayer;
                    l.LineWeight = LineWeight.ByLayer;
                    l.Linetype = "ByLayer";
                    l.ColorIndex = (int)ColorIndex.BYLAYER;
                });
                if (mark != null)
                {
                    mark.Layer = parameter.MarkTextLayer;
                    mark.TextStyleId = DbHelper.GetTextStyleId(parameter.MarkTextStyle);
                    mark.ColorIndex = (int)ColorIndex.BYLAYER;
                }

                // 创建块
                var blkName = GetBlockName(acadDb, parameter.PQKBlockNamePrefix);
                if(!string.IsNullOrEmpty(blkName))
                {
                    var blkId = CreateSectionFrameBlk(database, midPt, objs, mark, blkName);
                    if (blkId.IsNull)
                    {
                        return ObjectId.Null;
                    }
                    else
                    {
                        return acadDb.CurrentSpace.ObjectId.InsertBlockReference(parameter.BlockLayer, blkName, midPt, new Scale3d(1.0), 0.0);
                    }
                }
                else
                {
                    return ObjectId.Null;
                }
            }
        }

        private string GetBlockName(AcadDatabase acadDb,string blkPrefix)
        {
            string pattern = @"\d+";
            var bt = acadDb.Element<BlockTable>(acadDb.Database.BlockTableId, true);
            int index = 0;
            foreach (var id in bt)
            {
                var subBtr = acadDb.Element<BlockTableRecord>(id);
                if (subBtr.Name.StartsWith(blkPrefix))
                {
                    var rest = subBtr.Name.Substring(blkPrefix.Length);
                    if (Regex.IsMatch(rest, pattern))
                    {
                        int currentIndex = int.Parse(rest);
                        if (currentIndex > index)
                        {
                            index = currentIndex;
                        }
                    }
                }
            }
            return blkPrefix + (index+1);
        }

        private ObjectId CreateSectionFrameBlk(Database database, Point3d basePt, DBObjectCollection objs,DBText mark,string blkName)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                if (objs.Count == 0)
                {
                    return ObjectId.Null;
                }
                var bt = acadDb.Element<BlockTable>(acadDb.Database.BlockTableId, true);
                var btr = new BlockTableRecord()
                {
                    Name = blkName,
                    Origin = basePt,
                };
                objs.OfType<Entity>().ForEach(o => btr.AppendEntity(o));
                if(mark!=null)
                {
                    btr.AppendEntity(mark);
                }
                var blkId = bt.Add(btr);
                acadDb.Database.TransactionManager.AddNewlyCreatedDBObject(btr, true);
                return blkId;
            }
        }

        private DBObjectCollection CreateSectionPath(Point3d sp, Point3d ep, Vector3d dir, double depth)
        {
            Vector3d lineDir = sp.GetVectorTo(ep).GetNormal();
            if (lineDir.IsParallelToEx(dir))
            {
                return new DBObjectCollection();
            }
            else
            {
                var objs = new DBObjectCollection();
                var line1 = new Line(sp, sp + dir.GetNormal().MultiplyBy(depth));
                var line2 = new Line(sp, ep);
                var line3 = new Line(ep, ep + dir.GetNormal().MultiplyBy(depth));
                objs.Add(line1);
                objs.Add(line2);
                objs.Add(line3);
                return objs;
            }
        }

        private Vector3d GetUprightEyeDirection(Point3d sp, Point3d ep, Vector3d dir)
        {
            // sp,ep 是分割线的起点和终点
            // dir->左视，右视，前视，后视
            Vector3d lineDir = sp.GetVectorTo(ep).GetNormal();
            if (lineDir.IsParallelToEx(dir))
            {
                return new Vector3d();
            }
            else
            {
                var perpendVec = lineDir.GetPerpendicularVector();
                if (perpendVec.DotProduct(dir) < 0.0)
                {
                    perpendVec = perpendVec.Negate();
                }
                return perpendVec;
            }
        }

        private DBText CreatePQKMark(string name, double height, double widthFactor)
        {
            return new DBText()
            {
                TextString = name,
                Height = height,
                WidthFactor = widthFactor,
                Position = Point3d.Origin,
                HorizontalMode = TextHorizontalMode.TextMid,
                VerticalMode = TextVerticalMode.TextVerticalMid,
                AlignmentPoint = Point3d.Origin,
            };
        }

        private void ImportTemplate(Database database,List<string> layers,List<string> textStyles)
        {
            using (var acadDb = AcadDatabase.Use(database))
            using (var blockDb = AcadDatabase.Open(ThBIMCommon.CadTemplatePath(), DwgOpenMode.ReadOnly, false))
            {
                // 导入图层
                layers.ForEach(layer => acadDb.Layers.Import(blockDb.Layers.ElementOrDefault(layer), true));

                // 导入文字样式
                textStyles.ForEach(textStyle => acadDb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(textStyle), true));
            }
        }
    }
}
