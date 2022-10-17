using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BolusEvaluator.Services.InputService;
internal class InputService : IInputService {
    public event Action<Point> OnImageLeftMouseDown;
    public event Action<Point> OnImageLeftMouseUp;
    public event Action<Point> OnImageMouseMove;

    public void Image_LeftMouseDown(Point mousePoint) {
        OnImageLeftMouseDown?.Invoke(mousePoint);
    }

    public void Image_LeftMouseUp(Point mousePoint) {
        OnImageLeftMouseUp?.Invoke(mousePoint);
    }

    public void Image_MouseMove(Point mousePoint) {
        OnImageMouseMove?.Invoke(mousePoint);
    }
}

