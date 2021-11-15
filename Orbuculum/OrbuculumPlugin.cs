using NINA.Plugin;
using NINA.Plugin.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbuculum {
    [Export(typeof(IPluginManifest))]
    public class OrbuculumPlugin : PluginBase {
    }
}
