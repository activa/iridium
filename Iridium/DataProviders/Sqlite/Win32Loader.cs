using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Iridium.DB.Core;

namespace Iridium.DB
{
    internal static class Win32Loader
    {
        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);

        public static void CheckAndLoadSqliteLibrary()
        {
            if (Platform.Properties.RuntimeEnvironment == Platform.RuntimeEnv.UWP)
            {
                //TODO: check for latest version of Win10
                //LoadLibrary("sqlite3.dll");
                return;
            }

            if (Platform.Properties.RuntimeEnvironment == Platform.RuntimeEnv.Win32)
            {
                var getExecutingAssemblyMethod = typeof(Assembly).GetRuntimeMethod("GetExecutingAssembly", new Type[0]);
                var locationProperty = typeof(Assembly).GetRuntimeProperty("CodeBase");

                var assemblyPath = new Uri((string)locationProperty.GetValue((Assembly)getExecutingAssemblyMethod.Invoke(null, new object[0])));

                var dllName = Path.Combine(Path.GetDirectoryName(assemblyPath.LocalPath), "win32-" + Platform.Properties.Architecture + "\\sqlite3.dll");

                var dll = LoadLibrary(dllName);

                if ((long)dll == 0)
                {
                    throw new Exception("Unable to load sqlite3 dll from " + dllName);
                }
            }
        }

    }
}
