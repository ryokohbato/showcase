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
      if (HexFormat.TryConvertToDeciminal(((ViewModel)DataContext).BackgroundColor, out uint backgroundColor))
      {
        if (((ViewModel)this.DataContext).IsBlurOn)
          // ウィンドウ背景のぼかし効果を有効にする
          ApplyBlur.ChangeState(this, AccentState.ACCENT_ENABLE_BLURBEHIND, backgroundColor);
        else
          // ぼかし効果を切る
          // 背景色は((ViewModel)DataContext).BackgroundColorから透明度を無視した値が割り当てられる
          // AccentState.ACCENT_ENABLE_GRADIENT以外を指定すれば、割り当てが変更される
          ApplyBlur.ChangeState(this, AccentState.ACCENT_ENABLE_GRADIENT, backgroundColor);
      }
      else
      {
        MessageBox.Show("Conversion failed.");
      }
      
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

  public class ApplyBlur
  {
    [DllImport("user32.dll")]
    // 非公開APIだが、これを用いることで背景をぼかすことができる
    // WPFでの実装例 >> https://github.com/riverar/sample-win10-aeroglass
    // 以下参考リンク
    // http://sourcechord.hatenablog.com/entry/2018/12/04/233335
    // https://stackoverflow.com/questions/32724187/how-do-you-set-the-glass-blend-colour-on-windows-10
    internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    internal static void ChangeState(Window win, AccentState state, uint backgroundColor)
    {
      var windowHelper = new WindowInteropHelper(win);

      var accent = new AccentPolicy
      {
        AccentState = state,
        AccentFlags = 2,
        // ABGRの順に指定
        GradientColor = backgroundColor
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

      _ = SetWindowCompositionAttribute(windowHelper.Handle, ref data);

      Marshal.FreeHGlobal(accentPtr);
    }
  }

  public class HexFormat
  {
    // 0x_hogehoge形式の16進文字列を符号無し整数値に変換
    // アンダースコアは無視
    public static bool TryConvertToDeciminal(string target, out uint num)
    {
      num = 0;

      target = target.Replace("_", "");
      if (!target.StartsWith("0x") || target.Length > 10) return false;

      for (int i = 1; i <= target.Length - 2; i++)
      {
        if (TryConvertHexCharToNumber(target[target.Length - i], out byte converted))
          num += converted * (uint)Math.Pow(16, (i - 1));
        else
          return false;
      }

      return true;
    }

    // 16進数を表す単一文字を10進数に変換する
    // 0-9およびa-f、A-Fのみ受け付ける
    private static bool TryConvertHexCharToNumber(char c, out byte num)
    {
      switch (c)
      {
        case '0': num = 0; break;
        case '1': num = 1; break;
        case '2': num = 2; break;
        case '3': num = 3; break;
        case '4': num = 4; break;
        case '5': num = 5; break;
        case '6': num = 6; break;
        case '7': num = 7; break;
        case '8': num = 8; break;
        case '9': num = 9; break;
        case 'A': case 'a': num = 10; break;
        case 'B': case 'b': num = 11; break;
        case 'C': case 'c': num = 12; break;
        case 'D': case 'd': num = 13; break;
        case 'E': case 'e': num = 14; break;
        case 'F': case 'f': num = 15; break;
        default: num = 15; return false;
      }

      return true;
    }
  }
}
