using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Velox.DB.Core;

namespace Velox.DB.Sqlite.win32
{
    internal static class Win32Loader
    {
        [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);

        public static void CheckAndLoadSqliteLibrary()
        {
            bool isWindows = false;
            string architecture = null;

            var osVersionProp = typeof (System.Environment).GetRuntimeProperty("OSVersion");

            if (osVersionProp != null)
            {
                var platform = osVersionProp.PropertyType.GetRuntimeProperty("Platform").GetValue(osVersionProp.GetValue(null));
                //var osVersion = osVersionProp.GetValue(null);

                isWindows = platform.ToString() == "Win32NT";
            }

            if (!isWindows)
                return;

            var moduleProperty = typeof(Type).Inspector().GetProperty("Module");

            if (moduleProperty == null)
                return;

            var getPeKindMethod = moduleProperty.PropertyType.Inspector().GetMember("GetPEKind").FirstOrDefault() as MethodInfo;

            if (getPeKindMethod != null)
            {
                object[] parameters = new object[] {null,null};

                getPeKindMethod.Invoke(moduleProperty.GetValue(typeof (object)), parameters);

                switch (parameters[1].ToString())
                {
                    case "I386":
                        architecture = "x86";
                        break;
                    case "AMD64":
                        architecture = "x64";
                        break;
                    case "ARM":
                        architecture = "ARM";
                        break;
                }
            }

            if (architecture != "x86" && architecture != "x64")
                return;

            var getExecutingAssemblyMethod = typeof (Assembly).GetRuntimeMethod("GetExecutingAssembly", new Type[0]);
            var locationProperty = typeof (Assembly).GetRuntimeProperty("CodeBase");

            var assemblyPath = new Uri((string) locationProperty.GetValue((Assembly) getExecutingAssemblyMethod.Invoke(null, new object[0])));

            var dllName = Path.Combine(Path.GetDirectoryName(assemblyPath.LocalPath), "win32-" + architecture + "\\sqlite3.dll");

            var dll = LoadLibrary(dllName);

            if ((long)dll == 0)
            {
                throw new Exception("Unable to load sqlite3 dll from " + dllName);
            }
        }

    }
}
