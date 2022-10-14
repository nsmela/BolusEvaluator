using BolusEvaluator.ImageTools;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BolusEvaluator.Messages;

public class DeleteImageTool : ValueChangedMessage<IImageTool> {
    public DeleteImageTool(IImageTool value) : base(value) {
    }
}