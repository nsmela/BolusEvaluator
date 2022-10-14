
using BolusEvaluator.MVVM.ViewModels;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BolusEvaluator.Messages;
public class ChangeImageViewState : ValueChangedMessage<IImageViewState> {
    public ChangeImageViewState(IImageViewState value) : base(value) {
    }
}

