using System.Text.Json;

namespace TangoFlexLSP.RPC
{
    public struct RpcMessage
    {
        public string jsonrpc;
        public string method;
        public JsonElement @params;
        public int? id;
    }
}