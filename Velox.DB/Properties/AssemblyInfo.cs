using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: LinkerSafe]

[assembly: AssemblyTitle("VeloxDB")]
[assembly: AssemblyDescription("Lightweight multi-platform ORM")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("VeloxDB")]
[assembly: AssemblyCopyright("Copyright © 2015 Philippe Leybaert")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en")]

[assembly: AssemblyVersion("0.9.15.411")]
[assembly: AssemblyFileVersion("0.9.15.411")]

[assembly: InternalsVisibleTo("Velox.DB.TextExpressions")]

class LinkerSafeAttribute : System.Attribute
{
    public LinkerSafeAttribute() : base() { }
}