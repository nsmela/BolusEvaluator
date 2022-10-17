
using System;
using System.Windows;

namespace BolusEvaluator.Services.InputService;
    public interface IInputService {
    event Action<Point> OnImageLeftMouseDown, OnImageLeftMouseUp, OnImageMouseMove;

    void Image_LeftMouseDown(Point mousePoint);
    void Image_LeftMouseUp(Point mousePoint);
    void Image_MouseMove(Point mousePoint);

}

