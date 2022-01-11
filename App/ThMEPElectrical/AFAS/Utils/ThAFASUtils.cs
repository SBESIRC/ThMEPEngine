using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;
using Linq2Acad;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmDistance.Data;
using ThMEPElectrical.FireAlarmFixLayout.Data;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.ViewModel;

namespace ThMEPElectrical.AFAS.Utils
{
    public static class ThAFASUtils
    {
        public static void MoveToOrigin(this ThBuildingElementVisitorManager vm, ThMEPOriginTransformer transformer)
        {
            vm.DB3ArchWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3WindowVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3RailingVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3CurtainWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3DoorMarkVisitor.Results.ForEach(o =>
            {
                if (o is ThRawDoorMark doorMark)
                {
                    transformer.Transform(doorMark.Data as Entity);
                }
                transformer.Transform(o.Geometry);
            });
            vm.DB3DoorStoneVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
        }

        public static void CleanPreviousEquipment(List<ThGeometry> CleanEquipments)
        {
            CleanEquipments.ForEach(x =>
            {
                var handle = x.Properties[ThExtractorPropertyNameManager.HandlerPropertyName].ToString();

                var dbTrans = new DBTransaction();
                var objId = dbTrans.GetObjectId(handle);
                var obj = dbTrans.GetObject(objId, OpenMode.ForWrite, false);
                obj.UpgradeOpen();
                obj.Erase();
                obj.DowngradeOpen();
                dbTrans.Commit();
                // Data.Remove(x);
            });


        }

        public static List<ThExtractorBase> GetBasicArchitectureData(Point3dCollection pts, ThMEPOriginTransformer transformer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ///////////获取数据元素,已转回原位置附近////////
                var datasetFactory = new ThAFASDataSetFactoryNew();
                datasetFactory.SetTransformer(transformer);
                datasetFactory.GetElements(acadDatabase.Database, pts);
                var extractors = datasetFactory.Extractors;

                return extractors;
            }
        }

        public static ThMEPOriginTransformer GetTransformer(Point3dCollection pts)
        {
            var center = pts.Envelope().CenterPoint();
            var transformer = new ThMEPOriginTransformer(center);
            return transformer;
        }
        public static List<ThGeometry> GetDistLayoutData2(ThAFASDataPass dataPass, List<string> extractBlkList, bool referBeam, double wallThickness, bool needConverage)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var geos = new List<ThGeometry>();
                var extractors = dataPass.Extractors;
                var selectPts = dataPass.SelectPts;
                var transformer = dataPass.Transformer;

                ///////////处理原始建筑数据,已转回原位置附近////////
                var localDataFactory = new ThAFASDistanceDataSetFactoryNew()
                {
                    ReferBeam = referBeam,
                    NeedConverage = needConverage,
                    InputExtractors = extractors,
                    WallThickness = wallThickness,
                };
                localDataFactory.SetTransformer(transformer);
                var localdataset = localDataFactory.Create(acadDatabase.Database, selectPts);
                geos.AddRange(localdataset.Container);

                ///////////获取图块数据,已转回原位置附近////////
                var previousEquipmentData = new ThAFASBusinessDataSetFactory()
                {
                    BlkNameList = extractBlkList,
                    InputExtractors = extractors,
                };
                previousEquipmentData.SetTransformer(transformer);
                var localEquipmentData = previousEquipmentData.Create(acadDatabase.Database, selectPts);
                geos.AddRange(localEquipmentData.Container);

                return geos;
            }
        }

        public static List<ThGeometry> GetFixLayoutData2(ThAFASDataPass dataPass, List<string> extractBlkList)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var geos = new List<ThGeometry>();
                var extractors = dataPass.Extractors;
                var selectPts = dataPass.SelectPts;
                var transformer = dataPass.Transformer;

                ///////////处理原始建筑数据,已转回原位置附近////////
                var localDataFactory = new ThAFASFixLayoutDataSetFactoryNew()
                {
                    InputExtractors = extractors,
                };
                localDataFactory.SetTransformer(transformer);
                var localdataset = localDataFactory.Create(acadDatabase.Database, selectPts);
                geos.AddRange(localdataset.Container);

                ///////////获取图块数据,已转回原位置附近////////
                var previousEquipmentData = new ThAFASBusinessDataSetFactory()
                {
                    BlkNameList = extractBlkList,
                    InputExtractors = extractors,
                };
                previousEquipmentData.SetTransformer(transformer);
                var localEquipmentData = previousEquipmentData.Create(acadDatabase.Database, selectPts);
                geos.AddRange(localEquipmentData.Container);

                return geos;
            }
        }

        public static List<ThGeometry> GetAreaLayoutData2(ThAFASDataPass dataPass, List<string> extractBlkList, bool referBeam, double wallThick, bool needDetective)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var geos = new List<ThGeometry>();
                var extractors = dataPass.Extractors;
                var selectPts = dataPass.SelectPts;
                var transformer = dataPass.Transformer;

                ///////////处理原始建筑数据,已转回原位置附近////////
                var localDataFactory = new ThAFASAreaDataSetFactoryNew()
                {
                    ReferBeam = referBeam,
                    WallThick = wallThick,
                    NeedDetective = needDetective,
                    InputExtractors = extractors,
                };
                localDataFactory.SetTransformer(transformer);
                var localdataset = localDataFactory.Create(acadDatabase.Database, selectPts);
                geos.AddRange(localdataset.Container);

                ///////////获取图块数据,已转回原位置附近////////
                var previousEquipmentData = new ThAFASBusinessDataSetFactory()
                {
                    BlkNameList = extractBlkList,
                    InputExtractors = extractors,
                };
                previousEquipmentData.SetTransformer(transformer);
                var localEquipmentData = previousEquipmentData.Create(acadDatabase.Database, selectPts);
                geos.AddRange(localEquipmentData.Container);

                return geos;
            }
        }

        /// <summary>
        /// for test get all data
        /// </summary>
        /// <param name="dataPass"></param>
        /// <param name="extractBlkList"></param>
        /// <param name="referBeam"></param>
        /// <param name="wallThick"></param>
        /// <returns></returns>
        public static List<ThGeometry> GetAllData(ThAFASDataPass dataPass, List<string> extractBlkList, bool referBeam, double wallThick)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var geos = new List<ThGeometry>();
                var extractors = dataPass.Extractors;
                var selectPts = dataPass.SelectPts;
                var transformer = dataPass.Transformer;

                ///////////处理原始建筑数据,已转回原位置附近////////
                var localDataFactory = new ThAFASAllSetTestDataFactory()
                {
                    ReferBeam = referBeam,
                    WallThick = wallThick,
                    InputExtractors = extractors,
                };
                localDataFactory.SetTransformer(transformer);
                var localdataset = localDataFactory.Create(acadDatabase.Database, selectPts);
                geos.AddRange(localdataset.Container);

                ///////////获取图块数据,已转回原位置附近////////
                var previousEquipmentData = new ThAFASBusinessDataSetFactory()
                {
                    BlkNameList = extractBlkList,
                    InputExtractors = extractors,
                };
                previousEquipmentData.SetTransformer(transformer);
                var localEquipmentData = previousEquipmentData.Create(acadDatabase.Database, selectPts);
                geos.AddRange(localEquipmentData.Container);

                return geos;
            }
        }

        public static void TransformToZero(ThMEPOriginTransformer transformer, List<ThGeometry> geos)
        {
            foreach (var o in geos)
            {
                if (o.Boundary != null)
                {
                    transformer.Transform(o.Boundary);
                }
            }

            geos.ProjectOntoXYPlane();

        }

        public static void TransformReset(ThMEPOriginTransformer transformer, List<ThGeometry> geos)
        {
            foreach (var o in geos)
            {
                if (o.Boundary != null)
                {
                    transformer.Reset(o.Boundary);
                }
            }

            geos.ProjectOntoXYPlane();

        }

        /// <summary>
        /// 计算blk外扩距离
        /// </summary>
        /// <param name="blkNameList"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static double GetPriorityExtendValue(List<string> blkNameList, double scale)
        {
            double extend = -1;
            var size = new List<double>();
            size.AddRange(blkNameList.Select(x => ThFaCommon.blk_size[x].Item1));
            size.AddRange(blkNameList.Select(x => ThFaCommon.blk_size[x].Item2));

            extend = size.OrderByDescending(x => x).First();
            extend = extend * scale / 2;
            return extend;
        }

        public static List<Polyline> ExtendPriority(List<Polyline> AvoidEquipment, double priorityExtend)
        {
            var extendAvoid = AvoidEquipment.Select(x => x.GetOffsetClosePolyline(priorityExtend)).ToList();
            return extendAvoid;
        }

        public static void ExtendPriority(List<ThGeometry> AvoidEquipment, double priorityExtend)
        {
            for (int i = 0; i < AvoidEquipment.Count; i++)
            {
                if (AvoidEquipment[i].Boundary is Polyline pl)
                {
                    AvoidEquipment[i].Boundary = pl.GetOffsetClosePolyline(priorityExtend);
                }
            }
        }

        public static List<ThGeometry> QueryCategory(List<ThGeometry> data, string category)
        {
            var result = new List<ThGeometry>();
            foreach (ThGeometry geo in data)
            {
                if (geo.Properties[ThExtractorPropertyNameManager.CategoryPropertyName].ToString() == category)
                {
                    result.Add(geo);
                }
            }
            return result;
        }

        public static void AFASPrepareStep()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //-----------选取图块
                var selectPts = ThAFASSelectFrameUtil.GetFrameBlk();
                //var selectPts = ThAFASSelectFrameUtil.GetFrame();
                //var selectPts = ThAFASSelectFrameUtil.GetRoomFrame();

                if (selectPts.Count == 0)
                {
                    return;
                }
                var transformer = ThAFASUtils.GetTransformer(selectPts);
                //transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

                //-----------导入所有块，图层信息
                var extractBlkList = ThFaCommon.BlkNameList;
                ThFireAlarmInsertBlk.PrepareInsert(extractBlkList, ThFaCommon.Blk_Layer.Select(x => x.Value).Distinct().ToList());

                //-----------清除所选的块
                var cleanBlkList = FireAlarmSetting.Instance.LayoutItemList.SelectMany(x => ThFaCommon.LayoutBlkList[x]).ToList();
                var previousEquipmentData = new ThAFASBusinessDataSetFactory()
                {
                    BlkNameList = cleanBlkList,
                };
                previousEquipmentData.SetTransformer(transformer);
                var localEquipmentData = previousEquipmentData.Create(acadDatabase.Database, selectPts);
                var cleanEquipment = localEquipmentData.Container;
                ThAFASUtils.CleanPreviousEquipment(cleanEquipment);

                //-----------获取数据元素,已转回原位置附近
                var extractors = ThAFASUtils.GetBasicArchitectureData(selectPts, transformer);
                ThAFASDataPass.Instance.Extractors = extractors;
                ThAFASDataPass.Instance.Transformer = transformer;
                ThAFASDataPass.Instance.SelectPts = selectPts;
            }
        }

        /////-------for no UI mode setting
        public static bool SettingBoolean(string hintString, int defaultValue)
        {
            var ans = false;
            var options = new PromptIntegerOptions(hintString);
            options.DefaultValue = defaultValue;
            var value = Active.Editor.GetInteger(options);
            if (value.Status == PromptStatus.OK)
            {
                ans = value.Value == 1 ? true : false;
            }

            return ans;
        }

        public static int SettingInt(string hintString, int defaultValue)
        {
            var ans = 0;
            var options = new PromptIntegerOptions(hintString);
            options.DefaultValue = defaultValue;
            var value = Active.Editor.GetInteger(options);
            if (value.Status == PromptStatus.OK)
            {
                ans = value.Value;
            }

            return ans;
        }

        public static string SettingString(string hintString)
        {
            var ans = "";
            var value = Active.Editor.GetString(hintString);
            if (value.Status == PromptStatus.OK)
            {
                ans = value.StringResult;
            }

            return ans;
        }

        public static double SettingDouble(string hintString,double defaultValue)
        {
            var ans = 0.0;

            var options = new PromptDoubleOptions(hintString);
            options.DefaultValue = defaultValue;
            var value = Active.Editor.GetDouble(options);
            if (value.Status == PromptStatus.OK)
            {
                ans = value.Value;
            }
            return ans;
        }

        public static string SettingSelection(string hintTitle, Dictionary<string, (string, string)> hintString, string defualt)
        {
            var ans = "";
            //var strResident = "住宅";
            //var strPublic = "公建";

            var options = new PromptKeywordOptions(hintTitle);
            foreach (var item in hintString)
            {
                options.Keywords.Add(item.Key, item.Value.Item1, item.Value.Item2);
            }
            if (defualt != "")
            {
                options.Keywords.Default = defualt;
            }

            //options.Keywords.Add(strResident, "R", "住宅(R)");
            //options.Keywords.Add(strPublic, "P", "公建(P)");

            var rst = Active.Editor.GetKeywords(options);
            if (rst.Status == PromptStatus.OK)
            {
                ans = rst.StringResult;
            }

            return ans;
        }

        //-------------no use
        //public static List<ThGeometry> GetAreaLayoutData(Point3dCollection pts, List<string> extractBlkList, bool referBeam, double wallThick, bool needDetective)
        //{

        //    var geos = new List<ThGeometry>();
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {

        //        var datasetFactory = new ThAFASAreaDataSetFactory()
        //        {
        //            ReferBeam = referBeam,
        //            WallThick = wallThick,
        //            NeedDetective = needDetective,
        //            BlkNameList = extractBlkList,
        //        };
        //        var dataset = datasetFactory.Create(acadDatabase.Database, pts);
        //        geos.AddRange(dataset.Container);

        //    }

        //    return geos;
        //}

        //public static List<ThGeometry> GetFixLayoutData(Point3dCollection pts, List<string> extractBlkList)
        //{
        //    var geos = new List<ThGeometry>();

        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        var datasetFactory = new ThAFASFixLayoutDataSetFactory()
        //        {
        //            BlkNameList = extractBlkList,
        //        };
        //        var dataset = datasetFactory.Create(acadDatabase.Database, pts);
        //        geos.AddRange(dataset.Container);

        //        return geos;
        //    }
        //}

        //public static List<ThGeometry> GetDistLayoutData(Point3dCollection pts, List<string> extractBlkList, bool referBeam, bool needConverage)
        //{
        //    var geos = new List<ThGeometry>();
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        var datasetFactory = new ThAFASDistanceDataSetFactory()
        //        {
        //            ReferBeam = referBeam,
        //            NeedConverage = needConverage,
        //            BlkNameList = extractBlkList,
        //        };

        //        var dataset = datasetFactory.Create(acadDatabase.Database, pts);
        //        geos.AddRange(dataset.Container);

        //        return geos;
        //    }
        //}

    }
}
