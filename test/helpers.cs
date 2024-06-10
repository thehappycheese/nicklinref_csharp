using System;
using System.Linq;
using System.Text;

namespace LinrefTestHelpers;

public static class BatchRequestHelper {
    private static readonly byte L = 0b0000_0100;
    private static readonly byte R = 0b0000_0001;
    private static readonly byte S = 0b0000_0010;
    private static readonly byte LR = 0b0000_0101;
    private static readonly byte LS = 0b0000_0110;
    private static readonly byte RS = 0b0000_0011;
    private static readonly byte LRS = 0b0000_0111;

    private static readonly Dictionary<string, byte> CWY_LOOKUP = new Dictionary<string, byte> {
        {"L", L},
        {"R", R},
        {"S", S},
        {"LR", LR},
        {"LS", LS},
        {"RS", RS},
        {"LRS", LRS}
    };

    public static byte[] BinaryEncodeRequest(string road, float slkFrom, float slkTo, float offset, string cwy) {
        var roadBytes = Encoding.UTF8.GetBytes(road);
        var buffer = new byte[1 + roadBytes.Length + 4 + 4 + 4 + 1];

        buffer[0] = (byte)roadBytes.Length;
        Array.Copy(roadBytes, 0, buffer, 1, roadBytes.Length);

        int offsetIndex = 1 + roadBytes.Length;
        BitConverter.GetBytes(slkFrom).CopyTo(buffer, offsetIndex);
        BitConverter.GetBytes(slkTo).CopyTo(buffer, offsetIndex + 4);
        BitConverter.GetBytes(offset).CopyTo(buffer, offsetIndex + 8);
        buffer[offsetIndex + 12] = CWY_LOOKUP.ContainsKey(cwy.ToUpper()) ? CWY_LOOKUP[cwy.ToUpper()] : (byte)0;

        return buffer;
    }

    public static byte[] CombineRequests(params byte[][] requests) {
        var combinedLength = requests.Sum(r => r.Length);
        var combinedBuffer = new byte[combinedLength];
        int offset = 0;

        foreach (var request in requests) {
            Array.Copy(request, 0, combinedBuffer, offset, request.Length);
            offset += request.Length;
        }

        return combinedBuffer;
    }
}