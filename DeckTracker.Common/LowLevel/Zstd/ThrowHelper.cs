using System;
using System.Runtime.InteropServices;
using size_t = System.UIntPtr;

namespace DeckTracker.LowLevel.Zstd
{
    internal static class ReturnValueExtensions
    {
        public static size_t EnsureZstdSuccess(this size_t returnValue)
        {
            if (ExternMethods.ZSTD_isError(returnValue) != 0)
                ThrowException(returnValue, Marshal.PtrToStringAnsi(ExternMethods.ZSTD_getErrorName(returnValue)));
            return returnValue;
        }

        private static void ThrowException(size_t returnValue, string message)
        {
            uint code = unchecked(0 - (uint)(ulong)returnValue); // Negate returnValue (UintPtr)
            if (code == ZSTD_error_dstSize_tooSmall)
#if NETSTANDARD2_0
                throw new OutOfMemoryException(message);
#else
                throw new InsufficientMemoryException(message);
#endif
            throw new ZstdException(message);
        }

        // ReSharper disable once InconsistentNaming
        // NOTE that this const may change on zstdlib update
        private const int ZSTD_error_dstSize_tooSmall = 12;

        public static IntPtr EnsureZstdSuccess(this IntPtr returnValue)
        {
            if (returnValue == IntPtr.Zero)
                throw new ZstdException("Failed to create a structure");
            return returnValue;
        }
    }

    internal class ZstdException : Exception
    {
        public ZstdException(string message) : base(message)
        {
        }
    }
}
