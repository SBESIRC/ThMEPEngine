﻿using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Xbim.IO;
using Xbim.Ifc;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Interfaces;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using ThMEPTCH.Model;

namespace ThMEPIFC.Ifc2x3
{
    public class ThTGL2IFC2x3Factory
    {
        static int epsilon = 0;

        static public IfcStore CreateAndInitModel(string projectName)
        {
            var model = CreateModel();
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

        private static IfcStore CreateModel()
        {
            return IfcStore.Create(XbimSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);
        }

        private static IfcGeometricRepresentationContext CreateGeometricRepresentationContext(IfcStore model)
        {
            return model.Instances.New<IfcGeometricRepresentationContext>(c =>
            {
                c.Precision = 1E-5;
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

        static public IfcBuildingStorey CreateStorey(IfcStore model, IfcBuilding building, ThTCHBuildingStorey storey)
        {
            using (var txn = model.BeginTransaction("Create Storey"))
            {
                var ret = model.Instances.New<IfcBuildingStorey>(s =>
                {
                    s.Name = storey.Number;
                    s.ObjectPlacement = model.ToIfcLocalPlacement(WCS(), building.ObjectPlacement);
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

        private static IfcProductDefinitionShape CreateProductDefinitionShape(IfcStore model, IfcExtrudedAreaSolid solid)
        {
            using (var txn = model.BeginTransaction("Create SweptSolid Shape"))
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

                txn.Commit();
                return rep;
            }
        }

        public static IfcRailing CreateRailing(IfcStore model, ThTCHRailing railing, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Railing"))
            {
                var ret = model.Instances.New<IfcRailing>();

                //geometry representation
                var centerline = railing.Outline as Polyline;
                var outlines = centerline.BufferFlatPL(railing.Thickness / 2.0);
                var profile = model.ToIfcArbitraryClosedProfileDef(outlines[0] as Entity);
                var solid = model.ToIfcExtrudedAreaSolid(profile, railing.ExtrudedDirection, railing.Depth);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(Vector3d.XAxis);
                    p.Location = model.ToIfcCartesianPoint(floor_origin);
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                txn.Commit();
                return ret;
            }
        }

        static public IfcWall CreateWall(IfcStore model, ThTCHWall wall, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Wall"))
            {
                var ret = model.Instances.New<IfcWall>();
                ret.Name = "A Standard rectangular wall";

                //model as a swept area solid
                IfcProfileDef profile = null;
                if (wall.Outline is Polyline pline)
                {
                    profile = model.ToIfcArbitraryClosedProfileDef(pline);
                }
                else
                {
                    profile = model.ToIfcRectangleProfileDef(wall.Length, wall.Width);
                }
                var body = model.ToIfcExtrudedAreaSolid(profile, wall.ExtrudedDirection, wall.Height);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(wall.XVector);
                    p.Location = model.ToIfcCartesianPoint(wall.Origin + floor_origin.GetAsVector());
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        //pset.HasProperties.AddRange(new[] {
                        //    model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "OpenDirection";
                        //        p.NominalValue=new IfcText(window.OpenDirection);
                        //    }),
                        //     model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "Test property";
                        //        p.NominalValue=new IfcText("nothing");
                        //    })
                        //});
                        foreach (var item in wall.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });
                /*
                // Where Clause: The IfcWallStandard relies on the provision of an IfcMaterialLayerSetUsage 
                var ifcMaterialLayerSetUsage = model.Instances.New<IfcMaterialLayerSetUsage>();
                var ifcMaterialLayerSet = model.Instances.New<IfcMaterialLayerSet>();
                var ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>();
                ifcMaterialLayer.LayerThickness = 10;
                ifcMaterialLayerSet.MaterialLayers.Add(ifcMaterialLayer);
                ifcMaterialLayerSetUsage.ForLayerSet = ifcMaterialLayerSet;
                ifcMaterialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS2;
                ifcMaterialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                ifcMaterialLayerSetUsage.OffsetFromReferenceLine = 150;

                // Add material to wall
                var material = model.Instances.New<IfcMaterial>();
                material.Name = "some material";
                var ifcRelAssociatesMaterial = model.Instances.New<IfcRelAssociatesMaterial>();
                ifcRelAssociatesMaterial.RelatingMaterial = material;
                ifcRelAssociatesMaterial.RelatedObjects.Add(ret);

                ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;

                // IfcPresentationLayerAssignment is required for CAD presentation in IfcWall or IfcWallStandardCase
                var ifcPresentationLayerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                ifcPresentationLayerAssignment.Name = "some ifcPresentationLayerAssignment";
                ifcPresentationLayerAssignment.AssignedItems.Add(shape);


                // linear segment as IfcPolyline with two points is required for IfcWall
                var ifcPolyline = model.Instances.New<IfcPolyline>();
                var startPoint = model.Instances.New<IfcCartesianPoint>();
                startPoint.SetXY(0, 0);
                var endPoint = model.Instances.New<IfcCartesianPoint>();
                endPoint.SetXY(4000, 0);
                ifcPolyline.Points.Add(startPoint);
                ifcPolyline.Points.Add(endPoint);

                var shape2D = model.Instances.New<IfcShapeRepresentation>();
                shape2D.ContextOfItems = modelContext;
                shape2D.RepresentationIdentifier = "Axis";
                shape2D.RepresentationType = "Curve2D";
                shape2D.Items.Add(ifcPolyline);
                rep.Representations.Add(shape2D);*/
                txn.Commit();
                return ret;
            }
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
        static public IfcDoor CreateDoor(IfcStore model, ThTCHDoor door, IfcElement openedElement, ThTCHWall thwall, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Door"))
            {
                var ret = model.Instances.New<IfcDoor>();
                ret.Name = "door";

                //model as a swept area solid
                var profile = model.ToIfcRectangleProfileDef(door.Width, door.Thickness - epsilon);
                var body = model.ToIfcExtrudedAreaSolid(profile, door.ExtrudedDirection, door.Height);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(door.XVector);
                    p.Location = model.ToIfcCartesianPoint(door.CenterPoint + floor_origin.GetAsVector());
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        //pset.HasProperties.AddRange(new[] {
                        //    model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "OpenDirection";
                        //        p.NominalValue=new IfcText(window.OpenDirection);
                        //    }),
                        //     model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "Test property";
                        //        p.NominalValue=new IfcText("nothing");
                        //    })
                        //});
                        foreach (var item in door.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });

                //create opening element
                var hole = model.Instances.New<IfcOpeningElement>();
                hole.Name = "hole";
                //todo: describe hole's geometry

                var hole_rectProf = model.Instances.New<IfcRectangleProfileDef>(p =>
                {
                    p.XDim = door.Width;
                    p.YDim = thwall.Width;
                    p.ProfileType = IfcProfileTypeEnum.AREA;
                    p.Position = model.ToIfcAxis2Placement2D(default);
                });

                var hole_body = model.Instances.New<IfcExtrudedAreaSolid>(s =>
                {
                    s.Depth = door.Height;
                    s.SweptArea = hole_rectProf;
                    s.ExtrudedDirection = model.ToIfcDirection(door.ExtrudedDirection);
                });
                hole_body.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);

                var hole_shape = model.Instances.New<IfcShapeRepresentation>();
                var hole_modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                hole_shape.ContextOfItems = modelContext;
                hole_shape.RepresentationType = "SweptSolid";
                hole_shape.RepresentationIdentifier = "Body";
                hole_shape.Items.Add(hole_body);

                var hole_rep = model.Instances.New<IfcProductDefinitionShape>();
                hole_rep.Representations.Add(hole_shape);
                hole.Representation = hole_rep;

                hole.ObjectPlacement = lp;

                //create relVoidsElement
                var relVoidsElement = model.Instances.New<IfcRelVoidsElement>();
                relVoidsElement.RelatedOpeningElement = hole;
                relVoidsElement.RelatingBuildingElement = openedElement;

                //create relFillsElement
                var relFillsElement = model.Instances.New<IfcRelFillsElement>();
                relFillsElement.RelatingOpeningElement = hole;
                relFillsElement.RelatedBuildingElement = ret;

                txn.Commit();
                return ret;
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
        static public IfcWindow CreateWindow(IfcStore model, ThTCHWindow window, IfcElement openedElement, ThTCHWall thwall, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Window"))
            {
                var ret = model.Instances.New<IfcWindow>();
                ret.Name = "window";

                //represent wall as a rectangular profile
                var rectProf = model.Instances.New<IfcRectangleProfileDef>(p =>
                {
                    p.XDim = window.Width;
                    p.YDim = window.Thickness - epsilon;
                    p.ProfileType = IfcProfileTypeEnum.AREA;
                    p.Position = model.ToIfcAxis2Placement2D(default);
                });

                //model as a swept area solid
                var body = model.Instances.New<IfcExtrudedAreaSolid>(s =>
                {
                    s.Depth = window.Height;
                    s.SweptArea = rectProf;
                    s.ExtrudedDirection = model.ToIfcDirection(window.ExtrudedDirection);
                });

                //parameters to insert the geometry in the model
                body.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(window.XVector);
                    p.Location = model.ToIfcCartesianPoint(window.CenterPoint + floor_origin.GetAsVector());
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        foreach (var item in window.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });

                //create opening element
                var hole = model.Instances.New<IfcOpeningElement>();
                hole.Name = "hole";
                //todo: describe hole's geometry

                var hole_rectProf = model.Instances.New<IfcRectangleProfileDef>(p =>
                {
                    p.XDim = window.Width;
                    p.YDim = thwall.Width;//todo;
                    p.ProfileType = IfcProfileTypeEnum.AREA;
                    p.Position = model.ToIfcAxis2Placement2D(default);
                });

                var hole_body = model.Instances.New<IfcExtrudedAreaSolid>(s =>
                {
                    s.Depth = window.Height;
                    s.SweptArea = hole_rectProf;
                    s.ExtrudedDirection = model.ToIfcDirection(window.ExtrudedDirection);
                });

                hole_body.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);

                var hole_shape = model.Instances.New<IfcShapeRepresentation>();
                var hole_modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                hole_shape.ContextOfItems = modelContext;
                hole_shape.RepresentationType = "SweptSolid";
                hole_shape.RepresentationIdentifier = "Body";
                hole_shape.Items.Add(hole_body);

                var hole_rep = model.Instances.New<IfcProductDefinitionShape>();
                hole_rep.Representations.Add(hole_shape);
                hole.Representation = hole_rep;

                hole.ObjectPlacement = lp;

                //create relVoidsElement
                var relVoidsElement = model.Instances.New<IfcRelVoidsElement>();
                relVoidsElement.RelatedOpeningElement = hole;
                relVoidsElement.RelatingBuildingElement = openedElement;

                //create relFillsElement
                var relFillsElement = model.Instances.New<IfcRelFillsElement>();
                relFillsElement.RelatingOpeningElement = hole;
                relFillsElement.RelatedBuildingElement = ret;

                txn.Commit();
                return ret;
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
        static public IfcOpeningElement CreateHole(IfcStore model, ThTCHOpening hole, IfcElement openedElement, ThTCHWall thwall, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Window"))
            {
                var ret = model.Instances.New<IfcOpeningElement>();
                ret.Name = "window";
                //represent wall as a rectangular profile
                var rectProf = model.Instances.New<IfcRectangleProfileDef>(p =>
                {
                    p.XDim = hole.Width;
                    p.YDim = hole.Thickness - epsilon;
                    p.ProfileType = IfcProfileTypeEnum.AREA;
                    p.Position = model.ToIfcAxis2Placement2D(default);
                });

                //model as a swept area solid
                var body = model.Instances.New<IfcExtrudedAreaSolid>(s =>
                {
                    s.Depth = hole.Height;
                    s.SweptArea = rectProf;
                    s.ExtrudedDirection = model.ToIfcDirection(hole.ExtrudedDirection);
                });

                //parameters to insert the geometry in the model
                body.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(hole.XVector);
                    p.Location = model.ToIfcCartesianPoint(hole.CenterPoint + floor_origin.GetAsVector());
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                //create relVoidsElement
                var relVoidsElement = model.Instances.New<IfcRelVoidsElement>();
                relVoidsElement.RelatedOpeningElement = ret;
                relVoidsElement.RelatingBuildingElement = openedElement;

                txn.Commit();
                return ret;
            }
        }
        static public IfcSlab CreateSlab(IfcStore model, ThTCHSlab slab, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Slab"))
            {
                var ret = model.Instances.New<IfcSlab>();
                ret.Name = "Standard Slab";

                // create extruded solid body 
                var body = model.Instances.New<IfcExtrudedAreaSolid>(s =>
                {
                    s.Depth = slab.Thickness;
                    s.ExtrudedDirection = model.ToIfcDirection(slab.ExtrudedDirection);
                });

                if (slab.Outline != null)
                {
                    var ArbitraryClosedProfileDef = model.Instances.New<IfcArbitraryClosedProfileDef>();
                    ArbitraryClosedProfileDef.ProfileType = IfcProfileTypeEnum.AREA;
                    if (slab.Outline is Polyline pline)
                    {
                        ArbitraryClosedProfileDef.OuterCurve = ThTGL2IFC2x3DbExtension.ToIfcCompositeCurve(model, pline);
                    }
                    else if (slab.Outline is MPolygon polygon)
                    {
                        var shell = ThMPolygonExtension.Shell(polygon);
                        ArbitraryClosedProfileDef.OuterCurve = ThTGL2IFC2x3DbExtension.ToIfcCompositeCurve(model, shell);
                    }
                    body.SweptArea = ArbitraryClosedProfileDef;
                }
                else
                {
                    return null;
                }

                //parameters to insert the geometry in the model
                body.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = model.Instances.New<IfcCartesianPoint>();
                ax3D.Location.SetXYZ(floor_origin.X, floor_origin.Y, floor_origin.Z);
                ax3D.RefDirection = model.Instances.New<IfcDirection>();
                ax3D.RefDirection.SetXYZ(1, 0, 0);//todo
                ax3D.Axis = model.Instances.New<IfcDirection>();
                ax3D.Axis.SetXYZ(0, 0, 1);
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                //now add holes inside

                if (slab.Outline != null && slab.Outline is MPolygon mPolygon)
                {
                    var holepolylines = ThMPolygonExtension.Holes(mPolygon);
                    foreach (var holepolyline in holepolylines)
                    {
                        var hole = model.Instances.New<IfcOpeningElement>();
                        hole.Name = "hole on the Slab";

                        // create extruded solid body 
                        var holesbody = model.Instances.New<IfcExtrudedAreaSolid>();
                        holesbody.Depth = slab.Thickness;
                        holesbody.ExtrudedDirection = model.ToIfcDirection(slab.ExtrudedDirection);

                        //build 2d area
                        var holesArbitraryClosedProfileDef = model.Instances.New<IfcArbitraryClosedProfileDef>();
                        holesArbitraryClosedProfileDef.ProfileType = IfcProfileTypeEnum.AREA;
                        holesArbitraryClosedProfileDef.OuterCurve = ThTGL2IFC2x3DbExtension.ToIfcCompositeCurve(model, holepolyline);

                        holesbody.SweptArea = holesArbitraryClosedProfileDef;

                        //parameters to insert the geometry of holes in the model
                        holesbody.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);

                        //Create a Definition shape to hold the geometry of holes
                        var holesshape = model.Instances.New<IfcShapeRepresentation>();
                        var holesmodelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                        holesshape.ContextOfItems = holesmodelContext;
                        holesshape.RepresentationType = "SweptSolid";
                        holesshape.RepresentationIdentifier = "Body";
                        holesshape.Items.Add(holesbody);

                        //Create a Product Definition and add the model geometry to the wall
                        var holesrep = model.Instances.New<IfcProductDefinitionShape>();
                        holesrep.Representations.Add(holesshape);
                        hole.Representation = holesrep;

                        hole.ObjectPlacement = lp;

                        //create relVoidsElement
                        var relVoidsElement = model.Instances.New<IfcRelVoidsElement>();
                        relVoidsElement.RelatedOpeningElement = hole;
                        relVoidsElement.RelatingBuildingElement = ret;
                    }
                }

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        foreach (var item in slab.Properties)
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
    }
}
