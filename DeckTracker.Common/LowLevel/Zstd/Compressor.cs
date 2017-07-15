using System;
using size_t = System.UIntPtr;

namespace DeckTracker.LowLevel.Zstd
{
    public sealed class Compressor : IDisposable
    {
        public static int MaxCompressionLevel => ExternMethods.ZSTD_maxCLevel();

        private readonly int compressionLevel;
        private readonly IntPtr cctx;
        private bool disposed;

        public Compressor(int compressionLevel)
        {
            this.compressionLevel = compressionLevel;
            cctx = ExternMethods.ZSTD_createCCtx().EnsureZstdSuccess();
        }

        ~Compressor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed) return;
            ExternMethods.ZSTD_freeCCtx(cctx);
            disposed = true;
        }

        public byte[] Compress(byte[] src)
        {
            return Compress(new ArraySegment<byte>(src));
        }

        private byte[] Compress(ArraySegment<byte> src)
        {
            if (src.Count == 0)
                return new byte[0];

            int dstCapacity = GetCompressBound(src.Count);
            var dst = new byte[dstCapacity];
            int dstSize = Compress(src, dst, 0);
            if (dstCapacity != dstSize)
                Array.Resize(ref dst, dstSize);

            return dst;
        }

        private int Compress(ArraySegment<byte> src, byte[] dst, int offset)
        {
            if (offset < 0 || offset >= dst.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (src.Count == 0)
                return 0;

            int dstCapacity = dst.Length - offset;
            size_t dstSize;
            using (var srcPtr = new ArraySegmentPtr(src))
            using (var dstPtr = new ArraySegmentPtr(new ArraySegment<byte>(dst, offset, dstCapacity)))
                dstSize = ExternMethods.ZSTD_compressCCtx(cctx, dstPtr, (size_t)dstCapacity, srcPtr, (size_t)src.Count, compressionLevel).EnsureZstdSuccess();
            return (int)dstSize;
        }

        private static int GetCompressBound(int size)
        {
            return (int)ExternMethods.ZSTD_compressBound((size_t)size);
        }
    }
}
