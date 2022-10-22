using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orbuculum.Instructions {
    [ExportMetadata("Name", "🕑 Wait for Hour Angle")]
    [ExportMetadata("Description", "Searches the sequencer for the current target and waits for as long as the hour angle of current target is below or above set amount of hours.")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Orbuculum")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitForHourAngle : SequenceItem, IValidatable {
        private IList<string> issues = new List<string>();
        private double hourAngle;
        private double currentHourAngle;
        private string expectedTimeStr = "";
        private IProfileService profileService;
        private ComparisonOperatorEnum comparator;

        [ImportingConstructor]
        public WaitForHourAngle(IProfileService profileService) {
            Comparator = ComparisonOperatorEnum.GREATER_THAN;
            this.profileService = profileService;

        }

        private WaitForHourAngle(WaitForHourAngle cloneMe) : this(cloneMe.profileService) {
            CopyMetaData(cloneMe);
            Comparator = cloneMe.Comparator;
            HourAngle = cloneMe.HourAngle;
        }

        public override object Clone() {
            return new WaitForHourAngle(this) {
            };
        }
        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

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
        public double HourAngle {
            get => hourAngle;
            set {
                hourAngle = value;
                RaisePropertyChanged();
                CalculateExpectedTime();
            }
        }
        public double CurrentHourAngle {
            get => currentHourAngle;
            set {
                currentHourAngle = value;
                RaisePropertyChanged();
                CalculateExpectedTime();
            }
        }
        public string ExpectedTimeStr { get => expectedTimeStr; set { expectedTimeStr = value; RaisePropertyChanged(); } }

        public void CalculateExpectedTime() {
            var now = DateTime.Now;
            double SIDEREAL_HRS_PER_HOUR = AstroUtil.SIDEREAL_RATE_ARCSECONDS_PER_SECOND /* 15.041 */ * 24.0 / 360.0;
            var siderealHoursDiff = (HourAngle - CurrentHourAngle) % 24;
            if (!double.IsNaN(siderealHoursDiff))
            {
                var approximateDateTime = now.AddHours(siderealHoursDiff / SIDEREAL_HRS_PER_HOUR);
                ExpectedTimeStr = approximateDateTime.ToString("HH:mm");
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var context = ItemUtility.RetrieveContextCoordinates(this.Parent);
            if(context?.Coordinates == null) { throw new SequenceEntityFailedException("Could not retrieve target"); }

            var target = context.Coordinates;

            CurrentHourAngle = CalculateHourAngle(target);

            Func<double, double, bool> compare;
            if(Comparator == ComparisonOperatorEnum.GREATER_THAN) {
                compare = (currentHA, targetHA) => currentHA <= targetHA;
            } else {
                compare = (currentHA, targetHA) => currentHA > targetHA;
            }
            try {
                while (compare(CurrentHourAngle, HourAngle)) {
                    progress?.Report(new ApplicationStatus() {
                        Status = $"Waiting for Hour Angle {CurrentHourAngle}h / {HourAngle}h"
                    });
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                    CurrentHourAngle = CalculateHourAngle(target);
                }
            } finally {
                progress?.Report(new ApplicationStatus() { Status = "" });
            }
            
        }

        private double CalculateHourAngle(Coordinates target) {

            var siderealTime = Angle.ByHours(AstroUtil.GetLocalSiderealTimeNow(profileService.ActiveProfile.AstrometrySettings.Longitude));

            var hourAngle = AstroUtil.GetHourAngle(siderealTime, Angle.ByHours(target.RA));

            var hours = hourAngle.Hours > 12 ? hourAngle.Hours - 24 : hourAngle.Hours;
            return Math.Round(hours, 2);
        }
        public bool Validate() {
            var i = new List<string>(); 
            if (this.Parent == null) {
                i.Add("🚫 The intruction has to be inside an instruction set.");
            } else {
                var target = ItemUtility.RetrieveContextCoordinates(this.Parent);
                if (target?.Coordinates == null) {
                    CurrentHourAngle = double.NaN;

                    i.Add("🚫 Must be placed into a Deep Sky Object Instruction Set");
                } else {
                    if (this.Status != SequenceEntityStatus.RUNNING) {
                        CurrentHourAngle = CalculateHourAngle(target.Coordinates);
                    }
                }
            }

            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForHourAngle)}, Comparator {Comparator}, Target Hour Angle {HourAngle}, Current Hour Angle {CurrentHourAngle}";
        }
    }
}
