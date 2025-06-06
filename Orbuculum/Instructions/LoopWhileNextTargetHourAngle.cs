﻿using Newtonsoft.Json;
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
    [ExportMetadata("Name", "🕑 Loop While Next Target Hour Angle")]
    [ExportMetadata("Description", "Searches the sequencer for the next target and loops the current set for as long as the hour angle of next target is below or above set amount of hours.")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Orbuculum")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class LoopWhileNextTargetHourAngle : SequenceCondition, IValidatable {
        private IList<string> issues = new List<string>();
        private string nextTargetName;
        private double nextTargetHourAngle;
        private double nextTargetCurrentHourAngle;
        private string expectedTimeStr = "";
        private IProfileService profileService;
        private ComparisonOperatorEnum comparator;

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        [ImportingConstructor]
        public LoopWhileNextTargetHourAngle(IProfileService profileService) {
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
          .Where(p => p == ComparisonOperatorEnum.GREATER_THAN || p== ComparisonOperatorEnum.LESS_THAN)
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
        public double NextTargetHourAngle {
            get => nextTargetHourAngle;
            set {
                nextTargetHourAngle = value;
                RaisePropertyChanged();
                CalculateExpectedTime();
            }
        }
        public double NextTargetCurrentHourAngle {
            get => nextTargetCurrentHourAngle;
            set {
                nextTargetCurrentHourAngle = value;
                RaisePropertyChanged();
                CalculateExpectedTime();
            }
        }

        public string NextTargetName { get => nextTargetName; set { nextTargetName = value; RaisePropertyChanged(); } }
        public string ExpectedTimeStr { get => expectedTimeStr; set { expectedTimeStr = value; RaisePropertyChanged(); } }
        private void CalculateExpectedTime()
        {
            var now = DateTime.Now;
            double SIDEREAL_HRS_PER_HOUR = AstroUtil.SIDEREAL_RATE_ARCSECONDS_PER_SECOND /* 15.041 */ * 24.0 / 360.0;
            var siderealHoursDiff = (NextTargetHourAngle - NextTargetCurrentHourAngle) % 24;
            if (!double.IsNaN(siderealHoursDiff))
            {
                var approximateDateTime = now.AddHours(siderealHoursDiff / SIDEREAL_HRS_PER_HOUR);
                ExpectedTimeStr = approximateDateTime.ToString("HH:mm");
            }
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            RunWatchdogIfInsideSequenceRoot();
        }

        public override void AfterParentChanged() {
            RunWatchdogIfInsideSequenceRoot();
        }

        private LoopWhileNextTargetHourAngle(LoopWhileNextTargetHourAngle copyMe) : this(copyMe.profileService) {
            CopyMetaData(copyMe);
            Comparator = copyMe.Comparator;
            NextTargetHourAngle = copyMe.NextTargetHourAngle;
        }

        public override object Clone() {
            return new LoopWhileNextTargetHourAngle(this);
        }

        public bool Validate() {
            var i = new List<string>();
            if (this.Parent == null) {
                i.Add("🚫 The condition has to be inside an instruction set.");
            } else {
                if (Parent.Status == SequenceEntityStatus.CREATED || Parent.Status == SequenceEntityStatus.RUNNING) {
                    var nextTarget = Scry.NextTarget(this.Parent);
                    if (nextTarget == null) {
                        NextTargetCurrentHourAngle = double.NaN;
                        NextTargetName = string.Empty;
                        i.Add("🚫 Scrying failed. No future target found");
                    } else {
                        NextTargetCurrentHourAngle = CalculateHourAngle(nextTarget);
                        if (nextTarget.Target.TargetName != NextTargetName) {
                            NextTargetName = nextTarget.Target.TargetName;
                        }
                    }
                }
            }


            Issues = i;
            return i.Count == 0;
        }
        private double CalculateHourAngle(IDeepSkyObjectContainer target) {

            var siderealTime = Angle.ByHours(AstroUtil.GetLocalSiderealTimeNow(profileService.ActiveProfile.AstrometrySettings.Longitude));

            var hourAngle = AstroUtil.GetHourAngle(siderealTime, Angle.ByHours(target.Target.InputCoordinates.Coordinates.RA));

            var hours = hourAngle.Hours > 12 ? hourAngle.Hours - 24: hourAngle.Hours;
            return Math.Round(hours, 2);
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            switch(Comparator) {
                case ComparisonOperatorEnum.LESS_THAN:
                    return NextTargetCurrentHourAngle < NextTargetHourAngle;
                case ComparisonOperatorEnum.GREATER_THAN:
                    return NextTargetCurrentHourAngle > NextTargetHourAngle;
                default:
                    return NextTargetCurrentHourAngle < NextTargetHourAngle;
            }
            
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(LoopWhileNextTargetHourAngle)}, Comparator {Comparator}, Next Target Hour Angle {NextTargetHourAngle}, Next Target Current Hour Angle {NextTargetCurrentHourAngle}";
        }
    }
}
