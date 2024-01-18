using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Threading;
using Automation.Enum;

namespace Automation.apps.General;

[NetDaemonApp(Id = nameof(FunApp))]
[Focus]
//ReSharper disable once UnusedType.Global
public class FunApp : BaseApp
{
    // ReSharper disable once SuggestBaseTypeForParameterInConstructor
    public FunApp(IHaContext ha, ILogger<FunApp> logger, INotify notify, IScheduler scheduler)
        : base(ha, logger, notify, scheduler)
    {
        Entities.Sensor.Ps5turnon.StateChanges().Where(x => x.New?.State?.ToLower() == "on")
            .Subscribe(_ =>
            {
                Ps5TurnedOn();
            });

        Friends();
        Parents();
    }

    private void Ps5TurnedOn()
    {
        if (DateTimeOffset.Now.DayOfWeek == DayOfWeek.Wednesday && DateTimeOffset.Now.Hour >= 19)
            Notify.NotifyHouse("Déwin",
                "Goede avond Déwin, ben je er klaar voor om weer vernederd te worden door Vincent?", true);
    }

    private void Friends()
    {
        Entities.InputButton.StartFriends.StateChanges()
            .Subscribe(_ =>
            {
                Notify.SendMusicToHome("http://192.168.50.189:8123/local/Friends.mp3");
                Entities.Light.Hal.TurnOn();
            });
    }

    private void Parents()
    {

        Entities.DeviceTracker.A52sVanEddy.StateChanges()
            .Where(x => x.Entity.State == "home")
            .Subscribe(_ => SendMessageParents());
        Entities.DeviceTracker.S20FeVanJannette.StateChanges()
            .Where(x => x.Entity.State == "home")
            .Subscribe(_ => SendMessageParents());      
    }

    private void SendMessageParents()
    {
        var houseState = Globals.GetHouseState(Entities);
        var message = houseState == HouseState.Morning ? "Goedemorgen Ed en Jannette, welkom bij Vincent!" : "Goedemiddag Ed en Jannette, Welkom bij Vincent";
        
        Notify.NotifyHouse("Parents", message, false, 300);
    }

    private void StartNewYearOnNewYear()
    {
        Scheduler.ScheduleCron("10 58 23 31 12 *", () =>
        {
            Notify.SendMusicToHome("http://192.168.50.189:8123/local/HappyNewYear.mp3", 0.4);
        }, true);
        
        Scheduler.ScheduleCron("59 58 23 31 12 *", () =>
        {
            Entities.MediaPlayer.HeleHuis.VolumeSet(0.9);
        }, true);
        
        Scheduler.ScheduleCron("00 00 01 01 *", ChristmasFirework);
    }

    private void NewYear()
    {
        StartNewYearOnNewYear();

        Entities.InputButton.Startnewyear.StateChanges().Subscribe(_ =>
        {
            Notify.SendMusicToHome("http://192.168.50.189:8123/local/HappyNewYear.mp3", 0.4);
            Thread.Sleep(TimeSpan.FromSeconds(49));
            Entities.MediaPlayer.HeleHuis.VolumeSet(0.9);
            ChristmasFirework();
        });
    }

    private void ChristmasFirework()
    {
        var rnd = new Random();
        var s = new Stopwatch();
        s.Start();
            
        do
        {
            var num = rnd.Next(1, 6);

            switch (num)
            {
                case 1:
                    Entities.Light.Tv.TurnOn(colorName: "GREEN");
                    Entities.Light.HuePlayMidden.TurnOn(colorName: "GREEN");
                    Entities.Light.HuePlayLinks.TurnOn(colorName: "GREEN");
                    Entities.Light.HuePlayRechts.TurnOn(colorName: "GREEN");
                    Entities.Light.HueGradientStringLight1.TurnOn(colorName: "RED");
                    break;
                case 2:
                    Entities.Light.Tv.TurnOn(colorName: "RED");
                    Entities.Light.HuePlayMidden.TurnOn(colorName: "RED");
                    Entities.Light.HuePlayLinks.TurnOn(colorName: "RED");
                    Entities.Light.HuePlayRechts.TurnOn(colorName: "RED");
                    Entities.Light.HueGradientStringLight1.TurnOn(colorName: "GREEN");
                    break;
                case 3:
                    Entities.Light.Tv.TurnOn(colorName: "BLUE");
                    Entities.Light.HuePlayMidden.TurnOn(colorName: "BLUE");
                    Entities.Light.HuePlayLinks.TurnOn(colorName: "BLUE");
                    Entities.Light.HuePlayRechts.TurnOn(colorName: "BLUE");
                    Entities.Light.HueGradientStringLight1.TurnOn(colorName: "YELLOW");
                    break;
                case 4:
                    Entities.Light.Tv.TurnOn(colorName: "WHITE");
                    Entities.Light.HuePlayMidden.TurnOn(colorName: "WHITE");
                    Entities.Light.HuePlayLinks.TurnOn(colorName: "WHITE");
                    Entities.Light.HuePlayRechts.TurnOn(colorName: "WHITE");
                    Entities.Light.HueGradientStringLight1.TurnOn(colorName: "BLUE");
                    break;
                case 5:
                    Entities.Light.Tv.TurnOn(colorName: "YELLOW");
                    Entities.Light.HuePlayMidden.TurnOn(colorName: "YELLOW");
                    Entities.Light.HuePlayLinks.TurnOn(colorName: "YELLOW");
                    Entities.Light.HuePlayRechts.TurnOn(colorName: "YELLOW");
                    Entities.Light.HueGradientStringLight1.TurnOn(colorName: "WHITE");
                    break;
            }

            Thread.Sleep(TimeSpan.FromSeconds(0.5));
        } while (s.Elapsed < TimeSpan.FromMinutes(4));
        
        Entities.MediaPlayer.HeleHuis.VolumeSet(0.4);
        Entities.Light.HueGradientStringLight1.TurnOn(effect: "opal");
    }
}