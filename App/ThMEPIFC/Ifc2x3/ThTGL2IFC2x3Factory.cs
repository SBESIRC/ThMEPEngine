using System.Linq;
using ThMEPTCH.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Xbim.Ifc;
using Xbim.Common;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Interfaces;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.GeometryResource;

namespace ThMEPIFC.Ifc2x3
{
    public partial class ThTGL2IFC2x3Factory
    {
        public static IfcStore CreateAndInitModel(string projectName, string projectId = "")
        {
            var model = ThIFC2x3Factory.CreateMemoryModel();
            using (var txn = model.BeginTransaction("Initialize Model"))
            {
                //there should always be one project in the model
                var project = model.Instances.New<IfcProject>(p => p.Name = projectName);
                //set the units to SI (mm and metres)
                project.Initialize(ProjectUnits.SIUnitsUK);
                //set GeometricRepresentationContext
                project.RepresentationContexts.Add(CreateGeometricRepresentationContext(model));
                //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                txn.Commit();
            }
            return model;
        }

        private static IfcGeometricRepresentationContext CreateGeometricRepresentationContext(IfcStore model)
        {
            return model.Instances.New<IfcGeometricRepresentationContext>(c =>
            {
                c.Precision = ThTGL2IFCCommon.PRECISION;
                c.CoordinateSpaceDimension = new IfcDimensionCount(3);
                c.WorldCoordinateSystem = model.ToIfcAxis2Placement3D(Point3d.Origin);
            });
        }

        public static IfcSite CreateSite(IfcStore model, ThTCHSite site)
        {
            using (var txn = model.BeginTransaction("Initialise Site"))
            {
                var ret = model.Instances.New<IfcSite>(s =>
                {
                    s.ObjectPlacement = model.ToIfcLocalPlacement(WCS());
                });
                //get the project there should only be one and it should exist
                var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
                project.AddSite(ret);
                txn.Commit();
                return ret;
            }
        }

        private static CoordinateSystem3d WCS()
        {
            return Matrix3d.Identity.CoordinateSystem3d;
        }

        public static IfcBuilding CreateBuilding(IfcStore model, IfcSite site, ThTCHBuilding building)
        {
            using (var txn = model.BeginTransaction("Initialise Building"))
            {
                var ret = model.Instances.New<IfcBuilding>(b =>
                {
                    b.Name = building.BuildingName;
                    b.CompositionType = IfcElementCompositionEnum.ELEMENT;
                    b.ObjectPlacement = model.ToIfcLocalPlacement(WCS(), site.ObjectPlacement);
                });
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        foreach (var item in building.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });
                site.AddBuilding(ret);
                txn.Commit();
                return ret;
            }
        }

        public static IfcBuildingStorey CreateStorey(IfcStore model, IfcBuilding building, ThTCHBuildingStorey storey)
        {
            using (var txn = model.BeginTransaction("Create Storey"))
            {
                var ret = model.Instances.New<IfcBuildingStorey>(s =>
                {
                    s.Name = storey.Number;
                    s.ObjectPlacement = model.ToIfcLocalPlacement(WCS(), building.ObjectPlacement);
                    s.Elevation = storey.Elevation;
                });
                // setup aggregation relationship
                var ifcRel = model.Instances.New<IfcRelAggregates>();
                ifcRel.RelatingObject = building;
                ifcRel.RelatedObjects.Add(ret);

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        foreach (var item in storey.Properties)
                        {
                            if (!item.Key.Equals("Height"))
                            {
                                pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = item.Key;
                                    p.NominalValue = new IfcText(item.Value.ToString());
                                }));
                            }
                            else
                            {
                                pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = item.Key;
                                    p.NominalValue = new IfcLengthMeasure(double.Parse(item.Value));
                                }));
                            }
                        }
                    });
                });
                txn.Commit();
                return ret;
            }
        }

        private static IfcProductDefinitionShape CreateProductDefinitionShape(IfcStore model, IfcExtrudedAreaSolid solid)
        {
            //Create a Definition shape to hold the geometry
            var shape = model.Instances.New<IfcShapeRepresentation>();
            var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
            shape.ContextOfItems = modelContext;
            shape.RepresentationType = "SweptSolid";
            shape.RepresentationIdentifier = "Body";
            shape.Items.Add(solid);

            //Create a Product Definition and add the model geometry to the wall
            var rep = model.Instances.New<IfcProductDefinitionShape>();
            rep.Representations.Add(shape);
            return rep;
        }

        public static void relContainWalls2Storey(IfcStore model, List<IfcWall> walls, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainWalls2Storey"))
            {
                //for ifc2x3
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var wall in walls)
                {
                    relContainedIn.RelatedElements.Add(wall);
                    //Storey.AddElement(wall);
                }
                relContainedIn.RelatingStructure = Storey;

                txn.Commit();
            }
        }

        public static void relContainColumns2Storey(IfcStore model, List<IfcColumn> columns, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainColumns2Storey"))
            {
                //for ifc2x3
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var wall in columns)
                {
                    relContainedIn.RelatedElements.Add(wall);
                    //Storey.AddElement(wall);
                }
                relContainedIn.RelatingStructure = Storey;

                txn.Commit();
            }
        }

        public static void relContainBeams2Storey(IfcStore model, List<IfcBeam> beams, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainColumns2Storey"))
            {
                //for ifc2x3
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var wall in beams)
                {
                    relContainedIn.RelatedElements.Add(wall);
                    //Storey.AddElement(wall);
                }
                relContainedIn.RelatingStructure = Storey;

                txn.Commit();
            }
        }

        public static void relContainDoors2Storey(IfcStore model, List<IfcDoor> doors, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainDoors2Storey"))
            {
                //for ifc2x3
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var door in doors)
                {
                    relContainedIn.RelatedElements.Add(door);
                    //Storey.AddElement(door);
                }
                relContainedIn.RelatingStructure = Storey;
                txn.Commit();
            }
        }

        public static void relContainWindows2Storey(IfcStore model, List<IfcWindow> windows, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainWindows2Storey"))
            {
                //for ifc2x3
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var window in windows)
                {
                    relContainedIn.RelatedElements.Add(window);
                    //Storey.AddElement(window);
                }
                relContainedIn.RelatingStructure = Storey;
                txn.Commit();
            }
        }

        public static void relContainSlabs2Storey(IfcStore model, List<IfcSlab> slabs, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainSlabs2Storey"))
            {
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var slab in slabs)
                {
                    relContainedIn.RelatedElements.Add(slab);
                }
                relContainedIn.RelatingStructure = Storey;
                txn.Commit();
            }
        }

        public static void relContainsRailings2Storey(IfcStore model, List<IfcRailing> railings, IfcBuildingStorey storey)
        {
            using (var txn = model.BeginTransaction("relContainsRailings2Storey"))
            {
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var railing in railings)
                {
                    relContainedIn.RelatedElements.Add(railing);
                }
                relContainedIn.RelatingStructure = storey;
                txn.Commit();
            }
        }

        public static void relContainsRooms2Storey(IfcStore model, List<IfcSpace> rooms, IfcBuildingStorey storey)
        {
            using (var txn = model.BeginTransaction("relContainsRooms2Storey"))
            {
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var room in rooms)
                {
                    relContainedIn.RelatedElements.Add(room);
                }
                relContainedIn.RelatingStructure = storey;
                txn.Commit();
            }
        }
    }
}
