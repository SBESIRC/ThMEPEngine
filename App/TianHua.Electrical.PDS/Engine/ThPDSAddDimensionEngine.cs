using System.IO;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

using ThCADExtension;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Diagram;
using TianHua.Electrical.PDS.Service;
using TianHua.Electrical.PDS.Project.Module;
using ProjectGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSAddDimensionEngine
    {
        public ThPDSAddDimensionEngine()
        {

        }

        /// <summary>
        /// 创建平面负载标注（无需抓取一遍当前图纸）
        /// </summary>
        /// <param name="node"></param>
        public void AddDimension(ThPDSProjectGraphNode node)
        {
            if (node.Load.Location == null)
            {
                return;
            }
            foreach (Document doc in Application.DocumentManager)
            {
                using (var docLock = doc.LockDocument())
                using (var activeDb = AcadDatabase.Use(doc.Database))
                using (var configDb = AcadDatabase.Open(ThCADCommon.PDSDiagramDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    var referenceDWG = Path.GetFileNameWithoutExtension(doc.Database.Filename);
                    if (node.Load.Location.ReferenceDWG.Equals(referenceDWG))
                    {
                        Application.DocumentManager.MdiActiveDocument = doc;
                        var scaleFactor = 8000;
                        var minPoint = new Point3d(node.Load.Location.BasePoint.X - scaleFactor,
                                                   node.Load.Location.BasePoint.Y - scaleFactor, 0);
                        var maxPoint = new Point3d(node.Load.Location.BasePoint.X + scaleFactor,
                                                   node.Load.Location.BasePoint.Y + scaleFactor, 0);
                        Active.Editor.ZoomWindow(minPoint, maxPoint);
                    }

                    if (!ThPDSSelectPointService.TrySelectPoint(out var firstPoint, "请选择插入点"))
                    {
                        return;
                    }
                    if (!ThPDSSelectPointService.TrySelectPoint(out var secondPoint, "请选择标注线端点"))
                    {
                        return;
                    }
                    if (!ThPDSSelectPointService.TrySelectPoint(out var thirdPoint, "请选择标注线方向"))
                    {
                        return;
                    }
                    if (!ThPDSSelectPointService.TryInputScale(out var scale, "请输入插入比例"))
                    {
                        return;
                    }

                    // 设备编号或用途
                    var loadIDOrPurpose = node.Load.ID.LoadID;
                    if (string.IsNullOrEmpty(loadIDOrPurpose))
                    {
                        loadIDOrPurpose = node.Load.ID.Description;
                        if (string.IsNullOrEmpty(loadIDOrPurpose))
                        {
                            if (node.Load.LoadTypeCat_3 != Model.ThPDSLoadTypeCat_3.None)
                            {
                                loadIDOrPurpose = node.Load.LoadTypeCat_3.GetDescription();
                            }
                            else if (node.Load.LoadTypeCat_2 != Model.ThPDSLoadTypeCat_2.None)
                            {
                                loadIDOrPurpose = node.Load.LoadTypeCat_2.GetDescription();
                            }
                            else
                            {
                                loadIDOrPurpose = node.Load.LoadTypeCat_1.GetDescription();
                            }
                        }
                    }

                    // 设备功率
                    var loadPower = (node.Details.LoadCalculationInfo.LowPower == 0
                        ? "" : (node.Details.LoadCalculationInfo.LowPower.ToString() + "/"))
                        + node.Details.LoadCalculationInfo.HighPower.ToString() + "kW";

                    // 主备关系
                    var primaryAndSpareAvail = node.Load.PrimaryAvail == 0
                        ? "" : (NumberToChinese(node.Load.PrimaryAvail) + "用" +
                        (node.Load.SpareAvail == 0 ? "" : NumberToChinese(node.Load.PrimaryAvail) + "备"));

                    var attributes = new Dictionary<string, string>
                    {
                        { ThPDSCommon.LOAD_ID_OR_PURPOSE, loadIDOrPurpose },
                        { ThPDSCommon.LOAD_POWER, loadPower },
                        { ThPDSCommon.PRIMARY_AND_SPARE_AVAIL, primaryAndSpareAvail},
                    };

                    var dimensionVector = thirdPoint - secondPoint;
                    var wcsVector = new Vector3d(1, 0, 0);
                    var insertEngine = new ThPDSBlockInsertEngine();
                    if (dimensionVector.DotProduct(wcsVector) >= 0)
                    {
                        var dimension = insertEngine.InsertCircuitDimension(activeDb, configDb, ThPDSCommon.LOAD_DIMENSION_R,
                            firstPoint, scale);
                        CircuitDimensionAssign(dimension, secondPoint, scale, attributes);
                    }
                    else
                    {
                        var dimension = insertEngine.InsertCircuitDimension(activeDb, configDb, ThPDSCommon.LOAD_DIMENSION_L,
                            firstPoint, scale);
                        CircuitDimensionAssign(dimension, secondPoint, scale, attributes);
                    }
                }
            }
        }

        /// <summary>
        /// 插入负载及标注
        /// </summary>
        /// <param name="node"></param>
        /// <param name="loadType"></param>
        public void InsertNewLoad(ThPDSProjectGraphNode node, ImageLoadType loadType, ThPDSInsertBlockInfo insertInfo)
        {
            using (var docLock = Active.Document.LockDocument())
            using (var activeDb = AcadDatabase.Active())
            using (var configDb = AcadDatabase.Open(ThCADCommon.PDSDiagramDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                // 填充节点信息
                var referenceDWG = Path.GetFileNameWithoutExtension(activeDb.Database.Filename);
                var location = new ThPDSLocation
                {
                    ReferenceDWG = referenceDWG,
                    BasePoint = insertInfo.InsertPoint.ToPDSPoint3d(),
                };

                // 设备编号或用途
                var loadIDOrPurpose = node.Load.ID.LoadID;
                if (string.IsNullOrEmpty(loadIDOrPurpose))
                {
                    loadIDOrPurpose = node.Load.ID.Description;
                    if (string.IsNullOrEmpty(loadIDOrPurpose))
                    {
                        if (node.Load.LoadTypeCat_3 != Model.ThPDSLoadTypeCat_3.None)
                        {
                            loadIDOrPurpose = node.Load.LoadTypeCat_3.GetDescription();
                        }
                        else if (node.Load.LoadTypeCat_2 != Model.ThPDSLoadTypeCat_2.None)
                        {
                            loadIDOrPurpose = node.Load.LoadTypeCat_2.GetDescription();
                        }
                        else
                        {
                            loadIDOrPurpose = node.Load.LoadTypeCat_1.GetDescription();
                        }
                    }
                }

                // 设备功率
                var loadPower = (node.Details.LoadCalculationInfo.LowPower == 0
                    ? "" : (node.Details.LoadCalculationInfo.LowPower.ToString() + "/"))
                    + node.Details.LoadCalculationInfo.HighPower.ToString() + "kW";

                // 主备关系
                var primaryAndSpareAvail = node.Load.PrimaryAvail == 0
                    ? "" : (NumberToChinese(node.Load.PrimaryAvail) + "用" +
                    (node.Load.SpareAvail == 0 ? "" : NumberToChinese(node.Load.PrimaryAvail) + "备"));

                var attributes = new Dictionary<string, string>
                {
                    { ThPDSCommon.LOAD_ID_OR_PURPOSE, loadIDOrPurpose },
                    { ThPDSCommon.LOAD_POWER, loadPower },
                    { ThPDSCommon.PRIMARY_AND_SPARE_AVAIL, primaryAndSpareAvail},
                };

                var dimensionVector = insertInfo.ThirdPoint - insertInfo.SecondPoint;
                var wcsVector = new Vector3d(1, 0, 0);
                var insertEngine = new ThPDSBlockInsertEngine();
                var match = ThPDSBlockNameMapService.Match(loadType);
                if (match.AttNameValues != null)
                {
                    var blockId = insertEngine.Insert(activeDb, configDb, match.BlockName, insertInfo.InsertPoint, insertInfo.Scale, match.AttNameValues);
                    ExtendAssign(activeDb, blockId, location);
                }
                else
                {
                    var blockId = insertEngine.Insert(activeDb, configDb, match.BlockName, insertInfo.InsertPoint, insertInfo.Scale);
                    ExtendAssign(activeDb, blockId, location);
                }

                if (dimensionVector.DotProduct(wcsVector) >= 0)
                {
                    var dimension = insertEngine.InsertCircuitDimension(activeDb, configDb, ThPDSCommon.LOAD_DIMENSION_R,
                        insertInfo.InsertPoint + match.LabelOffset, insertInfo.Scale);
                    CircuitDimensionAssign(dimension, insertInfo.SecondPoint, insertInfo.Scale, attributes);
                }
                else
                {
                    var dimension = insertEngine.InsertCircuitDimension(activeDb, configDb, ThPDSCommon.LOAD_DIMENSION_L,
                        insertInfo.InsertPoint + match.LabelOffset, insertInfo.Scale);
                    CircuitDimensionAssign(dimension, insertInfo.SecondPoint, insertInfo.Scale, attributes);
                }
            }
        }

        private void ExtendAssign(AcadDatabase activeDb, ObjectId blockId, ThPDSLocation location)
        {
            var extend = activeDb.Element<BlockReference>(blockId, false).GeometricExtents;
            location.MinPoint = extend.MinPoint.ToPDSPoint3d();
            location.MaxPoint = extend.MaxPoint.ToPDSPoint3d();
        }

        /// <summary>
        /// 创建平面负载标注（需抓取一遍当前图纸，并传入）
        /// </summary>
        /// <param name="projectNode"></param>
        /// <param name="projectGraph"></param>
        public void AddDimension(ThPDSProjectGraphNode projectNode, ProjectGraph projectGraph)
        {
            var nodeList = projectGraph.Vertices
                .Where(o => o.Load.ID.LoadID.Equals(projectNode.Load.ID.LoadID)).ToList();
            if (nodeList.Count != 1)
            {
                return;
            }
            var node = nodeList[0];
            AddDimension(node);
        }

        /// <summary>
        /// 创建平面回路标注（无需抓取一遍当前图纸）
        /// </summary>
        /// <param name="edge"></param>
        public void AddDimension(ThPDSProjectGraphEdge edge)
        {
            if (edge.Target.Load.Location == null)
            {
                return;
            }
            foreach (Document doc in Application.DocumentManager)
            {
                using (var docLock = doc.LockDocument())
                using (var configDb = AcadDatabase.Open(ThCADCommon.PDSDiagramDwgPath(), DwgOpenMode.ReadOnly, false))
                using (var activeDb = AcadDatabase.Use(doc.Database))
                {
                    var referenceDWG = Path.GetFileNameWithoutExtension(doc.Database.Filename);
                    if (edge.Target.Load.Location.ReferenceDWG.Equals(referenceDWG))
                    {
                        Application.DocumentManager.MdiActiveDocument = doc;
                        var scaleFactor = 8000;
                        var minPoint = new Point3d(edge.Target.Load.Location.BasePoint.X - scaleFactor,
                                                   edge.Target.Load.Location.BasePoint.Y - scaleFactor, 0);
                        var maxPoint = new Point3d(edge.Target.Load.Location.BasePoint.X + scaleFactor,
                                                   edge.Target.Load.Location.BasePoint.Y + scaleFactor, 0);
                        Active.Editor.ZoomWindow(minPoint, maxPoint);
                    }

                    if (!ThPDSSelectPointService.TrySelectPoint(out var firstPoint, "请选择插入点"))
                    {
                        return;
                    }
                    if (!ThPDSSelectPointService.TrySelectPoint(out var secondPoint, "请选择标注线端点"))
                    {
                        return;
                    }
                    if (!ThPDSSelectPointService.TrySelectPoint(out var thirdPoint, "请选择标注线方向"))
                    {
                        return;
                    }
                    if (!ThPDSSelectPointService.TryInputScale(out var scale, "请输入插入比例"))
                    {
                        return;
                    }

                    var insertEngine = new ThPDSBlockInsertEngine();
                    var circuitNumber = edge.Circuit.ID.CircuitNumber;
                    var attributes = new Dictionary<string, string>
                    {
                        { ThPDSCommon.ENTER_CIRCUIT_ID, circuitNumber },
                    };

                    var dimensionVector = thirdPoint - secondPoint;
                    var wcsVector = new Vector3d(1, 0, 0);
                    if (dimensionVector.DotProduct(wcsVector) >= 0)
                    {
                        var dimension = insertEngine.InsertCircuitDimension(activeDb, configDb, ThPDSCommon.CIRCUIT_DIMENSION_R,
                            firstPoint, scale);
                        CircuitDimensionAssign(dimension, secondPoint, scale, attributes);
                    }
                    else
                    {
                        var dimension = insertEngine.InsertCircuitDimension(activeDb, configDb, ThPDSCommon.CIRCUIT_DIMENSION_L,
                            firstPoint, scale);
                        CircuitDimensionAssign(dimension, secondPoint, scale, attributes);
                    }
                }
            }
        }

        /// <summary>
        /// 创建回路负载标注（需抓取一遍当前图纸，并传入）
        /// </summary>
        /// <param name="projectEdge"></param>
        /// <param name="projectGraph"></param>
        public void AddDimension(ThPDSProjectGraphEdge projectEdge, ProjectGraph projectGraph)
        {
            var edgeList = projectGraph.Edges
                .Where(e => e.Equals(projectEdge)).ToList();
            if (edgeList.Count != 1)
            {
                return;
            }
            var edge = edgeList[0];
            AddDimension(edge);
        }

        private void CircuitDimensionAssign(BlockReference dimension, Point3d secondPoint, Scale3d scale, Dictionary<string, string> dictionary)
        {
            var leaderVector = secondPoint - dimension.Position;
            var customProperties = dimension.Id.GetDynProperties();
            customProperties.SetValue(ThPDSCommon.POSITION_1_X, leaderVector.X);
            customProperties.SetValue(ThPDSCommon.POSITION_1_Y, leaderVector.Y);

            dimension.Id.UpdateAttributesInBlock(dictionary);

            var textMaxWidth = 200.0;
            dictionary.ForEach(o =>
            {
                var text = new DBText
                {
                    Height = 350 * scale.X,
                    TextString = o.Value,
                    TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
                };
                var textWidth = GetMaxWidth(text) + 200.0;
                if (textMaxWidth < textWidth)
                {
                    textMaxWidth = textWidth;
                }
            });
            customProperties.SetValue(ThPDSCommon.PROPERTY_TABLE_WIDTH, textMaxWidth);
        }

        private double GetMaxWidth(DBObjectCollection objs)
        {
            return objs.OfType<DBText>()
                       .Where(o => o.Visible)
                       .Where(o => o.Bounds.HasValue)
                       .Select(o => o.GeometricExtents.Width())
                       .OrderByDescending(o => o)
                       .FirstOrDefault();
        }

        private double GetMaxWidth(DBText text)
        {
            return GetMaxWidth(new DBObjectCollection { text });
        }

        private string NumberToChinese(int number)
        {
            var result = number.NumberToChinese();
            if (result == "二")
            {
                result = "两";
            }
            return result;
        }
    }
}
