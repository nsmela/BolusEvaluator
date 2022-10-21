using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.Services.ImageOverlayService;
public class ImageOverlayService : IImageOverlayService {
    public WriteableBitmap OverlayImage { get; private set; }

    public event Action OnImageUpdated;

    public void AppendImage(WriteableBitmap newBitmap) {
        //added given image to existing image

        newBitmap.ForEach((x, y, color) => {
            OverlayImage.SetPixel(x, y, color);
            return Colors.White;
        });
        OnImageUpdated?.Invoke();
    }

    public void SetImage(WriteableBitmap newBitmap) {
        //sets image to given image
        OverlayImage = newBitmap;
        OnImageUpdated?.Invoke();
    }

    public void ClearImage() {
        OverlayImage = BitmapFactory.New(OverlayImage.PixelWidth, OverlayImage.PixelHeight);
        OnImageUpdated?.Invoke();
    }

}

