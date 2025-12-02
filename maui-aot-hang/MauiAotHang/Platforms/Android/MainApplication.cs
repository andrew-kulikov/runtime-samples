using Android.App;
using Android.Runtime;
using Android.Util;
using MauiAotHang.Features;

namespace MauiAotHang
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp()
        {
            var program = MauiProgram.CreateMauiApp();

            Log.Debug("MyLog", $"Calling mt1 from thread #{Environment.CurrentManagedThreadId}");
            var mt1 = new ManualType2(42);
            if (Environment.CurrentManagedThreadId > -1)
            {
                mt1.Num = 100;
            }
            Log.Debug("MyLog", $"Mt1 result: {mt1}");

            return program;
        }
    }
}
