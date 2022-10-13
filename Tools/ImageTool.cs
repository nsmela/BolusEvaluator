using BolusEvaluator.MVVM.ViewModels;
using FellowOakDicom.Imaging.Render;
using FellowOakDicom.Imaging;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows;
using System;
using FellowOakDicom;

namespace BolusEvaluator.ImageTools;
    public class HighlightImageWindow : IImageTool {


            
public void Execute(ImageViewModel viewModel) {
        if (_dicomData is null) return;
        // Define parameters used to create the BitmapSource.
        // Initialize the image with data.
        var frame = _dicomData[CurrentFrame];
        var header = DicomPixelData.Create(frame);
        var pixelMap = (PixelDataFactory.Create(header, 0));

        double pixel;
        var bitmap = new Bitmap(pixelMap.Width, pixelMap.Height);
        for (int x = 0; x < pixelMap.Width; x++) {
            for (int y = 0; y < pixelMap.Height; y++) {
                pixel = GetHUValue(pixelMap, x, y);
                if (pixel > LowerWindowValue && pixel < UpperWindowValue) bitmap.SetPixel(x, y, Color.Red);
                else bitmap.SetPixel(x, y, Color.Black);
            }
        }

        LayerImage = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                  bitmap.GetHbitmap(),
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  BitmapSizeOptions.FromEmptyOptions());
    }

 
}


public interface IImageTool {
    public void Execute(ImageViewModel viewModel);

}