using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using static OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionRegistrationOptions;

namespace TangoFlexLSP.Lsp
{
    public class TangoFlexCapabilities
    {
        public static ServerCapabilities Create()
        {
            ServerCapabilities serverCapabilities = new ServerCapabilities()
            {
                TextDocumentSync = new TextDocumentSyncOptions()
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Full,
                    Save = new SaveOptions()
                    {
                        IncludeText = true,
                    }
                },

                CompletionProvider = new StaticOptions
                {
                    ResolveProvider = true,
                    TriggerCharacters = new Container<string>(".", ":")
                }
            };
            return serverCapabilities;
        }
    }
}