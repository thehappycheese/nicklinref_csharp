using System;
using System.Collections.Generic;
using System.Text;

namespace Helpers;

public static class BatchRequestDecoder {
    public static List<(string road, float slkFrom, float slkTo, float offset, string cwy)> DecodeRequests(byte[] batch) {
        var requests = new List<(string road, float slkFrom, float slkTo, float offset, string cwy)>();
        int offset = 0;

        while (offset < batch.Length) {
            int roadNameLength = batch[offset];
            string road = Encoding.UTF8.GetString(batch, offset + 1, roadNameLength);

            int requestOffset = offset + 1 + roadNameLength;
            float slkFrom = BitConverter.ToSingle(batch, requestOffset);
            float slkTo = BitConverter.ToSingle(batch, requestOffset + 4);
            float offsetMetres = BitConverter.ToSingle(batch, requestOffset + 8);
            byte cwyByte = batch[requestOffset + 12];

            string cwy = cwyByte switch {
                0b0000_0100 => "L",
                0b0000_0001 => "R",
                0b0000_0010 => "S",
                0b0000_0101 => "LR",
                0b0000_0110 => "LS",
                0b0000_0011 => "RS",
                0b0000_0111 => "LRS",
                _ => throw new ArgumentException("Invalid carriageway value")
            };

            requests.Add((road, slkFrom, slkTo, offsetMetres, cwy));
            offset += 1 + roadNameLength + 4 + 4 + 4 + 1;
        }

        return requests;
    }
}
