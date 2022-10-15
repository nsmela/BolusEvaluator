using BolusEvaluator.MVVM.ViewModels;
using FellowOakDicom.Imaging.Render;
using FellowOakDicom.Imaging;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows;
using System;
using FellowOakDicom;
using System.Windows.Media;

namespace BolusEvaluator.ImageTools;
public class HighlightImageWindow : IImageTool {

    public string Label => "HighlightImageWindow";

    //write over pixels
    public void Execute(ImageViewModel viewModel) {
       /* if (viewModel.LayerImage is null) return;
        var dicomData = _dicom
        if (dicomData is null) return;

        // Define parameters used to create the BitmapSource.
        // Initialize the image with data.
        var header = DicomPixelData.Create(dicomData);
        var pixelMap = (PixelDataFactory.Create(header, 0));

        double pixel;
        var bitmap = BitmapFactory.New(pixelMap.Width, pixelMap.Height);
        for (int x = 0; x < pixelMap.Width; x++) {
            for (int y = 0; y < pixelMap.Height; y++) {
                pixel = viewModel.GetHUValue(pixelMap, x, y);
                if (pixel > viewModel.LowerWindowValue && pixel < viewModel.UpperWindowValue) 
                    bitmap.SetPixel(x, y, Colors.Red);
            }
        }

        viewModel.LayerImage = bitmap; */
    }
}


public interface IImageTool {
    public string Label { get; }
    public void Execute(ImageViewModel viewModel);

}