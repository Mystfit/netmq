using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NetMQ.zmq.Native
{

    public static class Opcode
    {

        private static IntPtr codeBuffer;
        private static ulong size;

        public static void Open()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                throw new Exception("Platform not supported!");

            byte[] rdtscCode;
            if (IntPtr.Size == 4)
            {
                rdtscCode = RDTSC_32;
            }
            else
            {
                rdtscCode = RDTSC_64;
            }

            size = (ulong)(rdtscCode.Length);

            codeBuffer = NativeMethods.VirtualAlloc(IntPtr.Zero,
                (UIntPtr)size, AllocationType.COMMIT | AllocationType.RESERVE,
                 MemoryProtection.EXECUTE_READWRITE);

            Marshal.Copy(rdtscCode, 0, codeBuffer, rdtscCode.Length);

            Rdtsc = Marshal.GetDelegateForFunctionPointer(
                codeBuffer, typeof(RdtscDelegate)) as RdtscDelegate;
        }

        public static void Close()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;

            Rdtsc = null;
            NativeMethods.VirtualFree(codeBuffer, UIntPtr.Zero, FreeType.RELEASE);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate ulong RdtscDelegate();

        public static RdtscDelegate Rdtsc;

        // unsigned __int64 __stdcall rdtsc() {
        //   return __rdtsc();
        // }

        private static readonly byte[] RDTSC_32 = new byte[] {
      0x0F, 0x31,                     // rdtsc   
      0xC3                            // ret  
    };

        private static readonly byte[] RDTSC_64 = new byte[] {
      0x0F, 0x31,                     // rdtsc  
      0x48, 0xC1, 0xE2, 0x20,         // shl rdx, 20h  
      0x48, 0x0B, 0xC2,               // or rax, rdx  
      0xC3                            // ret  
    };

        [Flags()]
        public enum AllocationType : uint
        {
            COMMIT = 0x1000,
            RESERVE = 0x2000,
            RESET = 0x80000,
            LARGE_PAGES = 0x20000000,
            PHYSICAL = 0x400000,
            TOP_DOWN = 0x100000,
            WRITE_WATCH = 0x200000
        }

        [Flags()]
        public enum MemoryProtection : uint
        {
            EXECUTE = 0x10,
            EXECUTE_READ = 0x20,
            EXECUTE_READWRITE = 0x40,
            EXECUTE_WRITECOPY = 0x80,
            NOACCESS = 0x01,
            READONLY = 0x02,
            READWRITE = 0x04,
            WRITECOPY = 0x08,
            GUARD = 0x100,
            NOCACHE = 0x200,
            WRITECOMBINE = 0x400
        }

        [Flags]
        public enum FreeType
        {
            DECOMMIT = 0x4000,
            RELEASE = 0x8000
        }

        private static class NativeMethods
        {
            private const string KERNEL = "kernel32.dll";

            [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi)]
            public static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize,
                AllocationType flAllocationType, MemoryProtection flProtect);

            [DllImport(KERNEL, CallingConvention = CallingConvention.Winapi)]
            public static extern bool VirtualFree(IntPtr lpAddress, UIntPtr dwSize,
                FreeType dwFreeType);
        }
    }
}

