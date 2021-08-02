using System;
using System.Collections.Generic;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Interface;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThCADCore.NTS;
using System.Linq;
using NFox.Cad;
using Dreambuild.AutoCAD;

namespace ThMEPElectrical.DCL.Service
{
    /// <summary>
    /// 外圈竖向构件识别
    /// </summary>
    public abstract class ThOuterVerticalComponentRecognizer
    {
        public DBObjectCollection OuterShearwalls { get; protected set; }
        public DBObjectCollection OuterColumns { get; protected set; }

        public DBObjectCollection OtherShearwalls { get; protected set; }
        public DBObjectCollection OtherColumns { get; protected set; }
        //public Dictionary<Entity, string> OuterColumnBelongedArchOutlineID { get; set; }
        //public Dictionary<Entity, string> OuterShearWallBelongedArchOutlineID { get; set; }
        public Dictionary<Entity, string> OuterArchOutlineID { get; set; } //外部传入
        public Dictionary<Entity, string> InnterArchOutlineID { get; set; } //外部传入

        protected Dictionary<Polyline, Polyline> OuterOutlineBufferDic { get; set; }
        protected Dictionary<Polyline, Polyline> InnerOutlineBufferDic { get; set; }

        public ThOuterVerticalComponentRecognizer()
        {
            OuterShearwalls = new DBObjectCollection();
            OuterColumns = new DBObjectCollection();
            OtherShearwalls = new DBObjectCollection();
            OtherColumns = new DBObjectCollection();
            OuterArchOutlineID = new Dictionary<Entity, string>();
            //OuterColumnBelongedArchOutlineID = new Dictionary<Entity, string>();
            //OuterShearWallBelongedArchOutlineID = new Dictionary<Entity, string>();
            InnterArchOutlineID = new Dictionary<Entity, string>();
            OuterOutlineBufferDic = new Dictionary<Polyline, Polyline>();
            InnerOutlineBufferDic= new Dictionary<Polyline, Polyline>();
        }
        public abstract void Recognize();
        /// <summary>
        /// 建筑框线内缩外扩的Polyline对
        /// </summary>
        /// <param name="outlines">原始建筑框线</param>
        /// <param name="length">buffer的距离</param>
        /// <returns>Key:原建筑框线, Value:内缩或外扩后的建筑框线</returns>
        public Dictionary<Polyline, Polyline> Buffer(List<Entity> outlines, double length)
        {
            var results = new Dictionary<Polyline, Polyline>();
            IBuffer bufferService = new ThNTSBufferService();
            outlines.ForEach(o =>
            {
                var buffer = bufferService.Buffer(o, length);
                if (buffer != null && o is Polyline polyline)
                {
                    results.Add(polyline, buffer as Polyline);
                }
            });
            return results;
        }
        protected MPolygon BuildMPolygon(Polyline shell, Polyline hole)
        {
            return ThMPolygonTool.CreateMPolygon(shell, new List<Curve> { hole });
        }
        protected void OuterLineHandleColumn(ThCADCoreNTSSpatialIndex columnSpatialIndex, Dictionary<Polyline, Polyline> bufferres)
        {
            foreach (var item in bufferres)
            {
                var SelectedColumn = DBObjectCollectionSubtraction(columnSpatialIndex.SelectCrossingPolygon(item.Key),
                                        columnSpatialIndex.SelectWindowPolygon(item.Value));
                foreach (Entity col in SelectedColumn)
                    if (!OuterColumns.Contains(col))
                    {
                        OuterColumns.Add(col);
                        //OuterColumnBelongedArchOutlineID.Add(col, OuterArchOutlineID[item.Key]);
                    }
            }

        }
        protected DBObjectCollection DBObjectCollectionSubtraction(DBObjectCollection Polylinecollection_1, DBObjectCollection Polylinecollection_2)
        {
            return Polylinecollection_1
                .Cast<Entity>()
                .Where(o => !Polylinecollection_2.Contains(o))
                .ToCollection();
        }
        protected void InnerLineHandleColumn(ThCADCoreNTSSpatialIndex columnSpatialIndex, Dictionary<Polyline, Polyline> bufferres)
        {
            foreach (var item in bufferres)
            {
                var SelectedColumn = DBObjectCollectionSubtraction(columnSpatialIndex.SelectCrossingPolygon(item.Value),
                                        columnSpatialIndex.SelectWindowPolygon(item.Key));
                foreach (DBObject col in SelectedColumn)
                    if (!OuterColumns.Contains(col))
                        OuterColumns.Add(col);
            }

        }
        protected void OuterLineHandleShearWall(ThCADCoreNTSSpatialIndex shearWallSpatialIndex, Dictionary<Polyline, Polyline> bufferres)
        {
            foreach (var item in bufferres)
            {
                var SelectedShearWall = DBObjectCollectionSubtraction(shearWallSpatialIndex.SelectCrossingPolygon(item.Key),
                                                                        shearWallSpatialIndex.SelectWindowPolygon(item.Value));
                foreach (Entity shearwall in SelectedShearWall)
                    if (!OuterShearwalls.Contains(shearwall))
                    {
                        OuterShearwalls.Add(shearwall);
                        //OuterShearWallBelongedArchOutlineID.Add(shearwall, OuterArchOutlineID[item.Key]);
                    }
            }

        }
        protected void InnerLineHandleShearWall(ThCADCoreNTSSpatialIndex shearWallSpatialIndex, Dictionary<Polyline, Polyline> bufferres)
        {
            foreach (var item in bufferres)
            {
                var SelectedShearWall = DBObjectCollectionSubtraction(shearWallSpatialIndex.SelectCrossingPolygon(item.Value),
                                                                        shearWallSpatialIndex.SelectWindowPolygon(item.Key));
                foreach (DBObject shearwall in SelectedShearWall)
                    if (!OuterShearwalls.Contains(shearwall))
                        OuterShearwalls.Add(shearwall);
            }

        }
        public virtual Dictionary<Entity,string> GetOuterColumnBelongedOutArchlineId()
        {
            var results = new Dictionary<Entity, string>();
            var columnSpatialIndex = new ThCADCoreNTSSpatialIndex(OuterColumns);
            foreach(var item in OuterArchOutlineID)
            {
                var objs = columnSpatialIndex.SelectCrossingPolygon(item.Key);
               objs.Cast<Entity>().ForEach(e => results.Add(e, item.Value));
            }
            foreach (var item in InnerOutlineBufferDic)
            {
                var objs = columnSpatialIndex.SelectCrossingPolygon(item.Value);
                objs.Cast<Entity>().ForEach(e => 
                {
                    if(!results.ContainsKey(e))
                    {
                        results.Add(e, InnterArchOutlineID[item.Key]);
                    }
                    else
                    {
                        results[e] = InnterArchOutlineID[item.Key];
                    }
                });
            }
            return results;
        }

        public virtual Dictionary<Entity, string> GetOuterShearWallBelongedOutArchlineId()
        {
            var results = new Dictionary<Entity, string>();
            var shearwallSpatialIndex = new ThCADCoreNTSSpatialIndex(OuterShearwalls);
            foreach (var item in OuterArchOutlineID)
            {
                var objs = shearwallSpatialIndex.SelectCrossingPolygon(item.Key);
                objs.Cast<Entity>().ForEach(e => results.Add(e, item.Value));
            }
            foreach (var item in InnerOutlineBufferDic)
            {
                var objs = shearwallSpatialIndex.SelectCrossingPolygon(item.Value);
                objs.Cast<Entity>().ForEach(e =>
                {
                    if (!results.ContainsKey(e))
                    {
                        results.Add(e, InnterArchOutlineID[item.Key]);
                    }
                    else
                    {
                        results[e] = InnterArchOutlineID[item.Key];
                    }
                });
            }
            return results;
        }
    }

}
