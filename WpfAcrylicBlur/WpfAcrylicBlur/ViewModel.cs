using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfAcrylicBlur
{
  public class ViewModel : BindBaseWithCommand
  {
    private bool _isBlurOn = true;
    public bool IsBlurOn
    {
      get { return this._isBlurOn; }
      set { SetProperty(ref this._isBlurOn, value, nameof(IsBlurOn), null); }
    }

    private string _backgroundColor = "#99333333";
    public string BackgroundColor
    {
      get { return this._backgroundColor; }
      set { SetProperty(ref this._backgroundColor, value, nameof(BackgroundColor), null); }
    }

    // コマンド本体の実装
    // ぼかしの切り替えを行うために、View側にテンプレートの再読み込みを行わせる
    public Action ReApplyTemplate { get; set; }

    private void ToggleBlurCommandExecute(object parameter)
    {
      ReApplyTemplate.Invoke();
    }

    // コマンドの発動条件を指定
    // 今回は特に無し
    private bool ToggleBlurCommandCanExecute(object parameter)
    {
      return true;
    }

    // コマンドの作成
    private ICommand _toggleBlurCommand;
    public ICommand ToggleBlurCommand
    {
      get
      {
        if (_toggleBlurCommand == null)
          _toggleBlurCommand = new RelayCommand
          {
            ExecuteHandler = ToggleBlurCommandExecute,
            CanExecuteHandler = ToggleBlurCommandCanExecute,
          };
        return _toggleBlurCommand;
      }
    }
  }

  public class BackgroundBrushConverter : IValueConverter
  {
    private BrushConverter _brushConverter = new BrushConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!(value is string)) { throw new NotImplementedException(); }

      return _brushConverter.ConvertFromString((string)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
  }

  // プロパティの変更通知を扱うヘルパークラス
  // コマンドの変更通知も同時に行う
  public class BindBaseWithCommand : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual bool SetProperty<T>(ref T property, T value,
      [CallerMemberName] string propertyName = null, ICommand command = null)
    {
      if (Equals(property, value)) return false;

      property = value;

      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      if (command != null) ((RelayCommand)command).RaiseCanExecuteChanged();
      return true;
    }
  }

  // Commandを扱うヘルパークラス
  public class RelayCommand : ICommand
  {
    public Action<object> ExecuteHandler { get; set; }
    public Func<object, bool> CanExecuteHandler { get; set; }

    public bool CanExecute(object parameter)
    {
      var d = CanExecuteHandler;
      return d == null ? true : d(parameter);
    }

    public void Execute(object parameter)
    {
      ExecuteHandler?.Invoke(parameter);
    }

    public event EventHandler CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
      CanExecuteChanged?.Invoke(this, null);
    }
  }
}
