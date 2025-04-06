using Pipeline.Console.App.Models;
using Pipeline.Console.App.Pipes;
using Pipeline.Lib.Abstraction;

namespace Pipeline.Console.App
{
    public class SamplePipelineDefinition : PipelineDefinition<Request, Response>
    {
        public override void Define()
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
