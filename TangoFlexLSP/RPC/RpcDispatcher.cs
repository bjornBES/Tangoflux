using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using TangoFlexLSP.Lsp;

namespace TangoFlexLSP.RPC
{
    public sealed class RpcDispatcher
    {
        public void Dispatch(RpcMessage msg)
        {
            RpcHandlers.ServerFields.RpcWriter.Notify(
                "window/logMessage", new LogMessageParams()
                {
                    Type = MessageType.Info,
                    Message = $"got {msg.method}"
                }
            );
            /*
            Console.Error.WriteLine($"got\n\"{msg.method}\"");
            */
            if (string.IsNullOrEmpty(msg.method))
            {
                // Console.Error.WriteLine($"method is empty");
                return;
            }
            switch (msg.method)
            {
                case "initialize":
                    RpcHandlers.HandleInitialize(msg);
                    break;

                case "textDocument/didOpen":
                    LspLogger.Info("didOpen");
                    RpcHandlers.HandleDidOpen(msg);
                    break;
                case "textDocument/didChange":
                    LspLogger.Info("didChange");
                    break;

                case "textDocument/completion":
                    // HandleCompletion(msg);
                    break;
                default:
                    RpcHandlers.ServerFields.RpcWriter.Notify(
                        "window/logMessage", new LogMessageParams()
                        {
                            Type = MessageType.Warning,
                            Message = $"method {msg.method} is not a case"
                        }
                    );
                    // Console.Error.WriteLine($"method \"{msg.method}\" is not a case");
                    break;
            }
        }
    }
}