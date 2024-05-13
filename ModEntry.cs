using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Objects;
using System.Reflection;
using Microsoft.Scripting.Runtime;
using StardewValley.Locations;





namespace MutantRings
{
    internal class ModEntry : Mod
    {

        public static IMonitor AMonitor;
        public static IModHelper AHelper;

        [XmlIgnore]
        public static NetBool hasUsedDarkPhoenixRevive = new NetBool(value: false);

        private static int fuckitall = 0;

       // private static readonly AccessTools.FieldRef<Ring, int?> lightSourceField = AccessTools.FieldRefAccess<Ring, int?>("_lightSourceID");

        private static FieldInfo lightSource = typeof(Ring).GetField("_lightSourceID", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public override void Entry(IModHelper helper)
        {

            AMonitor = Monitor;
            AHelper = helper;


            var harmony = new Harmony(ModManifest.UniqueID);

            harmony.Patch(
              original: AccessTools.Method(typeof(Ring), nameof(Ring.onEquip)),
              postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Ring_OnEquip_Postfix))
           );


            harmony.Patch(
              original: AccessTools.Method(typeof(Ring), nameof(Ring.onUnequip)),
              postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Ring_OnUnequip_Postfix))
           );

            harmony.Patch(
              original: AccessTools.Method(typeof(Farmer), nameof(Farmer.takeDamage)),
              postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Farmer_takeDamage_Postfix))
           );
            /*
            harmony.Patch(
             original: AccessTools.PropertyGetter(typeof(Monster), nameof(Monster.focusedOnFarmers)),
             postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Monster_focusedOnFarmer_Postfix))
          );
            */


            harmony.Patch(
              original: AccessTools.Method(typeof(Ring), nameof(Ring.update)),
              postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Ring_Update_Postfix))
           );


        }

        /*
        private static bool Monster_focusedOnFarmer_Postfix(Monster __instance)
        {
            if(Game1.player.isWearingRing("ApryllForever.MutantRings_ShadowCatRing"))
            {
                __instance.focusedOnFarmers = false;
                return false;
            }

            return true;
        }
        */

        private static void Ring_OnEquip_Postfix(Ring __instance, Farmer who)
        {
            GameLocation location;
            location = who.currentLocation;

            //var id = lightSource.GetValue(__instance);

            //lightSource.GetValue(__instance);

            // lightSource.SetValue(__instance, (int)__instance.uniqueID + (int)who.UniqueMultiplayerID);

            int jewel = (int)__instance.uniqueID + (int)who.UniqueMultiplayerID;


            if (__instance.Name.Equals("ApryllForever.MutantRings_DarkPhoenixRing"))
                {

                while (location.sharedLights.ContainsKey(jewel))
                {
                    jewel = jewel + 1;
                }

                location.sharedLights[jewel] = new LightSource(1, new Vector2(who.Position.X + 21f, who.Position.Y + 64f), 5f, new Color(0, 50, 170), (int)__instance.uniqueID + (int)who.UniqueMultiplayerID, LightSource.LightContext.None, who.UniqueMultiplayerID);

                fuckitall = jewel;
             
                


                // if (who.isWearingRing("ApryllForever.MutantRings_DarkPhoenixRing"))
                // {
                //   location.sharedLights[goat] = new LightSource(1, new Vector2(who.Position.X + 21f, who.Position.Y + 64f), 5f, new Color(0, 50, 170), (int)__instance.uniqueID + (int)who.UniqueMultiplayerID, LightSource.LightContext.None, who.UniqueMultiplayerID);

                //  Game1.player.startGlowing(Color.OrangeRed, true, .25f);
                //  Game1.player.glowingTransparency = 1f;
                //}
            }
        }

        private static void Ring_OnUnequip_Postfix(Ring __instance, Farmer who)
        {
            GameLocation location;
            location = who.currentLocation;

            if (__instance.Name.Equals("ApryllForever.MutantRings_DarkPhoenixRing"))
            {
                if (location.sharedLights.ContainsKey(fuckitall))
                {
                    who.currentLocation.removeLightSource(fuckitall);
                
                }
            }

        }


            private static void Farmer_takeDamage_Postfix(Farmer __instance, int damage, bool overrideParry, Monster damager)
        {
        if(Game1.player.isWearingRing("ApryllForever.MutantRings_DarkPhoenixRing"))
            {
                if (__instance.health <= 0 && !hasUsedDarkPhoenixRevive.Value)
                {
                    Game1.player.startGlowing(new Color(255, 255, 0), border: false, 0.25f);
                    DelayedAction.functionAfterDelay(delegate
                    {
                        Game1.player.stopGlowing();
                    }, 500);
                    Game1.playSound("yoba");
                    for (int i = 0; i < 13; i++)
                    {
                        float xPos;
                        xPos = Game1.random.Next(-32, 33);
                        Game1.player.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(114, 46, 2, 2), 200f, 5, 1, new Vector2(xPos + 32f, -96f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
                        {
                            attachedCharacter = __instance,
                            positionFollowsAttachedCharacter = true,
                            motion = new Vector2(xPos / 32f, -3f),
                            delayBeforeAnimationStart = i * 50,
                            alphaFade = 0.001f,
                            acceleration = new Vector2(0f, 0.1f)
                        });
                    }
                    Game1.player.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Microsoft.Xna.Framework.Rectangle(157, 280, 28, 19), 2000f, 1, 1, new Vector2(-20f, -16f), flicker: false, flipped: false, 1E-06f, 0f, Color.White, 4f, 0f, 0f, 0f)
                    {
                        attachedCharacter = __instance,
                        positionFollowsAttachedCharacter = true,
                        alpha = 0.1f,
                        alphaFade = -0.01f,
                        alphaFadeFade = -0.00025f
                    });
                    __instance.health = __instance.maxHealth;
                    hasUsedDarkPhoenixRevive.Value = true;
                }






            }
        if(Game1.player.isWearingRing("ApryllForever.MutantRings_MystiqueRing"))
            { }
        
        
        }

        private static void Ring_Update_Postfix(GameTime time, GameLocation environment, Farmer who, Ring __instance)
        {
           
            
            if (who.isWearingRing("ApryllForever.MutantRings_DarkPhoenixRing"))
            {
                Vector2 offset;
                offset = Vector2.Zero;
                if (who.shouldShadowBeOffset)
                {
                    offset += who.drawOffset;
                }

                int goat = __instance.uniqueID + (int)who.UniqueMultiplayerID;

                environment.repositionLightSource(goat, new Vector2(who.Position.X + 21f, who.Position.Y) + offset);
                if (!environment.isOutdoors && !(environment is MineShaft) && !(environment is VolcanoDungeon))
                {
                    LightSource i;
                    i = environment.getLightSource(goat);
                    if (i != null)
                    {
                        i.radius.Value = 3f;
                    }
                }



            }


        }




        }
    }





//tries of the thing

//int goat = __instance.uniqueID + (int)who.UniqueMultiplayerID;

//int? _lightID = AHelper.Reflection.GetField<int?>(__instance, nameof(Ring._lightSourceID)).GetValue();

//var _lightID = typeof(Ring).GetField("_lightSourceID", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(id);

// var _lightID = typeof(Ring).GetField(nameof(Ring._lightSourceID), BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

//int? _lightID = FieldInfo.GetValue(__instance);

//

// int? light = lightSourceField(__instance);

// light = (int)__instance.uniqueID + (int)who.UniqueMultiplayerID;

//id = (int)__instance.uniqueID + (int)who.UniqueMultiplayerID;
// var _lightID = typeof(Ring).GetField("_lightSourceID", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
