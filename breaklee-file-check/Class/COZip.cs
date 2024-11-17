﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static breaklee_file_check.Class.Zlib;

namespace breaklee_file_check.Class
{
    internal class COZip
    {
        public const int CHUNK = 16384;

        public static void Deflate(Stream source, Stream dest, uint xor = 0x57676592, int level = 9)
        {
            int ret, flush;
            var length = (uint)source.Length;
            var writer = new BinaryWriter(dest);
            var reader = new BinaryReader(source);

            writer.Write(length);

            var zs = new ZStream
            {
                ZAlloc = IntPtr.Zero,
                ZFree = IntPtr.Zero,
                Opaque = IntPtr.Zero
            };

            var size = Marshal.SizeOf(zs);
            ret = DeflateInit2(
                ref zs,
                level,
                CompressionMethod.Deflated,
                -MAXWBITS,
                8,
                CompressionStrategy.Default,
                Version(),
                size);

            if (ret > 0)
            {   // Z_OK = 0
                var err = Marshal.PtrToStringAuto(zs.Msg);
                throw new Exception(err);
            }

            var outd = Marshal.AllocHGlobal(CHUNK);

            int i = 0;

            do
            {
                var ind = reader.ReadBytes(CHUNK);
                zs.AvailIn = (uint)ind.Length;
                flush = (source.Position == length) ? ZFINISH : ZNOFLUSH;
                var nextIn = Marshal.AllocHGlobal(ind.Length);
                Marshal.Copy(ind, 0, nextIn, ind.Length);
                zs.NextIn = nextIn;

                do
                {
                    zs.AvailOut = CHUNK;
                    zs.NextOut = outd;
                    ret = Zlib.Deflate(ref zs, flush);

                    if (ret == (int)Result.StreamError)
                    {
                        var err = Marshal.PtrToStringAuto(zs.Msg);
                        throw new Exception(err);
                    }

                    var have = CHUNK - zs.AvailOut;

                    if (have <= 0)
                        continue;

                    var buf = new byte[have];
                    Marshal.Copy(outd, buf, 0, (int)have);

                    for (; i < 4; i++)
                    {
                        int j = i % 4;
                        byte key = (byte)((xor >> (8 * j)) & 0xFF);
                        buf[i] ^= key;
                    }

                    writer.Write(buf);
                } while (zs.AvailOut == 0);

                Marshal.FreeHGlobal(nextIn);
            } while (flush != ZFINISH);

            Marshal.FreeHGlobal(outd);
            DeflateEnd(ref zs);

            writer.Flush();
        }

        public static void Inflate(Stream source, Stream dest, uint xor = 0x57676592)
        {
            uint have;

            var reader = new BinaryReader(source);
            var writer = new BinaryWriter(dest);

            var dataSize = reader.ReadUInt32();

            var zs = new ZStream();

            var size = Marshal.SizeOf(zs);
            var ret = InflateInit2(ref zs, -MAXWBITS, Version(), size);

            if (ret != (int)Result.OK)
            {
                var err = Marshal.PtrToStringAuto(zs.Msg);
                throw new Exception(err);
            }

            var outd = Marshal.AllocHGlobal(CHUNK);

            int i = 0;

            do
            {
                var ind = reader.ReadBytes(CHUNK);
                zs.AvailIn = (uint)ind.Length;

                if (zs.AvailIn == 0)
                    break;

                for (; i < 4; i++)
                {
                    int j = i % 4;
                    byte key = (byte)((xor >> (8 * j)) & 0xFF);
                    ind[i] ^= key;
                }

                var nextIn = Marshal.AllocHGlobal(ind.Length);
                Marshal.Copy(ind, 0, nextIn, ind.Length);
                zs.NextIn = nextIn;

                do
                {
                    zs.AvailOut = CHUNK;
                    zs.NextOut = outd;
                    ret = Zlib.Inflate(ref zs, ZNOFLUSH);

                    if (ret != (int)Result.OK &&
                        ret != (int)Result.StreamEnd)
                    {
                        var err = Marshal.PtrToStringAuto(zs.Msg);
                        throw new Exception(err);
                    }


                    have = CHUNK - zs.AvailOut;

                    var buf = new byte[have];
                    Marshal.Copy(outd, buf, 0, (int)have);

                    writer.Write(buf);
                } while (zs.AvailOut == 0);

                Marshal.FreeHGlobal(nextIn);
            } while (ret != (int)Result.StreamEnd);

            Marshal.FreeHGlobal(outd);
            InflateEnd(ref zs);

            writer.Flush();
        }
    }
}