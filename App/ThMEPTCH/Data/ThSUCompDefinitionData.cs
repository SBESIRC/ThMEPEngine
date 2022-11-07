// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ThSUCompDefinitionData.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from ThSUCompDefinitionData.proto</summary>
public static partial class ThSUCompDefinitionDataReflection {

  #region Descriptor
  /// <summary>File descriptor for ThSUCompDefinitionData.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ThSUCompDefinitionDataReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "ChxUaFNVQ29tcERlZmluaXRpb25EYXRhLnByb3RvGhZUaFNVRmFjZUJyZXBE",
          "YXRhLnByb3RvGhZUaFNVRmFjZU1lc2hEYXRhLnByb3RvIn8KFlRoU1VDb21w",
          "RGVmaW5pdGlvbkRhdGESFwoPZGVmaW5pdGlvbl9uYW1lGAEgASgJEiUKCmJy",
          "ZXBfZmFjZXMYAiADKAsyES5UaFNVRmFjZUJyZXBEYXRhEiUKCm1lc2hfZmFj",
          "ZXMYAyADKAsyES5UaFNVRmFjZU1lc2hEYXRhYgZwcm90bzM="));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { global::ThSUFaceBrepDataReflection.Descriptor, global::ThSUFaceMeshDataReflection.Descriptor, },
        new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::ThSUCompDefinitionData), global::ThSUCompDefinitionData.Parser, new[]{ "DefinitionName", "BrepFaces", "MeshFaces" }, null, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class ThSUCompDefinitionData : pb::IMessage<ThSUCompDefinitionData>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
  private static readonly pb::MessageParser<ThSUCompDefinitionData> _parser = new pb::MessageParser<ThSUCompDefinitionData>(() => new ThSUCompDefinitionData());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pb::MessageParser<ThSUCompDefinitionData> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ThSUCompDefinitionDataReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUCompDefinitionData() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUCompDefinitionData(ThSUCompDefinitionData other) : this() {
    definitionName_ = other.definitionName_;
    brepFaces_ = other.brepFaces_.Clone();
    meshFaces_ = other.meshFaces_.Clone();
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUCompDefinitionData Clone() {
    return new ThSUCompDefinitionData(this);
  }

  /// <summary>Field number for the "definition_name" field.</summary>
  public const int DefinitionNameFieldNumber = 1;
  private string definitionName_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public string DefinitionName {
    get { return definitionName_; }
    set {
      definitionName_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  /// <summary>Field number for the "brep_faces" field.</summary>
  public const int BrepFacesFieldNumber = 2;
  private static readonly pb::FieldCodec<global::ThSUFaceBrepData> _repeated_brepFaces_codec
      = pb::FieldCodec.ForMessage(18, global::ThSUFaceBrepData.Parser);
  private readonly pbc::RepeatedField<global::ThSUFaceBrepData> brepFaces_ = new pbc::RepeatedField<global::ThSUFaceBrepData>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThSUFaceBrepData> BrepFaces {
    get { return brepFaces_; }
  }

  /// <summary>Field number for the "mesh_faces" field.</summary>
  public const int MeshFacesFieldNumber = 3;
  private static readonly pb::FieldCodec<global::ThSUFaceMeshData> _repeated_meshFaces_codec
      = pb::FieldCodec.ForMessage(26, global::ThSUFaceMeshData.Parser);
  private readonly pbc::RepeatedField<global::ThSUFaceMeshData> meshFaces_ = new pbc::RepeatedField<global::ThSUFaceMeshData>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThSUFaceMeshData> MeshFaces {
    get { return meshFaces_; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override bool Equals(object other) {
    return Equals(other as ThSUCompDefinitionData);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public bool Equals(ThSUCompDefinitionData other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (DefinitionName != other.DefinitionName) return false;
    if(!brepFaces_.Equals(other.brepFaces_)) return false;
    if(!meshFaces_.Equals(other.meshFaces_)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override int GetHashCode() {
    int hash = 1;
    if (DefinitionName.Length != 0) hash ^= DefinitionName.GetHashCode();
    hash ^= brepFaces_.GetHashCode();
    hash ^= meshFaces_.GetHashCode();
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
    if (DefinitionName.Length != 0) {
      output.WriteRawTag(10);
      output.WriteString(DefinitionName);
    }
    brepFaces_.WriteTo(output, _repeated_brepFaces_codec);
    meshFaces_.WriteTo(output, _repeated_meshFaces_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  #endif
  }

  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
    if (DefinitionName.Length != 0) {
      output.WriteRawTag(10);
      output.WriteString(DefinitionName);
    }
    brepFaces_.WriteTo(ref output, _repeated_brepFaces_codec);
    meshFaces_.WriteTo(ref output, _repeated_meshFaces_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(ref output);
    }
  }
  #endif

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public int CalculateSize() {
    int size = 0;
    if (DefinitionName.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(DefinitionName);
    }
    size += brepFaces_.CalculateSize(_repeated_brepFaces_codec);
    size += meshFaces_.CalculateSize(_repeated_meshFaces_codec);
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(ThSUCompDefinitionData other) {
    if (other == null) {
      return;
    }
    if (other.DefinitionName.Length != 0) {
      DefinitionName = other.DefinitionName;
    }
    brepFaces_.Add(other.brepFaces_);
    meshFaces_.Add(other.meshFaces_);
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
          DefinitionName = input.ReadString();
          break;
        }
        case 18: {
          brepFaces_.AddEntriesFrom(input, _repeated_brepFaces_codec);
          break;
        }
        case 26: {
          meshFaces_.AddEntriesFrom(input, _repeated_meshFaces_codec);
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
          DefinitionName = input.ReadString();
          break;
        }
        case 18: {
          brepFaces_.AddEntriesFrom(ref input, _repeated_brepFaces_codec);
          break;
        }
        case 26: {
          meshFaces_.AddEntriesFrom(ref input, _repeated_meshFaces_codec);
          break;
        }
      }
    }
  }
  #endif

}

#endregion


#endregion Designer generated code
