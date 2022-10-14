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

    public string Label => "HighlightImageWindow";

    public void Execute(ImageViewModel viewModel) {
        var dicomData = viewModel.GetCurrentFrameData();
        if (dicomData is null) return;
        // Define parameters used to create the BitmapSource.
        // Initialize the image with data.
        var header = DicomPixelData.Create(dicomData);
        var pixelMap = (PixelDataFactory.Create(header, 0));

        double pixel;
        var bitmap = new Bitmap(pixelMap.Width, pixelMap.Height);
        for (int x = 0; x < pixelMap.Width; x++) {
            for (int y = 0; y < pixelMap.Height; y++) {
                pixel = viewModel.GetHUValue(pixelMap, x, y);
                if (pixel > viewModel.LowerWindowValue && pixel < viewModel.UpperWindowValue) bitmap.SetPixel(x, y, Color.Red);
            }
        }

        viewModel.LayerImage = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                  bitmap.GetHbitmap(),
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  BitmapSizeOptions.FromEmptyOptions());
    }
}


public interface IImageTool {
    public string Label { get; }
    public void Execute(ImageViewModel viewModel);

}