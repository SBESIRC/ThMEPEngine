
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThMEPIFC.Ifc2x3;
using ThMEPTCH.Model;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;

namespace ThMEPIFC
{
    class ThTGL2GeoFileService
    {
		protected int gIndex = 0;
		protected int pIndex = 1;
		private double _deflectionOverride = double.NaN;
		private double _angularDeflectionOverride = double.NaN;
		public IfcStore Model { get; private set; }
		public void GenerateXBimMeshAndSave(ThTCHProject project, string file)
		{
			Model = ThTGL2IFC2x3Factory.CreateAndInitModel("ThTGL2IFCProject");
			if (Model != null)
			{
				ThTGL2IFC2x3Builder.BuildIfcModel(Model, project);
				// mesh direct model
				if (Model.GeometryStore.IsEmpty)
				{
					try
					{
                        var context = new Xbim3DModelContext(Model);
						context.MaxThreads = 10;
						//context.UseSimplifiedFastExtruder = _simpleFastExtrusion;
						SetDeflection(Model);
						//upgrade to new geometry representation, uses the default 3D model
						context.CreateContext(null, false);
					}
					catch
					{
					}
				}
				//var ifcProject = Model.Instances.OfType<IIfcProject>().FirstOrDefault();
				//var allElems = Model.Instances.OfType<IIfcElement>().ToList();
				//var allElemIds = allElems.Select(c => c.EntityLabel).ToList();
				//var ifcStorys = ifcProject.Buildings.SelectMany(c => c.BuildingStoreys).ToList();
				var ifcGeo = new IfcStoreToEngineFile();
				var allModelMeshs = ifcGeo.ReadGeomtry(Model, out List<PointNormal> allModelPointNormals);
				//var notElems = allModelMeshs.Where(c => !allElemIds.Any(x => x == c.IfcIndex)).ToList();
				//foreach (var item in project.Site.Building.Storeys) 
				//{
				//	if (string.IsNullOrEmpty(item.MemoryStoreyId))
				//		continue;
				//	var memoryStory = project.Site.Building.Storeys.Where(c => c.Uuid == item.MemoryStoreyId).FirstOrDefault();
				//	if (null == memoryStory)
				//		continue;
				//	IIfcBuildingStorey buildingStorey = null;
				//	foreach (var story in ifcStorys) 
				//	{
				//		var name = story.Name.Value.ToString();
				//		if (name == memoryStory.Number)
				//		{
				//			buildingStorey = story;
				//			break;
				//		}
				//	}
				//	if (buildingStorey == null)
				//		continue;
    //                foreach (var ifcType in buildingStorey.ContainsElements)
    //                {
    //                    //foreach (var ifcEntity in ifcType.)

    //                }

    //            }
				WriteMidFile(allModelMeshs, allModelPointNormals, file);
				Model.Dispose();
			}
		}

		private void SetDeflection(IModel model)
		{
			var mf = model.ModelFactors;
			if (mf == null)
				return;
			if (!double.IsNaN(_angularDeflectionOverride))
				mf.DeflectionAngle = _angularDeflectionOverride;
			if (!double.IsNaN(_deflectionOverride))
				mf.DeflectionTolerance = mf.OneMilliMetre * _deflectionOverride;
		}


		private void WriteMidFile(List<IfcMeshModel> meshModels, List<PointNormal> meshPoints, string midFilePath)
		{
			if (null == meshModels || meshModels.Count < 1 || null == meshPoints || meshPoints.Count < 1)
				return;
			if (!string.IsNullOrEmpty(midFilePath) && File.Exists(midFilePath))
				File.Delete(midFilePath);
			var create = new FileStream(midFilePath, FileMode.Create, FileAccess.Write);
			BinaryWriter writer = new BinaryWriter(create);
			ulong ptCount = (ulong)meshPoints.Count();
			//vertices
			writer.Write(ptCount * 3);
			for (int i = 0; i < meshPoints.Count; i++)
			{
				var point = meshPoints[i];
				writer.Write(point.Point.X);
				writer.Write(point.Point.Y);
				writer.Write(point.Point.Z);
			}
			//normals
			writer.Write(ptCount * 3);
			for (int i = 0; i < meshPoints.Count; i++)
			{
				var point = meshPoints[i];
				writer.Write(point.Normal.X);
				writer.Write(point.Normal.Y);
				writer.Write(point.Normal.Z);
			}
			//global_indices, All triangle faces info
			var sumCount = (ulong)meshModels.Sum(c => c.FaceTriangles.Sum(x => x.PtIndexs.Count()));
			writer.Write(sumCount);
			for (int i = 0; i < meshModels.Count; i++)
			{
				var meshModel = meshModels[i];
				foreach (var item in meshModel.FaceTriangles)
				{
					foreach (int ptIndex in item.PtIndexs)
						writer.Write(ptIndex);
				}
			}
			//components' indices, all components indices
			ulong cIdCount = (ulong)meshModels.Count;
			writer.Write(cIdCount);
			foreach (var item in meshModels)
			{
				ulong vCount = (ulong)item.FaceTriangles.Sum(c => c.PtIndexs.Count);
				writer.Write(vCount);
				foreach (var value in item.FaceTriangles)
				{
					foreach (int ptIndex in value.PtIndexs)
						writer.Write(ptIndex);
				}
			}
			//material datas
			ulong mCount = (ulong)meshModels.Sum(c => c.FaceTriangles.Count());
			writer.Write(mCount);
			foreach (var mesh in meshModels)
			{
				foreach (var item in mesh.FaceTriangles)
				{
					writer.Write(item.TriangleMaterial.Kd_R);
					writer.Write(item.TriangleMaterial.Kd_G);
					writer.Write(item.TriangleMaterial.Kd_B);
					writer.Write(item.TriangleMaterial.Ks_R);
					writer.Write(item.TriangleMaterial.Ks_G);
					writer.Write(item.TriangleMaterial.Ks_B);
					writer.Write(item.TriangleMaterial.K);
					writer.Write(item.TriangleMaterial.NS);
				}
			}
			//writer.Close();
			writer.Dispose();
			writer = null;
			create.Dispose();
			create = null;
		}
	}
	class PointNormal
	{
		public int PointIndex { get; set; }
		public PointVector Point { get; set; }
		public PointVector Normal { get; set; }

	}
	class FaceTriangle
	{
		public List<int> PtIndexs { get; }
		public IfcMaterial TriangleMaterial { get; set; }
		public FaceTriangle()
		{
			PtIndexs = new List<int>();
		}
	}
	class IfcMeshModel
	{
		public int CIndex { get; set; }
		public int IfcIndex { get; }
		public List<FaceTriangle> FaceTriangles { get; }
		public IfcMeshModel(int index, int ifcIndex)
		{
			CIndex = index;
			IfcIndex = ifcIndex;
			FaceTriangles = new List<FaceTriangle>();
		}
	}
	class PointVector
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
	}
	class IfcMaterial
	{
		public float Kd_R { get; set; }
		public float Kd_G { get; set; }
		public float Kd_B { get; set; }
		public float Ks_R { get; set; }
		public float Ks_G { get; set; }
		public float Ks_B { get; set; }
		public float K { get; set; }
		public int NS { get; set; }
	}
}
