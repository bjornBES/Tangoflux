
namespace TangoFlexLSP.RPC
{
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public sealed class RpcWriter
    {
        private readonly Stream output;

        static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };


        public RpcWriter(Stream output)
        {
            this.output = output;
        }

        public void WriteRaw(string json)
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            string header = $"Content-Length: {body.Length}\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);

            output.Write(headerBytes, 0, headerBytes.Length);
            output.Write(body, 0, body.Length);
            output.Flush();
        }

        public void Reply<T>(int id, T result)
        {
            var msg = new
            {
                jsonrpc = "2.0",
                id = id,
                result = result
            };

            WriteRaw(JsonSerializer.Serialize(msg, JsonOptions));
        }

        public void Error(int id, int code, string message)
        {
            var msg = new
            {
                jsonrpc = "2.0",
                id = id,
                error = new
                {
                    code = code,
                    message = message
                }
            };

            WriteRaw(JsonSerializer.Serialize(msg, JsonOptions));
        }

        public void Notify<T>(string method, T @params)
        {
            var msg = new
            {
                jsonrpc = "2.0",
                method = method,
                @params = @params
            };

            WriteRaw(JsonSerializer.Serialize(msg, JsonOptions));
        }
    }
}