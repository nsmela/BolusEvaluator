using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolusEvaluator.Messages;
public class InfoMessage : ValueChangedMessage<string> {
    public InfoMessage(string value) : base(value) {
    }
}

