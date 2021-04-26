using System;
using System.Collections.Generic;

namespace ConsoleCheckbox
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args.Length == 0) Environment.Exit(1);    // 選択肢が無い場合を除外

      Checkbox checkbox = new();
      // すべての選択肢を登録
      for (int i = 0; i < args.Length; i ++)
      {
        checkbox.Add(args[i]);
      }

      int choice;

      do
      {
        ConsoleKeyInfo input = checkbox.Render(out choice);
        
        if (input.Key == ConsoleKey.Q) Environment.Exit(1);
        if (input.Key == ConsoleKey.Enter)
        {
          Console.WriteLine($"あなたが選んだのは、{args[choice]}です。");
          break;
        }

        if (input.Key == ConsoleKey.J) checkbox.ChooseNextChoice();
        if (input.Key == ConsoleKey.K) checkbox.ChoosePreviousChoice();
      } while (true);
    }
  }

  class Checkbox
  {
    private List<string> _checkboxItems;
    private int _currentChoice;
    private string selectedSymbol = "◉";
    private string unselectedSymbol = "◯";
    private int _availableBufferHeight;
    private bool _isRequireOffset;
    private int _offsetSize;

    public Checkbox()
    {
      _checkboxItems = new();
      _currentChoice = 0;
      _availableBufferHeight = Console.WindowHeight - 1;    // 最後の行は入力欄なので使用できない
      _offsetSize = 0;
      _isRequireOffset = false;
    }

    // 選択肢を登録
    public void Add(string item)
    {
      _checkboxItems.Add(item);
      if (!_isRequireOffset && _availableBufferHeight < _checkboxItems.Count)
      {
        _isRequireOffset = true;
      }
    }

    // 選択肢を描画
    public ConsoleKeyInfo Render(out int choice)
    {
      Refresh();
      choice = 0;

      if (_isRequireOffset)
      {
        for (int i = _offsetSize; i < _availableBufferHeight + _offsetSize; i ++)
        {
          if (i == _currentChoice) Console.WriteLine($"{selectedSymbol} {_checkboxItems[i]}");
          else Console.WriteLine($"{unselectedSymbol} {_checkboxItems[i]}");
        }
      }
      else
      {
        for (int i = 0; i < _checkboxItems.Count; i ++)
        {
          if (i == _currentChoice) Console.WriteLine($"{selectedSymbol} {_checkboxItems[i]}");
          else Console.WriteLine($"{unselectedSymbol} {_checkboxItems[i]}");
        }

        for (int i = 0; i < _availableBufferHeight - _checkboxItems.Count; i ++)
        {
          Console.WriteLine();
        }
      }


      if (_isRequireOffset) Console.Write($"[表示中 : {_offsetSize + 1}-{_availableBufferHeight + _offsetSize}/{_checkboxItems.Count}] 上: K, 下: J, 選択: Enter, 終了: Q  >>>  ");
      else Console.Write("上: K, 下: J, 選択: Enter, 終了: Q  >>>  ");

      ConsoleKeyInfo input = Console.ReadKey();
      Refresh();

      if (input.Key == ConsoleKey.Enter) choice = _currentChoice;

      return input;
    }

    // 選択を次に移動する
    // 既に一番下を選択している場合は、移動しない
    public void ChooseNextChoice()
    {
      if (_isRequireOffset)
      {
        if (_currentChoice >= _availableBufferHeight + _offsetSize - 1 && _currentChoice < _checkboxItems.Count - 1) _offsetSize++;
      }

      if (_currentChoice >= _checkboxItems.Count - 1) _currentChoice = _checkboxItems.Count - 1;
      else _currentChoice++;
    }

    // 選択を上に移動する
    // 既に一番上を選択している場合は、移動しない
    public void ChoosePreviousChoice()
    {
      if (_isRequireOffset)
      {
        if (_currentChoice <= _offsetSize && _currentChoice > 0) _offsetSize--;
      }

      if (_currentChoice <= 0) _currentChoice = 0;
      else _currentChoice--;
    }

    private void Refresh()
    {
      Console.Clear();
    }
  }
}
