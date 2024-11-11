using System.Globalization;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TheLighthouseWavesPlayer.Core.Messages;

public class CultureChangedMessage :
    ValueChangedMessage<CultureInfo>
{
    public CultureChangedMessage(CultureInfo value) :
        base(value)
    { }
}