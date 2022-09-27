// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ThSUComponentData.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from ThSUComponentData.proto</summary>
public static partial class ThSUComponentDataReflection {

  #region Descriptor
  /// <summary>File descriptor for ThSUComponentData.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ThSUComponentDataReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "ChdUaFNVQ29tcG9uZW50RGF0YS5wcm90bxoTVGhUQ0hHZW9tZXRyeS5wcm90",
          "bxoWVGhTVU1hdGVyaWFsRGF0YS5wcm90byJ7ChFUaFNVQ29tcG9uZW50RGF0",
          "YRIYChBkZWZpbml0aW9uX2luZGV4GAEgASgFEicKD3RyYW5zZm9ybWF0aW9u",
          "cxgCIAEoCzIOLlRoVENITWF0cml4M2QSIwoIbWF0ZXJpYWwYAyABKAsyES5U",
          "aFNVTWF0ZXJpYWxEYXRhYgZwcm90bzM="));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { global::ThTCHGeometryReflection.Descriptor, global::ThSUMaterialDataReflection.Descriptor, },
        new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::ThSUComponentData), global::ThSUComponentData.Parser, new[]{ "DefinitionIndex", "Transformations", "Material" }, null, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class ThSUComponentData : pb::IMessage<ThSUComponentData>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
  private static readonly pb::MessageParser<ThSUComponentData> _parser = new pb::MessageParser<ThSUComponentData>(() => new ThSUComponentData());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pb::MessageParser<ThSUComponentData> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ThSUComponentDataReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUComponentData() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUComponentData(ThSUComponentData other) : this() {
    definitionIndex_ = other.definitionIndex_;
    transformations_ = other.transformations_ != null ? other.transformations_.Clone() : null;
    material_ = other.material_ != null ? other.material_.Clone() : null;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUComponentData Clone() {
    return new ThSUComponentData(this);
  }

  /// <summary>Field number for the "definition_index" field.</summary>
  public const int DefinitionIndexFieldNumber = 1;
  private int definitionIndex_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public int DefinitionIndex {
    get { return definitionIndex_; }
    set {
      definitionIndex_ = value;
    }
  }

  /// <summary>Field number for the "transformations" field.</summary>
  public const int TransformationsFieldNumber = 2;
  private global::ThTCHMatrix3d transformations_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHMatrix3d Transformations {
    get { return transformations_; }
    set {
      transformations_ = value;
    }
  }

  /// <summary>Field number for the "material" field.</summary>
  public const int MaterialFieldNumber = 3;
  private global::ThSUMaterialData material_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThSUMaterialData Material {
    get { return material_; }
    set {
      material_ = value;
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override bool Equals(object other) {
    return Equals(other as ThSUComponentData);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public bool Equals(ThSUComponentData other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (DefinitionIndex != other.DefinitionIndex) return false;
    if (!object.Equals(Transformations, other.Transformations)) return false;
    if (!object.Equals(Material, other.Material)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override int GetHashCode() {
    int hash = 1;
    if (DefinitionIndex != 0) hash ^= DefinitionIndex.GetHashCode();
    if (transformations_ != null) hash ^= Transformations.GetHashCode();
    if (material_ != null) hash ^= Material.GetHashCode();
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
    if (DefinitionIndex != 0) {
      output.WriteRawTag(8);
      output.WriteInt32(DefinitionIndex);
    }
    if (transformations_ != null) {
      output.WriteRawTag(18);
      output.WriteMessage(Transformations);
    }
    if (material_ != null) {
      output.WriteRawTag(26);
      output.WriteMessage(Material);
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
    if (DefinitionIndex != 0) {
      output.WriteRawTag(8);
      output.WriteInt32(DefinitionIndex);
    }
    if (transformations_ != null) {
      output.WriteRawTag(18);
      output.WriteMessage(Transformations);
    }
    if (material_ != null) {
      output.WriteRawTag(26);
      output.WriteMessage(Material);
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
    if (DefinitionIndex != 0) {
      size += 1 + pb::CodedOutputStream.ComputeInt32Size(DefinitionIndex);
    }
    if (transformations_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(Transformations);
    }
    if (material_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(Material);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(ThSUComponentData other) {
    if (other == null) {
      return;
    }
    if (other.DefinitionIndex != 0) {
      DefinitionIndex = other.DefinitionIndex;
    }
    if (other.transformations_ != null) {
      if (transformations_ == null) {
        Transformations = new global::ThTCHMatrix3d();
      }
      Transformations.MergeFrom(other.Transformations);
    }
    if (other.material_ != null) {
      if (material_ == null) {
        Material = new global::ThSUMaterialData();
      }
      Material.MergeFrom(other.Material);
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
        case 8: {
          DefinitionIndex = input.ReadInt32();
          break;
        }
        case 18: {
          if (transformations_ == null) {
            Transformations = new global::ThTCHMatrix3d();
          }
          input.ReadMessage(Transformations);
          break;
        }
        case 26: {
          if (material_ == null) {
            Material = new global::ThSUMaterialData();
          }
          input.ReadMessage(Material);
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
        case 8: {
          DefinitionIndex = input.ReadInt32();
          break;
        }
        case 18: {
          if (transformations_ == null) {
            Transformations = new global::ThTCHMatrix3d();
          }
          input.ReadMessage(Transformations);
          break;
        }
        case 26: {
          if (material_ == null) {
            Material = new global::ThSUMaterialData();
          }
          input.ReadMessage(Material);
          break;
        }
      }
    }
  }
  #endif

}

#endregion


#endregion Designer generated code
