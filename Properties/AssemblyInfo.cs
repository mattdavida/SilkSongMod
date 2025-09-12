using System.Reflection;
using System.Runtime.InteropServices;
#if MELONLOADER
using MelonLoader;
#endif

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SilkSong")]
#if MELONLOADER
[assembly: AssemblyDescription("A MelonLoader mod for Hollow Knight: Silksong")]
#elif BEPINEX
[assembly: AssemblyDescription("A BepInEx mod for Hollow Knight: Silksong")]
#endif
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SilkSongMod")]
[assembly: AssemblyCopyright("Copyright ©  2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("12345678-1234-1234-1234-123456789012")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

#if MELONLOADER
// MelonLoader attributes
[assembly: MelonInfo(typeof(SilkSong.SilkSongMod), "SilkSongMod", "1.0.0", "YourName")]
[assembly: MelonGame("Team Cherry", "Hollow Knight Silksong")]
#endif
