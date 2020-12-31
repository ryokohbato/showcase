using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WpfAcrylicBlur
{
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();

      // ViewModel側のアクションをView側と紐付ける
      this.DataContextChanged += (s, e) =>
      {
        if (e.NewValue is ViewModel)
        {
          var viewModel = e.NewValue as ViewModel;
          viewModel.ReApplyTemplate = this.ViewAction;
        }
      };

      this.DataContext = new ViewModel();
    }

    // テンプレートの再読み込みを行う
    private void ViewAction()
    {
      this.OnApplyTemplate();
    }

    public override void OnApplyTemplate()
    {
      if (((ViewModel)this.DataContext).IsBlurOn)
        // ウィンドウ背景のぼかし効果を有効にする
        ApplyBlur.ChangeState(this, AccentState.ACCENT_ENABLE_BLURBEHIND);
      else
        // ぼかし効果を切る
        ApplyBlur.ChangeState(this, AccentState.ACCENT_DISABLED);
      
      base.OnApplyTemplate();
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct WindowCompositionAttributeData
  {
    public WindowCompositionAttribute Attribute;
    public IntPtr Data;
    public int SizeOfData;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct AccentPolicy
  {
    public AccentState AccentState;
    public int AccentFlags;
    public uint GradientColor;
    public int AnimationId;
  }

  internal enum AccentState
  {
    ACCENT_DISABLED = 0,
    ACCENT_ENABLE_GRADIENT = 1,
    ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
    ACCENT_ENABLE_BLURBEHIND = 3,
    ACCENT_INVALID_STATE = 4
  }

  internal enum WindowCompositionAttribute
  {
    WCA_ACCENT_POLICY = 19
  }

  internal class NativeMethods
  {
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    // 非公開APIだが、これを用いることで背景をぼかすことができる
    // WPFでの実装例 >> https://github.com/riverar/sample-win10-aeroglass
    // 以下参考リンク
    // http://sourcechord.hatenablog.com/entry/2018/12/04/233335
    // https://stackoverflow.com/questions/32724187/how-do-you-set-the-glass-blend-colour-on-windows-10
    internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
  }

  public class ApplyBlur
  {
    internal static void ChangeState(Window win, AccentState state)
    {
      var windowHelper = new WindowInteropHelper(win);

      var accent = new AccentPolicy
      {
        AccentState = state,
        AccentFlags = 2,
        // ABGRの順に指定
        GradientColor = 0x00000000,
      };

      var accentStructSize = Marshal.SizeOf(accent);
      var accentPtr = Marshal.AllocHGlobal(accentStructSize);
      Marshal.StructureToPtr(accent, accentPtr, false);

      var data = new WindowCompositionAttributeData
      {
        Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
        SizeOfData = accentStructSize,
        Data = accentPtr
      };

      _ = NativeMethods.SetWindowCompositionAttribute(windowHelper.Handle, ref data);

      Marshal.FreeHGlobal(accentPtr);
    }
  }
}
