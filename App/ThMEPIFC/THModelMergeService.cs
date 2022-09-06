using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPIFC.Ifc2x3;
using ThMEPTCH.Model;
using Xbim.Ifc;

namespace ThMEPIFC
{
    public class THModelMergeService
    {
        public IfcStore ModelMerge(string filePath1, string filePath2)
        {
            var model = IfcStore.Open(filePath1);
            var model2 = IfcStore.Open(filePath2);
            if (model.Instances.Count > model2.Instances.Count)
            {
                return ModelMerge(model, model2);
            }
            else
            {
                return ModelMerge(model2, model);
            }
        }

        public IfcStore ModelMerge(IfcStore bigModel, IfcStore smallModel)
        {
            //...do something with the model
            {
                //if (bigModel.SchemaVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3)
                //{
                //    var project = bigModel.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();
                //    //暂时先认为大部分是CAD出的图，是"标准"的。而小部分可能数据不全，不含楼层信息等定义
                //    //var
                //}
                //else if (bigModel.SchemaVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc4)
                //{
                //    var project = bigModel.Instances.FirstOrDefault<Xbim.Ifc4.Kernel.IfcProject>();
                //}
                //else
                //{
                //    throw new NotSupportedException("No Support Version");
                //}
            }
            //先做一个最简单的，两个Ifc2X3的IFC模型合并，后续再考虑版本问题
            var bigProject = bigModel.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();
            var smallProject = smallModel.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();

            var bigBuildings = bigProject.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as Xbim.Ifc2x3.ProductExtension.IfcBuilding;
            var smallBuildings = smallProject.Sites.FirstOrDefault()?.Buildings.FirstOrDefault();

            //处理95%
            List<Tuple<int, double, double>> StoreyDic = new List<Tuple<int, double, double>>();
            foreach (Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey BuildingStorey in bigBuildings.BuildingStoreys)
            {
                double Storey_Elevation = BuildingStorey.Elevation.Value;
                double Storey_Height = double.Parse(((BuildingStorey.PropertySets.FirstOrDefault().PropertySetDefinitions.FirstOrDefault() as Xbim.Ifc2x3.Kernel.IfcPropertySet).HasProperties.FirstOrDefault(o => o.Name == "Height") as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue).NominalValue.Value.ToString());
                StoreyDic.Add((int.Parse(BuildingStorey.Name.ToString()), Storey_Elevation, Storey_Height).ToTuple());
            }
            StoreyDic = StoreyDic.OrderBy(x => x.Item1).ToList();

            //处理5%
            foreach (Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey BuildingStorey in smallBuildings.BuildingStoreys)
            {
                var bigStorey = StoreyDic.FirstOrDefault(o => o.Item1.ToString() == BuildingStorey.Name);
                if (bigStorey.IsNull())
                {
                    var Storey_z = ((BuildingStorey.ObjectPlacement as Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement).RelativePlacement as Xbim.Ifc2x3.GeometryResource.IfcPlacement).Location.Z;
                    var relatedElements = BuildingStorey.ContainsElements.SelectMany(o => o.RelatedElements).Where(o =>
                    o is Xbim.Ifc2x3.SharedBldgElements.IfcWall || o is Xbim.Ifc2x3.SharedBldgElements.IfcBeam || o is Xbim.Ifc2x3.SharedBldgElements.IfcSlab || o is Xbim.Ifc2x3.SharedBldgElements.IfcColumn || o is Xbim.Ifc2x3.SharedBldgElements.IfcWindow || o is Xbim.Ifc2x3.SharedBldgElements.IfcDoor);
                    if (relatedElements.Any())
                    {
                        //找到该楼层的所有构建，找到最低的Location.Z
                        var relatedElement_z = relatedElements.Min(o => ((o.ObjectPlacement as Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement).RelativePlacement as Xbim.Ifc2x3.GeometryResource.IfcPlacement).Location.Z);
                        Storey_z += relatedElement_z;
                    }
                    bigStorey = StoreyDic.FirstOrDefault(o => Math.Abs(o.Item2 - Storey_z) <= 200);
                    if (bigStorey.IsNull())
                    {
                        if (Math.Abs(Storey_z - (StoreyDic.Last().Item2 + StoreyDic.Last().Item3)) <= 200)
                        {
                            //楼层高度 = 最顶层的标高 + 最顶层的层高，说明这个是新的一层
                            var storeyNo = StoreyDic.Last().Item1 + 1;
                            StoreyDic.Add((storeyNo, Storey_z, 0.0).ToTuple());
                            bigStorey = StoreyDic.Last();
                        }
                        else if (Storey_z < StoreyDic.First().Item2)
                        {
                            var storeyNo = StoreyDic.First().Item1 - 1;
                            if (storeyNo == 0)
                            {
                                storeyNo--;
                            }
                            StoreyDic.Insert(0, (storeyNo, Storey_z, StoreyDic.First().Item2 - Storey_z).ToTuple());
                            bigStorey = StoreyDic.First();
                        }
                        else if (Storey_z > (StoreyDic.Last().Item2 + StoreyDic.Last().Item3))
                        {
                            var storeyNo = StoreyDic.Last().Item1 + 1;
                            StoreyDic.Add((storeyNo, Storey_z, 0.0).ToTuple());
                            bigStorey = StoreyDic.Last();
                        }
                        else
                        {
                            bigStorey = StoreyDic.FirstOrDefault(o => Storey_z - o.Item2 > -200);
                        }
                    }
                }
                var storeyName = bigStorey.Item1.ToString().Replace('-', 'B');
                var storey = bigBuildings.BuildingStoreys.FirstOrDefault(o => o.Name==storeyName) as Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey;
                if (storey.IsNull())
                {
                    storey = BuildingStorey.CloneAndCreateNew(bigModel, bigBuildings, storeyName);
                }
                var CreatWalls = new List<Xbim.Ifc2x3.SharedBldgElements.IfcWall>();
                var CreatSlabs = new List<Xbim.Ifc2x3.SharedBldgElements.IfcSlab>();
                var CreatBeams = new List<Xbim.Ifc2x3.SharedBldgElements.IfcBeam>();
                var CreatColumns = new List<Xbim.Ifc2x3.SharedBldgElements.IfcColumn>();
                foreach (var spatialStructure in BuildingStorey.ContainsElements)
                {
                    {
                        //var elements = spatialStructure.RelatedElements;
                        //var walls = elements.OfType<Xbim.Ifc2x3.SharedBldgElements.IfcWall>();
                        //var wall = walls.FirstOrDefault();
                        ////示例： 一个墙最终表达到Viewer的坐标。 是自己的坐标 + wall_Location + Storey_Location
                        //var wall_z = ((wall.ObjectPlacement as Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement).RelativePlacement as Xbim.Ifc2x3.GeometryResource.IfcPlacement).Location.Z;
                    }
                    var elements = spatialStructure.RelatedElements;
                    var walls = elements.OfType<Xbim.Ifc2x3.SharedBldgElements.IfcWall>();
                    var slabs = elements.OfType<Xbim.Ifc2x3.SharedBldgElements.IfcSlab>();
                    var beams = elements.OfType<Xbim.Ifc2x3.SharedBldgElements.IfcBeam>();
                    var columns = elements.OfType<Xbim.Ifc2x3.SharedBldgElements.IfcColumn>();
                    {
                        //var wall = walls.FirstOrDefault();
                        ////示例： 一个墙最终表达到Viewer的坐标。 是自己的坐标 + wall_Location + Storey_Location
                        //var wall_z = ((wall.ObjectPlacement as Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement).RelativePlacement as Xbim.Ifc2x3.GeometryResource.IfcPlacement).Location.Z;
                    }
                    foreach (var wall in walls)
                    {
                        var newWall = wall.CloneAndCreateNew(bigModel);
                        CreatWalls.Add(newWall);
                    }
                    foreach (var slab in slabs)
                    {
                        var newSlab = slab.CloneAndCreateNew(bigModel);
                        CreatSlabs.Add(newSlab);
                    }
                    foreach (var beam in beams)
                    {
                        var newBeam = beam.CloneAndCreateNew(bigModel);
                        CreatBeams.Add(newBeam);
                    }
                    foreach (var column in columns)
                    {
                        var newColumn = column.CloneAndCreateNew(bigModel);
                        CreatColumns.Add(newColumn);
                    }
                }
                using (var txn = bigModel.BeginTransaction("relContainEntitys2Storey"))
                {
                    //for ifc2x3
                    var relContainedIn = bigModel.Instances.New<Xbim.Ifc2x3.ProductExtension.IfcRelContainedInSpatialStructure>();
                    storey.ContainsElements.Append<Xbim.Ifc2x3.Interfaces.IIfcRelContainedInSpatialStructure>(relContainedIn);

                    relContainedIn.RelatingStructure = storey;
                    relContainedIn.RelatedElements.AddRange(CreatWalls);
                    relContainedIn.RelatedElements.AddRange(CreatSlabs);
                    relContainedIn.RelatedElements.AddRange(CreatBeams);
                    relContainedIn.RelatedElements.AddRange(CreatColumns);
                    txn.Commit();
                }
            }

            //返回
            return bigModel;
        }

        public IfcStore ModelMerge(string filePath, ThTCHProject tchProject)
        {
            var bigModel = IfcStore.Open(filePath);
            if (tchProject != null)
            {
                return ModelMerge(bigModel, tchProject);
            }
            else
            {
                return null;
            }
        }

        public IfcStore ModelMerge(IfcStore bigModel, ThTCHProject tchProject)
        {
            var bigProject = bigModel.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();
            var bigBuildings = bigProject.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as Xbim.Ifc2x3.ProductExtension.IfcBuilding;
            //处理95%
            List<Tuple<int, double, double>> StoreyDic = new List<Tuple<int, double, double>>();
            foreach (Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey BuildingStorey in bigBuildings.BuildingStoreys)
            {
                double Storey_Elevation = BuildingStorey.Elevation.Value;
                double Storey_Height = double.Parse(((BuildingStorey.PropertySets.FirstOrDefault().PropertySetDefinitions.FirstOrDefault() as Xbim.Ifc2x3.Kernel.IfcPropertySet).HasProperties.FirstOrDefault(o => o.Name == "Height") as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue).NominalValue.Value.ToString());
                StoreyDic.Add((int.Parse(BuildingStorey.Name.ToString()), Storey_Elevation, Storey_Height).ToTuple());
            }
            StoreyDic = StoreyDic.OrderBy(x => x.Item1).ToList();
            //处理5%
            foreach (ThTCHBuildingStorey BuildingStorey in tchProject.Site.Building.Storeys)
            {
                    var Storey_z = BuildingStorey.Elevation;
                    var bigStorey = StoreyDic.FirstOrDefault(o => Math.Abs(o.Item2 - Storey_z) <= 200);
                    if (bigStorey.IsNull())
                    {
                        if (Math.Abs(Storey_z - (StoreyDic.Last().Item2 + StoreyDic.Last().Item3)) <= 200)
                        {
                            //楼层高度 = 最顶层的标高 + 最顶层的层高，说明这个是新的一层
                            var storeyNo = StoreyDic.Last().Item1 + 1;
                            StoreyDic.Add((storeyNo, Storey_z, 0.0).ToTuple());
                            bigStorey = StoreyDic.Last();
                        }
                        else if (Storey_z < StoreyDic.First().Item2)
                        {
                            var storeyNo = StoreyDic.First().Item1 - 1;
                            if (storeyNo == 0)
                            {
                                storeyNo--;
                            }
                            StoreyDic.Insert(0, (storeyNo, Storey_z, StoreyDic.First().Item2 - Storey_z).ToTuple());
                            bigStorey = StoreyDic.First();
                        }
                        else if (Storey_z > (StoreyDic.Last().Item2 + StoreyDic.Last().Item3))
                        {
                            var storeyNo = StoreyDic.Last().Item1 + 1;
                            StoreyDic.Add((storeyNo, Storey_z, 0.0).ToTuple());
                            bigStorey = StoreyDic.Last();
                        }
                        else
                        {
                            bigStorey = StoreyDic.FirstOrDefault(o => Storey_z - o.Item2 > -200);
                        }
                    }
                var storeyName = bigStorey.Item1.ToString().Replace('-', 'B');
                var storey = bigBuildings.BuildingStoreys.FirstOrDefault(o => o.Name==storeyName) as Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey;
                if (storey.IsNull())
                {
                    BuildingStorey.Number = storeyName;
                    BuildingStorey.Properties["FloorNo"] = storeyName;
                    BuildingStorey.Properties["StdFlrNo"] = storeyName;
                    storey = ThTGL2IFC2x3Factory.CreateStorey(bigModel, bigBuildings, BuildingStorey);
                }
                var CreatWalls = new List<Xbim.Ifc2x3.SharedBldgElements.IfcWall>();
                var CreatSlabs = new List<Xbim.Ifc2x3.SharedBldgElements.IfcSlab>();
                var CreatBeams = new List<Xbim.Ifc2x3.SharedBldgElements.IfcBeam>();
                var CreatColumns = new List<Xbim.Ifc2x3.SharedBldgElements.IfcColumn>();
                var floor_origin = BuildingStorey.Origin;
                foreach (var thtchwall in BuildingStorey.Walls)
                {
                    var wall = ThTGL2IFC2x3Factory.CreateWall(bigModel, thtchwall, floor_origin);
                    CreatWalls.Add(wall);
                }
                foreach (var thtchbeam in BuildingStorey.Beams)
                {
                    var beam = ThTGL2IFC2x3Factory.CreateBeam(bigModel, thtchbeam, floor_origin);
                    CreatBeams.Add(beam);
                }
                foreach (var thtchcolumn in BuildingStorey.Columns)
                {
                    var column = ThTGL2IFC2x3Factory.CreateColumn(bigModel, thtchcolumn, floor_origin);
                    CreatColumns.Add(column);
                }
                foreach (var thtchslab in BuildingStorey.Slabs)
                {
                    var slab = ThTGL2IFC2x3Factory.CreateMeshSlab(bigModel, thtchslab, floor_origin);
                    if (null !=slab)
                        CreatSlabs.Add(slab);
                }
                
                using (var txn = bigModel.BeginTransaction("relContainEntitys2Storey"))
                {
                    //for ifc2x3
                    var relContainedIn = bigModel.Instances.New<Xbim.Ifc2x3.ProductExtension.IfcRelContainedInSpatialStructure>();
                    storey.ContainsElements.Append<Xbim.Ifc2x3.Interfaces.IIfcRelContainedInSpatialStructure>(relContainedIn);

                    relContainedIn.RelatingStructure = storey;
                    relContainedIn.RelatedElements.AddRange(CreatWalls);
                    relContainedIn.RelatedElements.AddRange(CreatSlabs);
                    relContainedIn.RelatedElements.AddRange(CreatBeams);
                    relContainedIn.RelatedElements.AddRange(CreatColumns);
                    txn.Commit();
                }
            }

            //返回
            return bigModel;
        }
    }
}
