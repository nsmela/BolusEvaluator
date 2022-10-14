using BolusEvaluator.ImageTools;
using BolusEvaluator.MVVM.ViewModels;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BolusEvaluator.Messages;

public class AddImageTool : ValueChangedMessage<IImageTool> {
    public AddImageTool(IImageTool value) : base(value) {
    }
}
