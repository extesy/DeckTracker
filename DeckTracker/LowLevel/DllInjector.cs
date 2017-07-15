using System;
using System.Runtime.InteropServices;
using System.Text;
using DeckTracker.Domain;

namespace DeckTracker.LowLevel
{
    internal static class DllInjector
    {
        #region Constants

        private const int ERROR_UNHANDLED_EXCEPTION = -1;
        private const int ERROR_NOT_DOS_PROGRAM = -2;
        private const int ERROR_NOT_WINDOWS_PROGRAM = -3;
        private const int ERROR_NO_OPTIONAL_HEADER = -4;
        private const int ERROR_NO_IMPORT_TABLE = -5;
        private const int ERROR_NO_IMPORT_DIRECTORY = -6;
        private const int ERROR_MISMATCHED_ORDINALS = -7;
        private const UInt32 INVALID_HANDLE_VALUE = 0xffffffff;

        private const uint IMAGE_DOS_SIGNATURE = 0x5A4D;            // MZ
        private const string IMAGE_DOS_SIGNATURE_STRING = "MZ";     // MZ

        private const uint IMAGE_OS2_SIGNATURE = 0x454E;      // NE
        private const uint IMAGE_OS2_SIGNATURE_LE = 0x454C;      // LE
        private const uint IMAGE_VXD_SIGNATURE = 0x454C;      // LE
        private const uint IMAGE_NT_SIGNATURE = 0x00004550;  // PE00

        private const uint IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10b;
        private const uint IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b;

        private enum DirectoryEntries
        {
            IMAGE_DIRECTORY_ENTRY_EXPORT = 0,   // Export Directory
            IMAGE_DIRECTORY_ENTRY_IMPORT = 1,   // Import Directory
            IMAGE_DIRECTORY_ENTRY_RESOURCE = 2,   // Resource Directory
            IMAGE_DIRECTORY_ENTRY_EXCEPTION = 3,   // Exception Directory
            IMAGE_DIRECTORY_ENTRY_SECURITY = 4,   // Security Directory
            IMAGE_DIRECTORY_ENTRY_BASERELOC = 5,   // Base Relocation Table
            IMAGE_DIRECTORY_ENTRY_DEBUG = 6,   // Debug Directory
            //      IMAGE_DIRECTORY_ENTRY_COPYRIGHT       7,   // (X86 usage)
            IMAGE_DIRECTORY_ENTRY_ARCHITECTURE = 7,   // Architecture Specific Data
            IMAGE_DIRECTORY_ENTRY_GLOBALPTR = 8,   // RVA of GP
            IMAGE_DIRECTORY_ENTRY_TLS = 9,   // TLS Directory
            IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG = 10,   // Load Configuration Directory
            IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT = 11,   // Bound Import Directory in headers
            IMAGE_DIRECTORY_ENTRY_IAT = 12,   // Import Address Table
            IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT = 13,   // Delay Load Import Descriptors
            IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR = 14,   // COM Runtime descriptor

        }

        [Flags]
        private enum SnapshotFlags : uint
        {
            TH32CS_SNAPHEAPLIST = 0x00000001,
            TH32CS_SNAPPROCESS = 0x00000002,
            TH32CS_SNAPTHREAD = 0x00000004,
            TH32CS_SNAPMODULE = 0x00000008,
            TH32CS_SNAPMODULE32 = 0x00000010,
            TH32CS_SNAPALL = TH32CS_SNAPHEAPLIST | TH32CS_SNAPMODULE | TH32CS_SNAPPROCESS | TH32CS_SNAPTHREAD,
            TH32CS_INHERIT = 0x80000000,
            NoHeaps = 0x40000000
        }

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            PROCESS_ALL_ACCESS = 0x001F0FFF,
            PROCESS_TERMINATE = 0x00000001,
            PROCESS_CREATE_THREAD = 0x00000002,
            PROCESS_VM_OPERATION = 0x00000008,
            PROCESS_VM_READ = 0x00000010,
            PROCESS_VM_WRITE = 0x00000020,
            PROCESS_DUP_HANDLE = 0x00000040,
            PROCESS_CREATE_PROCESS = 0x000000080,
            PROCESS_SET_QUOTA = 0x00000100,
            PROCESS_SET_INFORMATION = 0x00000200,
            PROCESS_QUERY_INFORMATION = 0x00000400,
            PROCESS_QUERY_LIMITED_INFORMATION = 0x00001000,
            SYNCHRONIZE = 0x00100000,
            PROCESS_SUSPEND_RESUME = 0x800
        }

        [Flags]
        private enum AllocationType
        {
            MEM_COMMIT = 0x1000,
            MEM_RESERVE = 0x2000,
            MEM_DECOMMIT = 0x4000,
            MEM_RELEASE = 0x8000,
            MEM_RESET = 0x80000,
            MEM_PHYSICAL = 0x400000,
            MEM_TOP_DOWN = 0x100000,
            MEM_RESET_UNDO = 0x1000000,
            MEM_WRITE_WATCH = 0x200000,
            MEM_LARGE_PAGES = 0x20000000
        }

        [Flags]
        private enum MemoryProtection
        {
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400,
            PAGE_TARGETS_INVALID = 0x40000000,
            PAGE_TARGETS_NO_UPDATE = 0x40000000
        }

        [Flags]
        private enum FreeType
        {
            Decommit = 0x4000,
            Release = 0x8000,
        }

        #endregion // Constants

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_DOS_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] e_magic;       // Magic number
            public UInt16 e_cblp;    // Bytes on last page of file
            public UInt16 e_cp;      // Pages in file
            public UInt16 e_crlc;    // Relocations
            public UInt16 e_cparhdr;     // Size of header in paragraphs
            public UInt16 e_minalloc;    // Minimum extra paragraphs needed
            public UInt16 e_maxalloc;    // Maximum extra paragraphs needed
            public UInt16 e_ss;      // Initial (relative) SS value
            public UInt16 e_sp;      // Initial SP value
            public UInt16 e_csum;    // Checksum
            public UInt16 e_ip;      // Initial IP value
            public UInt16 e_cs;      // Initial (relative) CS value
            public UInt16 e_lfarlc;      // File address of relocation table
            public UInt16 e_ovno;    // Overlay number
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public UInt16[] e_res1;    // Reserved words
            public UInt16 e_oemid;       // OEM identifier (for e_oeminfo)
            public UInt16 e_oeminfo;     // OEM information; e_oemid specific
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public UInt16[] e_res2;    // Reserved words
            public Int32 e_lfanew;      // File address of new exe header

            public static int SizeOf => Marshal.SizeOf(typeof(IMAGE_DOS_HEADER));

            public string e_magic_string => new string(e_magic);

            public bool isValid => e_magic_string == "MZ";
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_DATA_DIRECTORY
        {
            public UInt32 VirtualAddress;
            public UInt32 Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_FILE_HEADER
        {
            public UInt16 Machine;
            public UInt16 NumberOfSections;
            public UInt32 TimeDateStamp;
            public UInt32 PointerToSymbolTable;
            public UInt32 NumberOfSymbols;
            public UInt16 SizeOfOptionalHeader;
            public UInt16 Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_OPTIONAL_HEADER32
        {
            public UInt16 Magic;
            public Byte MajorLinkerVersion;
            public Byte MinorLinkerVersion;
            public UInt32 SizeOfCode;
            public UInt32 SizeOfInitializedData;
            public UInt32 SizeOfUninitializedData;
            public UInt32 AddressOfEntryPoint;
            public UInt32 BaseOfCode;
            public UInt32 BaseOfData;
            public UInt32 ImageBase;
            public UInt32 SectionAlignment;
            public UInt32 FileAlignment;
            public UInt16 MajorOperatingSystemVersion;
            public UInt16 MinorOperatingSystemVersion;
            public UInt16 MajorImageVersion;
            public UInt16 MinorImageVersion;
            public UInt16 MajorSubsystemVersion;
            public UInt16 MinorSubsystemVersion;
            public UInt32 Win32VersionValue;
            public UInt32 SizeOfImage;
            public UInt32 SizeOfHeaders;
            public UInt32 CheckSum;
            public UInt16 Subsystem;
            public UInt16 DllCharacteristics;
            public UInt32 SizeOfStackReserve;
            public UInt32 SizeOfStackCommit;
            public UInt32 SizeOfHeapReserve;
            public UInt32 SizeOfHeapCommit;
            public UInt32 LoaderFlags;
            public UInt32 NumberOfRvaAndSizes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] DataDirectory;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_OPTIONAL_HEADER64
        {
            public UInt16 Magic;
            public Byte MajorLinkerVersion;
            public Byte MinorLinkerVersion;
            public UInt32 SizeOfCode;
            public UInt32 SizeOfInitializedData;
            public UInt32 SizeOfUninitializedData;
            public UInt32 AddressOfEntryPoint;
            public UInt32 BaseOfCode;
            public UInt64 ImageBase;
            public UInt32 SectionAlignment;
            public UInt32 FileAlignment;
            public UInt16 MajorOperatingSystemVersion;
            public UInt16 MinorOperatingSystemVersion;
            public UInt16 MajorImageVersion;
            public UInt16 MinorImageVersion;
            public UInt16 MajorSubsystemVersion;
            public UInt16 MinorSubsystemVersion;
            public UInt32 Win32VersionValue;
            public UInt32 SizeOfImage;
            public UInt32 SizeOfHeaders;
            public UInt32 CheckSum;
            public UInt16 Subsystem;
            public UInt16 DllCharacteristics;
            public UInt64 SizeOfStackReserve;
            public UInt64 SizeOfStackCommit;
            public UInt64 SizeOfHeapReserve;
            public UInt64 SizeOfHeapCommit;
            public UInt32 LoaderFlags;
            public UInt32 NumberOfRvaAndSizes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] DataDirectory;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_NT_HEADERS32
        {
            public UInt32 Signature;
            public IMAGE_FILE_HEADER FileHeader;
            public IMAGE_OPTIONAL_HEADER32 OptionalHeader32;

            public static int SizeOf = Marshal.SizeOf(typeof(IMAGE_NT_HEADERS32));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_NT_HEADERS64
        {
            public UInt32 Signature;
            public IMAGE_FILE_HEADER FileHeader;
            public IMAGE_OPTIONAL_HEADER64 OptionalHeader64;

            public static int SizeOf = Marshal.SizeOf(typeof(IMAGE_NT_HEADERS64));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_EXPORT_DIRECTORY
        {
            public UInt32 Characteristics;
            public UInt32 TimeDateStamp;
            public UInt16 MajorVersion;
            public UInt16 MinorVersion;
            public UInt32 Name;
            public UInt32 Base;
            public UInt32 NumberOfFunctions;
            public UInt32 NumberOfNames;
            public UInt32 AddressOfFunctions;     // RVA from base of image
            public UInt32 AddressOfNames;     // RVA from base of image
            public UInt32 AddressOfNameOrdinals;  // RVA from base of image

            public static int SizeOf = Marshal.SizeOf(typeof(IMAGE_EXPORT_DIRECTORY));
        }

        private struct MODULEENTRY32
        {   //http://pastebin.com/BzD1jdmH
            private const int MAX_PATH = 255;
            internal uint dwSize;
            internal uint th32ModuleID;
            internal uint th32ProcessID;
            internal uint GlblcntUsage;
            internal uint ProccntUsage;
            internal IntPtr modBaseAddr;
            internal uint modBaseSize;
            internal IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH + 1)]
            internal string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH + 5)]
            internal string szExePath;
        }

        #endregion // Structures

        #region DllImports

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            out IMAGE_DOS_HEADER lpBuffer,
            int nSize,
            IntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            out IMAGE_NT_HEADERS32 lpBuffer,
            int nSize,
            IntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            out IMAGE_NT_HEADERS64 lpBuffer,
            int nSize,
            IntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            out IMAGE_EXPORT_DIRECTORY lpBuffer,
            int nSize,
            IntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int nSize,
            IntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] ushort[] lpBuffer,
            int nSize,
            IntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] uint[] lpBuffer,
            int nSize,
            IntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(
            IntPtr hObject
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(
            SnapshotFlags dwFlags,
            uint th32ProcessID
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Module32First(
            IntPtr hSnapshot,
            ref MODULEENTRY32 lpme
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Module32Next(
            IntPtr hSnapshot,
            ref MODULEENTRY32 lpme
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
             ProcessAccessFlags processAccess,
             bool bInheritHandle,
             uint processId
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            int dwSize,
            FreeType dwFreeType
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out IntPtr lpThreadId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UInt32 WaitForSingleObject(
            IntPtr hHandle,
            UInt32 dwMilliseconds
        );

        #endregion // DllImports

        /// <summary>
        /// Given a ProccesID, this routine will return the base address of the module specified by moduleName.
        /// </summary>
        /// <returns>Returns NULL on failure.</returns>
        private static IntPtr? GetProcessModuleHandle(GameType gameType, uint processId, string moduleName, out int errorCode)
        {
            errorCode = 0;
            var hSnapShot = IntPtr.Zero;

            moduleName = moduleName.ToLower();  // Process/Module names are not case sensitive.

            try {
                // Getting snapshot of current running processes so we can search through thier loaded modules.
                hSnapShot = CreateToolhelp32Snapshot(SnapshotFlags.TH32CS_SNAPMODULE | SnapshotFlags.TH32CS_SNAPMODULE32, processId);

                if ((uint)hSnapShot == INVALID_HANDLE_VALUE) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"CreateToolhelp32Snapshot failed with error {errorCode}");
                    return null;
                }

                // Creating space to hold our MODULEENTRY32 structure
                var mod = new MODULEENTRY32 { dwSize = (uint)Marshal.SizeOf(typeof(MODULEENTRY32)) };

                // Retrieving the first module out of our snap shot.
                if (!Module32First(hSnapShot, ref mod)) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"Module32First failed with error {errorCode}");
                    return null;
                }

                do {
                    if (mod.szModule.ToLower() == moduleName)
                        return mod.modBaseAddr;
                } while (Module32Next(hSnapShot, ref mod));

                // Did not find the module in the specified process.
                return IntPtr.Zero;
            } catch (Exception e) {
                Logger.LogDebug(gameType, $"Unhandled exception in GetProcessModuleHandle: {e.Message}");
                errorCode = ERROR_UNHANDLED_EXCEPTION;
                return null;
            } finally {
                if (hSnapShot != IntPtr.Zero)
                    CloseHandle(hSnapShot);
            }
        }

        private static IntPtr? GetProcessProcAddress(GameType gameType, IntPtr hProcess, uint processId, string moduleName, string procName, out int errorCode)
        {
            errorCode = 0;

            try {
                // Get handle to the Kernel32.dll library thats located inside the process specified by ProcessID
                var ret = GetProcessModuleHandle(gameType, processId, "Kernel32.dll", out errorCode);
                if (ret == null) {
                    Logger.LogDebug(gameType, $"GetProcessModuleHandle failed with error {errorCode}");
                    return null;
                }

                var hKernel32 = (IntPtr)ret;

                // Retrieving the dos header of our Kernel32 library module inside of the process indicated by hProcess
                if (!ReadProcessMemory(hProcess, hKernel32, out IMAGE_DOS_HEADER dosHeader, IMAGE_DOS_HEADER.SizeOf, IntPtr.Zero)) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"ReadProcessMemory failed with error {errorCode}");
                    return null;
                }

                // checking to make sure this is a DOS program
                if (dosHeader.e_magic_string != IMAGE_DOS_SIGNATURE_STRING) {
                    errorCode = ERROR_NOT_DOS_PROGRAM;
                    return null;
                }

                // parse nt header
                var ntHeaderPtr = new IntPtr(dosHeader.e_lfanew + hKernel32.ToInt32());

                IMAGE_DATA_DIRECTORY[] dataDirectory;
                if (IntPtr.Size == 4) {
                    // this is block is for 32-bit Architectures.
                    IMAGE_NT_HEADERS32 ntHeader;
                    if (!ReadProcessMemory(hProcess, ntHeaderPtr, out ntHeader, IMAGE_NT_HEADERS32.SizeOf, IntPtr.Zero)) {
                        errorCode = Marshal.GetLastWin32Error();
                        Logger.LogDebug(gameType, $"ReadProcessMemory failed with error {errorCode}");
                        return null;
                    }

                    // Checking to make sure this is a windows program
                    if (ntHeader.Signature != IMAGE_NT_SIGNATURE) {
                        errorCode = ERROR_NOT_WINDOWS_PROGRAM;
                        return null;
                    }

                    // optional header (pretty much not optional)
                    if (ntHeader.OptionalHeader32.Magic != IMAGE_NT_OPTIONAL_HDR32_MAGIC) {
                        errorCode = ERROR_NO_OPTIONAL_HEADER;
                        return null;
                    }

                    dataDirectory = ntHeader.OptionalHeader32.DataDirectory;
                } else {
                    // this is block is for 64-bit Architectures.
                    IMAGE_NT_HEADERS64 ntHeader;
                    if (!ReadProcessMemory(hProcess, ntHeaderPtr, out ntHeader, IMAGE_NT_HEADERS64.SizeOf, IntPtr.Zero)) {
                        errorCode = Marshal.GetLastWin32Error();
                        Logger.LogDebug(gameType, $"ReadProcessMemory failed with error {errorCode}");
                        return null;
                    }

                    // Checking to make sure this is a windows program
                    if (ntHeader.Signature != IMAGE_NT_SIGNATURE) {
                        errorCode = ERROR_NOT_WINDOWS_PROGRAM;
                        return null;
                    }

                    // optional header (pretty much not optional)
                    if (ntHeader.OptionalHeader64.Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC) {
                        errorCode = ERROR_NO_OPTIONAL_HEADER;
                        return null;
                    }

                    dataDirectory = ntHeader.OptionalHeader64.DataDirectory;
                }

                IMAGE_DATA_DIRECTORY entryExport = dataDirectory[(int)DirectoryEntries.IMAGE_DIRECTORY_ENTRY_EXPORT];

                if (entryExport.Size == 0) {
                    errorCode = ERROR_NO_IMPORT_TABLE;
                    return null; // no import table
                }

                if (entryExport.VirtualAddress == 0) {
                    errorCode = ERROR_NO_IMPORT_DIRECTORY;
                    return null; // no import directory
                }

                var pExportsPtr = new IntPtr(entryExport.VirtualAddress + hKernel32.ToInt32());

                if (!ReadProcessMemory(hProcess, pExportsPtr, out IMAGE_EXPORT_DIRECTORY pExports, IMAGE_EXPORT_DIRECTORY.SizeOf, IntPtr.Zero)) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"ReadProcessMemory failed with error {errorCode}");
                    return null;
                }

                var functionsPtr = new IntPtr(hKernel32.ToInt32() + pExports.AddressOfFunctions);
                var ordinalsPtr = new IntPtr(hKernel32.ToInt32() + pExports.AddressOfNameOrdinals);
                var namesPtr = new IntPtr(hKernel32.ToInt32() + pExports.AddressOfNames);

                var functions = new uint[pExports.NumberOfFunctions];
                if (!ReadProcessMemory(hProcess, functionsPtr, functions, (int)pExports.NumberOfFunctions * sizeof(uint), IntPtr.Zero)) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"ReadProcessMemory failed with error {errorCode}");
                    return null;
                }

                var ordinals = new ushort[pExports.NumberOfNames];
                if (!ReadProcessMemory(hProcess, ordinalsPtr, ordinals, (int)pExports.NumberOfNames * sizeof(ushort), IntPtr.Zero)) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"ReadProcessMemory failed with error {errorCode}");
                    return null;
                }

                var names = new uint[pExports.NumberOfNames];
                if (!ReadProcessMemory(hProcess, namesPtr, names, (int)pExports.NumberOfNames * sizeof(uint), IntPtr.Zero)) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"ReadProcessMemory failed with error {errorCode}");
                    return null;
                }

                for (uint i = 0; i < ordinals.Length; i++) {
                    uint ord = ordinals[i];
                    if (i >= pExports.NumberOfNames || ord >= pExports.NumberOfFunctions) {
                        errorCode = ERROR_MISMATCHED_ORDINALS;
                        return null; // Mismatched ordinals
                    }

                    if (functions[ord] < entryExport.VirtualAddress || functions[ord] >= entryExport.VirtualAddress + entryExport.Size) {
                        var namePtr = new IntPtr(hKernel32.ToInt32() + names[i]);

                        if (namePtr != IntPtr.Zero) {
                            var nameBuf = new byte[procName.Length + 1];    // +1 for terminating null '\0'
                            if (!ReadProcessMemory(hProcess, namePtr, nameBuf, nameBuf.Length, IntPtr.Zero))
                                continue;

                            if (nameBuf[nameBuf.Length - 1] == 0) {    // check for partial name that does not end with terminating zero
                                string name = Encoding.ASCII.GetString(nameBuf, 0, nameBuf.Length - 1);   // NB! buf length - 1

                                if (name == procName) {
                                    IntPtr pFunctionAddress = new IntPtr(hKernel32.ToInt32() + functions[ord]);
                                    return pFunctionAddress;
                                }
                            }
                        }
                    }
                }

                return IntPtr.Zero;
            } catch (Exception e) {
                errorCode = ERROR_UNHANDLED_EXCEPTION;
                Logger.LogDebug(gameType, $"Unhandled exception in GetProcessProcAddress: {e.Message}");
                return null;
            }
        }

        public static bool InjectDll(GameType gameType, uint processId, string dllPath, out int errorCode, uint dllExitWaitTime = 5000)
        {
            errorCode = 0;
            var hProcess = IntPtr.Zero;
            var hDllPathArg = IntPtr.Zero;
            var hRemoteThread = IntPtr.Zero;

            try {
                // Get handle to the process identified by processID
                hProcess = OpenProcess(ProcessAccessFlags.PROCESS_ALL_ACCESS, false, processId);

                // Error checking
                if (hProcess == IntPtr.Zero) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"OpenProcess failed with error {errorCode}");
                    return false;
                }

                // Get Address of the LoadLibrary routine withint the process identified by processID
                var ret = GetProcessProcAddress(gameType, hProcess, processId, "Kernel32.dll", "LoadLibraryW", out errorCode);
                if (ret == null) {
                    Logger.LogDebug(gameType, $"GetProcessProcAddress failed with error {errorCode}");
                    return false;
                }

                var hLoadLibrary = (IntPtr)ret;

                // Allocate some memory in the remote process to store the dllPath string argument.
                hDllPathArg = VirtualAllocEx(
                    hProcess,
                    IntPtr.Zero,
                    (uint)(dllPath.Length + 1) * 2,
                    AllocationType.MEM_COMMIT,
                    MemoryProtection.PAGE_READWRITE
                );

                // Error checking
                if (hDllPathArg == IntPtr.Zero) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"VirtualAllocEx failed with error {errorCode}");
                    return false;
                }

                // write dll file path argument into remote process's memory space
                bool isSucceeded = WriteProcessMemory(
                    hProcess,
                    hDllPathArg,
                    Encoding.Unicode.GetBytes(dllPath),
                    (dllPath.Length + 1) * 2,
                    out var bytesWritten
                );

                // Error checking
                if (!isSucceeded) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"WriteProcessMemory failed with error {errorCode}");
                    return false;
                }

                // invoke the LoadLibrary method in the remote process.
                hRemoteThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, hLoadLibrary, hDllPathArg, 0, out var dwThreadId);

                // Error checking
                if (hRemoteThread == IntPtr.Zero) {
                    errorCode = Marshal.GetLastWin32Error();
                    Logger.LogDebug(gameType, $"CreateRemoteThread failed with error {errorCode}");
                    return false;
                }

                // Waiting for thread to exit.
                if (dllExitWaitTime > 0) {
                    uint result = WaitForSingleObject(hRemoteThread, dllExitWaitTime);
                    Logger.LogDebug(gameType, $"Waiting for remote thread finished with result {result}");
                }

                return true;
            } catch (Exception e) {
                errorCode = ERROR_UNHANDLED_EXCEPTION;
                Logger.LogDebug(gameType, $"Unhandled exception in InjectDll: {e.Message}");
                return false;
            } finally {
                if (hProcess != IntPtr.Zero) {
                    if (hDllPathArg != IntPtr.Zero)
                        VirtualFreeEx(hProcess, hDllPathArg, 0, FreeType.Release);
                    CloseHandle(hProcess);
                }

                if (hRemoteThread != IntPtr.Zero)
                    CloseHandle(hRemoteThread);
            }
        }
    }
}
