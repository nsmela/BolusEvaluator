using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.Services.ImageOverlayService {
    public interface IImageOverlayService {
        event Action OnImageUpdated; //called image to be updated

        public WriteableBitmap OverlayImage { get; } 

        void SetImage(WriteableBitmap newBitmap);
        void AppendImage(WriteableBitmap newBitmap);
        void ClearImage();
    }
}
