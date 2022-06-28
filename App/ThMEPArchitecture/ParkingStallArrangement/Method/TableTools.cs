using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPArchitecture.ViewModel;
using ThParkingStall.Core.InterProcess;
using ThMEPArchitecture.ParkingStallArrangement.PostProcess;
using System.Text.RegularExpressions;
using Dreambuild.AutoCAD;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class TableTools
    {
        static bool TableImported = false;
        static string FilePath = ThCADCommon.ParkingStallTablePath();
        //static string FilePath = "C://Users//zhangwenxuan//Desktop//地库指标表格.dwg";
        static Table _OrgTable;
        static Table OrgTable { get { return _OrgTable; } }
        static Point3d OrgMidPt;
        static List<double> LisA;
        static List<double> LisR;
        public static void ShowTables(Point3d NewMidPt, int ParkingStallCnt, string layer = "AI-指标表")
        {
            if (!TableImported) Import(layer);
            //Table table;
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var table = OrgTable.Clone() as Table;
                table.Layer = layer;
                var blkID = acdb.CurrentSpace.ObjectId.InsertBlockReference("AI-指标表", "Introduction", new Point3d(0, 0, 0), new Scale3d(1), 0.0, 0);
                var br = acdb.Element<BlockReference>(blkID);
                var vector = new Vector3d(NewMidPt.X - OrgMidPt.X, NewMidPt.Y - OrgMidPt.Y, 0);
                table.TransformBy(Matrix3d.Displacement(vector));
                br.TransformBy(Matrix3d.Displacement(vector));
                table.UpdateTable(ParkingStallCnt);
                table.AddToCurrentSpace();
                DisplayParkingStall.Add(br);
                DisplayParkingStall.Add(table);
            }
        }

        private static void Import(string layer = "AI-指标表")
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                if (!acdb.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acdb.Database, layer, 0);
                var objIDs = ImportTables(FilePath);
                _OrgTable = GetTable(acdb, objIDs);
                ImportBlock();
            }
            var extend = ((Extents3d)(OrgTable.Bounds));
            OrgMidPt = new Point3d((extend.MinPoint.X + extend.MaxPoint.X) / 2, extend.MaxPoint.Y, 0);
            UpdateList();
            TableImported = true;
        }
        private static ObjectIdCollection ImportTables(string fileName)
        {
            var results = new ObjectIdCollection();
            var tableIds = new ObjectIdCollection();
            using (var sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(fileName, FileOpenMode.OpenForReadAndAllShare, true, "");
                using (var tr = new OpenCloseTransaction())
                {
                    var blockTable = tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    foreach (ObjectId id in blockTable)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        if (btr.Name != "*Model_Space")
                        {
                            continue;
                        }
                        foreach (var objId in btr)
                        {
                            var entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                            if (entity is Table)
                            {
                                tableIds.Add(entity.Id);
                            }
                        }
                    }
                }
                var db = Active.Database;
                var targetModelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                var mapping = new IdMapping();
                sourceDb.WblockCloneObjects(tableIds, targetModelSpaceId, mapping, DuplicateRecordCloning.Ignore, false);
                foreach (ObjectId tblId in tableIds)
                {
                    var idPair = mapping[tblId];
                    results.Add(idPair.Value);
                }
                return results;
            }
        }
        private static Table GetTable(AcadDatabase acdb, ObjectIdCollection objIDs)
        {
            foreach (var id in objIDs)
            {
                return acdb.Element<Table>((ObjectId)id, true);
            }

            return null;
        }
        private static void ImportBlock()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(FilePath, DwgOpenMode.ReadOnly, false))
            {
                var block = blockDb.Blocks.ElementOrDefault("Introduction");
                currentDb.Blocks.Import(block, true);
            }
        }

        private static void UpdateList()
        {
            LisA = new List<double>();
            LisR = new List<double>();
            int start = 9;
            int end = OrgTable.Columns.Count;
            for (int i = start; i < end; i++)
            {
                LisA.Add(ParseDoubleFromString(OrgTable.Cells[1, i].TextString.Split(';').Last().Replace("}", "")));
                LisR.Add(double.Parse(OrgTable.Cells[2, i].TextString.Split(';').Last()));
            }

        }

        private static double ParseDoubleFromString(string num)
        {
            //removes multiple spces between characters, cammas, and leading/trailing whitespace
            num = Regex.Replace(num.Replace(",", ""), @"\s+", " ").Trim();
            double d = 0;
            int whole = 0;
            double numerator;
            double denominator;

            //is there a fraction?
            if (num.Contains("/"))
            {
                //is there a space?
                if (num.Contains(" "))
                {
                    //seperate the integer and fraction
                    int firstspace = num.IndexOf(" ");
                    string fraction = num.Substring(firstspace, num.Length - firstspace);
                    //set the integer
                    whole = int.Parse(num.Substring(0, firstspace));
                    //set the numerator and denominator
                    numerator = double.Parse(fraction.Split("/".ToCharArray())[0]);
                    denominator = double.Parse(fraction.Split("/".ToCharArray())[1]);
                }
                else
                {
                    //set the numerator and denominator
                    numerator = double.Parse(num.Split("/".ToCharArray())[0]);
                    denominator = double.Parse(num.Split("/".ToCharArray())[1]);
                }

                //is it a valid fraction?
                if (denominator != 0)
                {
                    d = whole + (numerator / denominator);
                }
            }
            else
            {
                //parse the whole thing
                d = double.Parse(num.Replace(" ", ""));
            }

            return d;
        }
        private static void UpdateTable(this Table table, int ParkingStallCnt)
        {
            var columnWidth = VMStock.ColumnWidth / 1000.0;
            table.Cells[1, 2].TextString = string.Format("{0:N1}", ParameterStock.TotalArea);
            table.Cells[7, 2].TextString = ParkingStallCnt.ToString();
            table.Cells[9, 2].TextString = string.Format("{0:N2}", columnWidth);
            table.Cells[15, 2].TextString = string.Format("{0:N1}", ParameterStock.BuildingArea);
            var a = ParameterStock.BuildingArea / ParameterStock.TotalArea;
            var R = GetRValue(a);
            table.Cells[1, 7].TextString = string.Format("{0:N2}", R);
            double Z;
            if (columnWidth > 7.799999)
            {
                Z = (columnWidth - 7.8) * 0.27 / 0.1;
            }
            else
            {
                Z = 0.5 + (0.41 * (columnWidth - 5.4) / 0.1);
            }
            table.Cells[7, 7].TextString = string.Format("{0:N2}", Z);

            if (ParameterStock.TotalArea >= 20000)//设备房修正值
            {
                table.Cells[14, 7].TextString = table.Cells[15, 11].TextString;
            }
            else
            {
                table.Cells[14, 7].TextString = table.Cells[15, 14].TextString;
            }
        }

        public static double GetRValue(double a)
        {
            if (!TableImported) Import();
            double prop;
            for (int i = 0; i < LisA.Count - 1; i++)
            {
                if (a >= LisA[i] && a < LisA[i + 1])
                {
                    var lb = LisR[i];
                    var ub = LisR[i + 1];
                    prop = (a - LisA[i]) / (LisA[i + 1] - LisA[i]);
                    var r = prop * (ub - lb) + lb;
                    //Active.Editor.WriteMessage(r.ToString() + " \n");
                    return r;
                }
            }
            var trans_a_start = Math.Atan(LisR.Last());
            var trans_a_end = Math.PI / 2;
            prop = (a - LisA.Last()) / (1 - LisA.Last());
            var trans_a = prop * (trans_a_end - trans_a_start) + trans_a_start;
            return Math.Tan(trans_a);

        }
        public static void hideOrgTable(string layer = "AI-原指标表")
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                if (!acdb.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acdb.Database, layer, 0);
                _OrgTable.Layer = layer;
                var id = DbHelper.GetLayerId(layer);
                id.QOpenForWrite<LayerTableRecord>(l =>
                {
                    l.IsOff = true;
                    l.IsFrozen = true;
                });
            }
        }
    }
}
