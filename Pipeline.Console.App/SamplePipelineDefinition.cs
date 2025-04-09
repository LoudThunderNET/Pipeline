using Pipeline.Console.App.Models;
using Pipeline.Console.App.Pipes;
using Pipeline.Lib;

namespace Pipeline.Console.App
{
    public class SamplePipelineDefinition : PipelineDefinition<Request, Response>
    {
        protected override void Define()
        {
            Pipeline()
                .AddPipe<ValidationPipe>()
                .If(
                ctx => ctx.IsValid() == true,
                pipe => pipe.AddPipe<NotifyInvalidRequest>())
                .AddPipe<GenerateDocuments>();
        }
    }
}
