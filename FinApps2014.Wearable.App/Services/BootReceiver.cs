using Android.App;
using Android.Content;

namespace FinApps2014.Wearable.App.Services {

    [BroadcastReceiver]
    [IntentFilter(new[] { "android.intent.action.BOOT_COMPLETED", "android.intent.action.MY_PACKAGE_REPLACED" })]
    public class BootReceiver : BroadcastReceiver {

        #region Overrides
        public override void OnReceive(Context context, Intent intent) {
            var stepServiceIntent = new Intent(context, typeof(StepService));
            context.StartService(stepServiceIntent);
        }
        #endregion

    }

}

