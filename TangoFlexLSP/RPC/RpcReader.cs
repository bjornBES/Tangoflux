namespace TangoFlexLSP.RPC
{
    public sealed class RpcReader
    {
        private readonly StreamReader reader;

        public RpcReader(Stream input)
        {
            reader = new StreamReader(input);
        }

        public string Read()
        {
            int length = 0;

            while (true)
            {
                string line = reader.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

                if (line.StartsWith("Content-Length:"))
                {
                    length = int.Parse(line.Substring(15));
                    // Console.Error.WriteLine($"json got length {length}");
                }
            }

            char[] buffer = new char[length];
            int readBytes = reader.ReadBlock(buffer, 0, length);
            // Console.Error.WriteLine($"read {readBytes} bytes out of {length}");

            return new string(buffer);
        }
    }
}