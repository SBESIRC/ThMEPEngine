// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ThTCHWallData.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from ThTCHWallData.proto</summary>
public static partial class ThTCHWallDataReflection {

  #region Descriptor
  /// <summary>File descriptor for ThTCHWallData.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ThTCHWallDataReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "ChNUaFRDSFdhbGxEYXRhLnByb3RvGhNUaFRDSEdlb21ldHJ5LnByb3RvGhNU",
          "aFRDSERvb3JEYXRhLnByb3RvGhVUaFRDSFdpbmRvd0RhdGEucHJvdG8aFlRo",
          "VENIT3BlbmluZ0RhdGEucHJvdG8aG1RoVENIQnVpbHRFbGVtZW50RGF0YS5w",
          "cm90byKUAgoNVGhUQ0hXYWxsRGF0YRItCg1idWlsZF9lbGVtZW50GAEgASgL",
          "MhYuVGhUQ0hCdWlsdEVsZW1lbnREYXRhEhIKCmxlZnRfd2lkdGgYAiABKAES",
          "EwoLcmlnaHRfd2lkdGgYAyABKAESIgoLc3RhcnRfcG9pbnQYBCABKAsyDS5U",
          "aFRDSFBvaW50M2QSIAoJZW5kX3BvaW50GAUgASgLMg0uVGhUQ0hQb2ludDNk",
          "Eh0KBWRvb3JzGAYgAygLMg4uVGhUQ0hEb29yRGF0YRIhCgd3aW5kb3dzGAcg",
          "AygLMhAuVGhUQ0hXaW5kb3dEYXRhEiMKCG9wZW5pbmdzGAggAygLMhEuVGhU",
          "Q0hPcGVuaW5nRGF0YWIGcHJvdG8z"));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { global::ThTCHGeometryReflection.Descriptor, global::ThTCHDoorDataReflection.Descriptor, global::ThTCHWindowDataReflection.Descriptor, global::ThTCHOpeningDataReflection.Descriptor, global::ThTCHBuiltElementDataReflection.Descriptor, },
        new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::ThTCHWallData), global::ThTCHWallData.Parser, new[]{ "BuildElement", "LeftWidth", "RightWidth", "StartPoint", "EndPoint", "Doors", "Windows", "Openings" }, null, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class ThTCHWallData : pb::IMessage<ThTCHWallData>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
  private static readonly pb::MessageParser<ThTCHWallData> _parser = new pb::MessageParser<ThTCHWallData>(() => new ThTCHWallData());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pb::MessageParser<ThTCHWallData> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ThTCHWallDataReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHWallData() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHWallData(ThTCHWallData other) : this() {
    buildElement_ = other.buildElement_ != null ? other.buildElement_.Clone() : null;
    leftWidth_ = other.leftWidth_;
    rightWidth_ = other.rightWidth_;
    startPoint_ = other.startPoint_ != null ? other.startPoint_.Clone() : null;
    endPoint_ = other.endPoint_ != null ? other.endPoint_.Clone() : null;
    doors_ = other.doors_.Clone();
    windows_ = other.windows_.Clone();
    openings_ = other.openings_.Clone();
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHWallData Clone() {
    return new ThTCHWallData(this);
  }

  /// <summary>Field number for the "build_element" field.</summary>
  public const int BuildElementFieldNumber = 1;
  private global::ThTCHBuiltElementData buildElement_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHBuiltElementData BuildElement {
    get { return buildElement_; }
    set {
      buildElement_ = value;
    }
  }

  /// <summary>Field number for the "left_width" field.</summary>
  public const int LeftWidthFieldNumber = 2;
  private double leftWidth_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public double LeftWidth {
    get { return leftWidth_; }
    set {
      leftWidth_ = value;
    }
  }

  /// <summary>Field number for the "right_width" field.</summary>
  public const int RightWidthFieldNumber = 3;
  private double rightWidth_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public double RightWidth {
    get { return rightWidth_; }
    set {
      rightWidth_ = value;
    }
  }

  /// <summary>Field number for the "start_point" field.</summary>
  public const int StartPointFieldNumber = 4;
  private global::ThTCHPoint3d startPoint_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHPoint3d StartPoint {
    get { return startPoint_; }
    set {
      startPoint_ = value;
    }
  }

  /// <summary>Field number for the "end_point" field.</summary>
  public const int EndPointFieldNumber = 5;
  private global::ThTCHPoint3d endPoint_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHPoint3d EndPoint {
    get { return endPoint_; }
    set {
      endPoint_ = value;
    }
  }

  /// <summary>Field number for the "doors" field.</summary>
  public const int DoorsFieldNumber = 6;
  private static readonly pb::FieldCodec<global::ThTCHDoorData> _repeated_doors_codec
      = pb::FieldCodec.ForMessage(50, global::ThTCHDoorData.Parser);
  private readonly pbc::RepeatedField<global::ThTCHDoorData> doors_ = new pbc::RepeatedField<global::ThTCHDoorData>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThTCHDoorData> Doors {
    get { return doors_; }
  }

  /// <summary>Field number for the "windows" field.</summary>
  public const int WindowsFieldNumber = 7;
  private static readonly pb::FieldCodec<global::ThTCHWindowData> _repeated_windows_codec
      = pb::FieldCodec.ForMessage(58, global::ThTCHWindowData.Parser);
  private readonly pbc::RepeatedField<global::ThTCHWindowData> windows_ = new pbc::RepeatedField<global::ThTCHWindowData>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThTCHWindowData> Windows {
    get { return windows_; }
  }

  /// <summary>Field number for the "openings" field.</summary>
  public const int OpeningsFieldNumber = 8;
  private static readonly pb::FieldCodec<global::ThTCHOpeningData> _repeated_openings_codec
      = pb::FieldCodec.ForMessage(66, global::ThTCHOpeningData.Parser);
  private readonly pbc::RepeatedField<global::ThTCHOpeningData> openings_ = new pbc::RepeatedField<global::ThTCHOpeningData>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThTCHOpeningData> Openings {
    get { return openings_; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override bool Equals(object other) {
    return Equals(other as ThTCHWallData);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public bool Equals(ThTCHWallData other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (!object.Equals(BuildElement, other.BuildElement)) return false;
    if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(LeftWidth, other.LeftWidth)) return false;
    if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(RightWidth, other.RightWidth)) return false;
    if (!object.Equals(StartPoint, other.StartPoint)) return false;
    if (!object.Equals(EndPoint, other.EndPoint)) return false;
    if(!doors_.Equals(other.doors_)) return false;
    if(!windows_.Equals(other.windows_)) return false;
    if(!openings_.Equals(other.openings_)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override int GetHashCode() {
    int hash = 1;
    if (buildElement_ != null) hash ^= BuildElement.GetHashCode();
    if (LeftWidth != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(LeftWidth);
    if (RightWidth != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(RightWidth);
    if (startPoint_ != null) hash ^= StartPoint.GetHashCode();
    if (endPoint_ != null) hash ^= EndPoint.GetHashCode();
    hash ^= doors_.GetHashCode();
    hash ^= windows_.GetHashCode();
    hash ^= openings_.GetHashCode();
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
    if (buildElement_ != null) {
      output.WriteRawTag(10);
      output.WriteMessage(BuildElement);
    }
    if (LeftWidth != 0D) {
      output.WriteRawTag(17);
      output.WriteDouble(LeftWidth);
    }
    if (RightWidth != 0D) {
      output.WriteRawTag(25);
      output.WriteDouble(RightWidth);
    }
    if (startPoint_ != null) {
      output.WriteRawTag(34);
      output.WriteMessage(StartPoint);
    }
    if (endPoint_ != null) {
      output.WriteRawTag(42);
      output.WriteMessage(EndPoint);
    }
    doors_.WriteTo(output, _repeated_doors_codec);
    windows_.WriteTo(output, _repeated_windows_codec);
    openings_.WriteTo(output, _repeated_openings_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  #endif
  }

  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
    if (buildElement_ != null) {
      output.WriteRawTag(10);
      output.WriteMessage(BuildElement);
    }
    if (LeftWidth != 0D) {
      output.WriteRawTag(17);
      output.WriteDouble(LeftWidth);
    }
    if (RightWidth != 0D) {
      output.WriteRawTag(25);
      output.WriteDouble(RightWidth);
    }
    if (startPoint_ != null) {
      output.WriteRawTag(34);
      output.WriteMessage(StartPoint);
    }
    if (endPoint_ != null) {
      output.WriteRawTag(42);
      output.WriteMessage(EndPoint);
    }
    doors_.WriteTo(ref output, _repeated_doors_codec);
    windows_.WriteTo(ref output, _repeated_windows_codec);
    openings_.WriteTo(ref output, _repeated_openings_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(ref output);
    }
  }
  #endif

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public int CalculateSize() {
    int size = 0;
    if (buildElement_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(BuildElement);
    }
    if (LeftWidth != 0D) {
      size += 1 + 8;
    }
    if (RightWidth != 0D) {
      size += 1 + 8;
    }
    if (startPoint_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(StartPoint);
    }
    if (endPoint_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(EndPoint);
    }
    size += doors_.CalculateSize(_repeated_doors_codec);
    size += windows_.CalculateSize(_repeated_windows_codec);
    size += openings_.CalculateSize(_repeated_openings_codec);
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(ThTCHWallData other) {
    if (other == null) {
      return;
    }
    if (other.buildElement_ != null) {
      if (buildElement_ == null) {
        BuildElement = new global::ThTCHBuiltElementData();
      }
      BuildElement.MergeFrom(other.BuildElement);
    }
    if (other.LeftWidth != 0D) {
      LeftWidth = other.LeftWidth;
    }
    if (other.RightWidth != 0D) {
      RightWidth = other.RightWidth;
    }
    if (other.startPoint_ != null) {
      if (startPoint_ == null) {
        StartPoint = new global::ThTCHPoint3d();
      }
      StartPoint.MergeFrom(other.StartPoint);
    }
    if (other.endPoint_ != null) {
      if (endPoint_ == null) {
        EndPoint = new global::ThTCHPoint3d();
      }
      EndPoint.MergeFrom(other.EndPoint);
    }
    doors_.Add(other.doors_);
    windows_.Add(other.windows_);
    openings_.Add(other.openings_);
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
          if (buildElement_ == null) {
            BuildElement = new global::ThTCHBuiltElementData();
          }
          input.ReadMessage(BuildElement);
          break;
        }
        case 17: {
          LeftWidth = input.ReadDouble();
          break;
        }
        case 25: {
          RightWidth = input.ReadDouble();
          break;
        }
        case 34: {
          if (startPoint_ == null) {
            StartPoint = new global::ThTCHPoint3d();
          }
          input.ReadMessage(StartPoint);
          break;
        }
        case 42: {
          if (endPoint_ == null) {
            EndPoint = new global::ThTCHPoint3d();
          }
          input.ReadMessage(EndPoint);
          break;
        }
        case 50: {
          doors_.AddEntriesFrom(input, _repeated_doors_codec);
          break;
        }
        case 58: {
          windows_.AddEntriesFrom(input, _repeated_windows_codec);
          break;
        }
        case 66: {
          openings_.AddEntriesFrom(input, _repeated_openings_codec);
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
          if (buildElement_ == null) {
            BuildElement = new global::ThTCHBuiltElementData();
          }
          input.ReadMessage(BuildElement);
          break;
        }
        case 17: {
          LeftWidth = input.ReadDouble();
          break;
        }
        case 25: {
          RightWidth = input.ReadDouble();
          break;
        }
        case 34: {
          if (startPoint_ == null) {
            StartPoint = new global::ThTCHPoint3d();
          }
          input.ReadMessage(StartPoint);
          break;
        }
        case 42: {
          if (endPoint_ == null) {
            EndPoint = new global::ThTCHPoint3d();
          }
          input.ReadMessage(EndPoint);
          break;
        }
        case 50: {
          doors_.AddEntriesFrom(ref input, _repeated_doors_codec);
          break;
        }
        case 58: {
          windows_.AddEntriesFrom(ref input, _repeated_windows_codec);
          break;
        }
        case 66: {
          openings_.AddEntriesFrom(ref input, _repeated_openings_codec);
          break;
        }
      }
    }
  }
  #endif

}

#endregion


#endregion Designer generated code
