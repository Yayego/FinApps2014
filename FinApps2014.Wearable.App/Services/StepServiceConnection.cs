﻿using Android.Content;
using Android.OS;

namespace FinApps2014.Wearable.App.Services {
    public class StepServiceConnection : Java.Lang.Object, IServiceConnection {

        #region Members
        MainActivity activity;
        #endregion

        #region Constructors
        public StepServiceConnection(MainActivity activity) {
            this.activity = activity;
        }
        #endregion

        #region Methods
        public void OnServiceConnected(ComponentName name, IBinder service) {
            var serviceBinder = service as StepServiceBinder;
            if (serviceBinder != null) {
                activity.Binder = serviceBinder;
                activity.IsBound = true;
            }
        }

        public void OnServiceDisconnected(ComponentName name) {
            activity.IsBound = false;
        }
        #endregion

    }
}

