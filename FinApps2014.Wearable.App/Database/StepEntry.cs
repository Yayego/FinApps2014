using System;

namespace FinApps2014.Wearable.App.Database {
    public class StepEntry {

        #region Constructors
        public StepEntry() {
        }
        #endregion

        #region Properties
        public int ID { get; set; }

        public long Steps { get; set; }

        public DateTime Date { get; set; }
        #endregion

    }
}