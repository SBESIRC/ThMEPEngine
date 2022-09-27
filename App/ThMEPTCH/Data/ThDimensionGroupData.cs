// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ThDimensionGroupData.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from ThDimensionGroupData.proto</summary>
public static partial class ThDimensionGroupDataReflection {

  #region Descriptor
  /// <summary>File descriptor for ThDimensionGroupData.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ThDimensionGroupDataReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "ChpUaERpbWVuc2lvbkdyb3VwRGF0YS5wcm90bxoTVGhUQ0hSb290RGF0YS5w",
          "cm90bxoTVGhUQ0hHZW9tZXRyeS5wcm90byLSAQoSVGhBbGlnbmVkRGltZW5z",
          "aW9uEhwKBHJvb3QYASABKAsyDi5UaFRDSFJvb3REYXRhEiQKDXhfbGluZTFf",
          "cG9pbnQYAiABKAsyDS5UaFRDSFBvaW50M2QSJAoNeF9saW5lMl9wb2ludBgD",
          "IAEoCzINLlRoVENIUG9pbnQzZBIlCg5kaW1fbGluZV9wb2ludBgEIAEoCzIN",
          "LlRoVENIUG9pbnQzZBIMCgRtYXJrGAUgASgJEh0KCWRpbV9saW5lcxgGIAMo",
          "CzIKLlRoVENITGluZSJdChRUaERpbWVuc2lvbkdyb3VwRGF0YRIcCgRyb290",
          "GAEgASgLMg4uVGhUQ0hSb290RGF0YRInCgpkaW1lbnNpb25zGAIgAygLMhMu",
          "VGhBbGlnbmVkRGltZW5zaW9uYgZwcm90bzM="));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { global::ThTCHRootDataReflection.Descriptor, global::ThTCHGeometryReflection.Descriptor, },
        new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::ThAlignedDimension), global::ThAlignedDimension.Parser, new[]{ "Root", "XLine1Point", "XLine2Point", "DimLinePoint", "Mark", "DimLines" }, null, null, null, null),
          new pbr::GeneratedClrTypeInfo(typeof(global::ThDimensionGroupData), global::ThDimensionGroupData.Parser, new[]{ "Root", "Dimensions" }, null, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class ThAlignedDimension : pb::IMessage<ThAlignedDimension>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
  private static readonly pb::MessageParser<ThAlignedDimension> _parser = new pb::MessageParser<ThAlignedDimension>(() => new ThAlignedDimension());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pb::MessageParser<ThAlignedDimension> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ThDimensionGroupDataReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThAlignedDimension() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThAlignedDimension(ThAlignedDimension other) : this() {
    root_ = other.root_ != null ? other.root_.Clone() : null;
    xLine1Point_ = other.xLine1Point_ != null ? other.xLine1Point_.Clone() : null;
    xLine2Point_ = other.xLine2Point_ != null ? other.xLine2Point_.Clone() : null;
    dimLinePoint_ = other.dimLinePoint_ != null ? other.dimLinePoint_.Clone() : null;
    mark_ = other.mark_;
    dimLines_ = other.dimLines_.Clone();
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThAlignedDimension Clone() {
    return new ThAlignedDimension(this);
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

  /// <summary>Field number for the "x_line1_point" field.</summary>
  public const int XLine1PointFieldNumber = 2;
  private global::ThTCHPoint3d xLine1Point_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHPoint3d XLine1Point {
    get { return xLine1Point_; }
    set {
      xLine1Point_ = value;
    }
  }

  /// <summary>Field number for the "x_line2_point" field.</summary>
  public const int XLine2PointFieldNumber = 3;
  private global::ThTCHPoint3d xLine2Point_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHPoint3d XLine2Point {
    get { return xLine2Point_; }
    set {
      xLine2Point_ = value;
    }
  }

  /// <summary>Field number for the "dim_line_point" field.</summary>
  public const int DimLinePointFieldNumber = 4;
  private global::ThTCHPoint3d dimLinePoint_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHPoint3d DimLinePoint {
    get { return dimLinePoint_; }
    set {
      dimLinePoint_ = value;
    }
  }

  /// <summary>Field number for the "mark" field.</summary>
  public const int MarkFieldNumber = 5;
  private string mark_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public string Mark {
    get { return mark_; }
    set {
      mark_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  /// <summary>Field number for the "dim_lines" field.</summary>
  public const int DimLinesFieldNumber = 6;
  private static readonly pb::FieldCodec<global::ThTCHLine> _repeated_dimLines_codec
      = pb::FieldCodec.ForMessage(50, global::ThTCHLine.Parser);
  private readonly pbc::RepeatedField<global::ThTCHLine> dimLines_ = new pbc::RepeatedField<global::ThTCHLine>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThTCHLine> DimLines {
    get { return dimLines_; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override bool Equals(object other) {
    return Equals(other as ThAlignedDimension);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public bool Equals(ThAlignedDimension other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (!object.Equals(Root, other.Root)) return false;
    if (!object.Equals(XLine1Point, other.XLine1Point)) return false;
    if (!object.Equals(XLine2Point, other.XLine2Point)) return false;
    if (!object.Equals(DimLinePoint, other.DimLinePoint)) return false;
    if (Mark != other.Mark) return false;
    if(!dimLines_.Equals(other.dimLines_)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override int GetHashCode() {
    int hash = 1;
    if (root_ != null) hash ^= Root.GetHashCode();
    if (xLine1Point_ != null) hash ^= XLine1Point.GetHashCode();
    if (xLine2Point_ != null) hash ^= XLine2Point.GetHashCode();
    if (dimLinePoint_ != null) hash ^= DimLinePoint.GetHashCode();
    if (Mark.Length != 0) hash ^= Mark.GetHashCode();
    hash ^= dimLines_.GetHashCode();
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
    if (xLine1Point_ != null) {
      output.WriteRawTag(18);
      output.WriteMessage(XLine1Point);
    }
    if (xLine2Point_ != null) {
      output.WriteRawTag(26);
      output.WriteMessage(XLine2Point);
    }
    if (dimLinePoint_ != null) {
      output.WriteRawTag(34);
      output.WriteMessage(DimLinePoint);
    }
    if (Mark.Length != 0) {
      output.WriteRawTag(42);
      output.WriteString(Mark);
    }
    dimLines_.WriteTo(output, _repeated_dimLines_codec);
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
    if (xLine1Point_ != null) {
      output.WriteRawTag(18);
      output.WriteMessage(XLine1Point);
    }
    if (xLine2Point_ != null) {
      output.WriteRawTag(26);
      output.WriteMessage(XLine2Point);
    }
    if (dimLinePoint_ != null) {
      output.WriteRawTag(34);
      output.WriteMessage(DimLinePoint);
    }
    if (Mark.Length != 0) {
      output.WriteRawTag(42);
      output.WriteString(Mark);
    }
    dimLines_.WriteTo(ref output, _repeated_dimLines_codec);
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
    if (xLine1Point_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(XLine1Point);
    }
    if (xLine2Point_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(XLine2Point);
    }
    if (dimLinePoint_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(DimLinePoint);
    }
    if (Mark.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(Mark);
    }
    size += dimLines_.CalculateSize(_repeated_dimLines_codec);
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(ThAlignedDimension other) {
    if (other == null) {
      return;
    }
    if (other.root_ != null) {
      if (root_ == null) {
        Root = new global::ThTCHRootData();
      }
      Root.MergeFrom(other.Root);
    }
    if (other.xLine1Point_ != null) {
      if (xLine1Point_ == null) {
        XLine1Point = new global::ThTCHPoint3d();
      }
      XLine1Point.MergeFrom(other.XLine1Point);
    }
    if (other.xLine2Point_ != null) {
      if (xLine2Point_ == null) {
        XLine2Point = new global::ThTCHPoint3d();
      }
      XLine2Point.MergeFrom(other.XLine2Point);
    }
    if (other.dimLinePoint_ != null) {
      if (dimLinePoint_ == null) {
        DimLinePoint = new global::ThTCHPoint3d();
      }
      DimLinePoint.MergeFrom(other.DimLinePoint);
    }
    if (other.Mark.Length != 0) {
      Mark = other.Mark;
    }
    dimLines_.Add(other.dimLines_);
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
          if (xLine1Point_ == null) {
            XLine1Point = new global::ThTCHPoint3d();
          }
          input.ReadMessage(XLine1Point);
          break;
        }
        case 26: {
          if (xLine2Point_ == null) {
            XLine2Point = new global::ThTCHPoint3d();
          }
          input.ReadMessage(XLine2Point);
          break;
        }
        case 34: {
          if (dimLinePoint_ == null) {
            DimLinePoint = new global::ThTCHPoint3d();
          }
          input.ReadMessage(DimLinePoint);
          break;
        }
        case 42: {
          Mark = input.ReadString();
          break;
        }
        case 50: {
          dimLines_.AddEntriesFrom(input, _repeated_dimLines_codec);
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
          if (xLine1Point_ == null) {
            XLine1Point = new global::ThTCHPoint3d();
          }
          input.ReadMessage(XLine1Point);
          break;
        }
        case 26: {
          if (xLine2Point_ == null) {
            XLine2Point = new global::ThTCHPoint3d();
          }
          input.ReadMessage(XLine2Point);
          break;
        }
        case 34: {
          if (dimLinePoint_ == null) {
            DimLinePoint = new global::ThTCHPoint3d();
          }
          input.ReadMessage(DimLinePoint);
          break;
        }
        case 42: {
          Mark = input.ReadString();
          break;
        }
        case 50: {
          dimLines_.AddEntriesFrom(ref input, _repeated_dimLines_codec);
          break;
        }
      }
    }
  }
  #endif

}

public sealed partial class ThDimensionGroupData : pb::IMessage<ThDimensionGroupData>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
  private static readonly pb::MessageParser<ThDimensionGroupData> _parser = new pb::MessageParser<ThDimensionGroupData>(() => new ThDimensionGroupData());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pb::MessageParser<ThDimensionGroupData> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ThDimensionGroupDataReflection.Descriptor.MessageTypes[1]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThDimensionGroupData() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThDimensionGroupData(ThDimensionGroupData other) : this() {
    root_ = other.root_ != null ? other.root_.Clone() : null;
    dimensions_ = other.dimensions_.Clone();
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThDimensionGroupData Clone() {
    return new ThDimensionGroupData(this);
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

  /// <summary>Field number for the "dimensions" field.</summary>
  public const int DimensionsFieldNumber = 2;
  private static readonly pb::FieldCodec<global::ThAlignedDimension> _repeated_dimensions_codec
      = pb::FieldCodec.ForMessage(18, global::ThAlignedDimension.Parser);
  private readonly pbc::RepeatedField<global::ThAlignedDimension> dimensions_ = new pbc::RepeatedField<global::ThAlignedDimension>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThAlignedDimension> Dimensions {
    get { return dimensions_; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override bool Equals(object other) {
    return Equals(other as ThDimensionGroupData);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public bool Equals(ThDimensionGroupData other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (!object.Equals(Root, other.Root)) return false;
    if(!dimensions_.Equals(other.dimensions_)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override int GetHashCode() {
    int hash = 1;
    if (root_ != null) hash ^= Root.GetHashCode();
    hash ^= dimensions_.GetHashCode();
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
    dimensions_.WriteTo(output, _repeated_dimensions_codec);
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
    dimensions_.WriteTo(ref output, _repeated_dimensions_codec);
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
    size += dimensions_.CalculateSize(_repeated_dimensions_codec);
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(ThDimensionGroupData other) {
    if (other == null) {
      return;
    }
    if (other.root_ != null) {
      if (root_ == null) {
        Root = new global::ThTCHRootData();
      }
      Root.MergeFrom(other.Root);
    }
    dimensions_.Add(other.dimensions_);
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
          dimensions_.AddEntriesFrom(input, _repeated_dimensions_codec);
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
          dimensions_.AddEntriesFrom(ref input, _repeated_dimensions_codec);
          break;
        }
      }
    }
  }
  #endif

}

#endregion


#endregion Designer generated code
