using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DotJEM.Web.Host.Providers.Concurrency;

public class SimpleDictionaryWriter
{
    //TODO: As Service! Or delete and replace with XML/JSON Serialization, it will hardly matter.
    public void Write(string file, Dictionary<string, long> values)
    {
        byte[] buffer = new byte[1024 * 16];
        int offset = values.Aggregate(0, (current, change) => WriteKeyValueToBuffer(change, buffer, current));
        byte[] outputBuffer = new byte[offset];
        Buffer.BlockCopy(buffer, 0, outputBuffer, 0, offset);
        File.WriteAllBytes(file, outputBuffer);
    }

    private int WriteKeyValueToBuffer(KeyValuePair<string, long> kvp, byte[] buffer, int offset)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(kvp.Key);
        byte[] token = BitConverter.GetBytes(kvp.Value);

        buffer[offset++] = (byte)bytes.Length;
        Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
        Buffer.BlockCopy(token, 0, buffer, offset + bytes.Length, token.Length);
        return offset + bytes.Length + 8;
    }

    private int ReadKeyValueFromBuffer(byte[] buffer, Dictionary<string, long> map, int offset)
    {
        byte[] bytes = new byte[buffer[offset++]];

        Buffer.BlockCopy(buffer, offset, bytes, 0, bytes.Length);
        offset += bytes.Length;
        string key = Encoding.UTF8.GetString(bytes);
        map[key] = BitConverter.ToInt64(buffer, offset);
        return offset + 8;
    }

    public Dictionary<string, long> Read(string file)
    {
        if (!File.Exists(file))
            return new Dictionary<string, long>();
        byte[] inputBuffer = File.ReadAllBytes(file);
        Dictionary<string, long> map = new Dictionary<string, long>();
        int offset = 0;
        while (offset < inputBuffer.Length)
        {
            offset = ReadKeyValueFromBuffer(inputBuffer, map, offset);
        }
        return map;
    }
}