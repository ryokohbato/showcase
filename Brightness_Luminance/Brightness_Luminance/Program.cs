using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Brightness_Luminance
{
  class Program
  {
    /// <summary>
    /// コマンドラインオプションは以下の通りに指定: 
    /// targetFileName -o outputFileName [-f 0|1]
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
      if (args.Length == 0)
      {
        Console.WriteLine("対象となるファイルを指定してください。");
        Environment.Exit(1);
      }

      IImageData imageData = new ImageData(args[0], out bool result);
      // インスタンス作成が失敗した場合は、終了する。
      if (!result) Environment.Exit(1);

      //動作オプションの設定
      RunOption.Set(args);

      imageData.ApplyFilter(byte.Parse(RunOption.Get()["filter"]));

      // 画像を保存
      imageData.Save(RunOption.Get()["outputDir"]);
    }
  }

  /// <summary>
  /// ImageDataクラスにアクセスするためのインターフェイス
  /// </summary>
  interface IImageData
  {
    byte GetBitDepth();
    void ApplyFilter(byte filter);
    void Save(string path);
  }

  /// <summary>
  /// 与えられたパスに対して画像データを保存している
  /// </summary>
  public class ImageData : IImageData
  {
    private Bitmap imageData;

    /// <summary>
    /// ファイルが存在しない場合と、画像として読み込めなかった場合は、resultがfalseになる。
    /// </summary>
    /// <param name="path"></param>
    /// <param name="result"></param>
    public ImageData(string path, out bool result)
    {
      result = false;

      if (!File.Exists(path))
      {
        Console.WriteLine($"ファイル {path} は存在しません。");
        return;
      }

      try
      {
        imageData = new Bitmap(path);
      }
      catch (Exception)
      {
        Console.WriteLine($"ファイル {path} を読み込めませんでした。");
        return;
      }

      result = true;
    }

    /// <summary>
    /// 画像を指定パスで保存
    /// </summary>
    /// <param name="path"></param>
    public void Save(string path)
    {
      try
      {
        imageData.Save(path);
      }
      catch (Exception)
      {
        Console.WriteLine($"画像を {path} に保存できませんでした。");
        Environment.Exit(1);
      }
    }

    /// <summary>
    /// 画像のビット深度を取得
    /// </summary>
    /// <returns></returns>
    public byte GetBitDepth()
    {
      return (byte)Image.GetPixelFormatSize(imageData.PixelFormat);
    }

    /// <summary>
    /// 画像にフィルタを適用する。
    /// </summary>
    /// <param name="filterCode"></param>
    public void ApplyFilter(byte filterCode)
    {
      // LockBitsでメモリ展開(高速化)
      BitmapData bitmapData = imageData.LockBits(
          new Rectangle(0, 0, imageData.Width, imageData.Height),
          ImageLockMode.ReadWrite,    // 書き込みも行う
          imageData.PixelFormat);

      // ビットマップの保存形式には、ボトムアップとトップダウンの2種類がある模様。
      // http://hp.vector.co.jp/authors/VA033743/swellhelp/func_option_bmp.html
      // bitmapData.Strideがマイナスの場合がボトムアップ
      // 今時のビットマップは大体トップダウンという認識で良いのだろうか、私の手元にボトムアップの画像は見当たらなかった。
      if (bitmapData.Stride < 0)
      {
        Console.WriteLine("ボトムアップはパス");
        imageData.UnlockBits(bitmapData);
        Environment.Exit(1);
      }

      if (GetBitDepth() != 24)
      {
        imageData.UnlockBits(bitmapData);
        Console.WriteLine("24bit画像でないため、処理を中断します。");
        Environment.Exit(1);
      }

      // ******** 以下、24bitのみに対応したコード ********

      // ここに画像データをコピー
      // Stride は、1行のピクセルの幅を表す。(8bit単位、ビット深度が24bitの場合はピクセル幅のほぼ3倍になる。)
      byte[] pixels = new byte[bitmapData.Stride * imageData.Height];

      // 最初のピクセルデータのアドレス
      IntPtr ptr = bitmapData.Scan0;

      // ポインタのデータを用意したbyte[]にコピー
      Marshal.Copy(ptr, pixels, 0, pixels.Length);

      for (int h = 0; h < bitmapData.Height; h++)
      {
        for (int w = 0; w < bitmapData.Width; w++)
        {
          int pixelPosition = h * bitmapData.Stride + w * GetBitDepth() / 8;

          // B, G, R の順に格納されている。
          Color pixelColor
            = Color.FromArgb(255, pixels[pixelPosition + 2], pixels[pixelPosition + 1], pixels[pixelPosition]);

          switch (filterCode)
          {
            // 明度(0～255)をRGBに割り当てる
            case (byte)ImageFilter.Code.InvertingBrightness:
              pixels[pixelPosition] = (byte)RGBColor.GetBrightness(pixelColor);
              pixels[pixelPosition + 1] = (byte)RGBColor.GetBrightness(pixelColor);
              pixels[pixelPosition + 2] = (byte)RGBColor.GetBrightness(pixelColor);
              break;

            // 輝度(0～255)をRGBに割り当てる
            case (byte)ImageFilter.Code.InvertingLuminance:
              pixels[pixelPosition] = (byte)RGBColor.GetLuminance(pixelColor);
              pixels[pixelPosition + 1] = (byte)RGBColor.GetLuminance(pixelColor);
              pixels[pixelPosition + 2] = (byte)RGBColor.GetLuminance(pixelColor);
              break;

            default:
              break;
          }
        }
      }

      // byte[]のデータをポインタに戻す
      Marshal.Copy(pixels, 0, ptr, pixels.Length);

      imageData.UnlockBits(bitmapData);
    }
  }

  public class RGBColor
  {
    /// <summary>
    /// 明度を0から255の範囲で取得。
    /// 明度はRGBのうちの最大値で表される。
    /// </summary>
    /// <param name="rgb"></param>
    /// <returns></returns>
    public static double GetBrightness(Color rgb)
    {
      return RGBColor.Max(rgb.R, rgb.G, rgb.B);
    }

    /// <summary>
    /// 輝度を取得。
    /// 輝度は経験則により、以下のRGBの加重平均で表される。
    /// 0.298912 * r + 0.586611 * g + 0.114478 * b
    /// </summary>
    /// <param name="rgb"></param>
    /// <returns></returns>
    public static double GetLuminance(Color rgb)
    {
      return 0.298912 * rgb.R + 0.586611 * rgb.G + 0.114478 * rgb.B;
    }

    /// <summary>
    /// 最大値を返す。
    /// </summary>
    /// <param name="num1"></param>
    /// <param name="num2"></param>
    /// <param name="num3"></param>
    /// <returns></returns>
    private static byte Max(byte num1, byte num2, byte num3)
    {
      return Math.Max(num1, Math.Max(num2, num3));
    }
  }

  /// <summary>
  /// フィルタ処理のオプションを管理
  /// </summary>
  public class ImageFilter
  {
    /// <summary>
    /// フィルタオプション
    /// </summary>
    public enum Code : byte
    {
      InvertingBrightness = 0,
      InvertingLuminance = 1,
    }
  }

  /// <summary>
  /// 動作オプションを管理
  /// </summary>
  public class RunOption
  {
    private static readonly Dictionary<string, string> option = new Dictionary<string, string>()
    {
      { "filter", "1" },
      { "outputDir", string.Empty },
    };

    /// <summary>
    /// 動作オプションを設定
    /// </summary>
    /// <param name="args"></param>
    public static void Set(string[] args)
    {
      for (int i = 0; i < args.Length; i++)
      {
        switch (args[i])
        {
          case "-f":
            if (args.Length != i + 1)
            {
              if (byte.TryParse(args[i + 1], out byte filterCode))
              {
                if (Enum.IsDefined(typeof(ImageFilter.Code), filterCode))
                {
                  RunOption.option["filter"] = args[i + 1];
                  break;
                }
              }

              Console.WriteLine($"フィルタオプション {args[i + 1]} は有効な値ではありません。規定値1を使用します。");
              break;
            }

            Console.WriteLine("フィルタオプションが指定されていません。規定値1を使用します。");
            break;
          case "-o":
            if (args.Length != i + 1)
            {
              RunOption.option["outputDir"] = args[i + 1];
              break;
            }

            Console.WriteLine("出力ファイルパスが指定されていません。");
            Environment.Exit(1);
            break;
          default:
            break;
        }
      }

      if (RunOption.option["outputDir"] == string.Empty)
      {
        Console.WriteLine("出力ファイルパスが指定されていません。");
        Environment.Exit(1);
      }
    }

    /// <summary>
    /// 動作オプションを取得
    /// </summary>
    /// <returns></returns>
    public static ReadOnlyDictionary<string, string> Get()
    {
      return new ReadOnlyDictionary<string, string>(option);
    }
  }
}
