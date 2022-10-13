
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BolusEvaluator.Messages;
public class ShowOverlayMessage : ValueChangedMessage<bool> {
    public ShowOverlayMessage(bool value) : base(value) {
    }
}

