using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Utility;
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
    public class LoopWhileNextTargetBelowAltitude : LoopForAltitudeBase, IValidatable {
        private string nextTargetName;
        private IProfileService profileService;
        

        [ImportingConstructor]
        public LoopWhileNextTargetBelowAltitude(IProfileService profileService): base(profileService, false) {
            this.profileService = profileService;
            Data.Comparator = ComparisonOperatorEnum.GREATER_THAN;

        }

        public string NextTargetName { get => nextTargetName; set { nextTargetName = value; RaisePropertyChanged(); } }

        private LoopWhileNextTargetBelowAltitude(LoopWhileNextTargetBelowAltitude copyMe) : this(copyMe.profileService) { 
            CopyMetaData(copyMe);
        }

        public override object Clone() {
            return new LoopWhileNextTargetBelowAltitude(this) {
                Data = Data.Clone()
            };
        }
        public override void AfterParentChanged() {
            RunWatchdogIfInsideSequenceRoot();
            Validate();
        }

        public bool Validate() {
            var i = new List<string>(); Issues = i;
            if (this.Parent == null) {
                i.Add("🚫 The condition has to be inside an instruction set.");
            } else {
                var nextTarget = Scry.NextTarget(this.Parent);
                if(nextTarget == null) {
                    Data.SetCoordinates(null);
                    Data.CurrentAltitude = double.NaN;
                    NextTargetName = string.Empty;
                    i.Add("🚫 Scrying failed. No future target found");
                } else {
                    Data.SetCoordinates(nextTarget.Target.InputCoordinates);
                    NextTargetName = nextTarget.Target.TargetName;

                    if (Data.TargetAltitude > nextTarget.Target.DeepSkyObject.MaxAltitude.Y) {
                        i.Add($"🚫 The next target will never reach the chosen altitude. Its max altitude is predicted to be {nextTarget.Target.DeepSkyObject.MaxAltitude.Y:#.##}°!");
                    }
                }
            }


            Issues = i;
            return i.Count == 0;
        }

        public double GetCurrentAltitude(DateTime time, ObserverInfo observer) {
            observer.Longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
            observer.Latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
            
            var altaz = Data.Coordinates
                .Coordinates
                .Transform(
                    Angle.ByDegree(observer.Latitude),
                    Angle.ByDegree(observer.Longitude),
                    time);

            return Math.Round(altaz.Altitude.Degree, 2);
        }

        public override void CalculateExpectedTime() {
            Data.CurrentAltitude = GetCurrentAltitude(DateTime.Now, Data.Observer);
            ItemUtility.CalculateExpectedTimeCommon(Data, 0, until: true, 30, GetCurrentAltitude);
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            CalculateExpectedTime();
            return Data.CurrentAltitude < Data.TargetAltitude;
        }


        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(LoopWhileNextTargetBelowAltitude)}, Next Target Altitude {Data.TargetAltitude}, Next Target Current Altitude {Data.CurrentAltitude}";
        }



        [Obsolete]
        [JsonIgnore]
        public double NextTargetAltitude { get; set; }

        [JsonProperty(propertyName: "NextTargetAltitude")]
        [Obsolete]
        private double DeprecatedNextTargetAltitude { set { Data.Offset = value; } }
    }
}
