namespace Pipeline.Lib.Abstraction
{
    public partial class PipelineContext<TRequest, TResponse> : IPipelineContext
        where TRequest : class
        where TResponse : class
    {
        private readonly Dictionary<string, PropertyValue> _properties = new Dictionary<string, PropertyValue>();

        public PipelineContext(TRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; private set; }

        public Dictionary<string, PropertyValue> Properties => _properties;

        public TRequest Request { get; private set; }

        public TResponse? Response { get; set; }
    }
}
