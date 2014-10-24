using Android.OS;

namespace FinApps2014.Wearable.App.Services {
    public class StepServiceBinder : Binder {
        StepService stepService;
        public StepServiceBinder(StepService service) {
            this.stepService = service;
        }

        public StepService StepService {
            get { return stepService; }
        }
    }
}

