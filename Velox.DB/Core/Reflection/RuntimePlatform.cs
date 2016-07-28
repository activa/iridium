using System;
using System.Linq;
using System.Reflection;

#if VELOX_DB
namespace Velox.DB.Core
#else
namespace Velox.Core
#endif
{
    public class RuntimePlatform
    {
        public enum OS
        {
            Unknown = -1,
            iOS = 0, Android, Win32, WinPhone, UWP
        }

        public enum Arch
        {
            Unknown = -1, x86 = 0, x64, ARM
        }

        public static OS OperatingSystem;
        public static Version Version;
        public static Arch Architecture;
        public static Version DotNetVersion;

        static RuntimePlatform()
        {
            OperatingSystem = OS.Unknown;
            Version = new Version(0,0,0,0);
            Architecture = Arch.Unknown;
            DotNetVersion = new Version(4,0,0,0);

            try
            {
                DotNetVersion = (Version) typeof(Environment).GetRuntimeProperty("Version").GetValue(null);
            }
            catch
            {
                DotNetVersion = new Version(0,0,0,0);
            }
            

            var analyticsInfoType = Type.GetType("Windows.System.Profile.AnalyticsInfo, Windows, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime");

            if (analyticsInfoType != null)
            {
                OperatingSystem = OS.UWP;
                GetWin10Props(analyticsInfoType);
                return;
            }

            var osVersionProp = typeof(Environment).GetRuntimeProperty("OSVersion");

            if (osVersionProp != null)
            {
                var platform = osVersionProp.PropertyType.GetRuntimeProperty("Platform").GetValue(osVersionProp.GetValue(null));

                if (platform.ToString() == "Win32NT")
                {
                    OperatingSystem = OS.Win32;

                    var moduleProperty = typeof(Type).Inspector().GetProperty("Module");

                    var getPeKindMethod =
                        moduleProperty?.PropertyType.Inspector().GetMember("GetPEKind").FirstOrDefault() as MethodInfo;

                    if (getPeKindMethod != null)
                    {
                        object[] parameters = { null, null };

                        getPeKindMethod.Invoke(moduleProperty.GetValue(typeof(object)), parameters);

                        switch (parameters[1].ToString())
                        {
                            case "I386":
                                Architecture = Arch.x86;
                                break;
                            case "AMD64":
                                Architecture = Arch.x64;
                                break;
                            case "ARM":
                                Architecture = Arch.ARM;
                                break;
                        }
                    }

                    return;
                }
            }

            var uiDeviceType =Type.GetType("UIKit.UIDevice, Xamarin.iOS, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

            if (uiDeviceType != null)
            {
                OperatingSystem = OS.iOS;

                var uiDevice = uiDeviceType.GetRuntimeProperty("CurrentDevice").GetValue(null);
                var systemVersionString = (string) uiDevice.GetType().GetRuntimeProperty("SystemVersion").GetValue(uiDevice);

                Version = new Version(systemVersionString);

                return;
            }

            var androidBuildType = Type.GetType("Android.OS.Build, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

            if (androidBuildType != null)
            {
                OperatingSystem = OS.Android;

                var buildVersion = androidBuildType.GetRuntimeProperty("VERSION").GetValue(null);
                var versionString = (string) buildVersion.GetType().GetRuntimeProperty("Release").GetValue(buildVersion);

                Version = new Version(versionString);

                return;
            }
        }

        private static void GetWin10Props(Type analyticsInfoType)
        {
            var versionProp = analyticsInfoType.GetRuntimeProperty("VersionInfo");
            var versionInfo = versionProp.GetValue(null);
            var deviceFamilyVersion = (string)versionInfo.GetType().GetRuntimeProperty("DeviceFamilyVersion").GetValue(versionInfo);

            ulong version = ulong.Parse(deviceFamilyVersion);
            int major = (int)((version & 0xFFFF000000000000L) >> 48);
            int minor = (int)((version & 0x0000FFFF00000000L) >> 32);
            int build = (int)((version & 0x00000000FFFF0000L) >> 16);
            int revision = (int)(version & 0x000000000000FFFFL);

            Version = new Version(major, minor, build, revision);

            var packageType = Type.GetType("Windows.ApplicationModel.Package, Windows, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime");
            var currentPackage = packageType.GetRuntimeProperty("Current").GetValue(null);
            var packageId = currentPackage.GetType().GetRuntimeProperty("Id").GetValue(currentPackage);

            switch (packageId.GetType().GetRuntimeProperty("Architecture").GetValue(packageId).ToString().ToUpper())
            {
                case "X86": Architecture = Arch.x86; break;
                case "X64": Architecture = Arch.x64; break;
                case "ARM": Architecture = Arch.ARM; break;
            }
        }
    }
}