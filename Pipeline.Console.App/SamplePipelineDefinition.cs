using Pipeline.Sample.App.Models;
using Pipeline.Sample.App.Pipes;
using Pipeline.Lib;

namespace Pipeline.Sample.App
{
    public class SamplePipelineDefinition : PipelineDefinition<Request, Response>
    {
        protected override void Define()
        {
            Pipeline()
                .AddPipe<ValidationPipe>()
                .Alter(
                ctx => ctx.IsValid() == true,
                pipe => pipe.AddPipe<NotifyInvalidRequest>())
                .AddPipe<GenerateDocuments>();
        }
    }
}
