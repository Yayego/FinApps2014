using Android.Content;
using Android.Preferences;
using Android.Util;

namespace FinApps2014.Wearable.App.Controls {
    /// <summary>
    /// Enforces and integer be entered in the edit text preference
    /// </summary>
    public class IntEditTextPreference : EditTextPreference {

        #region Constructors
        public IntEditTextPreference(Context context)
            : base(context) {
        }

        public IntEditTextPreference(Context context, IAttributeSet attrs)
            : base(context, attrs) {

        }

        public IntEditTextPreference(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle) {
        }
        #endregion

        #region Overrides
        protected override string GetPersistedString(string defaultReturnValue) {
            return GetPersistedInt(1).ToString();
        }

        protected override bool PersistString(string value) {
            int persistValue;
            int.TryParse(value, out persistValue);
            return PersistInt(persistValue);
        }
        #endregion

    }
}