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

using LiveChartsCore.Drawing;
using LiveChartsCore.Motion;
using SkiaSharp;

namespace LiveChartsCore.SkiaSharpView.Drawing.Geometries;

/// <summary>
/// Defines a rectangle geometry with a specified color.
/// </summary>
public class ColoredRectangleGeometry : BoundedDrawnGeometry, IColoredGeometry, IDrawnElement<SkiaSharpDrawingContext>
{
    private readonly ColorMotionProperty _colorProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColoredRectangleGeometry"/> class.
    /// </summary>
    public ColoredRectangleGeometry() : base()
    {
        _colorProperty = RegisterMotionProperty(new ColorMotionProperty(nameof(Color)));
    }

    /// <inheritdoc cref="IColoredGeometry.Color" />
    public LvcColor Color
    {
        get => _colorProperty.GetMovement(this);
        set => _colorProperty.SetMovement(value, this);
    }

    /// <inheritdoc cref="IDrawnElement{TDrawingContext}.Draw(TDrawingContext)" />
    public virtual void Draw(SkiaSharpDrawingContext context)
    {
        // it seems strange that this geometry modifies the paint of the context
        // but this geometry is normally used in heat maps, i guess we can live with it.

        var c = Color;
        var activePaint = context.ActiveSkiaPaint;

        activePaint.Color = new SKColor(c.R, c.G, c.B, c.A);

        context.Canvas.DrawRect(
            new SKRect { Top = Y, Left = X, Size = new SKSize { Height = Height, Width = Width } }, activePaint);
    }
}
