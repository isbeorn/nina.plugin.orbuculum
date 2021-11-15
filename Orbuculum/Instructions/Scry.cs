using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbuculum.Instructions {
    internal class Scry {
        public static IDeepSkyObjectContainer NextTarget(ISequenceContainer container) {
            if (container != null && container.Parent != null) {

                var snapshot = (IList<ISequenceItem>)container.Parent.GetItemsSnapshot();
                var idx = snapshot.IndexOf(container) + 1;

                // Check for next target on same level as current target
                if (idx < snapshot.Count) {
                    for (var i = idx; i < snapshot.Count; i++) {
                        var item = snapshot[i];
                        if (item is IDeepSkyObjectContainer target) {
                            return target;
                        } else if (item is ISequenceContainer subcontainer) {
                            var downTarget = LookForTargetDownwards(subcontainer);
                            if(downTarget != null) {
                                return downTarget;
                            }
                        }
                    }
                }

                // Look up in the hierarchy
                if (container.Parent != null) {
                    return NextTarget(container.Parent);
                }
            }
            return null;
        }

        private static IDeepSkyObjectContainer LookForTargetDownwards(ISequenceContainer container) {
            var children = (IList<ISequenceItem>)container.GetItemsSnapshot();
            if(children != null) { 
                foreach (var child in children) {
                    if (child is IDeepSkyObjectContainer skyObjectContainer2) {
                        return skyObjectContainer2;
                    } else if (child is ISequenceContainer childContainer) {
                        var check = LookForTargetDownwards(childContainer);
                        if (check != null) {
                            return check;
                        }
                    }
                }
            }
            return null;
        }
    }
}
