﻿using BolusEvaluator.Services.InputService;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BolusEvaluator.Controls;
public class ZoomBorder : Border {
    private UIElement child = null;
    private Point origin;
    private Point start;

    private double scaleMax = 3.0;
    private double transformMax = 128;

    private IInputService input;

    private TranslateTransform GetTranslateTransform(UIElement element) {
        return (TranslateTransform)((TransformGroup)element.RenderTransform)
          .Children.First(tr => tr is TranslateTransform);
    }

    private ScaleTransform GetScaleTransform(UIElement element) {
        return (ScaleTransform)((TransformGroup)element.RenderTransform)
          .Children.First(tr => tr is ScaleTransform);
    }

    public override UIElement Child {
        get { return base.Child; }
        set {
            if (value != null && value != this.Child)
                this.Initialize(value);
            base.Child = value;
        }
    }

    public void Initialize(UIElement element) {
        this.child = element;
        if (child != null) {
            TransformGroup group = new TransformGroup();
            ScaleTransform st = new ScaleTransform();
            group.Children.Add(st);
            TranslateTransform tt = new TranslateTransform();
            group.Children.Add(tt);
            child.RenderTransform = group;
            child.RenderTransformOrigin = new Point(0.0, 0.0);
            this.MouseWheel += child_MouseWheel;
            this.MouseRightButtonDown += child_MouseRightButtonDown;
            this.MouseRightButtonUp += child_MouseRightButtonUp;
            this.MouseMove += child_MouseMove;
            this.MouseLeftButtonDown += child_MouseLeftButtonDown;
            this.MouseLeftButtonUp += child_MouseLeftButtonUp;

            input = App.AppHost.Services.GetService<IInputService>();
        }
    }

    public void Reset() {
        if (child != null) {
            // reset zoom
            var st = GetScaleTransform(child);
            st.ScaleX = 1.0;
            st.ScaleY = 1.0;

            // reset pan
            var tt = GetTranslateTransform(child);
            tt.X = 0.0;
            tt.Y = 0.0;
        }
    }

    #region Child Events

    private void child_MouseWheel(object sender, MouseWheelEventArgs e) {
        if (child != null) {
            var st = GetScaleTransform(child);
            var tt = GetTranslateTransform(child);

            double zoom = e.Delta > 0 ? .2 : -.2;
            if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                return;

            Point relative = e.GetPosition(child);
            double absoluteX;
            double absoluteY;

            absoluteX = relative.X * st.ScaleX + tt.X;
            absoluteY = relative.Y * st.ScaleY + tt.Y;

            st.ScaleX = Math.Clamp(st.ScaleX + zoom, 1.0f, scaleMax);
            st.ScaleY = Math.Clamp(st.ScaleY + zoom, 1.0f, scaleMax);

            tt.X = absoluteX - relative.X * st.ScaleX;
            tt.Y = absoluteY - relative.Y * st.ScaleY;
        }
    }

    private void child_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
        if (child != null) {
            var tt = GetTranslateTransform(child);
            start = e.GetPosition(this);
            origin = new Point(tt.X, tt.Y);
            this.Cursor = Cursors.Hand;
            child.CaptureMouse();
        }
    }

    private void child_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
        if (child != null) {
            child.ReleaseMouseCapture();
            this.Cursor = Cursors.Arrow;
        }
    }

    private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        if (child != null) {
            input.Image_LeftMouseDown(GetImageCoordsAt(e));
        }
    }

    private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        if (child != null) {
            input.Image_LeftMouseUp(GetImageCoordsAt(e));
        }
    }

    private void child_MouseMove(object sender, MouseEventArgs e) {
        if (child != null) {
            if (child.IsMouseCaptured) {
                var tt = GetTranslateTransform(child);
                Vector v = start - e.GetPosition(this);
                tt.X = origin.X - v.X;
                tt.Y = origin.Y - v.Y;
            } else {
                input.Image_MouseMove(GetImageCoordsAt(e));
            }
        }
    }

    public Point GetImageCoordsAt(MouseButtonEventArgs e) => GetImageCoordsAt(e.GetPosition(child));

    public Point GetImageCoordsAt(MouseEventArgs e) => GetImageCoordsAt(e.GetPosition(child));

    public Point GetImageCoordsAt(Point point) {
        var controlSpacePosition = point;
        var imageControl = this.Child as Image;
        if (imageControl != null && imageControl.Source != null) {
            // Convert from control space to image space
            var x = Math.Floor(controlSpacePosition.X * imageControl.Source.Width / imageControl.ActualWidth);
            var y = Math.Floor(controlSpacePosition.Y * imageControl.Source.Height / imageControl.ActualHeight);

            return new Point(x, y);
        }
        return new Point(-1, -1);
    
    }
    #endregion
}
