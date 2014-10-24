using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using System;

namespace FinApps2014.Wearable.App.Controls {

    public class FontFitTextView : TextView {

        #region Constructors
        public FontFitTextView(Context context)
            : base(context) {
            Initialize();
        }

        public FontFitTextView(Context context, IAttributeSet attrs)
            : base(context, attrs) {
            Initialize();
        }

        public FontFitTextView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle) {
        }

        public FontFitTextView(IntPtr pointer, JniHandleOwnership handle)
            : base(pointer, handle) {
        }
        #endregion

        #region Properties
        protected Android.Graphics.Paint TestPaint {
            get;
            set;
        }
        #endregion

        #region Methods
        private void Initialize() {
            TestPaint = new Paint();
            TestPaint.Set(this.Paint);
            //max size defaults to the initially specified text size unless it is too small
        }

        /* Re size the font so the specified text fits in the text box
     * assuming the text box is the specified width.
     */
        private void RefitText(String text, int textWidth) {
            if (textWidth <= 0)
                return;
            int targetWidth = textWidth - this.PaddingLeft - this.PaddingRight;
            float hi = (float)MeasuredHeight * .8f;
            float lo = 2;
            float threshold = 0.5f; // How close we have to be

            TestPaint.Set(this.Paint);

            while ((hi - lo) > threshold) {
                float size = (hi + lo) / 2;
                TestPaint.TextSize = (size);
                if (TestPaint.MeasureText(text) >= targetWidth)
                    hi = size; // too big
                else
                    lo = size; // too small
            }
            // Use lo so that we undershoot rather than overshoot
            this.SetTextSize(ComplexUnitType.Px, lo);
        }
        #endregion

        #region Overrides
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            int parentWidth = MeasureSpec.GetSize(widthMeasureSpec);
            int height = MeasuredHeight;
            RefitText(Text.ToString(), parentWidth);
            this.SetMeasuredDimension(parentWidth, height);
        }

        protected override void OnTextChanged(Java.Lang.ICharSequence text, int start, int before, int after) {
            RefitText(text.ToString(), Width);
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh) {
            if (w != oldw) {
                RefitText(Text, w);
            }
        }
        #endregion

    }
}

