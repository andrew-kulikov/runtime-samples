namespace IosVaExhaustion;

[Register("SceneDelegate")]
public class SceneDelegate : UIResponder, IUIWindowSceneDelegate
{
    static int started;

    [Export("window")]
    public UIWindow? Window { get; set; }

    [Export("scene:willConnectToSession:options:")]
    public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
            return;

        Window ??= new UIWindow(windowScene);
        var label = new UILabel(Window.Frame)
        {
            BackgroundColor = UIColor.SystemBackground,
            TextAlignment = UITextAlignment.Center,
            Lines = 0,
            Text = "VA exhaustion\nlogging to the Mac console",
            AutoresizingMask = UIViewAutoresizing.All,
        };
        var vc = new UIViewController();
        vc.View!.AddSubview(label);
        Window.RootViewController = vc;
        Window.MakeKeyAndVisible();

        if (Interlocked.Exchange(ref started, 1) != 0)
            return;

        var thread = new Thread(() => Probe.Run(text => label.BeginInvokeOnMainThread(() => label.Text = text)))
        {
            Name = "va-alloc",
        };
        thread.Start();
    }
}
