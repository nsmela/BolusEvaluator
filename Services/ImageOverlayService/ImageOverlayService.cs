using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BolusEvaluator.Services.ImageOverlayService;
public class ImageOverlayService : IImageOverlayService {
    public WriteableBitmap OverlayImage { get; private set; }
}

