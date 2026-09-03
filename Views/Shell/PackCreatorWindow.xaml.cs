using System;
using Loc = AnikiVisualPackCreator.Localization.LocalizationService;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace AnikiVisualPackCreator;

public partial class PackCreatorWindow : Window
{
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 0x00000002;
    private const double StartupWorkAreaMargin = 12.0;

    private bool isInitialized;
    private LoginPackWindow? loginPackView;
    private CompletePackWindow? completePackView;
    private AboutWindow? aboutView;

    public PackCreatorWindow()
    {
        InitializeComponent();
        SourceInitialized += WindowSourceInitialized;
        Loaded += (_, _) =>
        {
            EnsureWindowFitsCurrentWorkArea();
            isInitialized = true;
            HomeTab.IsChecked = true;
            ShowHome();
        };
    }

    internal void NavigateToVisualPack()
    {
        VisualPackTab.IsChecked = true;
    }

    private void VisualPackTabChecked(object sender, RoutedEventArgs e)
    {
        if (!isInitialized)
        {
            return;
        }

        ShowVisualPack();
    }

    private void SoundPackTabChecked(object sender, RoutedEventArgs e)
    {
        if (!isInitialized)
        {
            return;
        }

        ShowSoundPack();
    }

    private void ColorPackTabChecked(object sender, RoutedEventArgs e)
    {
        if (!isInitialized)
        {
            return;
        }

        ShowColorPack();
    }

    private void LoginPackTabChecked(object sender, RoutedEventArgs e)
    {
        if (!isInitialized)
        {
            return;
        }

        ShowLoginPack();
    }

    private void CompletePackTabChecked(object sender, RoutedEventArgs e)
    {
        if (!isInitialized)
        {
            return;
        }

        ShowCompletePack();
    }

    private void HomeTabChecked(object sender, RoutedEventArgs e)
    {
        if (!isInitialized)
        {
            return;
        }

        ShowHome();
    }

    private void ShowVisualPack()
    {
        SoundPackView.Deactivate();
        ColorPackView.Deactivate();
        loginPackView?.Deactivate();
        VisualPackView.Visibility = Visibility.Visible;
        SoundPackView.Visibility = Visibility.Collapsed;
        ColorPackView.Visibility = Visibility.Collapsed;
        LoginPackHost.Visibility = Visibility.Collapsed;
        CompletePackHost.Visibility = Visibility.Collapsed;
        AboutHost.Visibility = Visibility.Collapsed;
        Title = "Aniki Pack Creator — Visual Pack";
    }

    private void ShowSoundPack()
    {
        ColorPackView.Deactivate();
        loginPackView?.Deactivate();
        VisualPackView.Visibility = Visibility.Collapsed;
        SoundPackView.Visibility = Visibility.Visible;
        ColorPackView.Visibility = Visibility.Collapsed;
        LoginPackHost.Visibility = Visibility.Collapsed;
        CompletePackHost.Visibility = Visibility.Collapsed;
        AboutHost.Visibility = Visibility.Collapsed;
        Title = "Aniki Pack Creator — Sound Pack";
    }

    private void ShowColorPack()
    {
        SoundPackView.Deactivate();
        loginPackView?.Deactivate();
        VisualPackView.Visibility = Visibility.Collapsed;
        SoundPackView.Visibility = Visibility.Collapsed;
        ColorPackView.Visibility = Visibility.Visible;
        LoginPackHost.Visibility = Visibility.Collapsed;
        CompletePackHost.Visibility = Visibility.Collapsed;
        AboutHost.Visibility = Visibility.Collapsed;
        Title = "Aniki Pack Creator — Color Pack";
    }

    private void ShowLoginPack()
    {
        try
        {
            SoundPackView.Deactivate();
            ColorPackView.Deactivate();

            if (loginPackView is null)
            {
                loginPackView = new LoginPackWindow();
            }

            if (!ReferenceEquals(LoginPackHost.Content, loginPackView))
            {
                LoginPackHost.Content = loginPackView;
            }

            VisualPackView.Visibility = Visibility.Collapsed;
            SoundPackView.Visibility = Visibility.Collapsed;
            ColorPackView.Visibility = Visibility.Collapsed;
            CompletePackHost.Visibility = Visibility.Collapsed;
            AboutHost.Visibility = Visibility.Collapsed;
            LoginPackHost.Visibility = Visibility.Visible;
            loginPackView.Activate();
            Title = "Aniki Pack Creator — Login Pack";
        }
        catch (Exception exception)
        {
            LoginPackHost.Visibility = Visibility.Collapsed;
            CompletePackHost.Visibility = Visibility.Collapsed;
            AboutHost.Visibility = Visibility.Collapsed;
            VisualPackView.Visibility = Visibility.Visible;
            VisualPackTab.IsChecked = true;
            Title = "Aniki Pack Creator — Visual Pack";

            MessageBox.Show(
                this,
                Loc.Get("LoginViewCouldNotOpen") + "\n\n" + exception,
                "Aniki Pack Creator",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowCompletePack()
    {
        try
        {
            SoundPackView.Deactivate();
            ColorPackView.Deactivate();
            loginPackView?.Deactivate();

            if (completePackView is null)
            {
                completePackView = new CompletePackWindow();
            }

            if (!ReferenceEquals(CompletePackHost.Content, completePackView))
            {
                CompletePackHost.Content = completePackView;
            }

            VisualPackView.Visibility = Visibility.Collapsed;
            SoundPackView.Visibility = Visibility.Collapsed;
            ColorPackView.Visibility = Visibility.Collapsed;
            LoginPackHost.Visibility = Visibility.Collapsed;
            CompletePackHost.Visibility = Visibility.Visible;
            AboutHost.Visibility = Visibility.Collapsed;
            Title = "Aniki Pack Creator — Complete Pack";
        }
        catch (Exception exception)
        {
            CompletePackHost.Visibility = Visibility.Collapsed;
            VisualPackView.Visibility = Visibility.Visible;
            VisualPackTab.IsChecked = true;
            Title = "Aniki Pack Creator — Visual Pack";
            MessageBox.Show(
                this,
                Loc.Get("CompleteViewCouldNotOpen") + "\n\n" + exception,
                "Aniki Pack Creator",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowHome()
    {
        try
        {
            SoundPackView.Deactivate();
            ColorPackView.Deactivate();
            loginPackView?.Deactivate();

            if (aboutView is null)
            {
                aboutView = new AboutWindow();
            }

            if (!ReferenceEquals(AboutHost.Content, aboutView))
            {
                AboutHost.Content = aboutView;
            }

            VisualPackView.Visibility = Visibility.Collapsed;
            SoundPackView.Visibility = Visibility.Collapsed;
            ColorPackView.Visibility = Visibility.Collapsed;
            LoginPackHost.Visibility = Visibility.Collapsed;
            CompletePackHost.Visibility = Visibility.Collapsed;
            AboutHost.Visibility = Visibility.Visible;
            Title = "Aniki Pack Creator — Home";
        }
        catch (Exception exception)
        {
            AboutHost.Visibility = Visibility.Collapsed;
            VisualPackView.Visibility = Visibility.Visible;
            VisualPackTab.IsChecked = true;
            Title = "Aniki Pack Creator — Visual Pack";
            MessageBox.Show(
                this,
                Loc.Get("HomeViewCouldNotOpen") + "\n\n" + exception,
                "Aniki Pack Creator",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        var hasUnsavedChanges = VisualPackView.HasUnsavedChanges
            || SoundPackView.HasUnsavedChanges
            || ColorPackView.HasUnsavedChanges
            || (completePackView?.HasUnsavedChanges ?? false)
            || (loginPackView?.HasUnsavedChanges ?? false);

        if (!hasUnsavedChanges)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            Loc.Get("CloseWithUnsavedProjectsMessage"),
            Loc.Get("UnsavedChangesTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            e.Cancel = true;
        }
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        SoundPackView.OnHostClosed();
        ColorPackView.OnHostClosed();
        completePackView?.OnHostClosed();
        loginPackView?.OnHostClosed();
    }


    private void MinimizeButtonClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButtonClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void CloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void WindowStateChanged(object? sender, EventArgs e)
    {
        UpdateMaximizeButton();
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateMaximizeButton()
    {
        if (MaximizeGlyph == null)
        {
            return;
        }

        var isMaximized = WindowState == WindowState.Maximized;
        MaximizeGlyph.Text = isMaximized ? "\uE923" : "\uE922";
        MaximizeButton.ToolTip = isMaximized ? "Restore" : "Maximize";
    }

    private void HeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsClickOnButton(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private static bool IsClickOnButton(DependencyObject? source)
    {
        var current = source;
        while (current != null)
        {
            if (current is ButtonBase)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void WindowSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(WindowMessageHook);
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmGetMinMaxInfo)
        {
            ApplyMonitorWorkAreaToMaximizedWindow(hwnd, lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static void ApplyMonitorWorkAreaToMaximizedWindow(IntPtr hwnd, IntPtr lParam)
    {
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return;
        }

        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };

        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var workArea = monitorInfo.WorkArea;
        var monitorArea = monitorInfo.MonitorArea;

        minMaxInfo.MaxPosition.X = workArea.Left - monitorArea.Left;
        minMaxInfo.MaxPosition.Y = workArea.Top - monitorArea.Top;
        minMaxInfo.MaxSize.X = workArea.Right - workArea.Left;
        minMaxInfo.MaxSize.Y = workArea.Bottom - workArea.Top;

        Marshal.StructureToPtr(minMaxInfo, lParam, true);
    }

    private void EnsureWindowFitsCurrentWorkArea()
    {
        if (WindowState != WindowState.Normal)
        {
            return;
        }

        var workArea = GetCurrentMonitorWorkAreaInDips();
        if (workArea.Width <= 0 || workArea.Height <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(1.0, workArea.Width - (StartupWorkAreaMargin * 2.0));
        var availableHeight = Math.Max(1.0, workArea.Height - (StartupWorkAreaMargin * 2.0));

        var requestedWidth = double.IsNaN(Width) ? ActualWidth : Width;
        var requestedHeight = double.IsNaN(Height) ? ActualHeight : Height;
        var targetWidth = Math.Min(requestedWidth, availableWidth);
        var targetHeight = Math.Min(requestedHeight, availableHeight);

        if (MinWidth > targetWidth)
        {
            MinWidth = targetWidth;
        }

        if (MinHeight > targetHeight)
        {
            MinHeight = targetHeight;
        }

        Width = targetWidth;
        Height = targetHeight;
        Left = workArea.Left + ((workArea.Width - targetWidth) / 2.0);
        Top = workArea.Top + ((workArea.Height - targetHeight) / 2.0);
    }

    private Rect GetCurrentMonitorWorkAreaInDips()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return SystemParameters.WorkArea;
        }

        var monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return SystemParameters.WorkArea;
        }

        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };

        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return SystemParameters.WorkArea;
        }

        var source = HwndSource.FromHwnd(handle);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new Point(monitorInfo.WorkArea.Left, monitorInfo.WorkArea.Top));
        var bottomRight = transform.Transform(new Point(monitorInfo.WorkArea.Right, monitorInfo.WorkArea.Bottom));

        return new Rect(topLeft, bottomRight);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;
    }

}
