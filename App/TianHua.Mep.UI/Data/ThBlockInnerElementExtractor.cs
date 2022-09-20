using System.Linq;
using System.Collections.Generic;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace TianHua.Mep.UI.Data
{
    public class ThBlockInnerElementExtractor
    {
        private ThBuildingElementExtractionVisitor _visitor;

        public ThBlockInnerElementExtractor(ThBuildingElementExtractionVisitor visitor)
        {
            _visitor = visitor;
        }

        public virtual void Extract(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => !o.BlockTableRecord.IsNull)
                    .ForEach(o =>
                    {
                        var mcs2wcs = o.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        var containers = new List<ContainerInfo>();
                        _visitor.Results.AddRange(DoExtract(o, mcs2wcs, containers));
                    });
            }
        }

        private List<ThRawIfcBuildingElementData> DoExtract(BlockReference blockReference,Matrix3d matrix,List<ContainerInfo> containers)
        {
            using (var acadDb = AcadDatabase.Use(blockReference.Database))
            {
                var results = new List<ThRawIfcBuildingElementData>();
                if (_visitor.IsBuildElementBlockReference(blockReference))
                {
                    var blockTableRecord = acadDb.Blocks.Element(blockReference.BlockTableRecord);
                    if (_visitor.IsBuildElementBlock(blockTableRecord))
                    {
                        // 提取图元信息
                        var newContainers = containers.Select(o => o).ToList();
                        newContainers.Add(new ContainerInfo(blockReference.GetEffectiveName(), blockReference.Layer));
                        var objIds = new ObjectIdCollection();
                        if (blockTableRecord.IsDynamicBlock)
                        {
                            var data = new ThBlockReferenceData(blockReference.ObjectId);
                            objIds = data.VisibleEntities();
                        }
                        else
                        {
                            foreach (var objId in blockTableRecord)
                            {
                                var dbObj = acadDb.Element<Entity>(objId);
                                if (dbObj.Visible)
                                {
                                    objIds.Add(objId);
                                }
                            }
                        }
                        foreach (ObjectId objId in objIds)
                        {
                            var dbObj = acadDb.Element<Entity>(objId);
                            if (dbObj is BlockReference blockObj)
                            {
                                if (blockObj.BlockTableRecord.IsNull)
                                {
                                    continue;
                                }
                                if (_visitor.IsBuildElementBlockReference(blockObj))
                                {
                                    if (_visitor is ISetContainer iSetContainer)
                                    {
                                        iSetContainer.SetContainers(newContainers);
                                    }
                                    if (_visitor.CheckLayerValid(blockObj) && _visitor.IsBuildElement(blockObj))
                                    {
                                        _visitor.DoExtract(results, blockObj, matrix);
                                        continue;
                                    }                                    
                                }
                                var mcs2wcs = blockObj.BlockTransform.PreMultiplyBy(matrix);
                                results.AddRange(DoExtract(blockObj, mcs2wcs, newContainers));
                            }
                            else if (dbObj.IsTCHElement())
                            {
                                if (_visitor is ISetContainer iSetContainer)
                                {
                                    iSetContainer.SetContainers(newContainers);
                                }
                                if (_visitor.CheckLayerValid(dbObj) && _visitor.IsBuildElement(dbObj))
                                {
                                    _visitor.DoExtract(results, dbObj, matrix);
                                }
                            }
                            else
                            {
                                if (_visitor is ISetContainer iSetContainer)
                                {
                                    iSetContainer.SetContainers(newContainers);
                                }                               
                                _visitor.DoExtract(results, dbObj, matrix);
                            }
                        }
                        // 过滤XClip外的图元信息
                        _visitor.DoXClip(results, blockReference, matrix);
                    }
                }
                return results;
            }
        }
    }
    internal class ContainerInfo
    {
        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
            }
        }
        private string _layer = "";
        public string Layer 
        {
            get => _layer;
            set
            {
                _layer = value;
            }
        }
        public string ShortName
        {
            get
            {
                return ThMEPXRefService.OriginalFromXref(Name);
            }
        }
        public string ShortLayer
        {
            get
            {
                return ThMEPXRefService.OriginalFromXref(Layer);
            }
        }
        public ContainerInfo()
        {
            Name = "";
            Layer = "";
        }
        public ContainerInfo(string name, string layer)
        {
            _name = name;
            _layer = layer;
        }
    }
    internal interface ISetContainer
    {
        List<ContainerInfo> Containers { get; }
        void SetContainers(List<ContainerInfo> containers);
    }
}
