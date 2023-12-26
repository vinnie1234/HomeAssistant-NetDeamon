using System.Reactive.Concurrency;

namespace Automation.apps.Rooms.Hall;

[NetDaemonApp(Id = nameof(HallLightOnMovement))]
//[Focus]
public class HallLightOnMovement : BaseApp
{
    private bool IsNighttime => Entities.InputSelect.Housemodeselect.State == "Night";
    private bool IsSleeping => Entities.InputBoolean.Sleeping.IsOn();
    private bool DisableLightAutomations => Entities.InputBoolean.Disablelightautomationhall.IsOn();

    public HallLightOnMovement(
        IHaContext ha,
        ILogger<HallLightOnMovement> logger,
        INotify notify,
        IScheduler scheduler)
        : base(ha, logger, notify, scheduler)
    {
        InitializeLights();

        HaContext.Events.Where(x => x.EventType == "hue_event").Subscribe(x =>
        {
            var eventModel = x.DataElement?.ToObject<EventModel>();
            if (eventModel != null) OverwriteSwitch(eventModel);
        });
    }

    private void InitializeLights()
    {
        Entities.BinarySensor.GangMotion
            .StateChanges()
            .Where(x => x.New.IsOn() && !DisableLightAutomations)
            .Subscribe(_ => ChangeLight(true, GetBrightness()));

        Entities.BinarySensor.GangMotion
            .StateChanges()
            .WhenStateIsFor(x => x.IsOff(), TimeSpan.FromMinutes(GetStateTime()), Scheduler)
            .Where(_ => !DisableLightAutomations)
            .Subscribe(_ => ChangeLight(false));
    }

    private int GetBrightness()
    {
        return IsSleeping switch
        {
            true  => 5,
            false => 100
        };
    }

    private int GetStateTime()
    {
        if (IsNighttime && IsSleeping) return Convert.ToInt32(Entities.InputNumber.Halllightnighttime.State);
        if (!IsNighttime && !IsSleeping) return Convert.ToInt32(Entities.InputNumber.Halllightdaytime.State);

        return 1;
    }

    private void ChangeLight(bool on, int brightnessPct = 0)
    {
        switch (on)
        {
            case true:
                Entities.Light.Hal2.TurnOn(brightnessPct: brightnessPct, transition: 15);
                if (!IsNighttime && !IsSleeping) Entities.Light.Hal.TurnOn();
                break;
            case false:
                Entities.Light.Hal.TurnOff();
                Entities.Light.Hal2.TurnOff();
                break;
        }
    }

    private void OverwriteSwitch(EventModel eventModel)
    {
        const string hueSwitchBathroomId = "4339833970e35ff10c568a94b59e50dd";

        if (eventModel is { DeviceId: hueSwitchBathroomId, Type: "initial_press" })
            switch (eventModel.Subtype)
            {
                //button one
                case 1:
                    if (Entities.Light.Hal.IsOn())
                    {
                        Entities.Light.Hal.TurnOff();
                        Entities.Light.Hal2.TurnOff();
                    }
                    else
                    {
                        Entities.Light.Hal.TurnOn();
                        Entities.Light.Hal2.TurnOn();
                    }

                    break;
                //button two
                case 2:
                    Entities.Light.Hal2.TurnOn(brightnessStepPct: 10);
                    break;
                //button three
                case 3:
                    Entities.Light.Hal2.TurnOn(brightnessStepPct: -10);
                    break;
                //button four
                case 4:
                    Entities.MediaPlayer.FriendsSpeakers.VolumeSet(0.5);
                    Entities.Light.Hal.TurnOn();
                    Entities.Light.Hal2.TurnOff();
                    Services.MediaPlayer.PlayMedia(new ServiceTarget
                    {
                        EntityIds = new[] { Entities.MediaPlayer.FriendsSpeakers.EntityId }
                    }, "http://192.168.50.189:8123/local/Friends.mp3", "music");

                    break;
            }
    }
}