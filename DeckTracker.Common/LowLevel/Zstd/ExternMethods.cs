using System;
using System.Runtime.InteropServices;
using size_t = System.UIntPtr;

namespace DeckTracker.LowLevel.Zstd
{
    internal static class ExternMethods
    {
        private const string DllName = "libzstd.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ZSTD_createCCtx();
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern size_t ZSTD_freeCCtx(IntPtr cctx);
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ZSTD_createDCtx();
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern size_t ZSTD_freeDCtx(IntPtr cctx);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern size_t ZSTD_compressCCtx(IntPtr ctx, IntPtr dst, size_t dstCapacity, IntPtr src, size_t srcSize, int compressionLevel);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern size_t ZSTD_decompressDCtx(IntPtr ctx, IntPtr dst, size_t dstCapacity, IntPtr src, size_t srcSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong ZSTD_getDecompressedSize(IntPtr src, size_t srcSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ZSTD_maxCLevel();
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern size_t ZSTD_compressBound(size_t srcSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ZSTD_isError(size_t code);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ZSTD_getErrorName(size_t code);
    }
}
