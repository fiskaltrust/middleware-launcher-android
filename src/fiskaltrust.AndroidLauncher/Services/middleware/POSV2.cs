using System;
using System.IO.Pipelines;
using System.Net.Mime;
using System.Threading.Tasks;

#pragma warning disable CS0618
namespace fiskaltrust.AndroidLauncher.Services.middleware
{
    public sealed class POSV2
    {
        public Func<string, Task<string>> Sign { get; init; }
        public Func<string, Task<string>> Echo { get; init; }
        public Func<string, Task<(ContentType contentType, PipeReader reader)>> Journal { get; init; }

        public POSV2((Func<string, Task<string>> echo, Func<string, Task<string>> sign, Func<string, Task<(ContentType contentType, PipeReader reader)>> journal) queue)
        {
            Sign = queue.sign;
            Echo = queue.echo;
            Journal = queue.journal;
        }
    }
}
