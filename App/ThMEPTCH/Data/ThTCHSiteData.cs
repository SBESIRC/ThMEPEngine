// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ThTCHSiteData.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from ThTCHSiteData.proto</summary>
public static partial class ThTCHSiteDataReflection {

  #region Descriptor
  /// <summary>File descriptor for ThTCHSiteData.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ThTCHSiteDataReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "ChNUaFRDSFNpdGVEYXRhLnByb3RvGhNUaFRDSFJvb3REYXRhLnByb3RvGhdU",
          "aFRDSEJ1aWxkaW5nRGF0YS5wcm90byJUCg1UaFRDSFNpdGVEYXRhEhwKBHJv",
          "b3QYASABKAsyDi5UaFRDSFJvb3REYXRhEiUKCWJ1aWxkaW5ncxgCIAMoCzIS",
          "LlRoVENIQnVpbGRpbmdEYXRhYgZwcm90bzM="));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { global::ThTCHRootDataReflection.Descriptor, global::ThTCHBuildingDataReflection.Descriptor, },
        new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::ThTCHSiteData), global::ThTCHSiteData.Parser, new[]{ "Root", "Buildings" }, null, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class ThTCHSiteData : pb::IMessage<ThTCHSiteData>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
  private static readonly pb::MessageParser<ThTCHSiteData> _parser = new pb::MessageParser<ThTCHSiteData>(() => new ThTCHSiteData());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pb::MessageParser<ThTCHSiteData> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ThTCHSiteDataReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHSiteData() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHSiteData(ThTCHSiteData other) : this() {
    root_ = other.root_ != null ? other.root_.Clone() : null;
    buildings_ = other.buildings_.Clone();
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHSiteData Clone() {
    return new ThTCHSiteData(this);
  }

  /// <summary>Field number for the "root" field.</summary>
  public const int RootFieldNumber = 1;
  private global::ThTCHRootData root_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHRootData Root {
    get { return root_; }
    set {
      root_ = value;
    }
  }

  /// <summary>Field number for the "buildings" field.</summary>
  public const int BuildingsFieldNumber = 2;
  private static readonly pb::FieldCodec<global::ThTCHBuildingData> _repeated_buildings_codec
      = pb::FieldCodec.ForMessage(18, global::ThTCHBuildingData.Parser);
  private readonly pbc::RepeatedField<global::ThTCHBuildingData> buildings_ = new pbc::RepeatedField<global::ThTCHBuildingData>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThTCHBuildingData> Buildings {
    get { return buildings_; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override bool Equals(object other) {
    return Equals(other as ThTCHSiteData);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public bool Equals(ThTCHSiteData other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (!object.Equals(Root, other.Root)) return false;
    if(!buildings_.Equals(other.buildings_)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override int GetHashCode() {
    int hash = 1;
    if (root_ != null) hash ^= Root.GetHashCode();
    hash ^= buildings_.GetHashCode();
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
    if (root_ != null) {
      output.WriteRawTag(10);
      output.WriteMessage(Root);
    }
    buildings_.WriteTo(output, _repeated_buildings_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  #endif
  }

  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
    if (root_ != null) {
      output.WriteRawTag(10);
      output.WriteMessage(Root);
    }
    buildings_.WriteTo(ref output, _repeated_buildings_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(ref output);
    }
  }
  #endif

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public int CalculateSize() {
    int size = 0;
    if (root_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(Root);
    }
    size += buildings_.CalculateSize(_repeated_buildings_codec);
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(ThTCHSiteData other) {
    if (other == null) {
      return;
    }
    if (other.root_ != null) {
      if (root_ == null) {
        Root = new global::ThTCHRootData();
      }
      Root.MergeFrom(other.Root);
    }
    buildings_.Add(other.buildings_);
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
          if (root_ == null) {
            Root = new global::ThTCHRootData();
          }
          input.ReadMessage(Root);
          break;
        }
        case 18: {
          buildings_.AddEntriesFrom(input, _repeated_buildings_codec);
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
          if (root_ == null) {
            Root = new global::ThTCHRootData();
          }
          input.ReadMessage(Root);
          break;
        }
        case 18: {
          buildings_.AddEntriesFrom(ref input, _repeated_buildings_codec);
          break;
        }
      }
    }
  }
  #endif

}

#endregion


#endregion Designer generated code