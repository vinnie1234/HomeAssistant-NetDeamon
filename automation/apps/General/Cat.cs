using System.Globalization;
using NetDaemon.Extensions.Scheduler;

namespace Automation.apps.General;

[NetDaemonApp(Id = nameof(Cat))]
// ReSharper disable once UnusedType.Global
public class Cat : BaseApp
{
    private readonly INetDaemonScheduler _scheduler;
    private readonly Dictionary<InputDatetimeEntity, InputNumberEntity> _feedDictionary;

    // ReSharper disable once SuggestBaseTypeForParameterInConstructor
    public Cat(IHaContext haContext, ILogger<Cat> logger, INotify notify, INetDaemonScheduler scheduler)
        : base(haContext, logger, notify)
    {
        _scheduler = scheduler;
        _feedDictionary = new Dictionary<InputDatetimeEntity, InputNumberEntity>
        {
            { Entities.InputDatetime.Zedarfeedfirsttime, Entities.InputNumber.Zedarfeedfirstamound },
            { Entities.InputDatetime.Zedarfeedsecondtime, Entities.InputNumber.Zedarfeedsecondamound },
            { Entities.InputDatetime.Zedarfeedthirdtime, Entities.InputNumber.Zedarfeedthirdamound },
            { Entities.InputDatetime.Zedarfeedfourthtime, Entities.InputNumber.Zedarfeedfourthamound }
        };

        Entities.InputButton.Feedcat.StateChanges()
            .Subscribe(_ =>
            {
                FeedCat(Convert.ToInt32(Entities.Sensor.ZedarLastAmountManualFeed.State));
                Entities.InputDatetime.Zedarlastmanualfeed.SetDatetime(new InputDatetimeSetDatetimeParameters
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            });

        Entities.InputButton.Zedargivenextfeedeary.StateChanges()
            .Subscribe(_ => GiveNextFeedEarly());

        AutoFeedCat();
        MonitorCar();
    }

    private void FeedCat(int amount)
    {
        Services.Localtuya.SetDp(new LocaltuyaSetDpParameters
        {
            DeviceId = @"bfe910316af564a2e4hgrm",
            Dp = 3,
            Value = amount
        });
    }

    private void MonitorCar()
    {
        Entities.InputDatetime.Zedarlastmanualfeed.StateChanges()
            .Subscribe(x =>
                Notify.NotifyGsmVincent(@"Pixel heeft handmatig eten gehad",
                    @$"Pixel heeft {x.New?.State} porties eten gehad"));

        Entities.InputDatetime.Zedarlastautomatedfeed.StateChanges()
            .Subscribe(x =>
                Notify.NotifyGsmVincent(@"Pixel heeft automatisch eten gehad",
                    @$"Pixel heeft {x.New?.State} porties eten gehad"));
    }

    private void AutoFeedCat()
    {
        foreach (var autoFeed in
                 _feedDictionary.Where(autoFeed => autoFeed.Key.State != null))
        {
            _scheduler.RunDaily(TimeSpan.Parse(autoFeed.Key.State!), () =>
            {
                if (Entities.InputBoolean.Zedarskipnextautofeed.IsOff())
                    FeedCat(Convert.ToInt32(autoFeed.Value.State));
                Entities.InputBoolean.Zedarskipnextautofeed.TurnOff();
                Entities.InputDatetime.Zedarlastautomatedfeed.SetDatetime(new InputDatetimeSetDatetimeParameters
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            });
        }
    }

    private void GiveNextFeedEarly()
    {
        var closestFeed = _feedDictionary.MinBy(t =>
            Math.Abs((DateTime.Parse(t.Key.State!) - DateTime.Now).Ticks));

        Entities.InputBoolean.Zedarskipnextautofeed.TurnOn();
        FeedCat(Convert.ToInt32(closestFeed.Value.State));

        Entities.InputDatetime.Zedarlastmanualfeed.SetDatetime(new InputDatetimeSetDatetimeParameters
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
    }
}