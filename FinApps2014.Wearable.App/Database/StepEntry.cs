using System;

namespace FinApps2014.Wearable.App.Database {
    public class StepEntry {
        public StepEntry() {
        }

        public int ID { get; set; }

        public Int64 Steps { get; set; }

        public DateTime Date { get; set; }
    }
}