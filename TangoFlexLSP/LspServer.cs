using System.Text.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using TangoFlexCompiler;
using TangoFlexCompiler.SymbolParser;
using TangoFlexLSP.Lsp;
using TangoFlexLSP.RPC;

namespace TangoFlexLSP
{
    public sealed class LspServer
    {
        private readonly RpcReader reader;
        private readonly RpcWriter writer;
        private readonly WorkspaceState workspace;
        private readonly RpcDispatcher dispatcher;

        public LspServer(Stream input, Stream output, CompilerState compilerState)
        {
            reader = new RpcReader(input);
            writer = new RpcWriter(output);
            workspace = new WorkspaceState(compilerState);
            dispatcher = new RpcDispatcher();
            RpcHandlers.ServerFields = new ServerFields()
            {
                RpcReader = reader,
                RpcWriter = writer,
                Workspace = workspace,
            };
        }

        public void Run()
        {
            writer.Notify("window/logMessage", new LogMessageParams()
            {
                Type = MessageType.Log,
                Message = "TangoFlex LSP started\nStarting loop now",
            });
            while (true)
            {
                string json = reader.Read();
                writer.Notify("window/logMessage", new LogMessageParams()
                {
                    Type = MessageType.Log,
                    Message = $"Received: {json}"
                });

                if (json == null)
                {
                    Console.Error.WriteLine("json is null");
                    break;
                }

                HandleMessage(json);
            }
        }

        private void HandleMessage(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            RpcMessage message = JsonSerializer.Deserialize<RpcMessage>(json);
            dispatcher.Dispatch(message);
        }

        public static LspRange ToRange(SourceSpan span)
        {
            return new LspRange(
                new LspPosition(span.StartLine - 1, span.StartColumn - 1),
                new LspPosition(span.EndLine - 1, span.EndColumn - 1)
            );
        }

    }

}