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
    [ExportMetadata("Name", "🔮 Loop While Next Target Is Below Horizon ")]
    [ExportMetadata("Description", "Searches the sequencer for the next target and loops the current set for as long as the altitude of next target is below the horizon.")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Orbuculum")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class LoopWhileNextTargetBelowHorizon : SequenceCondition, IValidatable {
        private IList<string> issues = new List<string>();
        private string nextTargetName;
        private double nextTargetAltitude;
        private double nextTargetCurrentAltitude;
        private IProfileService profileService;
        private double altitudeOffset;

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        [ImportingConstructor]
        public LoopWhileNextTargetBelowHorizon(IProfileService profileService) {
            altitudeOffset = 0;
            this.profileService = profileService;
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenTargetBelowAltitude, TimeSpan.FromSeconds(5));

        }

        [JsonProperty]
        public double AltitudeOffset {
            get => altitudeOffset;
            set {
                altitudeOffset = value;
                RaisePropertyChanged();
            }
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

        private LoopWhileNextTargetBelowHorizon(LoopWhileNextTargetBelowHorizon copyMe) : this(copyMe.profileService) { 
            CopyMetaData(copyMe);
            NextTargetAltitude = copyMe.NextTargetAltitude;
            AltitudeOffset = copyMe.AltitudeOffset;
        }

        public override object Clone() {
            return new LoopWhileNextTargetBelowHorizon(this);
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
                    var altaz = CalculateAltAz(nextTarget);
                    NextTargetCurrentAltitude = CalculateAltitude(altaz);
                    NextTargetAltitude = CalculateHorizonAltitude(altaz);
                    NextTargetName = nextTarget.Target.TargetName;                    
                }
            }

            Issues = i;
            return i.Count == 0;
        }
        private TopocentricCoordinates CalculateAltAz(IDeepSkyObjectContainer target) {
            var location = profileService.ActiveProfile.AstrometrySettings;
            var altaz = target.Target.InputCoordinates
                .Coordinates
                .Transform(
                    Angle.ByDegree(location.Latitude),
                    Angle.ByDegree(location.Longitude));

            return altaz;
        }
        private double CalculateAltitude(TopocentricCoordinates altaz) {
            return Math.Round(altaz.Altitude.Degree, 2);
        }

        private double CalculateHorizonAltitude(TopocentricCoordinates altaz) {
            var currentAz = Math.Round(altaz.Azimuth.Degree, 2);

            var horizon = profileService.ActiveProfile.AstrometrySettings.Horizon;
            var horizonAltitude = 0d;
            if (horizon != null) {
                horizonAltitude = horizon.GetAltitude(currentAz);
            }
            return Math.Round(Math.Max(-90, Math.Min(90, horizonAltitude + AltitudeOffset)), 2);
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            return NextTargetCurrentAltitude < NextTargetAltitude;
        }
        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(LoopWhileNextTargetBelowHorizon)}, Next Target Horizon {NextTargetCurrentAltitude}, Next Target Current Hour Angle {NextTargetAltitude}";
        }
    }
}
