using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using TangoFlexLSP.Lsp;

namespace TangoFlexLSP
{
    public static class LspLogger
    {

        public static void Info(string message)
        {
            RpcHandlers.ServerFields.RpcWriter.Notify("window/logMessage",
                new LogMessageParams
                {
                    Type = MessageType.Info,
                    Message = message
                });
        }

        public static void Error(string message)
        {
            RpcHandlers.ServerFields.RpcWriter.Notify("window/logMessage",
                new LogMessageParams
                {
                    Type = MessageType.Error,
                    Message = message
                });
        }
    }

}