// The MIT License(MIT)
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

using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Geo;
using LiveChartsCore.Kernel;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.VisualElements;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CoreTests.ChartTests;

// GeoVisualElement took a VisualElement only, so a marker had to be built on the older, partly
// obsolete family: the markers sample used GeometryVisual, which is [Obsolete]. It positions the
// inner visual by writing its location, and a Visual has none of its own -- its location lives on
// the drawn element -- which is why the newer family did not fit.
[TestClass]
public class GeoVisualElementTests
{
    private const double Longitude = -74.00;
    private const double Latitude = 40.71;
    private const float Size = 14f;

    private class MarkerVisual : Visual
    {
        public CircleGeometry Circle { get; } = new()
        {
            Width = Size,
            Height = Size,
            Fill = new SolidColorPaint(SKColors.Red)
        };

        protected internal override IDrawnElement? DrawnElement => Circle;

        protected override void Measure(Chart chart)
        { }
    }

    private static SKGeoMap BuildMap(params IChartElement[] visuals) =>
        new()
        {
            Width = 800,
            Height = 600,
            MapProjection = MapProjection.Mercator,
            Series = [new HeatLandSeries { Lands = [new() { Name = "usa", Value = 15 }] }],
            VisualElements = visuals
        };

    [TestMethod]
    public void VisualIsProjectedToTheSamePixelAsAVisualElement()
    {
        // Parity is the whole contract: the new branch must place a Visual exactly where the old
        // branch places a VisualElement for the same coordinate.
        var visual = new MarkerVisual();
        var visualElement = new GeometryVisual<CircleGeometry>
        {
            Width = Size,
            Height = Size,
            Fill = new SolidColorPaint(SKColors.Red),
            LocationUnit = MeasureUnit.Pixels,
            SizeUnit = MeasureUnit.Pixels
        };

        var map = BuildMap(
            new GeoVisualElement(visual) { Longitude = Longitude, Latitude = Latitude },
            new GeoVisualElement(visualElement) { Longitude = Longitude, Latitude = Latitude });

        _ = map.GetImage();

        Assert.AreEqual(
            (float)visualElement.X, visual.Circle.X,
            "A Visual must be projected to the same X as a VisualElement.");
        Assert.AreEqual(
            (float)visualElement.Y, visual.Circle.Y,
            "A Visual must be projected to the same Y as a VisualElement.");
    }

    [TestMethod]
    public void VisualIsMovedFromItsOrigin()
    {
        // Guards the parity test above: if neither family were positioned, both would sit at 0
        // and the assert would still pass.
        var visual = new MarkerVisual();

        var map = BuildMap(
            new GeoVisualElement(visual) { Longitude = Longitude, Latitude = Latitude });

        _ = map.GetImage();

        Assert.AreNotEqual(0f, visual.Circle.X, "The visual must be projected, not left at its origin.");
        Assert.AreNotEqual(0f, visual.Circle.Y, "The visual must be projected, not left at its origin.");
    }

    [TestMethod]
    public void DifferentCoordinatesLandOnDifferentPixels()
    {
        var newYork = new MarkerVisual();
        var tokyo = new MarkerVisual();

        var map = BuildMap(
            new GeoVisualElement(newYork) { Longitude = Longitude, Latitude = Latitude },
            new GeoVisualElement(tokyo) { Longitude = 139.69, Latitude = 35.69 });

        _ = map.GetImage();

        Assert.AreNotEqual(
            newYork.Circle.X, tokyo.Circle.X,
            "Each coordinate must be projected on its own.");
    }
}
