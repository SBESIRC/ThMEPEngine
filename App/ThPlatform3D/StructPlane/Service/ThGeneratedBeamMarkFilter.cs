using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThPlatform3D.StructPlane.Model;

namespace ThPlatform3D.StructPlane.Service
{
    /// <summary>
    /// 过滤在一定范围内已经生成的梁标注
    /// </summary>
    internal class ThGeneratedBeamMarkFilter : IDisposable
    {
        private double _angleTolerance = 0.0;
        private DBObjectCollection _garbageCollector;
        private DBObjectCollection _generatedBeamMarkBlks;              
        #region ---------- Output --------------
        public List<ThBeamMarkBlkInfo> Results { get; private set; }
        /// <summary>
        /// 保留已生成的梁标注
        /// </summary>
        public DBObjectCollection KeepGeneratedBeamMarkBlks { get; private set; }
        /// <summary>
        /// 更新已生成的梁标注
        /// </summary>
        public List<ThBeamMarkBlkInfo> UpdateGeneratedBeamBlks { get; private set; }
        #endregion
        public ThGeneratedBeamMarkFilter(DBObjectCollection generatedBeamMarkBlks)
        {
            _angleTolerance = 10.0.AngToRad();
            _generatedBeamMarkBlks = generatedBeamMarkBlks;
            _garbageCollector = new DBObjectCollection();
            KeepGeneratedBeamMarkBlks = new DBObjectCollection();
            Results = new List<ThBeamMarkBlkInfo>();
            UpdateGeneratedBeamBlks = new List<ThBeamMarkBlkInfo>();
        }

        public void Dispose()
        {
            _garbageCollector.MDispose();
        }

        public void FilterByArea(List<ThBeamMarkBlkInfo> beamMarkGroups)
        {
            // init
            var generatedBeamMarkInfoDict = GetBeamMarkXDataInfo(_generatedBeamMarkBlks);
            var generatedBeamMarkAreaDict =generatedBeamMarkInfoDict
                .Select(o => new KeyValuePair<BlockReference, Polyline>(o.Key, o.Value.Item1.CreatePolyline()))
                .ToDictionary(o=>o.Key,o=>o.Value);
            generatedBeamMarkAreaDict.ForEach(o => AddToGarbage(o.Value));

            beamMarkGroups.ForEach(g =>
            {
                //从已存在的块对象中
                if (g.OrginArea.Count < 2)
                {
                    Results.Add(g);
                }
                else
                {
                    var currentTectMovDir = g.TextMoveDir;
                    var currentOriginArea = g.OrginArea.CreatePolyline();
                    AddToGarbage(currentOriginArea);
                    var areaBlks = QueryOverlapedGeneratedBeamMarks(currentOriginArea, generatedBeamMarkAreaDict);
                    areaBlks = areaBlks.OfType<BlockReference>()
                    .Where(o => IsTextMovDirValid(currentTectMovDir, generatedBeamMarkInfoDict[o].Item2, _angleTolerance))
                    .ToCollection();
                    if (areaBlks.Count == 1) 
                    {                        
                        string content = g.Marks.GetMultiTextString();
                        var firstBlk = areaBlks.OfType <BlockReference>().First();
                        if (firstBlk.Name == content)
                        {
                            KeepGeneratedBeamMarkBlks.Add(firstBlk);
                        }
                        else
                        {
                            // 更新firstBlk，
                            UpdateGeneratedBeamBlks.Add(new ThBeamMarkBlkInfo(firstBlk, g.Marks,g.OrginArea,g.TextMoveDir));
                        }
                    }
                    else if (areaBlks.Count > 1)
                    {
                        string content = g.Marks.GetMultiTextString();                       
                        var existedBlks = areaBlks
                        .OfType<BlockReference>()
                        .Where(o => o.Name == content)
                        .ToCollection();
                        if(existedBlks.Count>0)
                        {
                            KeepGeneratedBeamMarkBlks.AddRange(existedBlks);
                            existedBlks.OfType<BlockReference>().ForEach(o => areaBlks.Remove(o));
                        }
                        areaBlks.OfType<BlockReference>().ForEach(b =>
                        {
                            UpdateGeneratedBeamBlks.Add(new ThBeamMarkBlkInfo(b, g.Marks, g.OrginArea,g.TextMoveDir));
                        });
                    }
                    else
                    {
                        Results.Add(g);
                    }
                }
            });
        }

        private bool IsTextMovDirValid(Vector3d dir1,Vector3d dir2,double angTolerance)
        {
            if(dir1.Length>0.0 && dir2.Length>0.0)
            {
                var angle = dir1.GetAngleTo(dir2);
                return angle <= angTolerance;
            }
            else
            {
                return false;
            }
        }

        private DBObjectCollection QueryOverlapedGeneratedBeamMarks(Polyline area,
            Dictionary<BlockReference,Polyline> generatedBeamMarkAreaDict)
        {
            var results = new DBObjectCollection();
            if(area.Area==0.0)
            {
                return results;
            }
            var areaPolygon = area.ToNTSPolygon();
            generatedBeamMarkAreaDict
                .Where(o=>o.Value.Area>0.0)
                .ForEach(o =>
            {
                // 判断两个区域是否有重叠部分
                var intersectPolygons = area.ToNTSPolygon()
                .Intersection(o.Value.ToNTSPolygon())
                .ToDbCollection(true);
                var intersectArea = intersectPolygons.OfType<Entity>().Sum(x =>
                {
                    if (x is Polyline polyline)
                    {
                        return polyline.Area;
                    }
                    else if (x is MPolygon mPolygon)
                    {
                        return mPolygon.Area;
                    }
                    else
                    {
                        return 0.0;
                    }
                });
                if (intersectArea > 1.0)
                {
                    results.Add(o.Key);
                }
            });
            return results;
        }

        private void AddToGarbage(DBObject obj)
        {
            _garbageCollector.Add(obj);
        }

        private Dictionary<BlockReference, Tuple<Point3dCollection,Vector3d>> GetBeamMarkXDataInfo(DBObjectCollection beamMarks)
        {
            var results = new Dictionary<BlockReference, Tuple<Point3dCollection, Vector3d>>();
            beamMarks.OfType<BlockReference>().ForEach(o =>
            {
                var info = ThBeamMarkXDataService.ReadBeamArea(o.ObjectId);
                results.Add(o, info);
            });
            return results;
        }
    }
}
