// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ThSUBuildingStoreyData.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from ThSUBuildingStoreyData.proto</summary>
public static partial class ThSUBuildingStoreyDataReflection {

  #region Descriptor
  /// <summary>File descriptor for ThSUBuildingStoreyData.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ThSUBuildingStoreyDataReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "ChxUaFNVQnVpbGRpbmdTdG9yZXlEYXRhLnByb3RvGhNUaFRDSFJvb3REYXRh",
          "LnByb3RvGh1UaFNVQnVpbGRpbmdFbGVtZW50RGF0YS5wcm90byKpAQoWVGhT",
          "VUJ1aWxkaW5nU3RvcmV5RGF0YRIcCgRyb290GAEgASgLMg4uVGhUQ0hSb290",
          "RGF0YRIrCglidWlsZGluZ3MYAiADKAsyGC5UaFNVQnVpbGRpbmdFbGVtZW50",
          "RGF0YRIOCgZudW1iZXIYAyABKAUSDgoGaGVpZ2h0GAQgASgBEhEKCWVsZXZh",
          "dGlvbhgFIAEoARIRCglzdGRGbHJfbm8YBiABKAViBnByb3RvMw=="));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { global::ThTCHRootDataReflection.Descriptor, global::ThSUBuildingElementDataReflection.Descriptor, },
        new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::ThSUBuildingStoreyData), global::ThSUBuildingStoreyData.Parser, new[]{ "Root", "Buildings", "Number", "Height", "Elevation", "StdFlrNo" }, null, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class ThSUBuildingStoreyData : pb::IMessage<ThSUBuildingStoreyData>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
  private static readonly pb::MessageParser<ThSUBuildingStoreyData> _parser = new pb::MessageParser<ThSUBuildingStoreyData>(() => new ThSUBuildingStoreyData());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pb::MessageParser<ThSUBuildingStoreyData> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ThSUBuildingStoreyDataReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUBuildingStoreyData() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUBuildingStoreyData(ThSUBuildingStoreyData other) : this() {
    root_ = other.root_ != null ? other.root_.Clone() : null;
    buildings_ = other.buildings_.Clone();
    number_ = other.number_;
    height_ = other.height_;
    elevation_ = other.elevation_;
    stdFlrNo_ = other.stdFlrNo_;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThSUBuildingStoreyData Clone() {
    return new ThSUBuildingStoreyData(this);
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
  private static readonly pb::FieldCodec<global::ThSUBuildingElementData> _repeated_buildings_codec
      = pb::FieldCodec.ForMessage(18, global::ThSUBuildingElementData.Parser);
  private readonly pbc::RepeatedField<global::ThSUBuildingElementData> buildings_ = new pbc::RepeatedField<global::ThSUBuildingElementData>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThSUBuildingElementData> Buildings {
    get { return buildings_; }
  }

  /// <summary>Field number for the "number" field.</summary>
  public const int NumberFieldNumber = 3;
  private int number_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public int Number {
    get { return number_; }
    set {
      number_ = value;
    }
  }

  /// <summary>Field number for the "height" field.</summary>
  public const int HeightFieldNumber = 4;
  private double height_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public double Height {
    get { return height_; }
    set {
      height_ = value;
    }
  }

  /// <summary>Field number for the "elevation" field.</summary>
  public const int ElevationFieldNumber = 5;
  private double elevation_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public double Elevation {
    get { return elevation_; }
    set {
      elevation_ = value;
    }
  }

  /// <summary>Field number for the "stdFlr_no" field.</summary>
  public const int StdFlrNoFieldNumber = 6;
  private int stdFlrNo_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public int StdFlrNo {
    get { return stdFlrNo_; }
    set {
      stdFlrNo_ = value;
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override bool Equals(object other) {
    return Equals(other as ThSUBuildingStoreyData);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public bool Equals(ThSUBuildingStoreyData other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (!object.Equals(Root, other.Root)) return false;
    if(!buildings_.Equals(other.buildings_)) return false;
    if (Number != other.Number) return false;
    if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(Height, other.Height)) return false;
    if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(Elevation, other.Elevation)) return false;
    if (StdFlrNo != other.StdFlrNo) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override int GetHashCode() {
    int hash = 1;
    if (root_ != null) hash ^= Root.GetHashCode();
    hash ^= buildings_.GetHashCode();
    if (Number != 0) hash ^= Number.GetHashCode();
    if (Height != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(Height);
    if (Elevation != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(Elevation);
    if (StdFlrNo != 0) hash ^= StdFlrNo.GetHashCode();
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
    if (Number != 0) {
      output.WriteRawTag(24);
      output.WriteInt32(Number);
    }
    if (Height != 0D) {
      output.WriteRawTag(33);
      output.WriteDouble(Height);
    }
    if (Elevation != 0D) {
      output.WriteRawTag(41);
      output.WriteDouble(Elevation);
    }
    if (StdFlrNo != 0) {
      output.WriteRawTag(48);
      output.WriteInt32(StdFlrNo);
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
    if (root_ != null) {
      output.WriteRawTag(10);
      output.WriteMessage(Root);
    }
    buildings_.WriteTo(ref output, _repeated_buildings_codec);
    if (Number != 0) {
      output.WriteRawTag(24);
      output.WriteInt32(Number);
    }
    if (Height != 0D) {
      output.WriteRawTag(33);
      output.WriteDouble(Height);
    }
    if (Elevation != 0D) {
      output.WriteRawTag(41);
      output.WriteDouble(Elevation);
    }
    if (StdFlrNo != 0) {
      output.WriteRawTag(48);
      output.WriteInt32(StdFlrNo);
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
    if (root_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(Root);
    }
    size += buildings_.CalculateSize(_repeated_buildings_codec);
    if (Number != 0) {
      size += 1 + pb::CodedOutputStream.ComputeInt32Size(Number);
    }
    if (Height != 0D) {
      size += 1 + 8;
    }
    if (Elevation != 0D) {
      size += 1 + 8;
    }
    if (StdFlrNo != 0) {
      size += 1 + pb::CodedOutputStream.ComputeInt32Size(StdFlrNo);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(ThSUBuildingStoreyData other) {
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
    if (other.Number != 0) {
      Number = other.Number;
    }
    if (other.Height != 0D) {
      Height = other.Height;
    }
    if (other.Elevation != 0D) {
      Elevation = other.Elevation;
    }
    if (other.StdFlrNo != 0) {
      StdFlrNo = other.StdFlrNo;
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
        case 24: {
          Number = input.ReadInt32();
          break;
        }
        case 33: {
          Height = input.ReadDouble();
          break;
        }
        case 41: {
          Elevation = input.ReadDouble();
          break;
        }
        case 48: {
          StdFlrNo = input.ReadInt32();
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
        case 24: {
          Number = input.ReadInt32();
          break;
        }
        case 33: {
          Height = input.ReadDouble();
          break;
        }
        case 41: {
          Elevation = input.ReadDouble();
          break;
        }
        case 48: {
          StdFlrNo = input.ReadInt32();
          break;
        }
      }
    }
  }
  #endif

}

#endregion


#endregion Designer generated code
