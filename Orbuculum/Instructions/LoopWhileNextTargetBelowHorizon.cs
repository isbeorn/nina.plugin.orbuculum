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
    [ExportMetadata("Name", "🔮 Loop While Next Target Is Below Horizon ")]
    [ExportMetadata("Description", "Searches the sequencer for the next target and loops the current set for as long as the altitude of next target is below the horizon.")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Orbuculum")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class LoopWhileNextTargetBelowHorizon : LoopForAltitudeBase, IValidatable {
        private string nextTargetName;
        private IProfileService profileService;


        [ImportingConstructor]
        public LoopWhileNextTargetBelowHorizon(IProfileService profileService) : base(profileService, true) {
            this.profileService = profileService;
            Data.Offset = 0;
            Data.Comparator = ComparisonOperatorEnum.LESS_THAN;

        }

        public string NextTargetName { get => nextTargetName; set { nextTargetName = value; RaisePropertyChanged(); } }
                

        private LoopWhileNextTargetBelowHorizon(LoopWhileNextTargetBelowHorizon copyMe) : this(copyMe.profileService) { 
            CopyMetaData(copyMe);
        }

        public override object Clone() {
            return new LoopWhileNextTargetBelowHorizon(this) {
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
                }
            }

            Issues = i;
            return i.Count == 0;
        }


        public override void CalculateExpectedTime() {
            if (Data.Coordinates == null) return;

            Data.CurrentAltitude = GetCurrentAltitude(DateTime.Now, Data.Observer);
            ItemUtility.CalculateExpectedTimeCommon(Data, offset: 0, until: false, 90, GetCurrentAltitude);
        }

        public double GetCurrentAltitude(DateTime time, ObserverInfo observer) {
            if (Data.Coordinates == null) return double.NaN;

            TopocentricCoordinates altaz = Data.Coordinates.Coordinates.Transform(Angle.ByDegree(observer.Latitude), Angle.ByDegree(observer.Longitude), time);
            return altaz.Altitude.Degree;
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            CalculateExpectedTime();
            return Data.CurrentAltitude < Data.TargetAltitude;
        }
        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(LoopWhileNextTargetBelowHorizon)}, Next Target Horizon {Data.TargetAltitude}, Next Target Current Altitude {Data.CurrentAltitude}";
        }



        [Obsolete]
        [JsonIgnore]
        public double AltitudeOffset { get; set; }

        [JsonProperty(propertyName: "AltitudeOffset")]
        [Obsolete]
        private double DeprecatedAltitudeOffset { set { Data.Offset = value; } }
    }
}
