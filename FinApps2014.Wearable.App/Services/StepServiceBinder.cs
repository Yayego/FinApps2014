using Android.OS;

namespace FinApps2014.Wearable.App.Services {
    public class StepServiceBinder : Binder {

        #region Members
        private StepService stepService;
        #endregion

        #region Constructors
        public StepServiceBinder(StepService service) {
            this.stepService = service;
        }
        #endregion

        #region Properties
        public StepService StepService {
            get { return stepService; }
        }
        #endregion

    }
}

