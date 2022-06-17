using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Xbim.Ifc;
using Xbim.Ifc2x3.Kernel;
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
    public partial class ThTGL2IFC2x3Factory
    {
        public static IfcSlab CreateBrepSlab(IfcStore model, ThTCHSlab slab, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Slab"))
            {
                var ret = model.Instances.New<IfcSlab>();
                ret.Name = "TH Slab";

                //create representation
                var solid = slab.CreateSlabSolid();
                var brep = model.ToIfcFacetedBrep(solid);
                var shape = CreateBrepBody(model, brep);
                ret.Representation = CreateProductDefinitionShape(model, shape);

                //object placement
                ret.ObjectPlacement = model.ToIfcLocalPlacement(floor_origin);

                txn.Commit();
                return ret;
            }
        }

        public static IfcSlab CreateSlab(IfcStore model, ThTCHSlab slab, Point3d floor_origin)
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
    }
}
