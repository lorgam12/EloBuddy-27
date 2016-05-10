using System;
using System.Collections.Generic;
using System.Diagnostics;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Events;
using EloBuddy.Networking;
using SharpDX;

using Color = System.Drawing.Color;

namespace RoyalAssistant
{
    class Program
    {
        static Menu menu;
        static int[] SRExpCumulative = { 0, 280, 660, 1140, 1720, 2400, 3180, 4060, 5040, 6120, 7300, 8580, 9960, 11440, 13020, 14700, 16480, 18360 };
        static bool bought = false;
        static System.Timers.Timer globalCooldown = new System.Timers.Timer();

        static CheckBox _expSelf = new CheckBox("Show your XP bar", false);
        static CheckBox _expAlly = new CheckBox("Show allies XP bar");
        static CheckBox _expEnemy = new CheckBox("Show enemies XP bar");
        static CheckBox _expDraw = new CheckBox("Draw XP count");

        static CheckBox _utilWard = new CheckBox("Show \"Buy ward\" reminder");
        static CheckBox _centerWard = new CheckBox("Place in on center of screen");
        static KeyBind _buyWard = new KeyBind("Press to buy ward", false, KeyBind.BindTypes.HoldActive, 'U');

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnGameLoad;
        }

        static void OnGameLoad(EventArgs args)
        {
            LoadMenu();

            if (Game.MapId != GameMapId.SummonersRift)
            {
                Chat.Print("RoyalAssistant: only SR support implemented!");
                return;
            }

            Game.OnUpdate += OnUpdate;
            Drawing.OnEndScene += Drawing_OnDraw;
            //Game.OnEnd += OnGameEnd;
            //AIHeroClient.OnProcessSpellCast += OnSpellCast;
            globalCooldown.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerProcs);

            Chat.Print("RoyalAssistant Loaded!");
        }

        static void OnUpdate(EventArgs args)
        {
            if (Player.Instance.IsInShopRange() && _utilWard.CurrentValue && !HasWard())
                if (_buyWard.CurrentValue && !bought)
                {
                    new Item(ItemId.Vision_Ward).Buy();
                    bought = true;
                }
        }
        /*
        static void OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name != "NocturneParanoia2" || !menu.Item("noct").GetValue<bool>()) return;
            Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(args.Target.Position.X, args.Target.Position.Y, 0, 0, Packet.PingType.Danger)).Process();
        }
        */
        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.Instance.IsInShopRange() && _utilWard.CurrentValue && !HasWard())
            {
                Drawing.DrawText(_centerWard.CurrentValue ? Drawing.Width / 2 - 40 : 200,
                                  _centerWard.CurrentValue ? Drawing.Height / 2 - 60 : 400, GetColor(),
                                  "Buy a ward, save a life!");
                if (bought) Core.DelayAction(() => bought = false, 1000);
            }

            foreach (AIHeroClient hero in ObjectManager.Get<AIHeroClient>())
                if (hero.Level != 18 && hero.IsVisible && hero.IsHPBarRendered && !hero.IsDead)
                {
                    int XOffset;
                    int YOffset;
                    int textXOffset;
                    int textYOffset;
                    int width;
                    if (hero.IsMe && _expSelf.CurrentValue)
                    {
                        XOffset = 0;
                        YOffset = 2;
                        width = 134;
                        textXOffset = 0;
                        textYOffset = -14;
                    }
                    else if (hero.IsAlly && !hero.IsMe && _expAlly.CurrentValue || hero.IsEnemy && _expEnemy.CurrentValue)
                    {
                            XOffset = -1;
                            YOffset = -1;
                            width = 132;
                            textXOffset = -1;
                            textYOffset = -17;
                    }
                    else return;
                    Drawing.DrawLine(
                        new Vector2(hero.HPBarPosition.X + XOffset, hero.HPBarPosition.Y + YOffset),
                        new Vector2(hero.HPBarPosition.X + XOffset + width * ((180 + 100 * hero.Level + hero.Experience.XP - SRExpCumulative[hero.Level]) / (180 + 100 * hero.Level)),
                            hero.HPBarPosition.Y + YOffset), 3, Color.Gold);
                    if (_expDraw.CurrentValue) Drawing.DrawText(hero.HPBarPosition.X + textXOffset, hero.HPBarPosition.Y + textYOffset, Color.PaleGoldenrod, (int)(180 + 100 * hero.Level + hero.Experience.XP - SRExpCumulative[hero.Level]) + "/" + (180 + 100 * hero.Level));
                }

        }

        static void OnGameEnd(EventArgs args)
        {
            /*
            globalCooldown.Interval = menu.Item("delay").GetValue<Slider>().Value;
            if (menu.SubMenu("util").Item("end").GetValue<bool>())
            {
                globalCooldown.Start();
            }*/
        }

        static void OnTimerProcs(object sender, System.Timers.ElapsedEventArgs e)
        {
            /*
            globalCooldown.Stop();
            globalCooldown.Dispose();
            Process.GetProcessesByName("League of Legends")[0].Close();*/
        }


        static bool HasWard()
        {
            foreach (InventorySlot slot in ObjectManager.Player.InventoryItems)
                if (slot.Name.ToLower().Contains("ward") && !slot.Name.ToLower().Contains("trinket"))
                    return true;
            return false;
        }

        static Color GetColor()
        {
            switch ((int)((Game.Time % 1) * 10))//SHITTY CODE! :D
            {
                case 0:
                    return Color.IndianRed;
                case 1:
                    return Color.LightGoldenrodYellow;
                case 2:
                    return Color.Goldenrod;
                case 3:
                    return Color.Green;
                case 4:
                    return Color.Blue;
                case 5:
                    return Color.Violet;
                case 6:
                    return Color.DeepPink;
                case 7:
                    return Color.DeepSkyBlue;
                case 8:
                    return Color.White;
                case 9:
                    return Color.Cyan;
                default:
                    return Color.ForestGreen;
            }
        }
        static void LoadMenu()
        {
            // Initialize the menu
            menu = MainMenu.AddMenu("RoyalAssistant", "RoyalAssistant");

            menu.AddLabel("Experience tracker");
            menu.Add("_expSelf", _expSelf);
            menu.Add("_expAlly", _expAlly);
            menu.Add("_expEnemy", _expEnemy);
            menu.Add("_expDraw", _expDraw);
            menu.AddLabel("Utilities");
            menu.Add("_utilWard", _utilWard);
            menu.Add("_centerWard", _centerWard);
            menu.Add("_buyWard", _buyWard);
        }
    }
}
