using System;
using System.IO;
using System.Drawing;
using Microsoft.Extensions.CommandLineUtils;
using Lapis.QRCode.Encoding;
using Lapis.QRCode.Imaging;
using Lapis.QRCode.Imaging.Drawing;
using Lapis.QRCode.Art;
using System.Collections.Generic;
using System.Linq;

namespace Lapis.QrArt
{
    partial class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication(false);
            app.Name = "qr-art";
            app.Description = "An artistic QR code generator.";
            app.HelpOption("-?|-h|--help");
            app.VersionOption("-v|--version", "qr-art 1.0");

            var contentArg = app.Argument("content", "Text to encode.");
            var imageArg = app.Argument("image", "An image to be used as background.");
            var formatArg = app.Argument("format", "Output image format. [png|gif|svg]");
            var pathArg = app.Argument("outpath", "Output path.");
            var typeOpt = app.Option("-t|--type <type>", "Type number of QR code. [1-39]", CommandOptionType.SingleValue);
            var errcorOpt = app.Option("-e|--errcor <level>", "Error correct level. [L|M|Q|H]", CommandOptionType.SingleValue);
            var foregdOpt = app.Option("-f|--foreground <color>", "Foreground color.", CommandOptionType.SingleValue);
            var backgdOpt = app.Option("-b|--background <color>", "Background color.", CommandOptionType.SingleValue);
            var cellOpt = app.Option("-c|--cell <size>", "Cell size.", CommandOptionType.SingleValue);
            var marginOpt = app.Option("-m|--margin <margin>", "Margin.", CommandOptionType.SingleValue);
            var animationOpt = app.Option("-a|--animation", "Generate animated QR code.", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                if (!animationOpt.HasValue())
                {                    
                    if (CheckContent(contentArg.Value) &&
                        CheckImagePath(imageArg.Value, out var bitmap) &&
                        CheckFormat(formatArg.Value, out var drawer) &&
                        CheckType(typeOpt.Value(), out var type) &&
                        CheckErrorCorrectLevel(errcorOpt.Value(), out var errcor) &&
                        CheckForeground(foregdOpt.Value(), out var foregd) &&
                        CheckBackground(backgdOpt.Value(), out var backgd) &&
                        CheckCell(cellOpt.Value(), out var cell) &&
                        CheckMargin(marginOpt.Value(), out var margin))
                    {
                        var builder = new QRArtCreator(
                            new QRCodeEncoder()
                            {
                                TypeNumber = type,
                                ErrorCorrectLevel = errcor
                            },
                            new Binarizer(),
                            new Merger(),
                            drawer
                        );
                        {
                            drawer.CellSize = cell;
                            drawer.Margin = margin;
                            drawer.Foreground = foregd;
                            drawer.Background = backgd;
                        }

                        var image = builder.Create(contentArg.Value, bitmap);
                        
                        Write(image, pathArg.Value ??
                            (imageArg.Value == null ? "output." + formatArg.Value :
                            Path.Combine(Path.GetDirectoryName(imageArg.Value), 
                            Path.GetFileNameWithoutExtension(imageArg.Value) + "_output." + formatArg.Value)));
                        (bitmap as IDisposable)?.Dispose();
                        (image as IDisposable)?.Dispose();
                    }
                    else
                        app.ShowHelp();
                }
                else
                {
                    if (CheckContent(contentArg.Value) &&
                        CheckImagePathAnimation(imageArg.Value, out var animation) &&
                        CheckFormatAnimation(formatArg.Value, out var drawer) &&
                        CheckType(typeOpt.Value(), out var type) &&
                        CheckErrorCorrectLevel(errcorOpt.Value(), out var errcor) &&
                        CheckForeground(foregdOpt.Value(), out var foregd) &&
                        CheckBackground(backgdOpt.Value(), out var backgd) &&
                        CheckCell(cellOpt.Value(), out var cell) &&
                        CheckMargin(marginOpt.Value(), out var margin))
                    {               
                        var builder = new QRAnimationCreator(
                            new QRCodeEncoder()
                            {
                                TypeNumber = type,
                                ErrorCorrectLevel = errcor
                            },
                            new Binarizer(),
                            new Merger(),
                            drawer,
                            // frames => new BitmapImage(frames.Select(f => f as BitmapFrame))
                            frames => new Rgb24Bitmap(frames.Select(f => f as Rgb24BitmapFrame))
                        );
                        {
                            drawer.CellSize = cell;
                            drawer.Margin = margin;
                            drawer.Foreground = foregd;
                            drawer.Background = backgd;
                        }

                        var image = builder.Create(contentArg.Value, animation);
                                                
                        Write(image, pathArg.Value ??
                            (imageArg.Value == null ? "output." + formatArg.Value :
                            Path.Combine(Path.GetDirectoryName(imageArg.Value), 
                            Path.GetFileNameWithoutExtension(imageArg.Value) + "_output." + formatArg.Value)));
                        (animation as IDisposable)?.Dispose();
                        (image as IDisposable)?.Dispose();
                    }
                    else
                        app.ShowHelp();
                }
                return 0;
            });

            app.Execute(args);
        }

        private static void LogError(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = color;
        }
    }
}
