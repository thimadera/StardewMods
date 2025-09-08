using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealClock.Models;
using RealClock.Network;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace RealClock
{
    internal sealed class RealClock : Mod
    {
        private ModConfig Config;
        public int LastTimeInterval { get; set; }
        private int _lastTimeOfDay = -1;
        private double _accumMsSinceLastTen = 0;
        private double _msPerMinute = 700.0;
        private double _observedBlockMs = 7000.0;
        private double _observedMsPerMinute = 700.0;
        private const double ObservedEma = 0.25;
        private int _prevExactTime = -1;
        private bool _prev24h = false;
        private string _cachedTimeText = "";
        private Vector2 _cachedTextSize;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            helper.Events.GameLoop.ReturnedToTitle += (_, __) => ResetInterpolators();
            helper.Events.GameLoop.SaveLoaded += (_, __) => ResetInterpolators();
            helper.Events.GameLoop.DayStarted += (_, __) => ResetInterpolators();

        }

        private void ResetInterpolators()
        {
            LastTimeInterval = 0;
            _lastTimeOfDay = -1;
            _accumMsSinceLastTen = 0;
            _observedBlockMs = 7000.0;
            _observedMsPerMinute = 700.0;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            GenericModConfigMenuIntegration.AddConfig(
                Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu"),
                ModManifest,
                Helper,
                Config
            );
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }

            _msPerMinute = 700.0 + (Game1.currentLocation?.ExtraMillisecondsPerInGameMinute ?? 0);

            if (Game1.timeOfDay != _lastTimeOfDay)
            {
                if (_accumMsSinceLastTen > 1.0)
                {
                    _observedBlockMs = (_observedBlockMs * (1.0 - ObservedEma)) + (_accumMsSinceLastTen * ObservedEma);
                    _observedMsPerMinute = Math.Clamp(_observedBlockMs / 10.0, 100.0, 60000.0);
                }

                _lastTimeOfDay = Game1.timeOfDay;
                _accumMsSinceLastTen = 0;
            }

            if (Context.IsMainPlayer && Config.TimeSpeedControl)
            {
                int delta;
                if (Game1.gameTimeInterval < LastTimeInterval)
                {
                    delta = Game1.gameTimeInterval;
                    LastTimeInterval = 0;
                }
                else
                {
                    delta = Game1.gameTimeInterval - LastTimeInterval;
                }

                float sec = Math.Clamp(Config.SecondsToMinutes, 0.05f, 20f);
                Game1.gameTimeInterval = LastTimeInterval + (int)(delta * .7f / sec);
            }
            else
            {
                if (Game1.shouldTimePass())
                {
                    _accumMsSinceLastTen += Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
                }
            }

            LastTimeInterval = Game1.gameTimeInterval;
        }

        [EventPriority(EventPriority.High)]
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (!Game1.displayHUD || Game1.eventUp || Game1.gameMode != 3 || Game1.freezeControls || Game1.panMode || Game1.HostPaused || Game1.game1.takingMapScreenshot || Game1.currentLocation is null)
            {
                return;
            }

            SpriteBatch b = e.SpriteBatch;

            double perMinute = Context.IsMainPlayer ? _msPerMinute : _observedMsPerMinute;
            double numerador = Context.IsMainPlayer ? Game1.gameTimeInterval
                                                    : _accumMsSinceLastTen;
            double progressMinutes = numerador / Math.Max(1.0, perMinute);
            int extraMinutes = (int)Math.Min(9.0, Math.Floor(progressMinutes + 1e-6));
            int exactTime = Game1.timeOfDay + extraMinutes;

            if (_prevExactTime != exactTime || _prev24h != Config.Show24Hours)
            {
                _prevExactTime = exactTime;
                _prev24h = Config.Show24Hours;

                _cachedTimeText = GetTimeFormatted(exactTime);
                _cachedTextSize = Game1.dialogueFont.MeasureString(_cachedTimeText);
            }

            string _timeText = _cachedTimeText;
            Vector2 txtSize = _cachedTextSize;

            DayTimeMoneyBox dayTimeMoneyBox = Game1.dayTimeMoneyBox;

            Rectangle sourceRect = new(333, 431, 71, 43);
            Vector2 offset = new(108f, 112f);
            Rectangle bounds = new(360, 459, 40, 9);
            b.Draw(Game1.mouseCursors, dayTimeMoneyBox.position + offset, (Rectangle?)bounds, Color.White, 0f, Vector2.Zero, 4f, 0, 0.9f);

            int timeShakeTimer = dayTimeMoneyBox.timeShakeTimer;
            SpriteFont font = Game1.dialogueFont;

            Vector2 timePosition = new((sourceRect.X * 0.55f) - (txtSize.X / 2f) + (timeShakeTimer > 0 ? Game1.random.Next(-2, 3) : 0), (sourceRect.Y * (LocalizedContentManager.CurrentLanguageLatin ? 0.31f : 0.31f)) - (txtSize.Y / 2f) + (timeShakeTimer > 0 ? Game1.random.Next(-2, 3) : 0));
            bool nofade = Game1.shouldTimePass() || Game1.fadeToBlack || Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000.0 > 1000.0;
            Utility.drawTextWithShadow(b, _timeText, font, dayTimeMoneyBox.position + timePosition, exactTime >= 2400 ? Color.Red : Game1.textColor * (nofade ? 1f : 0.5f));
        }

        private string GetTimeFormatted(int exactTime)
        {
            string _amString = Game1.content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10370");
            string _pmString = Game1.content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10371");

            StringBuilder _timeText = new();
            if (Config.Show24Hours)
            {
                if (exactTime / 100 % 24 <= 9)
                {
                    _ = _timeText.Append('0');
                }
                _ = _timeText.AppendEx(exactTime / 100 % 24);
            }
            else
            {
                _ = exactTime / 100 % 12 == 0 ? _timeText.Append("12") : _timeText.AppendEx(exactTime / 100 % 12);
            }

            _ = _timeText.Append(':');

            if (exactTime / 10 % 10 == 0)
            {
                _ = _timeText.Append('0');
            }

            _ = _timeText.AppendEx(exactTime % 100);

            if (!Config.Show24Hours)
            {
                switch (LocalizedContentManager.CurrentLanguageCode)
                {
                    case LocalizedContentManager.LanguageCode.en:
                    case LocalizedContentManager.LanguageCode.it:
                        _ = _timeText.Append(' ');
                        _ = exactTime is < 1200 or >= 2400 ? _timeText.Append(_amString) : _timeText.Append(_pmString);
                        break;
                    case LocalizedContentManager.LanguageCode.ko:
                        _ = exactTime is < 1200 or >= 2400 ? _timeText.Append(_amString) : _timeText.Append(_pmString);
                        break;
                    case LocalizedContentManager.LanguageCode.ja:
                        StringBuilder _temp = new();
                        _ = _temp.Append(_timeText);
                        _ = _timeText.Clear();
                        if (exactTime is < 1200 or >= 2400)
                        {
                            _ = _timeText.Append(_amString);
                            _ = _timeText.Append(' ');
                            _ = _timeText.AppendEx(_temp);
                        }
                        else
                        {
                            _ = _timeText.Append(_pmString);
                            _ = _timeText.Append(' ');
                            _ = _timeText.AppendEx(_temp);
                        }
                        break;
                    case LocalizedContentManager.LanguageCode.mod:
                        _ = _timeText.Clear();
                        _ = _timeText.Append(LocalizedContentManager.FormatTimeString(exactTime, LocalizedContentManager.CurrentModLanguage.ClockTimeFormat));
                        break;
                    default:
                        _ = exactTime is < 1200 or >= 2400 ? _timeText.Append("am") : _timeText.Append("pm");
                        break;
                }
            }

            return _timeText.ToString();
        }
    }
}
