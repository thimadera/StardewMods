using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Text;

namespace Thimadera.StardewMods.RealClock.Patching
{
    internal class DayTimeMoneyBoxPatches
    {
        public static ModConfig Config;
        private static IMonitor Monitor;
        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        private static StringBuilder _hoverText = new StringBuilder();

        private static StringBuilder _timeText = new StringBuilder();

        private static StringBuilder _hours = new StringBuilder();

        private static StringBuilder _padZeros = new StringBuilder();

        private static StringBuilder _temp = new StringBuilder();

        private static string _amString = "am";

        private static string _pmString = "pm";

        private static int _currentTimeOfDay = 0;

        public static bool draw(SpriteBatch b, ref DayTimeMoneyBox __instance,
            ref Rectangle ___sourceRect,
            ref int ____lastDayOfMonth,
            ref string ____lastDayOfMonthString,
            ref StringBuilder ____dateText,
            ref int ___questNotificationTimer,
            ref Texture2D ___questPingTexture,
            ref string ___questPingString,
            ref string ___goldCoinString,
            ref int ___goldCoinTimer,
            ref Rectangle ___questPingSourceRect)
        {
            if (Config is null || Config.Enabled is false)
                return true;

            try
            {
                SpriteFont font = (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko) ? Game1.smallFont : Game1.dialogueFont;

                __instance.position = new Vector2(Game1.uiViewport.Width - 300, 8f);
                if (Game1.isOutdoorMapSmallerThanViewport())
                {
                    __instance.position = new Vector2(Math.Min(__instance.position.X, -Game1.uiViewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64 - 300), 8f);
                }
                Utility.makeSafe(ref __instance.position, 300, 284);
                __instance.xPositionOnScreen = (int)__instance.position.X;
                __instance.yPositionOnScreen = (int)__instance.position.Y;
                __instance.questButton.bounds = new Rectangle(__instance.xPositionOnScreen + 212, __instance.yPositionOnScreen + 240, 44, 46);
                __instance.zoomOutButton.bounds = new Rectangle(__instance.xPositionOnScreen + 92, __instance.yPositionOnScreen + 244, 28, 32);
                __instance.zoomInButton.bounds = new Rectangle(__instance.xPositionOnScreen + 124, __instance.yPositionOnScreen + 244, 28, 32);

                if (__instance.timeShakeTimer > 0)
                {
                    __instance.timeShakeTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
                }
                if (__instance.questPulseTimer > 0)
                {
                    __instance.questPulseTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
                }
                if (__instance.whenToPulseTimer >= 0)
                {
                    __instance.whenToPulseTimer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
                    if (__instance.whenToPulseTimer <= 0)
                    {
                        __instance.whenToPulseTimer = 3000;
                        if (Game1.player.hasNewQuestActivity())
                        {
                            __instance.questPulseTimer = 1000;
                        }
                    }
                }
                b.Draw(Game1.mouseCursors, __instance.position, ___sourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                if (Game1.dayOfMonth != ____lastDayOfMonth)
                {
                    ____lastDayOfMonth = Game1.dayOfMonth;
                    ____lastDayOfMonthString = Game1.shortDayDisplayNameFromDayOfSeason(____lastDayOfMonth);
                }
                ____dateText.Clear();
                switch (LocalizedContentManager.CurrentLanguageCode)
                {
                    case LocalizedContentManager.LanguageCode.ja:
                        ____dateText.AppendEx(Game1.dayOfMonth);
                        ____dateText.Append("日 (");
                        ____dateText.Append(____lastDayOfMonthString);
                        ____dateText.Append(')');
                        break;
                    case LocalizedContentManager.LanguageCode.zh:
                        ____dateText.AppendEx(Game1.dayOfMonth);
                        ____dateText.Append("日 ");
                        ____dateText.Append(____lastDayOfMonthString);
                        ____dateText.Append(' ');
                        break;
                    case LocalizedContentManager.LanguageCode.mod:
                        ____dateText.Append(LocalizedContentManager.CurrentModLanguage.ClockDateFormat.Replace("[DAY_OF_WEEK]", ____lastDayOfMonthString).Replace("[DAY_OF_MONTH]", Game1.dayOfMonth.ToString()));
                        break;
                    default:
                        ____dateText.Append(____lastDayOfMonthString);
                        ____dateText.Append(". ");
                        ____dateText.AppendEx(Game1.dayOfMonth);
                        break;
                }
                Vector2 daySize = font.MeasureString(____dateText);
                Vector2 dayPosition = new(___sourceRect.X * 0.5625f - daySize.X / 2f, ___sourceRect.Y * (LocalizedContentManager.CurrentLanguageLatin ? 0.1f : 0.1f) - daySize.Y / 2f);
                Utility.drawTextWithShadow(b, ____dateText, font, __instance.position + dayPosition, Game1.textColor);
                b.Draw(Game1.mouseCursors, __instance.position + new Vector2(212f, 68f), new Rectangle(406, 441 + Game1.seasonIndex * 8, 12, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                if (Game1.weatherIcon == 999)
                {
                    b.Draw(Game1.mouseCursors_1_6, __instance.position + new Vector2(116f, 68f), new Rectangle(243, 293, 12, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                }
                else
                {
                    b.Draw(Game1.mouseCursors, __instance.position + new Vector2(116f, 68f), new Rectangle(317 + 12 * Game1.weatherIcon, 421, 12, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                }

                bool isPaused = Game1.gameTimeInterval < 0;

                int exactTimeOfDay =
                isPaused
                ? _currentTimeOfDay
                : Game1.timeOfDay +
                    (int)((float)Game1.gameTimeInterval / 1000 / Config?.SecondsToMinutes ?? .7f);

                _currentTimeOfDay = exactTimeOfDay;

                _padZeros.Clear();
                if (exactTimeOfDay % 100 < 10)
                {
                    _padZeros.Append('0');
                }
                _hours.Clear();
                switch (LocalizedContentManager.CurrentLanguageCode)
                {
                    case LocalizedContentManager.LanguageCode.ru:
                    case LocalizedContentManager.LanguageCode.zh:
                    case LocalizedContentManager.LanguageCode.pt:
                    case LocalizedContentManager.LanguageCode.es:
                    case LocalizedContentManager.LanguageCode.de:
                    case LocalizedContentManager.LanguageCode.th:
                    case LocalizedContentManager.LanguageCode.fr:
                    case LocalizedContentManager.LanguageCode.tr:
                    case LocalizedContentManager.LanguageCode.hu:
                        _temp.Clear();
                        _temp.AppendEx(exactTimeOfDay / 100 % 24);
                        if (exactTimeOfDay / 100 % 24 <= 9)
                        {
                            _hours.Append('0');
                        }
                        _hours.AppendEx(_temp);
                        break;
                    default:
                        if (exactTimeOfDay / 100 % 12 == 0)
                        {
                            if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ja)
                            {
                                _hours.Append('0');
                            }
                            else
                            {
                                _hours.Append("12");
                            }
                        }
                        else
                        {
                            _hours.AppendEx(exactTimeOfDay / 100 % 12);
                        }
                        break;
                }
                _timeText.Clear();
                _timeText.AppendEx(_hours);
                _timeText.Append(':');
                _timeText.AppendEx(_padZeros);
                _timeText.AppendEx(exactTimeOfDay % 100);
                switch (LocalizedContentManager.CurrentLanguageCode)
                {
                    case LocalizedContentManager.LanguageCode.en:
                    case LocalizedContentManager.LanguageCode.it:
                        _timeText.Append(' ');
                        if (exactTimeOfDay < 1200 || exactTimeOfDay >= 2400)
                        {
                            _timeText.Append(_amString);
                        }
                        else
                        {
                            _timeText.Append(_pmString);
                        }
                        break;
                    case LocalizedContentManager.LanguageCode.ko:
                        if (exactTimeOfDay < 1200 || exactTimeOfDay >= 2400)
                        {
                            _timeText.Append(_amString);
                        }
                        else
                        {
                            _timeText.Append(_pmString);
                        }
                        break;
                    case LocalizedContentManager.LanguageCode.ja:
                        _temp.Clear();
                        _temp.AppendEx(_timeText);
                        _timeText.Clear();
                        if (exactTimeOfDay < 1200 || exactTimeOfDay >= 2400)
                        {
                            _timeText.Append(_amString);
                            _timeText.Append(" ");
                            _timeText.AppendEx(_temp);
                        }
                        else
                        {
                            _timeText.Append(_pmString);
                            _timeText.Append(" ");
                            _timeText.AppendEx(_temp);
                        }
                        break;
                    case LocalizedContentManager.LanguageCode.mod:
                        _timeText.Clear();
                        _timeText.Append(LocalizedContentManager.FormatTimeString(exactTimeOfDay, LocalizedContentManager.CurrentModLanguage.ClockTimeFormat));
                        break;
                }
                Vector2 txtSize = font.MeasureString(_timeText);
                Vector2 timePosition = new(___sourceRect.X * 0.55f - txtSize.X / 2f + ((__instance.timeShakeTimer > 0) ? Game1.random.Next(-2, 3) : 0), (float)___sourceRect.Y * (LocalizedContentManager.CurrentLanguageLatin ? 0.31f : 0.31f) - txtSize.Y / 2f + (float)((__instance.timeShakeTimer > 0) ? Game1.random.Next(-2, 3) : 0));
                bool nofade = Game1.shouldTimePass() || Game1.fadeToBlack || Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000.0 > 1000.0;
                Utility.drawTextWithShadow(b, _timeText, font, __instance.position + timePosition, (exactTimeOfDay >= 2400) ? Color.Red : (Game1.textColor * (nofade ? 1f : 0.5f)));
                int adjustedTime = (int)(exactTimeOfDay - exactTimeOfDay % 100 + exactTimeOfDay % 100 / 10 * 16.66f);
                if (Game1.player.hasVisibleQuests)
                {
                    __instance.questButton.draw(b);
                    if (__instance.questPulseTimer > 0)
                    {
                        float scaleMult = 1f / (Math.Max(300f, Math.Abs(__instance.questPulseTimer % 1000 - 500)) / 500f);
                        b.Draw(Game1.mouseCursors, new Vector2(__instance.questButton.bounds.X + 24, __instance.questButton.bounds.Y + 32) + ((scaleMult > 1f) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero), new Rectangle(395, 497, 3, 8), Color.White, 0f, new Vector2(2f, 4f), 4f * scaleMult, SpriteEffects.None, 0.99f);
                    }
                    if (__instance.questPingTimer > 0)
                    {
                        b.Draw(Game1.mouseCursors, new Vector2(Game1.dayTimeMoneyBox.questButton.bounds.Left - 16, Game1.dayTimeMoneyBox.questButton.bounds.Bottom + 8), new Rectangle(128 + ((__instance.questPingTimer / 200 % 2 != 0) ? 16 : 0), 208, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                    }
                }
                if (Game1.options.zoomButtons)
                {
                    __instance.zoomInButton.draw(b, Color.White * ((Game1.options.desiredBaseZoomLevel >= 2f) ? 0.5f : 1f), 1f);
                    __instance.zoomOutButton.draw(b, Color.White * ((Game1.options.desiredBaseZoomLevel <= 0.75f) ? 0.5f : 1f), 1f);
                }
                __instance.drawMoneyBox(b);
                if (_hoverText.Length > 0 && __instance.isWithinBounds(Game1.getOldMouseX(), Game1.getOldMouseY()))
                {
                    IClickableMenu.drawHoverText(b, _hoverText, Game1.dialogueFont);
                }
                b.Draw(Game1.mouseCursors, __instance.position + new Vector2(88f, 88f), new Rectangle(324, 477, 7, 19), Color.White, (float)(Math.PI + Math.Min(Math.PI, (double)(((float)adjustedTime + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f - 600f) / 2000f) * Math.PI)), new Vector2(3f, 17f), 4f, SpriteEffects.None, 0.9f);
                if (___questNotificationTimer > 0)
                {
                    Vector2 basePosition = __instance.position + new Vector2(27f, 76f) * 4f;
                    b.Draw(Game1.mouseCursors_1_6, basePosition, new Rectangle(257, 228, 39, 18), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                    b.Draw(___questPingTexture, basePosition + new Vector2(1f, 1f) * 4f, ___questPingSourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.91f);
                    if (___questPingString != null)
                    {
                        Utility.drawTextWithShadow(b, ___questPingString, Game1.smallFont, basePosition + new Vector2(27f, 9.5f) * 4f - Game1.smallFont.MeasureString(___questPingString) * 0.5f, Game1.textColor);
                    }
                    else
                    {
                        b.Draw(Game1.mouseCursors_1_6, basePosition + new Vector2(22f, 5f) * 4f, new Rectangle(297, 229, 9, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.91f);
                    }
                }
                if (___goldCoinTimer > 0)
                {
                    SpriteText.drawSmallTextBubble(b, ___goldCoinString, __instance.position + new Vector2(5f, 73f) * 4f, -1, 0.99f, drawPointerOnTop: true);
                }
                return false;
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Failed in {nameof(draw)}:\n{ex}", LogLevel.Error);
                return true;
            }
        }
    }
}
