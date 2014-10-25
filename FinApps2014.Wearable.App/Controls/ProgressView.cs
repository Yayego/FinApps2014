using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using System;

namespace FinApps2014.Wearable.App.Controls {
    public partial class ProgressView : FrameLayout {

        #region Constructors
        public ProgressView(Context context) :
            base(context) {
            this.Initialize();
        }

        public ProgressView(Context context, IAttributeSet attrs) :
            base(context, attrs) {
            this.Initialize();
        }

        public ProgressView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle) {
            this.Initialize();
        }
        public ProgressView(IntPtr pointer, JniHandleOwnership handle)
            : base(pointer, handle) {
            this.Initialize();
        }
        #endregion


    }
}

