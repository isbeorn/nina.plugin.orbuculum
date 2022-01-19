using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Orbuculum.Instructions {

    [ExportMetadata("Name", "🧙 Auto Balancing Exposure")]
    [ExportMetadata("Description", "A sequence item that will pick one of the specified exposure definitions based on the progress and ratio each time it is executed.")]
    [ExportMetadata("Icon", "CameraSVG")]
    [ExportMetadata("Category", "Orbuculum")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class AutoBalancingExposure : SequentialContainer, IImmutableContainer, IValidatable {
        private IProfileService profileService;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IImageHistoryVM imageHistoryVM;
        private IFilterWheelMediator filterWheelMediator;
        private AsyncObservableCollection<ExposureItem> exposureItems;

        [ImportingConstructor]
        public AutoBalancingExposure(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                IImagingMediator imagingMediator,
                IImageSaveMediator imageSaveMediator,
                IImageHistoryVM imageHistoryVM,
                IFilterWheelMediator filterWheelMediator) {
            this.Items = new List<ISequenceItem>();
            IsExpanded = false;
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.filterWheelMediator = filterWheelMediator;
            CameraInfo = this.cameraMediator.GetInfo();
            ExposureItems = new AsyncObservableCollection<ExposureItem>();
            AddRowCommand = new GalaSoft.MvvmLight.Command.RelayCommand(AddRow);
            DeleteRowCommand = new GalaSoft.MvvmLight.Command.RelayCommand<ExposureItem>(DeleteRow);
        }

        private void DeleteRow(ExposureItem obj) {
            ExposureItems.Remove(obj);
        }

        private void AddRow() {
            ExposureItems.Add(new ExposureItem());
        }

        private AutoBalancingExposure(AutoBalancingExposure copyMe) : this(copyMe.profileService,
                                                                             copyMe.cameraMediator,
                                                                             copyMe.imagingMediator,
                                                                             copyMe.imageSaveMediator,
                                                                             copyMe.imageHistoryVM,
                                                                             copyMe.filterWheelMediator) {
            CopyMetaData(copyMe);
        }

        public override object Clone() {
            var balanced = new AutoBalancingExposure(this) {
            };

            foreach (var entry in this.ExposureItems) {
                balanced.ExposureItems.Add(entry.Clone());
            }
            if (balanced.ExposureItems.Count == 0) {
            }
            return balanced;
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get => cameraInfo;
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        private ExposureItem currentExposureItem;

        public ExposureItem CurrentExposureItem {
            get => currentExposureItem;
            set {
                currentExposureItem = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            try {
                this.Items.Clear();

                ExposureItem item = GetNextExposureItem();
                CurrentExposureItem = item;

                var switchFilter = new SwitchFilter(profileService, filterWheelMediator);
                switchFilter.Name = nameof(SwitchFilter);
                switchFilter.Category = "Orbuculum";
                switchFilter.Description = "";
                switchFilter.ErrorBehavior = this.ErrorBehavior;
                switchFilter.Attempts = this.Attempts;
                switchFilter.Filter = item.Filter;
                switchFilter.AttachNewParent(this);

                var exposure = new TakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM);
                exposure.ErrorBehavior = this.ErrorBehavior;
                exposure.Attempts = this.Attempts;
                // Fill in exposure details
                exposure.Name = nameof(TakeExposure);
                exposure.Category = "Orbuculum";
                exposure.Description = "";
                exposure.ImageType = CaptureSequence.ImageTypes.LIGHT;
                exposure.ExposureCount = item.Progress;
                exposure.ExposureTime = item.ExposureTime;
                exposure.Binning = item.Binning;
                exposure.Gain = item.Gain;
                exposure.Offset = item.Offset;
                exposure.AttachNewParent(this);

                this.Items.Add(switchFilter);
                this.Items.Add(exposure);
                await base.Execute(progress, token);

                // Increment progress after successful capture
                if (exposure.Status == NINA.Core.Enum.SequenceEntityStatus.FINISHED) {
                    item.Progress = exposure.ExposureCount;
                }
                CurrentExposureItem = null;
            } finally {
                foreach (var seq in Items) {
                    seq.AttachNewParent(null);
                }
                this.Items.Clear();
            }
        }

        private ExposureItem GetNextExposureItem() {
            return ExposureItems.Where(x => x.Ratio > 0).MinBy(x => (double)x.Progress / x.Ratio);
        }

        [JsonProperty]
        public AsyncObservableCollection<ExposureItem> ExposureItems {
            get => exposureItems;
            set {
                exposureItems = value;
                RaisePropertyChanged();
            }
        }

        public override bool Validate() {
            var i = new ObservableCollection<string>();
            CameraInfo = this.cameraMediator.GetInfo();

            if (ExposureItems.Where(x => x.Ratio > 0).Count() == 0) {
                i.Add("At least one exposure definition has to be added with a ratio greater than 0");
            } else {
                var nextItem = GetNextExposureItem();

                if (!CameraInfo.Connected) {
                    i.Add(Loc.Instance["LblCameraNotConnected"]);
                } else {
                    if (CameraInfo.CanSetGain && nextItem.Gain > -1 && (nextItem.Gain < CameraInfo.GainMin || nextItem.Gain > CameraInfo.GainMax)) {
                        i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Gain"], CameraInfo.GainMin, CameraInfo.GainMax, nextItem.Gain));
                    }
                    if (CameraInfo.CanSetOffset && nextItem.Offset > -1 && (nextItem.Offset < CameraInfo.OffsetMin || nextItem.Offset > CameraInfo.OffsetMax)) {
                        i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Offset"], CameraInfo.OffsetMin, CameraInfo.OffsetMax, nextItem.Offset));
                    }
                }

                if (nextItem.Filter != null && !filterWheelMediator.GetInfo().Connected) {
                    i.Add(Loc.Instance["LblFilterWheelNotConnected"]);
                }
            }

            var fileSettings = profileService.ActiveProfile.ImageFileSettings;

            if (string.IsNullOrWhiteSpace(fileSettings.FilePath)) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_FilePathEmpty"]);
            } else if (!Directory.Exists(fileSettings.FilePath)) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_FilePathInvalid"]);
            }

            Issues = i;
            RaisePropertyChanged(nameof(Issues));
            return i.Count == 0;
        }

        public override string ToString() {
            var nextItem = GetNextExposureItem();
            return $"Category: {Category}, Item: {nameof(AutoBalancingExposure)}, Next Item: Progress {nextItem.Progress}, Ratio {nextItem.Ratio}, ExposureTime {nextItem.ExposureTime}, Gain {nextItem.Gain}, Offset {nextItem.Offset}, Binning {nextItem.Binning?.Name}";
        }

        public ICommand AddRowCommand { get; }
        public ICommand DeleteRowCommand { get; }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ExposureItem : BaseINPC {
        private FilterInfo filter;
        private double exposureTime;
        private int gain;
        private int offset;
        private BinningMode binning;
        private int ratio;
        private int progress;

        public ExposureItem() {
            Gain = -1;
            Offset = -1;
            Binning = new BinningMode(1, 1);
            Ratio = 1;
        }

        [JsonProperty]
        public FilterInfo Filter {
            get => filter;
            set {
                filter = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public BinningMode Binning {
            get => binning;
            set {
                binning = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public double ExposureTime {
            get => exposureTime;
            set {
                exposureTime = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int Gain {
            get => gain;
            set {
                gain = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int Offset {
            get => offset;
            set {
                offset = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int Ratio {
            get => ratio;
            set {
                ratio = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int Progress {
            get => progress;
            set {
                progress = value;
                RaisePropertyChanged();
            }
        }

        public ExposureItem Clone() {
            return new ExposureItem() {
                Binning = Binning,
                ExposureTime = ExposureTime,
                Filter = Filter,
                Gain = Gain,
                Offset = Offset,
                Progress = Progress,
                Ratio = Ratio
            };
        }

        private ObservableCollection<string> _imageTypes;

        public ObservableCollection<string> ImageTypes {
            get {
                if (_imageTypes == null) {
                    _imageTypes = new ObservableCollection<string>();

                    Type type = typeof(CaptureSequence.ImageTypes);
                    foreach (var p in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
                        var v = p.GetValue(null);
                        _imageTypes.Add(v.ToString());
                    }
                }
                return _imageTypes;
            }
            set {
                _imageTypes = value;
                RaisePropertyChanged();
            }
        }
    }
}