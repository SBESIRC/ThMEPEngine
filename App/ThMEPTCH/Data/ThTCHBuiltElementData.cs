// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ThTCHBuiltElementData.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from ThTCHBuiltElementData.proto</summary>
public static partial class ThTCHBuiltElementDataReflection {

  #region Descriptor
  /// <summary>File descriptor for ThTCHBuiltElementData.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ThTCHBuiltElementDataReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "ChtUaFRDSEJ1aWx0RWxlbWVudERhdGEucHJvdG8aE1RoVENIUm9vdERhdGEu",
          "cHJvdG8aE1RoVENIR2VvbWV0cnkucHJvdG8i1wEKFVRoVENIQnVpbHRFbGVt",
          "ZW50RGF0YRIcCgRyb290GAEgASgLMg4uVGhUQ0hSb290RGF0YRIOCgZsZW5n",
          "dGgYAiABKAESDQoFd2lkdGgYAyABKAESDgoGaGVpZ2h0GAQgASgBEh0KBm9y",
          "aWdpbhgFIAEoCzINLlRoVENIUG9pbnQzZBIgCgh4X3ZlY3RvchgGIAEoCzIO",
          "LlRoVENIVmVjdG9yM2QSJAoHb3V0bGluZRgHIAEoCzIOLlRoVENIUG9seWxp",
          "bmVIAIgBAUIKCghfb3V0bGluZWIGcHJvdG8z"));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { global::ThTCHRootDataReflection.Descriptor, global::ThTCHGeometryReflection.Descriptor, },
        new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::ThTCHBuiltElementData), global::ThTCHBuiltElementData.Parser, new[]{ "Root", "Length", "Width", "Height", "Origin", "XVector", "Outline" }, new[]{ "Outline" }, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class ThTCHBuiltElementData : pb::IMessage<ThTCHBuiltElementData>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
  private static readonly pb::MessageParser<ThTCHBuiltElementData> _parser = new pb::MessageParser<ThTCHBuiltElementData>(() => new ThTCHBuiltElementData());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pb::MessageParser<ThTCHBuiltElementData> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ThTCHBuiltElementDataReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHBuiltElementData() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHBuiltElementData(ThTCHBuiltElementData other) : this() {
    root_ = other.root_ != null ? other.root_.Clone() : null;
    length_ = other.length_;
    width_ = other.width_;
    height_ = other.height_;
    origin_ = other.origin_ != null ? other.origin_.Clone() : null;
    xVector_ = other.xVector_ != null ? other.xVector_.Clone() : null;
    outline_ = other.outline_ != null ? other.outline_.Clone() : null;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHBuiltElementData Clone() {
    return new ThTCHBuiltElementData(this);
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

  /// <summary>Field number for the "length" field.</summary>
  public const int LengthFieldNumber = 2;
  private double length_;
  /// <summary>
  /// geometry size
  /// </summary>
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public double Length {
    get { return length_; }
    set {
      length_ = value;
    }
  }

  /// <summary>Field number for the "width" field.</summary>
  public const int WidthFieldNumber = 3;
  private double width_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public double Width {
    get { return width_; }
    set {
      width_ = value;
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

  /// <summary>Field number for the "origin" field.</summary>
  public const int OriginFieldNumber = 5;
  private global::ThTCHPoint3d origin_;
  /// <summary>
  /// geometry location
  /// </summary>
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHPoint3d Origin {
    get { return origin_; }
    set {
      origin_ = value;
    }
  }

  /// <summary>Field number for the "x_vector" field.</summary>
  public const int XVectorFieldNumber = 6;
  private global::ThTCHVector3d xVector_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHVector3d XVector {
    get { return xVector_; }
    set {
      xVector_ = value;
    }
  }

  /// <summary>Field number for the "outline" field.</summary>
  public const int OutlineFieldNumber = 7;
  private global::ThTCHPolyline outline_;
  /// <summary>
  /// geometry profile
  /// </summary>
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public global::ThTCHPolyline Outline {
    get { return outline_; }
    set {
      outline_ = value;
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override bool Equals(object other) {
    return Equals(other as ThTCHBuiltElementData);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public bool Equals(ThTCHBuiltElementData other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (!object.Equals(Root, other.Root)) return false;
    if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(Length, other.Length)) return false;
    if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(Width, other.Width)) return false;
    if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(Height, other.Height)) return false;
    if (!object.Equals(Origin, other.Origin)) return false;
    if (!object.Equals(XVector, other.XVector)) return false;
    if (!object.Equals(Outline, other.Outline)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override int GetHashCode() {
    int hash = 1;
    if (root_ != null) hash ^= Root.GetHashCode();
    if (Length != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(Length);
    if (Width != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(Width);
    if (Height != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(Height);
    if (origin_ != null) hash ^= Origin.GetHashCode();
    if (xVector_ != null) hash ^= XVector.GetHashCode();
    if (outline_ != null) hash ^= Outline.GetHashCode();
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
    if (Length != 0D) {
      output.WriteRawTag(17);
      output.WriteDouble(Length);
    }
    if (Width != 0D) {
      output.WriteRawTag(25);
      output.WriteDouble(Width);
    }
    if (Height != 0D) {
      output.WriteRawTag(33);
      output.WriteDouble(Height);
    }
    if (origin_ != null) {
      output.WriteRawTag(42);
      output.WriteMessage(Origin);
    }
    if (xVector_ != null) {
      output.WriteRawTag(50);
      output.WriteMessage(XVector);
    }
    if (outline_ != null) {
      output.WriteRawTag(58);
      output.WriteMessage(Outline);
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
    if (Length != 0D) {
      output.WriteRawTag(17);
      output.WriteDouble(Length);
    }
    if (Width != 0D) {
      output.WriteRawTag(25);
      output.WriteDouble(Width);
    }
    if (Height != 0D) {
      output.WriteRawTag(33);
      output.WriteDouble(Height);
    }
    if (origin_ != null) {
      output.WriteRawTag(42);
      output.WriteMessage(Origin);
    }
    if (xVector_ != null) {
      output.WriteRawTag(50);
      output.WriteMessage(XVector);
    }
    if (outline_ != null) {
      output.WriteRawTag(58);
      output.WriteMessage(Outline);
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
    if (Length != 0D) {
      size += 1 + 8;
    }
    if (Width != 0D) {
      size += 1 + 8;
    }
    if (Height != 0D) {
      size += 1 + 8;
    }
    if (origin_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(Origin);
    }
    if (xVector_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(XVector);
    }
    if (outline_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(Outline);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(ThTCHBuiltElementData other) {
    if (other == null) {
      return;
    }
    if (other.root_ != null) {
      if (root_ == null) {
        Root = new global::ThTCHRootData();
      }
      Root.MergeFrom(other.Root);
    }
    if (other.Length != 0D) {
      Length = other.Length;
    }
    if (other.Width != 0D) {
      Width = other.Width;
    }
    if (other.Height != 0D) {
      Height = other.Height;
    }
    if (other.origin_ != null) {
      if (origin_ == null) {
        Origin = new global::ThTCHPoint3d();
      }
      Origin.MergeFrom(other.Origin);
    }
    if (other.xVector_ != null) {
      if (xVector_ == null) {
        XVector = new global::ThTCHVector3d();
      }
      XVector.MergeFrom(other.XVector);
    }
    if (other.outline_ != null) {
      if (outline_ == null) {
        Outline = new global::ThTCHPolyline();
      }
      Outline.MergeFrom(other.Outline);
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
        case 17: {
          Length = input.ReadDouble();
          break;
        }
        case 25: {
          Width = input.ReadDouble();
          break;
        }
        case 33: {
          Height = input.ReadDouble();
          break;
        }
        case 42: {
          if (origin_ == null) {
            Origin = new global::ThTCHPoint3d();
          }
          input.ReadMessage(Origin);
          break;
        }
        case 50: {
          if (xVector_ == null) {
            XVector = new global::ThTCHVector3d();
          }
          input.ReadMessage(XVector);
          break;
        }
        case 58: {
          if (outline_ == null) {
            Outline = new global::ThTCHPolyline();
          }
          input.ReadMessage(Outline);
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
        case 17: {
          Length = input.ReadDouble();
          break;
        }
        case 25: {
          Width = input.ReadDouble();
          break;
        }
        case 33: {
          Height = input.ReadDouble();
          break;
        }
        case 42: {
          if (origin_ == null) {
            Origin = new global::ThTCHPoint3d();
          }
          input.ReadMessage(Origin);
          break;
        }
        case 50: {
          if (xVector_ == null) {
            XVector = new global::ThTCHVector3d();
          }
          input.ReadMessage(XVector);
          break;
        }
        case 58: {
          if (outline_ == null) {
            Outline = new global::ThTCHPolyline();
          }
          input.ReadMessage(Outline);
          break;
        }
      }
    }
  }
  #endif

}

#endregion


#endregion Designer generated code