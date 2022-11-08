// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ThSUFaceMeshData.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from ThSUFaceMeshData.proto</summary>
public static partial class ThSUFaceMeshDataReflection {

  #region Descriptor
  /// <summary>File descriptor for ThSUFaceMeshData.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ThSUFaceMeshDataReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "ChZUaFNVRmFjZU1lc2hEYXRhLnByb3RvGhJUaFNVR2VvbWV0cnkucHJvdG8a",
          "E1RoVENIR2VvbWV0cnkucHJvdG8iVwoQVGhTVUZhY2VNZXNoRGF0YRIeCgRt",
          "ZXNoGAEgASgLMhAuVGhTVVBvbHlnb25NZXNoEiMKC2ZhY2Vfbm9ybWFsGAIg",
          "ASgLMg4uVGhUQ0hWZWN0b3IzZGIGcHJvdG8z"));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { global::ThSUGeometryReflection.Descriptor, global::ThTCHGeometryReflection.Descriptor, },
        new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::ThSUFaceMeshData), global::ThSUFaceMeshData.Parser, new[]{ "Mesh", "FaceNormal" }, null, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class ThSUFaceMeshData : pb::IMessage<ThSUFaceMeshData>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
  private static readonly pb::MessageParser<ThSUFaceMeshData> _parser = new pb::MessageParser<ThSUFaceMeshData>(() => new ThSUFaceMeshData());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pb::MessageParser<ThSUFaceMeshData> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ThSUFaceMeshDataReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUFaceMeshData() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUFaceMeshData(ThSUFaceMeshData other) : this() {
    mesh_ = other.mesh_ != null ? other.mesh_.Clone() : null;
    faceNormal_ = other.faceNormal_ != null ? other.faceNormal_.Clone() : null;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUFaceMeshData Clone() {
    return new ThSUFaceMeshData(this);
  }

  /// <summary>Field number for the "mesh" field.</summary>
  public const int MeshFieldNumber = 1;
  private global::ThSUPolygonMesh mesh_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThSUPolygonMesh Mesh {
    get { return mesh_; }
    set {
      mesh_ = value;
    }
  }

  /// <summary>Field number for the "face_normal" field.</summary>
  public const int FaceNormalFieldNumber = 2;
  private global::ThTCHVector3d faceNormal_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHVector3d FaceNormal {
    get { return faceNormal_; }
    set {
      faceNormal_ = value;
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override bool Equals(object other) {
    return Equals(other as ThSUFaceMeshData);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public bool Equals(ThSUFaceMeshData other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (!object.Equals(Mesh, other.Mesh)) return false;
    if (!object.Equals(FaceNormal, other.FaceNormal)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override int GetHashCode() {
    int hash = 1;
    if (mesh_ != null) hash ^= Mesh.GetHashCode();
    if (faceNormal_ != null) hash ^= FaceNormal.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void WriteTo(pb::CodedOutputStream output) {
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    output.WriteRawMessage(this);
  #else
    if (mesh_ != null) {
      output.WriteRawTag(10);
      output.WriteMessage(Mesh);
    }
    if (faceNormal_ != null) {
      output.WriteRawTag(18);
      output.WriteMessage(FaceNormal);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  #endif
  }

  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
    if (mesh_ != null) {
      output.WriteRawTag(10);
      output.WriteMessage(Mesh);
    }
    if (faceNormal_ != null) {
      output.WriteRawTag(18);
      output.WriteMessage(FaceNormal);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(ref output);
    }
  }
  #endif

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public int CalculateSize() {
    int size = 0;
    if (mesh_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(Mesh);
    }
    if (faceNormal_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(FaceNormal);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(ThSUFaceMeshData other) {
    if (other == null) {
      return;
    }
    if (other.mesh_ != null) {
      if (mesh_ == null) {
        Mesh = new global::ThSUPolygonMesh();
      }
      Mesh.MergeFrom(other.Mesh);
    }
    if (other.faceNormal_ != null) {
      if (faceNormal_ == null) {
        FaceNormal = new global::ThTCHVector3d();
      }
      FaceNormal.MergeFrom(other.FaceNormal);
    }
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(pb::CodedInputStream input) {
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    input.ReadRawMessage(this);
  #else
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 10: {
          if (mesh_ == null) {
            Mesh = new global::ThSUPolygonMesh();
          }
          input.ReadMessage(Mesh);
          break;
        }
        case 18: {
          if (faceNormal_ == null) {
            FaceNormal = new global::ThTCHVector3d();
          }
          input.ReadMessage(FaceNormal);
          break;
        }
      }
    }
  #endif
  }

  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
          break;
        case 10: {
          if (mesh_ == null) {
            Mesh = new global::ThSUPolygonMesh();
          }
          input.ReadMessage(Mesh);
          break;
        }
        case 18: {
          if (faceNormal_ == null) {
            FaceNormal = new global::ThTCHVector3d();
          }
          input.ReadMessage(FaceNormal);
          break;
        }
      }
    }
  }
  #endif

}

#endregion


#endregion Designer generated code