using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Iridium.DB.Sqlite
{
    internal class NativeLibraryLoader : INativeLibraryLoader
    {
        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);

        [DllImport("libc")]
        internal static extern int uname(IntPtr buf);

        [DllImport("__Internal")]
        internal static extern IntPtr mono_dl_open(string varname, int flags, IntPtr errMsg);

        [DllImport("__Internal")]
        internal static extern void mono_loader_register_module(string name, IntPtr module);

        public object LoadLibrary()
        {
            var platform = Environment.OSVersion.Platform;

            string resourceName = null;
            string resourcePrefix = null;

            if (platform == PlatformID.MacOSX || platform == PlatformID.Unix)
            {
                if (platform == PlatformID.MacOSX || IsMacOS())
                {
                    resourcePrefix = "macos";
                    resourceName = "libsqlite_emb.dylib";
                }
                else
                {
                    resourcePrefix = Environment.Is64BitProcess ? "linux64" : "linux32";
                    resourceName = "libsqlite_emb.so";
                }

                var soName = LoadSqliteLibraryFromResource(resourcePrefix, resourceName);

                var monoModule = mono_dl_open(soName, 9, IntPtr.Zero);

                if (monoModule == IntPtr.Zero)
                    throw new Exception("Unable to load " + soName);

                mono_loader_register_module("sqlite_emb", monoModule);

                return monoModule;
            }

            // Windows

            var architecture = Environment.Is64BitProcess ? "x64" : "x86";

            resourceName = "sqlite_emb.dll";
            resourcePrefix = "win32_" + architecture;

            var dllName = LoadSqliteLibraryFromResource(resourcePrefix, resourceName);

            var handle = LoadLibrary(dllName);

            if (handle == IntPtr.Zero)
            {
                throw new Exception("Unable to load " + dllName);
            }

            return handle;
        }

        public string LoadSqliteLibraryFromResource(string prefix, string name)
        {
            var dllName = Path.Combine(GetTemporaryWindowsDirectory(prefix), name);

            if (File.Exists(dllName))
            {
                return dllName;
            }

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Iridium.DB." + prefix + "." + name))
            {
                if (stream == null)
                    throw new Exception("Unable to load " + prefix + "." + name + " from assembly");

                using (var destinationStream = File.Create(dllName))
                {
                    stream.CopyTo(destinationStream);
                }
            }

            return dllName;
        }

        public string GetTemporaryWindowsDirectory(string prefix)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), "iridium-sqlite", prefix);

            Directory.CreateDirectory(tempDirectory);
            
            return tempDirectory;
        }

        public static bool IsMacOS()
        {
            try
            {
                IntPtr buf = IntPtr.Zero;

                try
                {
                    buf = Marshal.AllocHGlobal(8192);

                    return (uname(buf) == 0 && Marshal.PtrToStringAnsi(buf) == "Darwin");
                }
                finally
                {
                    if (buf != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(buf);
                    }
                }
            }
            catch(Exception ex)
            {
                return false;
            }
        }


    }
}
