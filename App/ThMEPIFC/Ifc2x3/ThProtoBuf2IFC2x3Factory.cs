using System.Linq;
using System.Collections.Generic;
using Xbim.Ifc;
using Xbim.Common;
using Xbim.Ifc2x3.Kernel;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.Interfaces;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.GeometryResource;
using ThMEPIFC.Geometry;

namespace ThMEPIFC.Ifc2x3
{
    public partial class ThProtoBuf2IFC2x3Factory
    {
        private static XbimVector3D ZAxis => new XbimVector3D(0, 0, 1);

        static public IfcStore CreateAndInitModel(string projectName, string projectId = "")
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
                c.WorldCoordinateSystem = model.ToIfcAxis2Placement3D(XbimPoint3D.Zero);
            });
        }

        public static IfcSite CreateSite(IfcStore model)
        {
            using (var txn = model.BeginTransaction("Initialise Site"))
            {
                var ret = model.Instances.New<IfcSite>(s =>
                {
                    s.ObjectPlacement = model.ToIfcLocalPlacement();
                });
                //get the project there should only be one and it should exist
                var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
                project.AddSite(ret);
                txn.Commit();
                return ret;
            }
        }

        public static IfcBuilding CreateBuilding(IfcStore model, IfcSite site, ThTCHBuildingData building)
        {
            using (var txn = model.BeginTransaction("Initialise Building"))
            {
                var ret = model.Instances.New<IfcBuilding>(b =>
                {
                    b.Name = building.Root.Name;
                    b.CompositionType = IfcElementCompositionEnum.ELEMENT;
                    b.ObjectPlacement = model.ToIfcLocalPlacement(site.ObjectPlacement);
                });
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        //foreach (var item in building.Properties)
                        //{
                        //    pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                        //    {
                        //        p.Name = item.Key;
                        //        p.NominalValue = new IfcText(item.Value.ToString());
                        //    }));
                        //}
                    });
                });
                site.AddBuilding(ret);
                txn.Commit();
                return ret;
            }
        }

        public static IfcBuilding CreateBuilding(IfcStore model, IfcSite site, string buildingName)
        {
            using (var txn = model.BeginTransaction("Initialise Building"))
            {
                var ret = model.Instances.New<IfcBuilding>(b =>
                {
                    b.Name = buildingName;
                    b.CompositionType = IfcElementCompositionEnum.ELEMENT;
                    b.ObjectPlacement = model.ToIfcLocalPlacement(site.ObjectPlacement);
                });
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        //foreach (var item in building.Properties)
                        //{
                        //    pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                        //    {
                        //        p.Name = item.Key;
                        //        p.NominalValue = new IfcText(item.Value.ToString());
                        //    }));
                        //}
                    });
                });
                site.AddBuilding(ret);
                txn.Commit();
                return ret;
            }
        }

        static public IfcBuildingStorey CreateStorey(IfcStore model, IfcBuilding building, ThTCHBuildingStoreyData storey)
        {
            using (var txn = model.BeginTransaction("Create Storey"))
            {
                var ret = model.Instances.New<IfcBuildingStorey>(s =>
                {
                    s.Name = storey.Number;
                    s.ObjectPlacement = model.ToIfcLocalPlacement(building.ObjectPlacement);
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
                        foreach (var item in storey.BuildElement.Properties)
                        {
                            if (!item.Key.Equals("Height"))
                            {
                                pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = item.Key;
                                    p.NominalValue = new IfcText(item.Value);
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

        static public IfcBuildingStorey CreateStorey(IfcStore model, IfcBuilding building, string storeyName)
        {
            using (var txn = model.BeginTransaction("Create Storey"))
            {
                var ret = model.Instances.New<IfcBuildingStorey>(s =>
                {
                    s.Name = storeyName;
                    s.ObjectPlacement = model.ToIfcLocalPlacement(building.ObjectPlacement);
                });
                // setup aggregation relationship
                var ifcRel = model.Instances.New<IfcRelAggregates>();
                ifcRel.RelatingObject = building;
                ifcRel.RelatedObjects.Add(ret);

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

        static public void relContainWalls2Storey(IfcStore model, List<IfcWall> walls, IfcBuildingStorey Storey)
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

        static public void relContainColumns2Storey(IfcStore model, List<IfcColumn> columns, IfcBuildingStorey Storey)
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

        static public void relContainBeams2Storey(IfcStore model, List<IfcBeam> beams, IfcBuildingStorey Storey)
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

        static public void relContainDoors2Storey(IfcStore model, List<IfcDoor> doors, IfcBuildingStorey Storey)
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

        static public void relContainWindows2Storey(IfcStore model, List<IfcWindow> windows, IfcBuildingStorey Storey)
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

        static public void relContainSlabs2Storey(IfcStore model, List<IfcSlab> slabs, IfcBuildingStorey Storey)
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

        static public void relContainsRailings2Storey(IfcStore model, List<IfcRailing> railings, IfcBuildingStorey storey)
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
        static public void relContainsRooms2Storey(IfcStore model, List<IfcSpace> rooms, IfcBuildingStorey storey)
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

        static public void relContainsSUElements2Storey(IfcStore model, List<IfcBuildingElementProxy> elements, IfcBuildingStorey storey)
        {
            using (var txn = model.BeginTransaction("relContainsSUElements2Storey"))
            {
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var element in elements)
                {
                    relContainedIn.RelatedElements.Add(element);
                }
                relContainedIn.RelatingStructure = storey;
                txn.Commit();
            }
        }

        #region Wall
        public static IfcWall CreateWall(IfcStore model, ThTCHWallData wall, ThTCHPoint3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Wall"))
            {
                var ret = model.Instances.New<IfcWall>();
                ret.Name = "A Standard rectangular wall";

                //create representation
                var profile = GetProfile(model, wall);
                var solid = model.ToIfcExtrudedAreaSolid(profile, ZAxis, wall.BuildElement.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var transform = GetTransfrom(wall, floor_origin);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(transform);

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        foreach (var item in wall.BuildElement.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });

                txn.Commit();
                return ret;
            }
        }

        private static ThTCHMatrix3d GetTransfrom(ThTCHWallData wall, ThTCHPoint3d floor_origin)
        {
            //IFC创建的平面初始化是在XY平面的。所以需要增加一个Z值
            var offset = new XbimVector3D(
                floor_origin.X + wall.BuildElement.Origin.X,
                floor_origin.Y + wall.BuildElement.Origin.Y,
                floor_origin.Z + wall.BuildElement.Outline.Shell.Points[0].Z + wall.BuildElement.Origin.Z);
            return ThXbimExtension.MultipleTransformFroms(1.0, wall.BuildElement.XVector.ToXbimVector3D(), offset).ToTCHMatrix3d();
        }

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHWallData wall)
        {
            if (wall.BuildElement.Outline.Shell.Points.Count > 0)
            {
                return model.ToIfcArbitraryClosedProfileDef(wall.BuildElement.Outline);
            }
            else
            {
                return model.ToIfcRectangleProfileDef(wall.BuildElement.Length, wall.BuildElement.Width);
            }
        }
        #endregion

        #region Door
        public static IfcDoor CreateDoor(IfcStore model, ThTCHDoorData door, ThTCHPoint3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Door"))
            {
                var ret = model.Instances.New<IfcDoor>();
                ret.Name = "Door";

                //create representation
                var profile = model.ToIfcRectangleProfileDef(door.BuildElement.Length, door.BuildElement.Width);
                var solid = model.ToIfcExtrudedAreaSolid(profile, ZAxis, door.BuildElement.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var transform = GetTransfrom(door, floor_origin);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(transform);

                // add properties
                //model.Instances.New<IfcRelDefinesByProperties>(rel =>
                //{
                //    rel.Name = "THifc properties";
                //    rel.RelatedObjects.Add(ret);
                //    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                //    {
                //        pset.Name = "Basic set of THifc properties";
                //        foreach (var item in wall.Properties)
                //        {
                //            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                //            {
                //                p.Name = item.Key;
                //                p.NominalValue = new IfcText(item.Value.ToString());
                //            }));
                //        }
                //    });
                //});

                txn.Commit();
                return ret;
            }
        }

        private static ThTCHMatrix3d GetTransfrom(ThTCHDoorData door, ThTCHPoint3d floor_origin)
        {
            var offset = new XbimVector3D(
                floor_origin.X + door.BuildElement.Origin.X,
                floor_origin.Y + door.BuildElement.Origin.Y,
                floor_origin.Z + door.BuildElement.Origin.Z);
            return ThXbimExtension.MultipleTransformFroms(1.0, door.BuildElement.XVector.ToXbimVector3D(), offset).ToTCHMatrix3d();
        }
        #endregion

        #region Hole
        public static IfcOpeningElement CreateHole(IfcStore model, ThTCHWallData wall, ThTCHDoorData door, ThTCHPoint3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Hole"))
            {
                var ret = model.Instances.New<IfcOpeningElement>();
                ret.Name = "Door Hole";

                //create representation
                var profile = GetProfile(model, wall, door);
                var solid = model.ToIfcExtrudedAreaSolid(profile, ZAxis, door.BuildElement.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var transform = GetTransfrom(door, floor_origin);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(transform);

                txn.Commit();
                return ret;
            }
        }

        public static IfcOpeningElement CreateHole(IfcStore model, ThTCHWallData wall, ThTCHWindowData window, ThTCHPoint3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Hole"))
            {
                var ret = model.Instances.New<IfcOpeningElement>();
                ret.Name = "Window Hole";

                //create representation
                var profile = GetProfile(model, wall, window);
                var solid = model.ToIfcExtrudedAreaSolid(profile, ZAxis, window.BuildElement.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var transform = GetTransfrom(window, floor_origin);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(transform);

                txn.Commit();
                return ret;
            }
        }

        public static IfcOpeningElement CreateHole(IfcStore model, ThTCHOpeningData hole, ThTCHPoint3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Hole"))
            {
                var ret = model.Instances.New<IfcOpeningElement>();
                ret.Name = "Generic Hole";

                //create representation
                var profile = GetProfile(model, hole);
                var solid = model.ToIfcExtrudedAreaSolid(profile, ZAxis, hole.BuildElement.Height);

                //object placement
                var transform = GetTransfrom(hole, floor_origin);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(transform);

                txn.Commit();
                return ret;
            }
        }

        private static ThTCHMatrix3d GetTransfrom(ThTCHOpeningData hole, ThTCHPoint3d floor_origin)
        {
            var offset = new XbimVector3D(
                floor_origin.X + hole.BuildElement.Origin.X,
                floor_origin.Y + hole.BuildElement.Origin.Y,
                floor_origin.Z + hole.BuildElement.Origin.Z);
            return ThXbimExtension.MultipleTransformFroms(1.0, hole.BuildElement.XVector.ToXbimVector3D(), offset).ToTCHMatrix3d();
        }

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHOpeningData hole)
        {
            return model.ToIfcRectangleProfileDef(hole.BuildElement.Length, hole.BuildElement.Width);
        }

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHWallData wall, ThTCHWindowData window)
        {
            // 为了确保开洞完全贯通墙，
            // 洞的宽度需要在墙的厚度的基础上增加一定的延伸
            // TODO：
            //  1）暂时只支持直墙和弧墙
            //  2）弧墙的延伸量需要考虑弧的半径以及墙的厚度，这里暂时给一个经验值
            return model.ToIfcRectangleProfileDef(window.BuildElement.Length, wall.BuildElement.Width + 120);
        }

        private static IfcProfileDef GetProfile(IfcStore model, ThTCHWallData wall, ThTCHDoorData door)
        {
            // 为了确保开洞完全贯通墙，
            // 洞的宽度需要在墙的厚度的基础上增加一定的延伸
            // TODO：
            //  1）暂时只支持直墙和弧墙
            //  2）弧墙的延伸量需要考虑弧的半径以及墙的厚度，这里暂时给一个经验值
            return model.ToIfcRectangleProfileDef(door.BuildElement.Length, wall.BuildElement.Width + 120);

        }
        #endregion

        #region Window
        public static IfcWindow CreateWindow(IfcStore model, ThTCHWindowData window, ThTCHPoint3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Window"))
            {
                var ret = model.Instances.New<IfcWindow>();
                ret.Name = "Window";

                //create representation
                var profile = model.ToIfcRectangleProfileDef(window.BuildElement.Length, window.BuildElement.Width);
                var solid = model.ToIfcExtrudedAreaSolid(profile, ZAxis, window.BuildElement.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var transform = GetTransfrom(window, floor_origin);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(transform);

                // add properties
                //model.Instances.New<IfcRelDefinesByProperties>(rel =>
                //{
                //    rel.Name = "THifc properties";
                //    rel.RelatedObjects.Add(ret);
                //    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                //    {
                //        pset.Name = "Basic set of THifc properties";
                //        foreach (var item in wall.Properties)
                //        {
                //            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                //            {
                //                p.Name = item.Key;
                //                p.NominalValue = new IfcText(item.Value.ToString());
                //            }));
                //        }
                //    });
                //});

                txn.Commit();
                return ret;
            }
        }

        private static ThTCHMatrix3d GetTransfrom(ThTCHWindowData window, ThTCHPoint3d floor_origin)
        {
            var offset = new XbimVector3D(
                floor_origin.X + window.BuildElement.Origin.X,
                floor_origin.Y + window.BuildElement.Origin.Y,
                floor_origin.Z + window.BuildElement.Origin.Z);
            return ThXbimExtension.MultipleTransformFroms(1.0, window.BuildElement.XVector.ToXbimVector3D(), offset).ToTCHMatrix3d();
        }
        #endregion

        #region Relationship
        public static void BuildRelationship(IfcStore model, IfcWall wall, IfcBuildingElement element, IfcOpeningElement hole)
        {
            using (var txn = model.BeginTransaction())
            {
                //create relVoidsElement
                var relVoidsElement = model.Instances.New<IfcRelVoidsElement>();
                relVoidsElement.RelatedOpeningElement = hole;
                relVoidsElement.RelatingBuildingElement = wall;

                //create relFillsElement
                var relFillsElement = model.Instances.New<IfcRelFillsElement>();
                relFillsElement.RelatingOpeningElement = hole;
                relFillsElement.RelatedBuildingElement = element;

                txn.Commit();
            }
        }

        public static void BuildRelationship(IfcStore model, IfcWall wall, IfcOpeningElement hole)
        {
            using (var txn = model.BeginTransaction())
            {
                //create relVoidsElement
                var relVoidsElement = model.Instances.New<IfcRelVoidsElement>();
                relVoidsElement.RelatedOpeningElement = hole;
                relVoidsElement.RelatingBuildingElement = wall;

                txn.Commit();
            }
        }
        #endregion

        #region Slab
        public static IfcSlab CreateBrepSlab(IfcStore model, ThTCHSlabData slab, ThTCHPoint3d floor_origin, ThXbimSlabEngine slabxbimEngine)
        {
            using (var txn = model.BeginTransaction("Create Slab"))
            {
                var ret = model.Instances.New<IfcSlab>();
                ret.Name = "Slab";

                //create representation
                var solids = slab.GetSlabSolid(slabxbimEngine);
                var body = model.CreateIfcFacetedBrep(solids);
                var shape = ThIFC2x3Factory.CreateBrepBody(model, body);
                ret.Representation = ThIFC2x3Factory.CreateProductDefinitionShape(model, shape);

                //object placement
                ret.ObjectPlacement = model.ToIfcLocalPlacement(floor_origin);

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        foreach (var item in slab.BuildElement.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });

                txn.Commit();
                return ret;
            }
        }
        #endregion

        #region Railing
        public static IfcRailing CreateRailing(IfcStore model, ThTCHRailingData railing, ThTCHPoint3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Railing"))
            {
                var ret = model.Instances.New<IfcRailing>();
                ret.Name = "TH Railing";
                //create representation
                var centerline = railing.BuildElement.Outline;
                //var outlines = centerline.Shell.ToPolyline().BufferFlatPL(railing.BuildElement.Width / 2.0);
                var profile = model.ToIfcArbitraryClosedProfileDef(railing.BuildElement.Outline);
                var solid = model.ToIfcExtrudedAreaSolid(profile, ZAxis, railing.BuildElement.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var planeOrigin = new ThTCHPoint3d
                {
                    X = floor_origin.X,
                    Y = floor_origin.Y,
                    Z = floor_origin.Z + centerline.Shell.Points[0].Z
                };
                ret.ObjectPlacement = model.ToIfcLocalPlacement(planeOrigin);

                txn.Commit();
                return ret;
            }
        }
        #endregion

        #region Room
        public static IfcSpace CreateRoom(IfcStore model, ThTCHRoomData space, ThTCHPoint3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Room"))
            {
                var ret = model.Instances.New<IfcSpace>();
                ret.Name = "Room";
                ret.Description = space.BuildElement.Root.Name;

                //create representation
                var profile = model.ToIfcArbitraryProfileDefWithVoids(space.BuildElement.Outline);
                var solid = model.ToIfcExtrudedAreaSolid(profile, ZAxis, space.BuildElement.Height);
                ret.Representation = CreateProductDefinitionShape(model, solid);
                ret.ObjectPlacement = model.ToIfcLocalPlacement(floor_origin);
                txn.Commit();
                return ret;
            }

        }
        #endregion
    }
}
