using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System.Diagnostics;
using System;
using System.IO;

namespace PaymProd.UITests;

public class AppSession : IDisposable
{
    private const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723";
    // Adjust this path if the build output location changes
    private const string AppPathRelative = @"..\..\..\..\PaymProdNet9\bin\Debug\net9.0-windows\PaymProdNet9.exe";

    public WindowsDriver<WindowsElement>? Session { get; private set; }

    public AppSession()
    {
        Setup();
    }

    private void Setup()
    {
        if (Session != null) return;

        var appPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppPathRelative));
        
        // Ensure the app exists before trying to launch
        if (!File.Exists(appPath))
        {
            // Fallback for Release mode if Debug doesn't exist? 
            // Or just fail with a clear message.
            throw new FileNotFoundException($"Could not find application executable at {appPath}. Please build the PaymProdNet9 project first.");
        }

        var options = new AppiumOptions();
        options.AddAdditionalCapability("app", appPath);
        options.AddAdditionalCapability("deviceName", "WindowsPC");
        options.AddAdditionalCapability("platformName", "Windows");
        options.AddAdditionalCapability("ms:waitForAppLaunch", "10");

        try 
        {
            Session = new WindowsDriver<WindowsElement>(new Uri(WindowsApplicationDriverUrl), options);
            Assert.NotNull(Session);
            
            // Set implicit timeout to allow elements to appear
            Session.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }
        catch (Exception ex)
        {
            // Providing a helpful error message if WinAppDriver is not running
            throw new InvalidOperationException("Failed to create WindowsDriver session. Ensure WinAppDriver is running as Administrator.", ex);
        }
    }

    public void Dispose()
    {
        if (Session != null)
        {
            Session.Quit();
            Session = null;
        }
    }
}
