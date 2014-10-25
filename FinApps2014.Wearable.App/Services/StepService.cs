using Android.App;
using Android.Content;
using Android.Hardware;
using FinApps2014.Wearable.App.Database;
using FinApps2014.Wearable.App.Helpers;
using System;
using System.ComponentModel;

namespace FinApps2014.Wearable.App.Services {

    [Service(Enabled = true)]
    [IntentFilter(new String[] { "FinApps2014.Wearable.App.StepService" })]
    public class StepService : Service, ISensorEventListener, INotifyPropertyChanged {

        #region Members
        private bool isRunning;
        private long stepsToday = 0;
        private StepServiceBinder binder;
        private long newSteps = 0;
        private long lastSteps = 0;
        #endregion


        #region Properties
        public bool WarningState {
            get;
            set;
        }

        public long StepsToday {
            get { return this.stepsToday; }
            set {
                if (this.stepsToday == value)
                    return;

                this.stepsToday = value;
                this.OnPropertyChanged("StepsToday");
                Helpers.Settings.CurrentDaySteps = value;
            }
        }
        #endregion

        #region Methods
        private void Startup(bool warning = false) {
            //check if kit kat can sensor compatible
            if (!Utils.IsKitKatWithStepCounter(this.PackageManager)) {

                Console.WriteLine("Not compatible with sensors, stopping service.");
                this.StopSelf();
                return;
            }

            this.CrunchDates(true);

            if (!this.isRunning) {
                this.RegisterListeners(warning ? SensorType.StepDetector : SensorType.StepCounter);
                this.WarningState = warning;
            }

            this.isRunning = true;
        }

        private void RegisterListeners(SensorType sensorType) {

            var sensorManager = (SensorManager)GetSystemService(Context.SensorService);
            var sensor = sensorManager.GetDefaultSensor(sensorType);

            //get faster why not, nearly fast already and when
            //sensor gets messed up it will be better
            sensorManager.RegisterListener(this, sensor, SensorDelay.Normal);
            Console.WriteLine("Sensor listener registered of type: " + sensorType);

        }

        private void UnregisterListeners() {

            if (!isRunning)
                return;

            try {
                var sensorManager = (SensorManager)GetSystemService(Context.SensorService);
                sensorManager.UnregisterListener(this);
                Console.WriteLine("Sensor listener unregistered.");
#if DEBUG
                Android.Util.Log.Debug("STEPSERVICE", "Sensor listener unregistered.");
#endif
                isRunning = false;
            } catch (Exception ex) {
#if DEBUG
                Android.Util.Log.Debug("STEPSERVICE", "Unable to unregister: " + ex);
#endif
            }
        }

        public void AddSteps(long count) {
            //if service rebooted or rebound then this will null out to 0, but count will still be since last boot.
            if (this.lastSteps == 0) {
                this.lastSteps = count;
            }

            //calculate new steps
            this.newSteps = count - this.lastSteps;

            //ensure we are never negative
            //if so, no worries as we are about to re-set the lastSteps to the
            //current count
            if (this.newSteps < 0)
                this.newSteps = 1;


            this.lastSteps = count;

            //ensure we don't need to re-boot day :)
            this.CrunchDates();
            this.CrunchHighScores();

            //save total steps!
            Helpers.Settings.TotalSteps += this.newSteps;

            this.StepsToday = Helpers.Settings.TotalSteps - Helpers.Settings.StepsBeforeToday;

            Console.WriteLine("New step detected by STEP_COUNTER sensor. Total step count: " + stepsToday);
#if DEBUG
            Android.Util.Log.Debug("STEPSERVICE", "New steps: " + newSteps + " total: " + stepsToday);
#endif
        }

        private void CrunchHighScores() {
            bool notification = Helpers.Settings.ProgressNotifications;

            int halfGoal = 5000;
            int fullGoal = 10000;
            int doubleGoal = 20000;
            if (stepsToday < halfGoal && stepsToday + newSteps >= halfGoal) {
                Helpers.Settings.GoalTodayDay = DateTime.Today;
                Helpers.Settings.GoalTodayMessage = Resources.GetString(Resource.String.goal_half);
            } else if (stepsToday < fullGoal && stepsToday + newSteps >= fullGoal) {
                Helpers.Settings.GoalTodayDay = DateTime.Today;
                Helpers.Settings.GoalTodayMessage = string.Format(Resources.GetString(Resource.String.goal_full), (fullGoal).ToString("N0"));
            } else if (stepsToday < doubleGoal && stepsToday + newSteps >= doubleGoal) {
                Helpers.Settings.GoalTodayDay = DateTime.Today;
                Helpers.Settings.GoalTodayMessage = string.Format(Resources.GetString(Resource.String.goal_double), (doubleGoal).ToString("N0"));
            } else {
                notification = false;
            }

            if (notification) {
                PopUpNotification(0, Resources.GetString(Resource.String.goal_update), Helpers.Settings.GoalTodayMessage);
            }

            notification = false;
            if (stepsToday + newSteps > Helpers.Settings.HighScore) {
                Helpers.Settings.HighScore = stepsToday + newSteps;
                //if not today
                if (!Helpers.Settings.TodayIsHighScore) {
                    //if first day of use then no notifications, else pop it up
                    if (Helpers.Settings.FirstDayOfUse.DayOfYear == DateTime.Today.DayOfYear &&
                        Helpers.Settings.FirstDayOfUse.Year == DateTime.Today.Year) {
                        notification = false;
                    } else {
                        notification = Helpers.Settings.ProgressNotifications;
                    }
                }
                //this triggers a new high score day so the next tiem it comes in TodayIsHighScore will be true
                Helpers.Settings.HighScoreDay = DateTime.Today;
            }

            //notifcation for high score
            if (notification) {
                PopUpNotification(1, Resources.GetString(Resource.String.high_score_title),
                    string.Format(Resources.GetString(Resource.String.high_score),
                        Utils.FormatSteps(Helpers.Settings.HighScore)));
            }

            notification = Helpers.Settings.AccumulativeNotifications;
            var notificationString = string.Empty;
            if (Helpers.Settings.TotalSteps + newSteps > Helpers.Settings.NextGoal) {
                notificationString = string.Format(Resources.GetString(Resource.String.awesome), Utils.FormatSteps(Helpers.Settings.NextGoal));
                if (Settings.NextGoal < 50) {
                    Settings.NextGoal = 50;
                } else if (Helpers.Settings.NextGoal < 100) {
                    Settings.NextGoal = 100;
                } else {
                    Settings.NextGoal += 50;
                }
            } else {
                notification = false;
            }

            //notifcation for accumulative records
            if (notification) {
                PopUpNotification(2, Resources.GetString(Resource.String.awesome_title), notificationString);
            }
        }

        private void PopUpNotification(int id, string title, string message) {
            Notification.Builder mBuilder =
                new Notification.Builder(this)
                    .SetSmallIcon(Resource.Drawable.ic_notification)
                    .SetContentTitle(title)
                    .SetContentText(message)
                    .SetAutoCancel(true);
            // Creates an explicit intent for an Activity in your app
            Intent resultIntent = new Intent(this, typeof(MainActivity));
            resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            // The stack builder object will contain an artificial back stack for the
            // started Activity.
            // This ensures that navigating backward from the Activity leads out of
            // your application to the Home screen.
            TaskStackBuilder stackBuilder = TaskStackBuilder.Create(this);
            // Adds the back stack for the Intent (but not the Intent itself)
            //stackBuilder.AddParentStack();
            // Adds the Intent that starts the Activity to the top of the stack
            stackBuilder.AddNextIntent(resultIntent);
            PendingIntent resultPendingIntent =
                stackBuilder.GetPendingIntent(
                    0,
                    PendingIntentFlags.UpdateCurrent
                );
            mBuilder.SetContentIntent(resultPendingIntent);

            NotificationManager mNotificationManager =
                (NotificationManager)GetSystemService(Context.NotificationService);
            // mId allows you to update the notification later on.
            mNotificationManager.Notify(id, mBuilder.Build());
        }

        private void CrunchDates(bool startup = false) {
            if (!Utils.IsSameDay) {
                //save our day from yesterday, we dont' do datetime.adddays(-1) because phone might have been off
                //for more then 1 day and it would not be correct!
                var yesterday = Helpers.Settings.CurrentDay;
                var dayEntry = StepEntryManager.GetStepEntry(yesterday);
                if (dayEntry == null || dayEntry.Date.DayOfYear != yesterday.DayOfYear) {
                    dayEntry = new StepEntry();
                }

                dayEntry.Date = yesterday;
                dayEntry.Steps = Helpers.Settings.CurrentDaySteps;

                Helpers.Settings.CurrentDay = DateTime.Today;
                Helpers.Settings.CurrentDaySteps = 0;
                Helpers.Settings.StepsBeforeToday = Helpers.Settings.TotalSteps;
                this.StepsToday = 0;
                try {
                    StepEntryManager.SaveStepEntry(dayEntry);
                } catch (Exception ex) {
                    Console.WriteLine("Something horrible has gone wrong attempting to save database entry, it is lost forever :(");
                }

            } else if (startup) {
                this.StepsToday = Helpers.Settings.TotalSteps - Helpers.Settings.StepsBeforeToday;
            }
        }
        #endregion

        #region Methods of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) {
            if (PropertyChanged == null)
                return;

            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        #region Overrides
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId) {
            Console.WriteLine("StartCommand Called, setting alarm");
#if DEBUG
            Android.Util.Log.Debug("STEPSERVICE", "Start command result called, incoming startup");
#endif

            var alarmManager = ((AlarmManager)ApplicationContext.GetSystemService(Context.AlarmService));
            var intent2 = new Intent(this, typeof(StepService));
            intent2.PutExtra("warning", WarningState);
            var stepIntent = PendingIntent.GetService(ApplicationContext, 10, intent2, PendingIntentFlags.UpdateCurrent);
            // Workaround as on Android 4.4.2 START_STICKY has currently no
            // effect
            // -> restart service every 60 mins
            alarmManager.Set(AlarmType.Rtc, Java.Lang.JavaSystem.CurrentTimeMillis() + 1000 * 60 * 60, stepIntent);

            var warning = false;
            if (intent != null)
                warning = intent.GetBooleanExtra("warning", false);
            this.Startup();

            return StartCommandResult.Sticky;
        }

        public override void OnTaskRemoved(Intent rootIntent) {
            base.OnTaskRemoved(rootIntent);

            this.UnregisterListeners();
#if DEBUG
            Console.WriteLine("OnTaskRemoved Called, setting alarm for 500 ms");
            Android.Util.Log.Debug("STEPSERVICE", "Task Removed, going down");
#endif
            var intent = new Intent(this, typeof(StepService));
            intent.PutExtra("warning", WarningState);
            // Restart service in 500 ms
            ((AlarmManager)GetSystemService(Context.AlarmService)).Set(AlarmType.Rtc, Java.Lang.JavaSystem.CurrentTimeMillis() + 500, PendingIntent.GetService(this, 11, intent, 0));
        }

        public override void OnDestroy() {
            base.OnDestroy();
            this.UnregisterListeners();
            this.isRunning = false;
            this.CrunchDates();
        }
        public override Android.OS.IBinder OnBind(Android.Content.Intent intent) {
            binder = new StepServiceBinder(this);
            return binder;
        }
        #endregion

        #region Events
        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy) {
            //do nothing here
        }

        public void OnSensorChanged(SensorEvent e) {
            switch (e.Sensor.Type) {

                case SensorType.StepCounter:

                    if (this.lastSteps < 0)
                        this.lastSteps = 0;

                    //grab out the current value.
                    var count = (long)e.Values[0];
                    //in some instances if things are running too long (about 4 days)
                    //the value flips and gets crazy and this will be -1
                    //so switch to step detector instead, but put up warning sign.
                    if (count < 0) {

                        this.UnregisterListeners();
                        this.RegisterListeners(SensorType.StepDetector);
                        this.isRunning = true;
#if DEBUG
                        Android.Util.Log.Debug("STEPSERVICE", "Something has gone wrong with the step counter, simulating steps, 2.");
#endif
                        count = lastSteps + 3;

                        this.WarningState = true;
                    } else {
                        this.WarningState = false;
                    }

                    this.AddSteps(count);

                    break;
                case SensorType.StepDetector:
                    count = this.lastSteps + 1;
                    this.AddSteps(count);
                    break;
            }
        }
        #endregion

    }
}

