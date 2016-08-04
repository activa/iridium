#region License
//=============================================================================
// Iridium-Core - Portable .NET Productivity Library 
//
// Copyright (c) 2008-2016 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.Linq;
using System.Reflection;

#if IRIDIUM_CORE_EMBEDDED
namespace Iridium.DB.CoreUtil
#else
namespace Iridium.Core
#endif
{
    public class Platform
    {
        private static PlatformProperties _properties;

        public static PlatformProperties Properties => _properties ?? (_properties = new PlatformProperties());

        public enum RuntimeEnv
        {
            Unknown = -1, iOS = 0, Android, Win32, WindowsRuntime, UWP
        }

        public enum Architecture
        {
            Unknown = -1, x86 = 0, x64, ARM
        }

        public class PlatformProperties
        {
            public RuntimeEnv RuntimeEnvironment;
            public Version Version;
            public Architecture Architecture;
            public Version DotNetVersion;

            public PlatformProperties()
            {
                RuntimeEnvironment = RuntimeEnv.Unknown;
                Version = new Version(0, 0, 0, 0);
                Architecture = Architecture.Unknown;
                DotNetVersion = new Version(4, 0, 0, 0);

                try
                {
                    DotNetVersion = (Version) typeof(Environment).GetRuntimeProperty("Version").GetValue(null);
                }
                catch
                {
                    DotNetVersion = new Version(0, 0, 0, 0);
                }


                Func<bool>[] detectionFuncs = {Detect_Win32, Detect_WindowsRuntime, Detect_UWP, Detect_iOS, Detect_Android};

                foreach (var func in detectionFuncs)
                {
                    try { if (func()) return; }
                    catch { }
                }
            }

            private bool Detect_Win32()
            {
                var osVersionProp = typeof(Environment).GetRuntimeProperty("OSVersion");

                if (osVersionProp != null)
                {
                    var platform = osVersionProp.PropertyType.GetRuntimeProperty("Platform").GetValue(osVersionProp.GetValue(null));

                    if (platform.ToString() == "Win32NT")
                    {
                        RuntimeEnvironment = RuntimeEnv.Win32;

                        var moduleProperty = typeof(Type).Inspector().GetProperty("Module");

                        var getPeKindMethod = moduleProperty?.PropertyType.Inspector().GetMember("GetPEKind").FirstOrDefault() as MethodInfo;

                        if (getPeKindMethod != null)
                        {
                            object[] parameters = {null, null};

                            getPeKindMethod.Invoke(moduleProperty.GetValue(typeof(object)), parameters);

                            switch (parameters[1].ToString())
                            {
                                case "I386":
                                    Architecture = Architecture.x86;
                                    break;
                                case "AMD64":
                                    Architecture = Architecture.x64;
                                    break;
                                case "ARM":
                                    Architecture = Architecture.ARM;
                                    break;
                            }
                        }

                        Version = (Version) osVersionProp.PropertyType.GetRuntimeProperty("Version").GetValue(osVersionProp.GetValue(null));

                        return true;
                    }
                }

                return false;
            }

            private bool Detect_WindowsRuntime()
            {
                var packageType = Type.GetType("Windows.ApplicationModel.Package, Windows, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime");

                if (packageType != null)
                {
                    RuntimeEnvironment = RuntimeEnv.WindowsRuntime;
                    Version = new Version(8,1,0,0);

                    var currentPackage = packageType.GetRuntimeProperty("Current").GetValue(null);
                    var packageId = currentPackage.GetType().GetRuntimeProperty("Id").GetValue(currentPackage);

                    switch (packageId.GetType().GetRuntimeProperty("Architecture").GetValue(packageId).ToString().ToUpper())
                    {
                        case "X86":
                            Architecture = Architecture.x86;
                            break;
                        case "X64":
                            Architecture = Architecture.x64;
                            break;
                        case "ARM":
                            Architecture = Architecture.ARM;
                            break;
                    }
                }

                return false; // continue to check for UWP
            }

            private bool Detect_UWP()
            {
                var analyticsInfoType = Type.GetType("Windows.System.Profile.AnalyticsInfo, Windows, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime");

                if (analyticsInfoType != null)
                {
                    RuntimeEnvironment = RuntimeEnv.UWP;

                    var versionProp = analyticsInfoType.GetRuntimeProperty("VersionInfo");
                    var versionInfo = versionProp.GetValue(null);
                    var deviceFamilyVersion = (string)versionInfo.GetType().GetRuntimeProperty("DeviceFamilyVersion").GetValue(versionInfo);

                    ulong version = ulong.Parse(deviceFamilyVersion);
                    int major = (int)((version & 0xFFFF000000000000L) >> 48);
                    int minor = (int)((version & 0x0000FFFF00000000L) >> 32);
                    int build = (int)((version & 0x00000000FFFF0000L) >> 16);
                    int revision = (int)(version & 0x000000000000FFFFL);

                    Version = new Version(major, minor, build, revision);


                    return true;
                }

                return false;
            }

            private bool Detect_Android()
            {
                var androidBuildVersionType = Type.GetType("Android.OS.Build+VERSION, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

                if (androidBuildVersionType != null)
                {
                    RuntimeEnvironment = RuntimeEnv.Android;

                    var versionString = (string) androidBuildVersionType.GetRuntimeProperty("Release").GetValue(null);

                    Version = new Version(versionString);

                    return true;
                }

                return false;
            }

            private bool Detect_iOS()
            {
                var uiDeviceType = Type.GetType("UIKit.UIDevice, Xamarin.iOS, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

                if (uiDeviceType != null)
                {
                    RuntimeEnvironment = RuntimeEnv.iOS;

                    var uiDevice = uiDeviceType.GetRuntimeProperty("CurrentDevice").GetValue(null);
                    var systemVersionString = (string) uiDevice.GetType().GetRuntimeProperty("SystemVersion").GetValue(uiDevice);

                    Version = new Version(systemVersionString);

                    return true;
                }

                return false;
            }
        }
    }
}