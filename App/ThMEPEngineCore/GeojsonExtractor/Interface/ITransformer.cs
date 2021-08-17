using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.GeojsonExtractor.Interface
{

    public interface ITransformer
    {
        ThMEPOriginTransformer Transformer { set; get; }
        void Transform();
        void Reset();
    }
}
