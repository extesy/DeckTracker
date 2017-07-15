using System;
using size_t = System.UIntPtr;

namespace DeckTracker.LowLevel.Zstd
{
    public sealed class Decompressor : IDisposable
    {
        private readonly IntPtr dctx;
        private bool disposed;

        public Decompressor()
        {
            dctx = ExternMethods.ZSTD_createDCtx().EnsureZstdSuccess();
        }

        ~Decompressor()
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
            ExternMethods.ZSTD_freeDCtx(dctx);
            disposed = true;
        }

        public byte[] Decompress(byte[] src, int maxDecompressedSize = int.MaxValue)
        {
            return Decompress(new ArraySegment<byte>(src), maxDecompressedSize);
        }

        private byte[] Decompress(ArraySegment<byte> src, int maxDecompressedSize = int.MaxValue)
        {
            if (src.Count == 0)
                return new byte[0];

            ulong expectedDstSize = GetDecompressedSize(src);
            if (expectedDstSize == 0)
                throw new ZstdException("Can't create buffer for data with unspecified decompressed size (provide your own buffer to Decompress instead)");
            if (expectedDstSize > (ulong)maxDecompressedSize)
                throw new ArgumentOutOfRangeException($"Decompressed size is too big ({expectedDstSize} bytes > authorized {maxDecompressedSize} bytes)");
            var dst = new byte[expectedDstSize];

            int dstSize;
            try {
                dstSize = Decompress(src, dst, 0, false);
            } catch (InsufficientMemoryException) {
                throw new ZstdException("Invalid decompressed size");
            }

            if ((int)expectedDstSize != dstSize)
                throw new ZstdException("Invalid decompressed size specified in the data");
            return dst;
        }

        private int Decompress(ArraySegment<byte> src, byte[] dst, int offset, bool bufferSizePrecheck = true)
        {
            if (offset < 0 || offset >= dst.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (src.Count == 0)
                return 0;

            int dstCapacity = dst.Length - offset;
            using (var srcPtr = new ArraySegmentPtr(src)) {
                if (bufferSizePrecheck) {
                    ulong expectedDstSize = ExternMethods.ZSTD_getDecompressedSize(srcPtr, (size_t)src.Count);
                    if ((int)expectedDstSize > dstCapacity)
                        throw new InsufficientMemoryException("Buffer size is less than specified decompressed data size");
                }

                size_t dstSize;
                using (var dstPtr = new ArraySegmentPtr(new ArraySegment<byte>(dst, offset, dstCapacity)))
                    dstSize = ExternMethods.ZSTD_decompressDCtx(dctx, dstPtr, (size_t)dstCapacity, srcPtr, (size_t)src.Count).EnsureZstdSuccess();
                return (int)dstSize;
            }
        }

        private static ulong GetDecompressedSize(ArraySegment<byte> src)
        {
            using (var srcPtr = new ArraySegmentPtr(src))
                return ExternMethods.ZSTD_getDecompressedSize(srcPtr, (size_t)src.Count);
        }
    }
}
