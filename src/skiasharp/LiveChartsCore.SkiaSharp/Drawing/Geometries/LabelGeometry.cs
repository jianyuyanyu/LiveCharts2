﻿// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace LiveChartsCore.SkiaSharpView.Drawing.Geometries;

/// <inheritdoc cref="BaseLabelGeometry" />
public class LabelGeometry : BaseLabelGeometry, IDrawnElement<SkiaSharpDrawingContext>
{
    internal float _maxTextHeight = 0f;
    internal int _lines;

    /// <summary>
    /// Initializes a new instance of the <see cref="LabelGeometry"/> class.
    /// </summary>
    public LabelGeometry()
    {
        TransformOrigin = new LvcPoint(0f, 0f);
    }

    /// <inheritdoc cref="IDrawnElement{TDrawingContext}.Draw(TDrawingContext)" />
    public virtual void Draw(SkiaSharpDrawingContext context)
    {
        var paint = context.ActiveSkiaPaint;
        paint.TextSize = TextSize;

        var p = Padding;
        var bg = Background;

        var size = Measure();

        var isFirstLine = true;
        var verticalPos =
            _lines > 1
                ? VerticalAlign switch // respect alignment on multiline labels
                {
                    Align.Start => 0,
                    Align.Middle => -_lines * _maxTextHeight * 0.5f,
                    Align.End => -_lines * _maxTextHeight,
                    _ => 0
                }
                : 0;

        var textBounds = new SKRect();
        var shaper = paint.Typeface is not null ? new SKShaper(paint.Typeface) : null;

        foreach (var line in GetLines(context.ActiveSkiaPaint))
        {
            _ = paint.MeasureText(line, ref textBounds);

            var lhd = (textBounds.Height * LineHeight - _maxTextHeight) * 0.5f;
            var ao = GetAlignmentOffset(textBounds);

            if (isFirstLine && bg != LvcColor.Empty)
            {
                var c = new SKColor(bg.R, bg.G, bg.B, (byte)(bg.A * Opacity));
                using var bgPaint = new SKPaint { Color = c };

                context.Canvas.DrawRect(
                    X + ao.X, Y + ao.Y - textBounds.Height, size.Width, size.Height, bgPaint);
            }

            if (paint.Typeface is not null)
            {
                context.Canvas.DrawShapedText(
                    shaper, line, X + ao.X + p.Left, Y + ao.Y + p.Top + lhd + verticalPos, paint);
            }
            else
            {
                context.Canvas.DrawText(line, X + ao.X + p.Left, Y + ao.Y + p.Top + lhd + verticalPos, paint);
            }

#if DEBUG
            if (ShowDebugLines)
            {
                using var r = new SKPaint { Color = new SKColor(255, 0, 0), Style = SKPaintStyle.Stroke };
                using var b = new SKPaint { Color = new SKColor(0, 0, 255), Style = SKPaintStyle.Stroke };

                context.Canvas.DrawRect(
                    X + ao.X,
                    Y + ao.Y - textBounds.Height + verticalPos,
                    textBounds.Width + Padding.Left + Padding.Right,
                    textBounds.Height * LineHeight + Padding.Top + Padding.Bottom,
                    r);

                context.Canvas.DrawRect(
                    X + ao.X + p.Left,
                    Y + ao.Y - textBounds.Height + p.Top + verticalPos,
                    textBounds.Width,
                    textBounds.Height * LineHeight,
                    b);
            }
#endif

            verticalPos += _maxTextHeight * LineHeight;
            isFirstLine = false;
        }

        shaper?.Dispose();
    }

    /// <inheritdoc cref="DrawnGeometry.Measure()" />
    public override LvcSize Measure()
    {
        if (Paint is null)
            throw new Exception(
                $"A paint is required to measure a label, please set the {nameof(Paint)} " +
                $"property with the paint that is drawing the label.");

        var skiaPaint = (SkiaPaint)Paint;
        var typeface = skiaPaint.GetSKTypeface();

        using var p = new SKPaint
        {
            IsAntialias = skiaPaint.IsAntialias,
            StrokeWidth = skiaPaint.StrokeThickness,
            TextSize = TextSize,
            Typeface = typeface
        };

        var w = 0f;
        _maxTextHeight = 0f;
        _lines = 0;

        foreach (var line in GetLines(p))
        {
            var bounds = new SKRect();
            _ = p.MeasureText(line, ref bounds);

            if (bounds.Width > w) w = bounds.Width;
            if (bounds.Height > _maxTextHeight) _maxTextHeight = bounds.Height;
            _lines++;
        }

        var h = _maxTextHeight * _lines * LineHeight;

        // Note #301222
        // Disposing typefaces could cause render issues (Blazor) at least on SkiaSharp (2.88.3)
        // Could this cause memory leaks?
        // Should the user dispose typefaces manually?
        // typeface.Dispose();

        var size = new LvcSize(
            w + Padding.Left + Padding.Right,
            h + Padding.Top + Padding.Bottom);

        return size.GetRotatedSize(RotateTransform);
    }

    internal IEnumerable<string> GetLines(SKPaint paint)
    {
        IEnumerable<string> lines = Text.Split([Environment.NewLine], StringSplitOptions.None);

        if (MaxWidth != float.MaxValue)
            lines = lines.SelectMany(x => GetLinesByMaxWidth(x, paint));

        return lines;
    }

    private IEnumerable<string> GetLinesByMaxWidth(string source, SKPaint paint)
    {
        // DISCLAIM ====================================================================
        // WE ARE USING A DOUBLE STRING BUILDER, AND MEASURE THE REAL STRING EVERY TIME
        // BECAUSE IT SEEMS THAT THE SKIA MEASURE TEXT IS INCONSISTENT, FOR EXAMPLE:

        //using var p = new SKPaint() { Color = SKColors.Black, TextSize = 15 };
        //var b = new SKRect();
        //_ = p.MeasureText("nullam. Ut tellus", ref b);

        //var w1 = b.Width;

        //var w2 = 0f;
        //_ = p.MeasureText("nullam.", ref b);
        //w2 += b.Width;
        //_ = p.MeasureText(" Ut", ref b);
        //w2 += b.Width;
        //_ = p.MeasureText(" tellus", ref b);
        //w2 += b.Width;

        //Assert.IsTrue(w1 == w2); THIS IS FALSE!!!!

        var sb = new StringBuilder();
        var sb2 = new StringBuilder();
        var words = source.Split([" ", Environment.NewLine], StringSplitOptions.None);
        var bounds = new SKRect();
        var mw = MaxWidth - Padding.Left - Padding.Right;

        foreach (var word in words)
        {
            _ = sb2.Clear();
            _ = sb2.Append(sb);
            _ = sb2.Append(' ');
            _ = sb2.Append(word);
            _ = paint.MeasureText(sb2.ToString(), ref bounds);

            // if the line has already content and the new word exceeds the max width
            // then we create a new line
            if (sb.Length > 0 && bounds.Width > mw)
            {
                yield return sb.ToString();
                _ = sb.Clear();
            }

            if (sb.Length > 0) _ = sb.Append(' ');
            _ = sb.Append(word);
        }

        if (sb.Length > 0) yield return sb.ToString();
    }

    private LvcPoint GetAlignmentOffset(SKRect bounds)
    {
        var p = Padding;

        var w = bounds.Width + p.Left + p.Right;
        var h = bounds.Height * LineHeight + p.Top + p.Bottom;

        float l = -bounds.Left, t = -bounds.Top;

        switch (VerticalAlign)
        {
            case Align.Start: t += 0; break;
            case Align.Middle: t -= h * 0.5f; break;
            case Align.End: t -= h + 0; break;
            default:
                break;
        }
        switch (HorizontalAlign)
        {
            case Align.Start: l += 0; break;
            case Align.Middle: l -= w * 0.5f; break;
            case Align.End: l -= w + 0; break;
            default:
                break;
        }

        return new(l, t);
    }
}
