using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using FinApps2014.Wearable.App.Controls;
using FinApps2014.Wearable.App.Helpers;
using FinApps2014.Wearable.App.Services;
using System;

namespace FinApps2014.Wearable.App {

    [Activity(Label = "FinApps2014.Wearable.App", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, GestureDetector.IOnGestureListener {

        #region Members
        private StepServiceBinder binder;
        private bool isServiceRegistered;
        private string calorieString, distanceString, percentString, completedString;
        private ProgressView progressView;
        private FrameLayout topLayer;
        private bool canAnimate = true;
        private bool fullAnimation = true;
        private Handler handler;
        private bool firstRun = true;
        private ImageView highScore, warning;
        private GestureDetector gestureDetector;
        private StepServiceConnection serviceConnection;
        //private int testSteps = 1;
        private TextView stepCount, calorieCount, distance, percentage;
        private TranslateAnimation animation;
        private float height, lastY;
        #endregion

        #region Properties
        public bool IsBound { get; set; }

        public StepServiceBinder Binder {
            get { return this.binder; }
            set {
                this.binder = value;
                if (this.binder == null)
                    return;

                this.RaisePropertyChanged("StepsToday");

                if (this.isServiceRegistered) {
                    this.binder.StepService.PropertyChanged -= HandlePropertyChanged;
                }

                this.binder.StepService.PropertyChanged += HandlePropertyChanged;
                this.isServiceRegistered = true;
            }
        }
        #endregion

        #region Methods
        private void StartStepService() {
            try {
                var service = new Intent(this, typeof(StepService));
                var componentName = this.StartService(service);
            } catch {

            }

        }

        private void AnimateTopLayer(float percent, bool force = false) {
            if (!this.canAnimate)
                return;

            if (this.height <= 0) {
                this.height = (float)this.topLayer.MeasuredHeight;
                if (this.height <= 0)
                    return;
            }

            this.canAnimate = false;

            var start = animation == null ? -this.height : lastY;
            var time = 300;
            IInterpolator interpolator;

            if (percent < 0)
                percent = 0;
            else if (percent > 100)
                percent = 100;

            this.lastY = -this.height * (percent / 100F);

            if ((int)this.lastY == (int)start && !force) {
                this.canAnimate = true;
                return;
            }

            //is new so do bound, else linear
            if (this.fullAnimation || !Utils.IsSameDay) {
                interpolator = new BounceInterpolator();
                time = 3000;
                fullAnimation = false;
            } else {
                interpolator = new LinearInterpolator();
            }

            this.animation = new TranslateAnimation(Dimension.Absolute, 0, Dimension.Absolute, 0, Dimension.Absolute, start, Dimension.Absolute, lastY);
            this.animation.Duration = time;
            this.animation.Interpolator = interpolator;
            this.animation.AnimationEnd += (object sender, Animation.AnimationEndEventArgs e) => {
                this.canAnimate = true;
            };

            this.animation.FillAfter = true;
            this.topLayer.StartAnimation(this.animation);
            if (this.topLayer.Visibility != Android.Views.ViewStates.Visible) {
                this.topLayer.Visibility = Android.Views.ViewStates.Visible;
            }
        }

        private void UpdateUI(bool force = false) {
            if (this.progressView == null)
                return;

            this.RunOnUiThread(() => {

                long steps = 0;
                var showWaring = false;
                if (this.Binder == null) {
                    if (Utils.IsSameDay)
                        steps = Helpers.Settings.CurrentDaySteps;
                } else {
                    steps = Binder.StepService.StepsToday;
                    showWaring = binder.StepService.WarningState;
                }

                this.progressView.SetStepCount(steps);
                this.stepCount.Text = Utils.FormatSteps(steps);

                var miles = Conversion.StepsToMiles(steps);
                this.distance.Text = string.Format(distanceString,
                    Helpers.Settings.UseKilometeres ?
                    Conversion.StepsToKilometers(steps).ToString("N2") :
                    miles.ToString("N2"));

                var lbs = Helpers.Settings.UseKilometeres ? Helpers.Settings.Weight * 2.20462 : Helpers.Settings.Weight;
                this.calorieCount.Text = string.Format(calorieString,
                    Helpers.Settings.Enhanced ?
                    Conversion.CaloriesBurnt(miles, (float)lbs, Helpers.Settings.Cadence) :
                    Conversion.CaloriesBurnt(miles));

                var percent = Conversion.StepCountToPercentage(steps);
                var percent2 = percent / 100;

                if (steps <= 50)
                    this.percentage.Text = steps == 0 ? string.Empty : string.Format(percentString, percent2.ToString("P2"));
                else
                    this.percentage.Text = completedString;

                /// set high score day
                this.highScore.Visibility = Settings.TodayIsHighScore ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Invisible;

                /// detect warning
                this.warning.Visibility = showWaring ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Invisible;
                /// Show daily goal message.
                if (!string.IsNullOrWhiteSpace(Settings.GoalTodayMessage) &&
                    Settings.GoalTodayDay.DayOfYear == DateTime.Today.DayOfYear &&
                    Settings.GoalTodayDay.Year == DateTime.Today.Year) {
                    Toast.MakeText(this, Settings.GoalTodayMessage, ToastLength.Long).Show();
                    Settings.GoalTodayMessage = string.Empty;
                }

                this.AnimateTopLayer((float)percent, force);

                this.Title = Utils.DateString;
            });
        }
        #endregion

        #region Method Events
        private void RaisePropertyChanged(string propertyName) {
            HandlePropertyChanged(null, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Methods of GestureDetector.IOnGestureListener
        public bool OnDown(MotionEvent e) {
            return true;
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY) {
            return true;
        }

        public void OnLongPress(MotionEvent e) {
            Helpers.Settings.CurrentDaySteps = 0;
            Helpers.Settings.TotalSteps = Helpers.Settings.StepsBeforeToday;
            if (this.Binder != null) {
                Binder.StepService.StepsToday = 0;
            }
            this.RaisePropertyChanged("StepsToday");
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY) {
            return true;
        }

        public void OnShowPress(MotionEvent e) {

        }

        public bool OnSingleTapUp(MotionEvent e) {
            return true;
        }
        #endregion

        #region Overrides
        protected override void OnCreate(Bundle savedInstanceState) {
            base.OnCreate(savedInstanceState);

            this.gestureDetector = new GestureDetector(this);
            this.gestureDetector.IsLongpressEnabled = true;

            // Set our view from the "main" layout resource
            this.SetContentView(Resource.Layout.Main);

            topLayer = FindViewById<FrameLayout>(Resource.Id.top_layer);
            handler = new Handler();
            if (!Utils.IsKitKatWithStepCounter(PackageManager)) {
                //no step detector detected :(
                var counter_layout = FindViewById<FrameLayout>(Resource.Id.counter_layout);
                var no_sensor = FindViewById<LinearLayout>(Resource.Id.no_sensor_box);
                var sensor_image = FindViewById<ImageView>(Resource.Id.no_sensor_image);
                sensor_image.SetImageResource(Resource.Drawable.ic_unsupporteddevice);
                no_sensor.Visibility = Android.Views.ViewStates.Visible;
                counter_layout.Visibility = Android.Views.ViewStates.Gone;
                this.Title = Resources.GetString(Resource.String.app_name);
                handler.PostDelayed(() => AnimateTopLayer(0), 500);
                return;
            }

            stepCount = FindViewById<TextView>(Resource.Id.stepcount);
            calorieCount = FindViewById<TextView>(Resource.Id.calories);
            distance = FindViewById<TextView>(Resource.Id.distance);
            percentage = FindViewById<TextView>(Resource.Id.percentage);
            progressView = FindViewById<ProgressView>(Resource.Id.progressView);
            highScore = FindViewById<ImageView>(Resource.Id.high_score);
            warning = FindViewById<ImageView>(Resource.Id.warning);

            calorieString = Resources.GetString(Resource.String.calories);
            distanceString = Resources.GetString(Helpers.Settings.UseKilometeres ? Resource.String.kilometeres : Resource.String.miles);
            percentString = Resources.GetString(Resource.String.percent_complete);
            completedString = Resources.GetString(Resource.String.completed);

            this.Title = Utils.DateString;

            handler.PostDelayed(() => UpdateUI(), 500);

            StartStepService();

            //            setOnLongClickListener(new View.OnLongClickListener() {
            //  public boolean onLongClick(View view) {
            //    activity.openContextMenu(view);  
            //    return true;  // avoid extra click events
            //  }
            //});


            //for testing

            //stepCount.Clickable = true;
            //stepCount.Click += (object sender, EventArgs e) => {
            //    if (binder != null) {
            //        if (testSteps == 1)
            //            testSteps = (int)binder.StepService.StepsToday;
            //        testSteps += 100;
            //        if (testSteps > 10000)
            //            testSteps += 1000;
            //        binder.StepService.AddSteps(testSteps);


            //        HandlePropertyChanged(null, new System.ComponentModel.PropertyChangedEventArgs("StepsToday"));
            //    }
            //};

        }


        public override bool DispatchTouchEvent(MotionEvent ev) {
            return this.gestureDetector.OnTouchEvent(ev);
        }

        public override bool OnTouchEvent(Android.Views.MotionEvent e) {
            return this.gestureDetector.OnTouchEvent(e);
        }

        protected override void OnStop() {
            base.OnStop();
            if (this.IsBound) {
                this.UnbindService(serviceConnection);
                this.IsBound = false;
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (this.IsBound) {
                this.UnbindService(serviceConnection);
                this.IsBound = false;
            }
        }

        protected override void OnStart() {
            base.OnStart();

            if (!Utils.IsKitKatWithStepCounter(PackageManager)) {
                Console.WriteLine("Not compatible with sensors, stopping service.");
                return;
            }

            if (!this.firstRun)
                this.StartStepService();

            if (this.IsBound)
                return;

            var serviceIntent = new Intent(this, typeof(StepService));
            this.serviceConnection = new StepServiceConnection(this);
            this.BindService(serviceIntent, this.serviceConnection, Bind.AutoCreate);
        }

        protected override void OnPause() {
            base.OnPause();
            if (this.isServiceRegistered && this.binder != null) {
                this.binder.StepService.PropertyChanged -= HandlePropertyChanged;
                this.isServiceRegistered = false;
            }
        }

        protected override void OnResume() {
            base.OnResume();
            if (!firstRun) {

                if (handler == null)
                    handler = new Handler();
                handler.PostDelayed(() => UpdateUI(true), 500);
            }

            this.firstRun = false;

            if (!this.isServiceRegistered && this.binder != null) {
                this.binder.StepService.PropertyChanged += HandlePropertyChanged;
                this.isServiceRegistered = true;
            }
        }
        #endregion

        #region Events
        private void HandlePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName != "StepsToday")
                return;
            UpdateUI();
        }
        #endregion

    }
}

