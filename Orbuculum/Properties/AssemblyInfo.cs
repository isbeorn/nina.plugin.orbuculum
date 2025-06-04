using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Orbuculum")]
[assembly: AssemblyDescription("A plugin that will provide sequencer instructions that are able to look for future targets and react accordingly.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Stefan Berg @isbeorn")]
[assembly: AssemblyProduct("NINA.Plugins")]
[assembly: AssemblyCopyright("Copyright ©  2021-2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: InternalsVisibleTo("NINA.Plugins.Test")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a6c614f3-c3ab-423a-8be1-d480a253f07c")]

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
[assembly: AssemblyVersion("2.0.0.2")]
[assembly: AssemblyFileVersion("2.0.0.2")]

//The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.1031")]

//Your plugin homepage - omit if not applicaple
[assembly: AssemblyMetadata("Homepage", "https://www.patreon.com/stefanberg/")]
//The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
//The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
//The repository where your pluggin is hosted
[assembly: AssemblyMetadata("Repository", "https://github.com/isbeorn/nina.plugin.orbuculum")]

[assembly: AssemblyMetadata("ChangelogURL", "https://github.com/isbeorn/nina.plugin.orbuculum/blob/master/Orbuculum/Changelog.md")]

//Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "Scrying,Divination,Crystallomancy" )]

//The featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "https://github.com/isbeorn/nina.plugin.orbuculum/blob/master/Orbuculum/CrystalBall.jpg?raw=true")]
//An example screenshot of your plugin in action
[assembly: AssemblyMetadata("ScreenshotURL", "")]
//An additional example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "")]
[assembly: AssemblyMetadata("LongDescription", @"This plugin is intended to provide some basic tools to improve multi target planning by scrying future targets and predicting their state to react on these.  

For example - you know that a target is obstructed until it reaches a certain altitude, so you want to loop another target for as long as that target is below that specific altitude.

## Loop Conditions
+ Loop While Hour Angle
    + Loop the current target until a certain hour angle is reached
+ Loop While Next Target Hour Angle
    + Loop the current target until a certain hour angle for the next target is reached
+ Loop While Next Target Below Altitude
    + Loop the current target until the next target rises above a specified altitude
+ Loop While Next Target Below Horizon
    + Loop the current target until the next target rises above the horizon

## Instructions
+ Auto Balancing Exposure
    + An imaging instruction that will automatically balance the exposures by the given exposure definitions
    + This is useful if you want to have a specific ratio between filters for example
    + To add a new exposure definition hit the '+' button above the table
    + Put the instruction inside a loop and it will automatically pick a fitting exposure definition each time it is executed
    + The exposure is picked by simply finding the minimum detail by (Progress / Ratio)
    + A ratio of 0 will deactivate the row")]
