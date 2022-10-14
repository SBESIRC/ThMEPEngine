// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: ThTCHGridData.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from ThTCHGridData.proto</summary>
public static partial class ThTCHGridDataReflection {

  #region Descriptor
  /// <summary>File descriptor for ThTCHGridData.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ThTCHGridDataReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "ChNUaFRDSEdyaWREYXRhLnByb3RvGhdUaFRDSEdyaWRBeGlzRGF0YS5wcm90",
          "byJXCg1UaFRDSEdyaWREYXRhEiIKBnVfYXhlcxgBIAMoCzISLlRoVENIR3Jp",
          "ZEF4aXNEYXRhEiIKBnZfYXhlcxgCIAMoCzISLlRoVENIR3JpZEF4aXNEYXRh",
          "YgZwcm90bzM="));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { global::ThTCHGridAxisDataReflection.Descriptor, },
        new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::ThTCHGridData), global::ThTCHGridData.Parser, new[]{ "UAxes", "VAxes" }, null, null, null, null)
        }));
  }
  #endregion

}
#region Messages
public sealed partial class ThTCHGridData : pb::IMessage<ThTCHGridData>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    , pb::IBufferMessage
#endif
{
  private static readonly pb::MessageParser<ThTCHGridData> _parser = new pb::MessageParser<ThTCHGridData>(() => new ThTCHGridData());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pb::MessageParser<ThTCHGridData> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::ThTCHGridDataReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHGridData() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHGridData(ThTCHGridData other) : this() {
    uAxes_ = other.uAxes_.Clone();
    vAxes_ = other.vAxes_.Clone();
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public ThTCHGridData Clone() {
    return new ThTCHGridData(this);
  }

  /// <summary>Field number for the "u_axes" field.</summary>
  public const int UAxesFieldNumber = 1;
  private static readonly pb::FieldCodec<global::ThTCHGridAxisData> _repeated_uAxes_codec
      = pb::FieldCodec.ForMessage(10, global::ThTCHGridAxisData.Parser);
  private readonly pbc::RepeatedField<global::ThTCHGridAxisData> uAxes_ = new pbc::RepeatedField<global::ThTCHGridAxisData>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThTCHGridAxisData> UAxes {
    get { return uAxes_; }
  }

  /// <summary>Field number for the "v_axes" field.</summary>
  public const int VAxesFieldNumber = 2;
  private static readonly pb::FieldCodec<global::ThTCHGridAxisData> _repeated_vAxes_codec
      = pb::FieldCodec.ForMessage(18, global::ThTCHGridAxisData.Parser);
  private readonly pbc::RepeatedField<global::ThTCHGridAxisData> vAxes_ = new pbc::RepeatedField<global::ThTCHGridAxisData>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public pbc::RepeatedField<global::ThTCHGridAxisData> VAxes {
    get { return vAxes_; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override bool Equals(object other) {
    return Equals(other as ThTCHGridData);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public bool Equals(ThTCHGridData other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if(!uAxes_.Equals(other.uAxes_)) return false;
    if(!vAxes_.Equals(other.vAxes_)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public override int GetHashCode() {
    int hash = 1;
    hash ^= uAxes_.GetHashCode();
    hash ^= vAxes_.GetHashCode();
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
    uAxes_.WriteTo(output, _repeated_uAxes_codec);
    vAxes_.WriteTo(output, _repeated_vAxes_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  #endif
  }

  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
    uAxes_.WriteTo(ref output, _repeated_uAxes_codec);
    vAxes_.WriteTo(ref output, _repeated_vAxes_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(ref output);
    }
  }
  #endif

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public int CalculateSize() {
    int size = 0;
    size += uAxes_.CalculateSize(_repeated_uAxes_codec);
    size += vAxes_.CalculateSize(_repeated_vAxes_codec);
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
  public void MergeFrom(ThTCHGridData other) {
    if (other == null) {
      return;
    }
    uAxes_.Add(other.uAxes_);
    vAxes_.Add(other.vAxes_);
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
          uAxes_.AddEntriesFrom(input, _repeated_uAxes_codec);
          break;
        }
        case 18: {
          vAxes_.AddEntriesFrom(input, _repeated_vAxes_codec);
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
          uAxes_.AddEntriesFrom(ref input, _repeated_uAxes_codec);
          break;
        }
        case 18: {
          vAxes_.AddEntriesFrom(ref input, _repeated_vAxes_codec);
          break;
        }
      }
    }
  }
  #endif

}

#endregion


#endregion Designer generated code