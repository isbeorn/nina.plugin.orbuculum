using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
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
using System.Threading.Tasks;

namespace Orbuculum.Instructions {
    [ExportMetadata("Name", "🕑 Loop While Hour Angle")]
    [ExportMetadata("Description", "Searches the sequencer for the current target and loops the current set for as long as the hour angle of current target is below or above set amount of hours.")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Orbuculum")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class LoopWhileHourAngle : SequenceCondition, IValidatable {
        private IList<string> issues = new List<string>();        
        private double hourAngle;
        private double currentHourAngle;
        private IProfileService profileService;
        private ComparisonOperatorEnum comparator;

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        [ImportingConstructor]
        public LoopWhileHourAngle(IProfileService profileService) {
            this.profileService = profileService;
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenTargetBelowAltitude, TimeSpan.FromSeconds(5));
            Comparator = ComparisonOperatorEnum.GREATER_THAN;

        }

        private async Task InterruptWhenTargetBelowAltitude() {
            if (!Check(null, null)) {
                if (this.Parent != null) {
                    if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING && this.Status != SequenceEntityStatus.DISABLED) {
                        Logger.Info("Next Target is above hour angle - Interrupting current Instruction Set");
                        await this.Parent.Interrupt();
                    }
                }
            }
        }
        public ComparisonOperatorEnum[] ComparisonOperators => Enum.GetValues(typeof(ComparisonOperatorEnum))
          .Cast<ComparisonOperatorEnum>()
          .Where(p => p == ComparisonOperatorEnum.GREATER_THAN || p == ComparisonOperatorEnum.LESS_THAN)
          .ToArray();


        [JsonProperty]
        public ComparisonOperatorEnum Comparator {
            get => comparator;
            set {
                comparator = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public double HourAngle { get => hourAngle; set { hourAngle = value; RaisePropertyChanged(); } }
        public double CurrentHourAngle { get => currentHourAngle; set { currentHourAngle = value; RaisePropertyChanged(); } }


        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            RunWatchdogIfInsideSequenceRoot();
        }

        public override void AfterParentChanged() {
            RunWatchdogIfInsideSequenceRoot();
        }

        private LoopWhileHourAngle(LoopWhileHourAngle copyMe) : this(copyMe.profileService) {
            CopyMetaData(copyMe);
            Comparator = copyMe.Comparator;
            HourAngle = copyMe.HourAngle;
        }

        public override object Clone() {
            return new LoopWhileHourAngle(this);
        }

        public bool Validate() {
            var i = new List<string>();
            if (this.Parent == null) {
                i.Add("🚫 The condition has to be inside an instruction set.");
            } else {
                var target = ItemUtility.RetrieveContextCoordinates(this.Parent);
                if (target?.Coordinates == null) {
                    CurrentHourAngle = double.NaN;

                    i.Add("🚫 Must be placed into a Deep Sky Object Instruction Set");
                } else {
                    CurrentHourAngle = CalculateHourAngle(target.Coordinates);
                }
            }


            Issues = i;
            return i.Count == 0;
        }
        private double CalculateHourAngle(Coordinates target) {

            var siderealTime = Angle.ByHours(AstroUtil.GetLocalSiderealTimeNow(profileService.ActiveProfile.AstrometrySettings.Longitude));

            var hourAngle = AstroUtil.GetHourAngle(siderealTime, Angle.ByHours(target.RA));

            var hours = hourAngle.Hours > 12 ? hourAngle.Hours - 24 : hourAngle.Hours;
            return Math.Round(hours, 2);
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            switch (Comparator) {
                case ComparisonOperatorEnum.LESS_THAN:
                    return CurrentHourAngle < HourAngle;
                case ComparisonOperatorEnum.GREATER_THAN:
                    return CurrentHourAngle > HourAngle;
                default:
                    return CurrentHourAngle < HourAngle;
            }

        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(LoopWhileHourAngle)}, Comparator {Comparator}, Target Hour Angle {HourAngle}, Current Hour Angle {CurrentHourAngle}";
        }
    }
}
