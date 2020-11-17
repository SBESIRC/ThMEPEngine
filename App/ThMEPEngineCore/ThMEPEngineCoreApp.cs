using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.BeamInfo;
using ThMEPEngineCore.Model.Segment;
using ThMEPEngineCore.BeamInfo.Business;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Newtonsoft.Json;
using ThCADExtension;
using Dreambuild.AutoCAD;
using NFox.Cad;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
            ThMPolygonTool.Initialize();
        }

        public void Terminate()
        {
            //
        }

        [CommandMethod("TIANHUACAD", "THExtractColumn", CommandFlags.Modal)]
        public void ThExtractColumn()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var columnRecognitionEngine = new ThColumnRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                columnRecognitionEngine.Recognize(acadDatabase.Database, frame.Vertices());
                columnRecognitionEngine.Elements.ForEach(o =>
                {
                    var curve = o.Outline as Curve;
                    acadDatabase.ModelSpace.Add(curve.WashClone());
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractBeam", CommandFlags.Modal)]
        public void ThExtractBeam()
        {
            using (var acadDatabase = AcadDatabase.Active())
            using (var beamEngine = ThMEPEngineCoreService.Instance.CreateBeamEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                beamEngine.Recognize(Active.Database, frame.Vertices());
                beamEngine.Elements.ForEach(o =>
                {
                    var curve = o.Outline as Curve;
                    acadDatabase.ModelSpace.Add(curve.WashClone());
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractBeamText", CommandFlags.Modal)]
        public void ThExtractBeamText()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var beamTextDbExtension = new ThStructureBeamTextDbExtension(Active.Database))
            {
                beamTextDbExtension.BuildElementTexts();
                beamTextDbExtension.BeamTexts.ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractShearWall", CommandFlags.Modal)]
        public void THExtractShearWall()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var shearWallEngine = new ThShearWallRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                shearWallEngine.Recognize(acadDatabase.Database, frame.Vertices());
                shearWallEngine.Elements.ForEach(o =>
                {
                    if (o.Outline is Curve curve)
                    {
                        acadDatabase.ModelSpace.Add(curve.WashClone());
                    }
                    else if (o.Outline is MPolygon mPolygon)
                    {
                        mPolygon.SetDatabaseDefaults(Active.Database);
                        acadDatabase.ModelSpace.Add(mPolygon);
                    }
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractIfcCloseTool", CommandFlags.Modal)]
        public void ThExtractIfcCloseTool()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var closetoolEngine = new ThClosestoolRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                closetoolEngine.Recognize(acadDatabase.Database, frame.Vertices());
                closetoolEngine.Elements.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o.Outline);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractIfcFloorDrain", CommandFlags.Modal)]
        public void ThExtractIfcFloorDrain()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var floorDrainEngine = new ThFloorDrainRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                floorDrainEngine.Recognize(acadDatabase.Database, frame.Vertices());
                floorDrainEngine.Elements.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o.Outline);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractBeamConnect", CommandFlags.Modal)]
        public void ThExtractBeamConnect()
        {
            List<ThBeamLink> totalBeamLinks = new List<ThBeamLink>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                var thBeamTypeRecogitionEngine = ThBeamConnectRecogitionEngine.ExecuteRecognize(
                    Active.Database, frame.Vertices());
                stopwatch.Stop();
                TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
                Active.Editor.WriteMessage("\n本次使用了：" + timespan.TotalSeconds + "秒");

                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_PRIMARY;
                }));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_HALFPRIMARY;
                }));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_OVERHANGINGPRIMARY;
                }));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_SECONDARY;
                }));

                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));

                List<ThIfcBeam> allBeams = new List<ThIfcBeam>();
                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => allBeams.AddRange(m.Beams));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => allBeams.AddRange(m.Beams));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => allBeams.AddRange(m.Beams));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => allBeams.AddRange(m.Beams));

                // 输出GeoJson文件
                // 梁
                var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                using (StreamWriter geoJson = File.CreateText(Path.Combine(path, string.Format("{0}.beam.geojson", Active.DocumentName))))
                using (JsonTextWriter writer = new JsonTextWriter(geoJson)
                {
                    Indentation = 4,
                    IndentChar = ' ',
                    Formatting = Formatting.Indented,
                })
                {
                    var geoJsonWriter = new ThBeamGeoJsonWriter();
                    geoJsonWriter.Write(allBeams, writer);
                }

                // 柱
                var columns = thBeamTypeRecogitionEngine.ColumnEngine.Elements.Cast<ThIfcColumn>();
                using (StreamWriter geoJson = File.CreateText(Path.Combine(path, string.Format("{0}.column.geojson", Active.DocumentName))))
                using (JsonTextWriter writer = new JsonTextWriter(geoJson)
                {
                    Indentation = 4,
                    IndentChar = ' ',
                    Formatting = Formatting.Indented,
                })
                {
                    var geoJsonWriter = new ThColumnGeoJsonWriter();
                    geoJsonWriter.Write(columns.ToList(), writer);
                }

                // 剪力墙
                var shearWalls = thBeamTypeRecogitionEngine.ShearWallEngine.Elements.Cast<ThIfcWall>();
                using (StreamWriter geoJson = File.CreateText(Path.Combine(path, string.Format("{0}.shearwall.geojson", Active.DocumentName))))
                using (JsonTextWriter writer = new JsonTextWriter(geoJson)
                {
                    Indentation = 4,
                    IndentChar = ' ',
                    Formatting = Formatting.Indented,
                })
                {
                    var geoJsonWriter = new ThShearWallGeoJsonWriter();
                    geoJsonWriter.Write(shearWalls.ToList(), writer);
                }
            }
        }
        private DBText CreateBeamMarkText(ThIfcBeam thIfcBeam)
        {
            string message = "";
            string beamtype = "未定义";
            switch (thIfcBeam.ComponentType)
            {
                case BeamComponentType.PrimaryBeam:
                    beamtype = "主梁";
                    break;
                case BeamComponentType.HalfPrimaryBeam:
                    beamtype = "半主梁";
                    break;
                case BeamComponentType.OverhangingPrimaryBeam:
                    beamtype = "悬挑主梁";
                    break;
                case BeamComponentType.SecondaryBeam:
                    beamtype = "次梁";
                    break;
            }
            message += beamtype + "，";
            message += thIfcBeam.Width + "，";
            message += thIfcBeam.Height;
            DBText dbText = new DBText();
            dbText.SetDatabaseDefaults(Active.Database);
            dbText.TextString = message;
            dbText.Position = ThGeometryTool.GetMidPt(thIfcBeam.StartPoint, thIfcBeam.EndPoint);
            Vector3d dir=Vector3d.XAxis.CrossProduct(thIfcBeam.StartPoint.GetVectorTo(thIfcBeam.EndPoint));
            if(dir.Z>=0)
            {
                dbText.Rotation = thIfcBeam.StartPoint.GetVectorTo(thIfcBeam.EndPoint).GetAngleTo(Vector3d.XAxis);
            }
            else
            {
                dbText.Rotation = thIfcBeam.EndPoint.GetVectorTo(thIfcBeam.StartPoint).GetAngleTo(Vector3d.XAxis)+Math.PI;
            }
            dbText.Layer = "0";
            dbText.Height = 200;
            return dbText;
        }
        [CommandMethod("TIANHUACAD", "THCDX", CommandFlags.Modal)]
        public void ThExtractLaneLine()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThLaneLineRecognitionEngine laneLineEngine = new ThLaneLineRecognitionEngine())
            {
                laneLineEngine.Recognize(Active.Database);
                ThLaneLineSimplifier.Simplify(laneLineEngine.Lanes.ToCollection(), 1500)
                    .ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
        /// <summary>
        /// 提取指定区域内的梁信息
        /// </summary>
        [CommandMethod("TIANHUACAD", "THGETBEAMINFO", CommandFlags.Modal)]
        public void THGETBEAMINFO()
        {
            // 选择楼层区域
            // 暂时只支持矩形区域
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                DBObjectCollection curves = new DBObjectCollection();
                var selRes = Active.Editor.GetSelection();
                foreach (ObjectId objId in selRes.Value.GetObjectIds())
                {
                    curves.Add(acadDatabase.Element<Curve>(objId));
                }
                var spatialIndex = new ThCADCoreNTSSpatialIndex(curves);
                Point3d pt1 = Active.Editor.GetPoint("select left down point: ").Value;
                Point3d pt2 = Active.Editor.GetPoint("select right up point: ").Value;
                DBObjectCollection filterCurves = spatialIndex.SelectCrossingWindow(pt1, pt2);
                ThDistinguishBeamInfo thDisBeamInfo = new ThDistinguishBeamInfo();
                var beams = thDisBeamInfo.CalBeamStruc(filterCurves);
                foreach (var beam in beams)
                {
                    acadDatabase.ModelSpace.Add(beam.BeamBoundary);
                }
            }
        }
        /// <summary>
        /// 提取所选图元的梁信息
        /// </summary>
        [CommandMethod("TIANHUACAD", "THGETBEAMINFO2", CommandFlags.Modal)]
        public void THGETBEAMINFO2()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 选择对象
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    RejectObjectsOnLockedLayers = true,
                };

                // 梁线的图元类型
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Arc)).DxfName,
                    RXClass.GetClass(typeof(Line)).DxfName,
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                // 梁线的图元图层
                var layers = ThBeamLayerManager.GeometryLayers(acdb.Database);
                var filter = ThSelectionFilterTool.Build(dxfNames, layers.ToArray());
                var entSelected = Active.Editor.GetSelection(options, filter);
                if (entSelected.Status != PromptStatus.OK)
                {
                    return;
                };

                // 执行操作
                DBObjectCollection dBObjects = new DBObjectCollection();
                foreach (ObjectId obj in entSelected.Value.GetObjectIds())
                {
                    var entity = acdb.Element<Entity>(obj);
                    dBObjects.Add(entity.GetTransformedCopy(Matrix3d.Identity));
                }

                ThDistinguishBeamInfo thDisBeamCommand = new ThDistinguishBeamInfo();
                var beams = thDisBeamCommand.CalBeamStruc(dBObjects);
                using (var acadDatabase = AcadDatabase.Active())
                {
                    foreach (var beam in beams)
                    {
                        acadDatabase.ModelSpace.Add(beam.BeamBoundary);
                    }
                }
            }
        }
        [CommandMethod("TIANHUACAD", "THHatchPrint", CommandFlags.Modal)]
        public void THHatchPrint()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())           
            {
                var hatchRes = Active.Editor.GetEntity("\nselect a hatch");
                Hatch hatch = acadDatabase.Element<Hatch>(hatchRes.ObjectId);
                hatch.Boundaries().ForEach(o=> acadDatabase.ModelSpace.Add(o));
            }            
        }
        [CommandMethod("TIANHUACAD", "THLineMergeTest", CommandFlags.Modal)]
        public void THLineMergeTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var lineRes = Active.Editor.GetSelection();
                if(lineRes.Status!=PromptStatus.OK)
                {
                    return;
                }
                List<Line> lines = new List<Line>();
                lineRes.Value.GetObjectIds().ForEach(o=> lines.Add(acadDatabase.Element<Line>(o)));
                var newLines=ThLineMerger.Merge(lines);
                newLines.ForEach(o =>
                {
                    o.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(o);
                });
            }
        }
    }
}
