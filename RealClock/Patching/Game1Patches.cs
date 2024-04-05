using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;

namespace Thimadera.StardewMods.RealClock.Patching
{
    public class Game1Patches
    {
        public static ModConfig Config;
        private static IMonitor Monitor;

        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        public static bool UpdateGameClock(GameTime time)
        {
            if (Config is null || Config.Enabled is false)
                return true;

            try
            {
                if (Game1.shouldTimePass() && !Game1.IsClient)
                {
                    Game1.gameTimeInterval += time.ElapsedGameTime.Milliseconds;
                }
                if (Game1.timeOfDay >= Game1.getTrulyDarkTime(Game1.currentLocation))
                {
                    int adjustedTime = (int)(
                        (float)(Game1.timeOfDay - Game1.timeOfDay % 100)
                        + (float)(Game1.timeOfDay % 100 / 10) * 16.66f
                    );
                    float transparency = Math.Min(
                        0.93f,
                        0.75f
                            + (
                                (float)(adjustedTime - Game1.getTrulyDarkTime(Game1.currentLocation))
                                + (float)Game1.gameTimeInterval
                                    / (float)Game1.realMilliSecondsPerGameTenMinutes
                                    * 16.6f
                            ) * 0.000625f
                    );
                    Game1.outdoorLight =
                        (Game1.IsRainingHere() ? Game1.ambientLight : Game1.eveningColor)
                        * transparency;
                }
                else if (Game1.timeOfDay >= Game1.getStartingToGetDarkTime(Game1.currentLocation))
                {
                    int adjustedTime = (int)(
                        (float)(Game1.timeOfDay - Game1.timeOfDay % 100)
                        + (float)(Game1.timeOfDay % 100 / 10) * 16.66f
                    );
                    float transparency = Math.Min(
                        0.93f,
                        0.3f
                            + (
                                (float)(
                                    adjustedTime - Game1.getStartingToGetDarkTime(Game1.currentLocation)
                                )
                                + (float)Game1.gameTimeInterval
                                    / (float)Game1.realMilliSecondsPerGameTenMinutes
                                    * 16.6f
                            ) * 0.00225f
                    );
                    Game1.outdoorLight =
                        (Game1.IsRainingHere() ? Game1.ambientLight : Game1.eveningColor)
                        * transparency;
                }
                else if (Game1.IsRainingHere())
                {
                    Game1.outdoorLight = Game1.ambientLight * 0.3f;
                }
                else
                {
                    Game1.outdoorLight = Game1.ambientLight;
                }
                int num = Game1.gameTimeInterval;
                float num2 = Game1.realMilliSecondsPerGameTenMinutes * (Config?.SecondsToMinutes ?? .7f) / .7f;
                GameLocation gameLocation = Game1.currentLocation;
                if (
                    num
                    > num2
                        + (
                            (gameLocation != null)
                                ? new int?(gameLocation.ExtraMillisecondsPerInGameMinute * 10)
                                : null
                        )
                )
                {
                    if (Game1.panMode)
                    {
                        Game1.gameTimeInterval = 0;
                    }
                    else
                    {
                        Game1.performTenMinuteClockUpdate();
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Failed in {nameof(UpdateGameClock)}:\n{ex}", LogLevel.Error);
                return true;
            }
        }
    }
}