using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Service
{
    /// <summary>
    /// 解析轮廓的规格，及哪段连接墙
    /// </summary>
    internal abstract class ThHuaRunSecAnalysisService
    {
        protected string AntiSeismicGrade { get; set; }
        protected DBObjectCollection Walls { get; set; }
        protected double EnvelopeSearchLength = 2.0;        
        private ThCADCoreNTSSpatialIndex WallSpatialIndex { get; set; }
        public ThHuaRunSecAnalysisService(DBObjectCollection walls,string antiSeismicGrade)
        {
            Walls = walls;
            AntiSeismicGrade = antiSeismicGrade;
            WallSpatialIndex = new ThCADCoreNTSSpatialIndex(Walls);
        }        
        public abstract void Analysis(EdgeComponentExtractInfo componentExtractInfo);
        protected DBObjectCollection Query(Polyline outline)
        {
            return WallSpatialIndex.SelectCrossingPolygon(outline);
        }
        protected bool IsLinkWall(Point3d edgeSp,Point3d edgeEp)
        {
            var midPt = edgeEp.GetMidPt(edgeEp);
            var outline = midPt.CreateSquare(EnvelopeSearchLength);
            var walls = Query(outline);
            outline.Dispose();
            return walls.Count > 0;
        }
    }
    internal class ThHuaRunRectSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public ThHuaRunRectSecAnalysisService(DBObjectCollection walls,string antiSeismicGrade):
            base(walls, antiSeismicGrade)
        {
        }
        public override void Analysis(EdgeComponentExtractInfo componentExtractInfo)
        {
            // 解析规格
            var specService = new ThRectangleSpecAnalysisService();
            specService.Analysis(componentExtractInfo.EdgeComponent);            

            // 查询
            var specInfos = ThHuaRunDataManager.Instance.QueryRect(
                this.AntiSeismicGrade, componentExtractInfo.GetCode(), specService.A, specService.B);
            if(specInfos.Count==0)
            {
                return;
            }

            // 查找是否有连接的墙
            int linkCount = 0;
            if (specService.A > specService.B)
            {
                var edges = new List<Tuple<Point3d, Point3d>> { specService.EdgeB, specService.EdgeD};
                linkCount = GetLinkWallEdgeCount(edges);
            }
            else
            {
                var edges = new List<Tuple<Point3d, Point3d>> { specService.EdgeA, specService.EdgeB, 
                    specService.EdgeC, specService.EdgeD };
                linkCount = GetLinkWallEdgeCount(edges);
            }

            if (linkCount > 0)
            {
                // 在标准库中存在，且连接墙
                componentExtractInfo.IsStandard = true; 
                componentExtractInfo.TypeCode = StandardType.A.ToString();
                componentExtractInfo.LinkWallPos = linkCount.ToString();
                // 附加规格
                componentExtractInfo.SpecDict.Add("Hc", specService.A);
                componentExtractInfo.SpecDict.Add("Bw", specService.B);
            }        
        }

        private int GetLinkWallEdgeCount(List<Tuple<Point3d,Point3d>> edges)
        {
            return edges.Where(e => IsLinkWall(e.Item1, e.Item2)).Count();
        }        
    }
    internal class ThHuaRunLTypeSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public ThHuaRunLTypeSecAnalysisService(DBObjectCollection walls,string antiSeismicGrade) :
            base(walls,antiSeismicGrade)
        {
        }
        public override void Analysis(EdgeComponentExtractInfo componentExtractInfo)
        {
            string code = componentExtractInfo.GetCode();
            var specService = new ThLTypeSpecAnalysisService();
            specService.Analysis(componentExtractInfo.EdgeComponent);
            // 查询连接墙
            bool isEdgeALinkWall = IsLinkWall(specService.EdgeA.Item1, specService.EdgeA.Item2);
            bool isEdgeDLinkWall = IsLinkWall(specService.EdgeD.Item1, specService.EdgeD.Item2);
            if (isEdgeALinkWall == false && isEdgeDLinkWall == false)
            {
                return;
            }

            // 查询A端规格是否存在于标准库中
            var specInfos = ThHuaRunDataManager.Instance.QueryLType(
               this.AntiSeismicGrade, code, specService.B, specService.D, specService.C, specService.A);
            if (specInfos.Count > 0)
            {
                componentExtractInfo.IsStandard = true; // 规格是标准的               
                if (isEdgeALinkWall)
                {
                    // A 端的规格存在于标准库中，且A端连接墙
                    componentExtractInfo.TypeCode = StandardType.A.ToString();
                }
                else
                {
                    // A 端的规格存在于标准库中，且D端连接墙
                    componentExtractInfo.TypeCode = StandardType.B.ToString();
                }
                componentExtractInfo.SpecDict.Add("Hc1", specService.B);
                componentExtractInfo.SpecDict.Add("Bw", specService.D);
                componentExtractInfo.SpecDict.Add("Hc2", specService.C);
                componentExtractInfo.SpecDict.Add("Bf", specService.A);
                if(isEdgeALinkWall)
                {
                    if(isEdgeDLinkWall)
                    {
                        componentExtractInfo.LinkWallPos = "2";
                    }
                    else
                    {
                        componentExtractInfo.LinkWallPos = "1";
                    }
                }
                else
                {
                    componentExtractInfo.LinkWallPos = "1";
                }
            }
            else
            {
                // 查询 D 端
                specInfos = ThHuaRunDataManager.Instance.QueryLType(
               this.AntiSeismicGrade, code, specService.C, specService.A, specService.B, specService.D);
                if (specInfos.Count > 0)
                {
                    componentExtractInfo.IsStandard = true;
                    if (isEdgeDLinkWall)
                    {
                        // D端的规格存在于标准库中，且D端连接墙
                        componentExtractInfo.TypeCode = StandardType.A.ToString();
                    }
                    else
                    {
                        // D端的规格存在于标准库中，且A端连接墙
                        componentExtractInfo.TypeCode = StandardType.B.ToString();
                    }
                    componentExtractInfo.SpecDict.Add("Hc1", specService.C);
                    componentExtractInfo.SpecDict.Add("Bw", specService.A);
                    componentExtractInfo.SpecDict.Add("Hc2", specService.B);
                    componentExtractInfo.SpecDict.Add("Bf", specService.D);
                    if(isEdgeDLinkWall)
                    {
                        if(isEdgeALinkWall)
                        {
                            componentExtractInfo.LinkWallPos = "2";
                        }
                        else
                        {
                            componentExtractInfo.LinkWallPos = "1";
                        }
                    }
                    else
                    {
                        componentExtractInfo.LinkWallPos = "1";
                    }
                }
            }
        }
    }
    internal class ThHuaRunTTypeSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public int Bw { get; private set; }
        public int Hc1 { get; private set; }
        public int Bf { get; private set; }
        public int Hc2s { get; private set; }
        public int Hc2l { get; private set; }

        public string Spec => Hc1 + "x" + Bw + "," + (Hc2s+ Hc2l+ Bf) + "x" + Bf;
        public ThHuaRunTTypeSecAnalysisService(DBObjectCollection walls, string antiSeismicGrade):
            base(walls, antiSeismicGrade)
        {
        }
        public override void Analysis(EdgeComponentExtractInfo componentExtractInfo)
        {
            var tType = componentExtractInfo.EdgeComponent;
            var code = componentExtractInfo.GetCode();
            var specService = new ThTTypeSpecAnalysisService();
            specService.Analysis(tType);
            var specInfos = ThHuaRunDataManager.Instance.QueryTType(
               this.AntiSeismicGrade, code, specService.D, specService.B, specService.A, specService.E);
            if(specInfos.Count==0)
            {
                return;
            }
            bool isEdgeBLinkWall = IsLinkWall(specService.EdgeB.Item1, specService.EdgeB.Item2);
            bool isEdgeHLinkWall = IsLinkWall(specService.EdgeH.Item1, specService.EdgeH.Item2);
            bool isEdgeELinkWall = IsLinkWall(specService.EdgeE.Item1, specService.EdgeE.Item2);
            if(!isEdgeBLinkWall && !isEdgeHLinkWall && !isEdgeELinkWall)
            {
                return;
            }

            // T型的规格存在于标准库中，且E端连接墙,类型为A,否则为B
            componentExtractInfo.IsStandard = true;
            componentExtractInfo.TypeCode = isEdgeELinkWall ? StandardType.A.ToString() : StandardType.B.ToString();

            // 设置墙连接位置
            if (isEdgeELinkWall)
            {
                if (isEdgeBLinkWall && isEdgeHLinkWall) 
                {
                    // 都连
                    componentExtractInfo.LinkWallPos = "3";
                }
                else if(!isEdgeBLinkWall && !isEdgeHLinkWall) 
                {
                    // 都不连
                    componentExtractInfo.LinkWallPos = "1";
                }
                else if(isEdgeBLinkWall && !isEdgeHLinkWall) 
                {
                    // B 连，H不连
                    if (specService.C<= specService.G)
                    {
                        componentExtractInfo.LinkWallPos = "2S";
                    }
                    else
                    {
                        componentExtractInfo.LinkWallPos = "2L";
                    }
                }
                else
                {
                    // H 连，B不连
                    if (specService.G <= specService.C)
                    {
                        componentExtractInfo.LinkWallPos = "2S";
                    }
                    else
                    {
                        componentExtractInfo.LinkWallPos = "2L";
                    }
                }
            }
            else
            {
                if (isEdgeBLinkWall && isEdgeHLinkWall)
                {
                    // 都连
                    componentExtractInfo.LinkWallPos = "2";
                }
                else if (isEdgeBLinkWall && !isEdgeHLinkWall)
                {
                    // B 连，H不连
                    if (specService.C <= specService.G)
                    {
                        componentExtractInfo.LinkWallPos = "1S";
                    }
                    else
                    {
                        componentExtractInfo.LinkWallPos = "1L";
                    }
                }
                else
                {
                    // H 连，B不连
                    if (specService.G <= specService.C)
                    {
                        componentExtractInfo.LinkWallPos = "1S";
                    }
                    else
                    {
                        componentExtractInfo.LinkWallPos = "1L";
                    }
                }
            }
            Hc1 = specService.D;
            Bw = specService.B;
            Hc2s = Math.Min(specService.C, specService.G);
            Hc2l = Math.Max(specService.C, specService.G);
            Bf = specService.E;
        }
    }
    internal enum StandardType
    {
        A,
        B,
        None
    }
}
