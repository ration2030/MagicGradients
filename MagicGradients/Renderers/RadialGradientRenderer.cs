﻿using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Linq;

namespace MagicGradients.Renderers
{
    public class RadialGradientRenderer
    {
        private readonly RadialGradient _gradient;

        public RadialGradientRenderer(RadialGradient gradient)
        {
            _gradient = gradient;
        }

        public void Render(RenderContext context)
        {
            var info = context.Info;

            var center = GetCenter(info.Width, info.Height);
            var (radiusX, radiusY) = GetRadius(center, info);
            var radius = Math.Min(radiusX, radiusY);

            var orderedStops = _gradient.Stops.OrderBy(x => x.Offset).ToArray();
            var colors = orderedStops.Select(x => x.Color.ToSKColor()).ToArray();
            var colorPos = orderedStops.Select(x => x.Offset).ToArray();

            var shader = SKShader.CreateRadialGradient(
                center,
                radius, 
                colors, 
                colorPos,
                SKShaderTileMode.Clamp,
                GetScaleMatrix(center, radiusX, radiusY));

            context.Paint.Shader = shader;
            context.Canvas.DrawRect(info.Rect, context.Paint);
        }

        private SKPoint GetCenter(int width, int height)
        {
            var point = _gradient.Center.ToSKPoint();

            var xIsProportional = IsProportional(RadialGradientFlags.XProportional);
            var yIsProportional = IsProportional(RadialGradientFlags.YProportional);

            return new SKPoint(
                xIsProportional ? width * point.X : point.X,
                yIsProportional ? height * point.Y : point.Y);
        }

        private (float, float) GetRadius(SKPoint center, SKImageInfo info)
        {
            var radiusX = 0f;
            var radiusY = 0f;

            if (_gradient.Shape == RadialGradientShape.Ellipse)
            {
                var distances = GetDistanceInPoints(center, info);

                // Closest
                if ((int)_gradient.ShapeSize < 3)
                {
                    radiusX = distances.Where(x => IsNotEmpty(x.X)).OrderBy(x => x.Length).Select(x => Math.Abs(x.X)).First();
                    radiusY = distances.Where(x => IsNotEmpty(x.Y)).OrderBy(x => x.Length).Select(x => Math.Abs(x.Y)).First();
                }
                // Farthest
                else
                {
                    radiusX = distances.Where(x => IsNotEmpty(x.X)).OrderByDescending(x => x.Length).Select(x => Math.Abs(x.X)).First();
                    radiusY = distances.Where(x => IsNotEmpty(x.Y)).OrderByDescending(x => x.Length).Select(x => Math.Abs(x.Y)).First();
                }

                bool IsNotEmpty(float value) => Math.Abs(value) > 0.0001;
            }

            if (_gradient.Shape == RadialGradientShape.Circle)
            {
                var distances = GetEuclideanDistance(center, info);
                var distance = (int)_gradient.ShapeSize < 3 ? distances.Min() : distances.Max();

                radiusX = distance;
                radiusY = distance;
            }

            if (_gradient.RadiusX > -1)
            {
                var widthIsProportional = IsProportional(RadialGradientFlags.WidthProportional);
                radiusX = widthIsProportional ? info.Width * _gradient.RadiusX : _gradient.RadiusX;
            }

            if (_gradient.RadiusY > -1)
            {
                var heightIsProportional = IsProportional(RadialGradientFlags.HeightProportional);
                radiusY = heightIsProportional ? info.Height * _gradient.RadiusY : _gradient.RadiusY;
            }
            
            return (radiusX, radiusY);
        }

        private SKPoint[] GetCornerPoints(SKImageInfo info)
        {
            var points = new[]
            {
                new SKPoint(info.Rect.Left, info.Rect.Top),     // leftTop
                new SKPoint(info.Rect.Right, info.Rect.Top),    // rightTop
                new SKPoint(info.Rect.Right, info.Rect.Bottom), // rightBottom
                new SKPoint(info.Rect.Left, info.Rect.Bottom)   // leftBottom
            };

            return points;
        }

        private SKPoint[] GetSidePoints(SKPoint center, SKImageInfo info)
        {
            var points = new[]
            {
                new SKPoint(info.Rect.Left, center.Y),      // left
                new SKPoint(center.X, info.Rect.Top),       // top
                new SKPoint(info.Rect.Right, center.Y),     // right
                new SKPoint(center.X, info.Rect.Bottom)     // bottom
            };

            return points;
        }

        private SKPoint[] GetDistanceInPoints(SKPoint center, SKImageInfo info)
        {
            var points = (int)_gradient.ShapeSize % 2 == 0 ?
                GetCornerPoints(info) :
                GetSidePoints(center, info);

            var distances = new SKPoint[points.Length];

            for (var i = 0; i < distances.Length; i++)
            {
                distances[i] = center - points[i];
            }

            return distances;
        }

        private float[] GetEuclideanDistance(SKPoint center, SKImageInfo info)
        {
            var points = (int)_gradient.ShapeSize % 2 == 0 ?
                GetCornerPoints(info) :
                GetSidePoints(center, info);

            var distances = new float[points.Length];

            for (var i = 0; i < distances.Length; i++)
            {
                distances[i] = SKPoint.Distance(center, points[i]);
            }

            return distances;
        }

        private SKMatrix GetScaleMatrix(SKPoint center, float radiusX, float radiusY)
        {
            if (radiusX > radiusY)
            {
                return SKMatrix.MakeScale(radiusX / radiusY, 1f, center.X, center.Y);
            }

            if (radiusY > radiusX)
            {
                return SKMatrix.MakeScale(1f, radiusY / radiusX, center.X, center.Y);
            }

            return SKMatrix.MakeIdentity();
        }

        private bool IsProportional(RadialGradientFlags flag) => (_gradient.Flags & flag) != 0;
    }
}
