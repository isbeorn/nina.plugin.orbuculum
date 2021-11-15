using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orbuculum.Instructions {
    [ExportMetadata("Name", "🔮 Loop While Next Target Is Below Altitude ")]
    [ExportMetadata("Description", "Searches the sequencer for the next target and loops the current set for as long as the altitude of next target is below set amount of degrees.")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Orbuculum")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class LoopWhileNextTargetBelowAltitude : SequenceCondition, IValidatable {
        private IList<string> issues = new List<string>();
        private string nextTargetName;
        private double nextTargetAltitude;
        private double nextTargetCurrentAltitude;
        private IProfileService profileService;

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        [ImportingConstructor]
        public LoopWhileNextTargetBelowAltitude(IProfileService profileService) {
            this.profileService = profileService;
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenTargetBelowAltitude, TimeSpan.FromSeconds(5));

        }

        private async Task InterruptWhenTargetBelowAltitude() {
            if (!Check(null, null)) {
                if (this.Parent != null) {
                    if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING) {
                        Logger.Info("Next Target is above altitude - Interrupting current Instruction Set");
                        await this.Parent.Interrupt();
                    }
                }
            }
        }


        [JsonProperty]
        public double NextTargetAltitude { get => nextTargetAltitude; set { nextTargetAltitude = value; RaisePropertyChanged(); } }
        public double NextTargetCurrentAltitude { get => nextTargetCurrentAltitude; set { nextTargetCurrentAltitude = value; RaisePropertyChanged(); } }

        public string NextTargetName { get => nextTargetName; set { nextTargetName = value; RaisePropertyChanged(); } }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            RunWatchdogIfInsideSequenceRoot();
        }

        public override void AfterParentChanged() {
            RunWatchdogIfInsideSequenceRoot();
            Validate();
        }

        private LoopWhileNextTargetBelowAltitude(LoopWhileNextTargetBelowAltitude copyMe) : this(copyMe.profileService) { 
            CopyMetaData(copyMe); 
        }

        public override object Clone() {
            return new LoopWhileNextTargetBelowAltitude(this);
        }

        public bool Validate() {
            var i = new List<string>(); Issues = i;
            if (this.Parent == null) {
                i.Add("🚫 The condition has to be inside an instruction set.");
            } else {
                var nextTarget = Scry.NextTarget(this.Parent);
                if(nextTarget == null) {
                    NextTargetCurrentAltitude = double.NaN;
                    NextTargetName = string.Empty;
                    i.Add("🚫 Scrying failed. No future target found");
                } else {
                    NextTargetCurrentAltitude = CalculateAltitude(nextTarget);
                    NextTargetName = nextTarget.Target.TargetName;

                    if (NextTargetAltitude > nextTarget.Target.DeepSkyObject.MaxAltitude.Y) {
                        i.Add($"🚫 The next target will never reach the chosen altitude. Its max altitude is predicted to be {nextTarget.Target.DeepSkyObject.MaxAltitude.Y:#.##}°!");
                    }
                }
            }


            Issues = i;
            return i.Count == 0;
        }
        private double CalculateAltitude(IDeepSkyObjectContainer target) {
            var location = profileService.ActiveProfile.AstrometrySettings;
            var altaz = target.Target.InputCoordinates
                .Coordinates
                .Transform(
                    Angle.ByDegree(location.Latitude),
                    Angle.ByDegree(location.Longitude));

            return Math.Round(altaz.Altitude.Degree, 2);            
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            return NextTargetCurrentAltitude < NextTargetAltitude;
        }


    }
}
