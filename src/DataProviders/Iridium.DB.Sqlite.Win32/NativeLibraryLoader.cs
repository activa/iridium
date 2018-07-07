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

        public object LoadLibrary()
        {
            var dllName = Path.Combine(GetTemporaryDirectory(), "sqlite3.dll");

            var architecture = Environment.Is64BitProcess ? "x64":"x86";

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Iridium.DB.win32_" + architecture + ".sqlite3.dll"))
            {
                if (stream == null)
                    throw new Exception("Unable to load sqlite3.dll from assembly");

                using (var destinationStream = File.Create(dllName))
                {
                    stream.CopyTo(destinationStream);
                }
            }

            var handle = LoadLibrary(dllName);

            if ((long)handle == 0)
            {
                throw new Exception("Unable to load sqlite3 dll from " + dllName);
            }

            return null;
        }

        public string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
