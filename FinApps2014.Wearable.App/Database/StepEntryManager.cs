using System;
using System.Collections.Generic;

namespace FinApps2014.Wearable.App.Database {
    /// <summary>
    /// Manager classes are an abstraction on the data access layers
    /// </summary>
    public static class StepEntryManager {

        #region Static Constructors
        static StepEntryManager() {
        }
        #endregion

        #region Static Methods
        public static StepEntry GetStepEntry(DateTime time) {
            return StepEntryRepositoryADO.GetStepEntry(time);
        }

        public static IList<StepEntry> GetStepEntries() {
            return new List<StepEntry>(StepEntryRepositoryADO.GetStepEntries());
        }

        public static int SaveStepEntry(StepEntry item) {
            return StepEntryRepositoryADO.SaveStepEntry(item);
        }

        public static int DeleteStepEntry(int id) {
            return StepEntryRepositoryADO.DeleteStepEntry(id);
        }
        #endregion

    }
}