
using System.Text.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using TangoFlexLSP.RPC;

namespace TangoFlexLSP.Lsp
{
    public static class RpcHandlers
    {
        public static ServerFields ServerFields { get; set; }
        public static void HandleInitialize(RpcMessage msg)
        {
            InitializeParams p = JsonSerializer.Deserialize<InitializeParams>(msg.@params);

            InitializeResult result = new InitializeResult
            {
                Capabilities = TangoFlexCapabilities.Create()
            };


            // ServerFields.RpcWriter.Reply(msg.id.Value, result);

            ServerFields.RpcWriter.Notify("initialized", new { });
        }
        public static void HandleDidOpen(RpcMessage msg)
        {
            DidOpenTextDocumentParams p = JsonSerializer.Deserialize<DidOpenTextDocumentParams>(msg.@params);
        }
    }

    public class ServerFields
    {
        public RpcReader RpcReader { get; set; }
        public RpcWriter RpcWriter { get; set; }
        public WorkspaceState Workspace { get; set; }
    }
}