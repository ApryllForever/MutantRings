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
using System.Threading;
using System.Xml.Linq;
using StardewValley.Buffs;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Projectiles;
using xTile.Dimensions;
using StardewValley.Tools;
using xTile.Tiles;





namespace MutantRings
{
    internal class ModEntry : Mod
    {

        public static IMonitor AMonitor;
        public static IModHelper AHelper;

        [XmlIgnore]
        public static NetBool hasUsedDarkPhoenixRevive = new NetBool(value: false);

        [XmlIgnore]
        public static NetBool hasUsedMadelyneRevive = new NetBool(value: false);

        public const float steamZoom = 4f;
        public const float steamYMotionPerMillisecond = 0.1f;
        private Texture2D steamAnimation;
        private Vector2 steamPosition;
        private float steamYOffset;


        private static Microsoft.Xna.Framework.Rectangle chaosSource = new Microsoft.Xna.Framework.Rectangle(640, 0, 64, 64);
        public static Vector2 chaosPos;

        private static int fuckitall;
        private static int fuckme;
        public static bool MystiqueHeal;
        public static bool StormStrike;
        public static bool JubileeFirework;
        public static bool Magnetobomb;
        public static bool DazzlerFlash;
        public static bool MagmaBlast;
        public static bool ScarletChaos;

        // private static readonly AccessTools.FieldRef<Ring, int?> lightSourceField = AccessTools.FieldRefAccess<Ring, int?>("_lightSourceID");

        private static FieldInfo lightSource = typeof(Ring).GetField("_lightSourceID", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public override void Entry(IModHelper helper)
        {

            AMonitor = Monitor;
            AHelper = helper;

            helper.Events.GameLoop.OneSecondUpdateTicked += GameLoop_OneSecondUpdateTicked;

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
            
            harmony.Patch(
            // original: AccessTools.PropertyGetter(typeof(Monster), nameof(Monster.focusedOnFarmers)),
            original: AccessTools.PropertyGetter(typeof(Monster), nameof(Monster.focusedOnFarmers)),
             postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Monster_focusedOnFarmer_Postfix))
             );
            


            harmony.Patch(
              original: AccessTools.Method(typeof(Ring), nameof(Ring.update)),
              postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Ring_Update_Postfix))
            );

            harmony.Patch(
            original: AccessTools.Method(typeof(Ring), nameof(Ring.onNewLocation)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Ring_onNewLocation_Postfix))
            );

            harmony.Patch(
            original: AccessTools.Method(typeof(Ring), nameof(Ring.onLeaveLocation)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Ring_onLeaveLocation_Postfix))
            );

            harmony.Patch(
            original: AccessTools.Method(typeof(Ring), nameof(Ring.onMonsterSlay)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Ring_onMonsterSlay_Postfix))
            );

            harmony.Patch(
            original: AccessTools.Method(typeof(Ring), nameof(Ring.AddEquipmentEffects)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Ring_AddEquipmentEffects_Postfix))
            );


            harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.drawAboveAlwaysFrontLayer)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.GameLocation_drawAboveAlwaysFrontLayer_Postfix))
            );

        }

        private void GameLoop_OneSecondUpdateTicked(object? sender, StardewModdingAPI.Events.OneSecondUpdateTickedEventArgs e)
        {
          if(e.IsMultipleOf(2))
            {
                MystiqueHeal = false;
            }

          if(e.IsMultipleOf(4))
            {
                StormStrike = false;
                JubileeFirework = false;
                Magnetobomb = false;
                DazzlerFlash = false;
                MagmaBlast = false;
            }
            if (e.IsMultipleOf(20))
            {
                if (Game1.player.isWearingRing("ApryllForever.MutantRings_WolverineRing"))
                {
                    if (Game1.player.health < Game1.player.maxHealth)
                    {
                        Random rnd = new Random();
                        int heal = rnd.Next(12, 23);

                        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + heal);

                    }

                }
            }
            if (e.IsMultipleOf(12))
            {
                if (Game1.player.isWearingRing("ApryllForever.MutantRings_WandaRing"))
                {
                    if (!Game1.shouldTimePass())
                    {
                        return;
                    }

                    {
                        GameLocation location = Game1.player.currentLocation;
                        if (location is SlimeHutch || location is FarmHouse)
                        {
                            return;
                        }
                        Monster closest_monster;
                        closest_monster = Utility.findClosestMonsterWithinRange(location, Game1.player.getStandingPosition(), 500, ignoreUntargetables: true);
                        if (closest_monster != null && !closest_monster.Name.Equals("Truffle Crab"))
                        {
                            Vector2 motion;
                            motion = Utility.getVelocityTowardPoint(Game1.player.getStandingPosition(), closest_monster.getStandingPosition(), 2f);
                            float projectile_rotation;
                            projectile_rotation = (float)Math.Atan2(motion.Y, motion.X) + (float)Math.PI / 2f;
                            BasicProjectile p;
                            p = new BasicProjectile((Game1.random.Next(13, 37)*3 ), 2, 3, 3, 7f, motion.X, motion.Y, Game1.player.getStandingPosition() - new Vector2(32f, 48f), null, null, null, explode: true, damagesMonsters: true, location, Game1.player);
                            p.IgnoreLocationCollision = true;
                            p.ignoreObjectCollisions.Value = true;
                            p.acceleration.Value = motion;
                            p.maxVelocity.Value = 20f;
                            //p.projectileID.Value = 1;
                            p.startingRotation.Value = projectile_rotation;
                            p.alpha.Value = 0.001f;
                            p.alphaChange.Value = 0.05f;
                            p.light.Value = true;
                            //p.collisionSound.Value = "crit";
                            location.projectiles.Add(p);
                            location.playSound("flameSpell");
                        }
                    }
                }


            }

            if (e.IsMultipleOf(9))
            {
                if (Game1.player.isWearingRing("ApryllForever.MutantRings_StormRing"))
                {
                    if (!Game1.shouldTimePass())
                    {
                        return;
                    }

                    {
                        GameLocation location = Game1.player.currentLocation;
                        if (location is SlimeHutch || location is FarmHouse)
                        {
                            return;
                        }

                        Monster closest_monster;
                        closest_monster = Utility.findClosestMonsterWithinRange(location, Game1.player.getStandingPosition(), 300, ignoreUntargetables: true);
                        if (closest_monster != null && !closest_monster.Name.Equals("Truffle Crab"))
                        {
                            bool attacked = location.damageMonster(
                            areaOfEffect: closest_monster.GetBoundingBox(),
                            minDamage: 13 * 1,
                            maxDamage: 37 * 1,
                            knockBackModifier: 1,
                            addedPrecision: 1,
                            critChance: 0,
                            critMultiplier: 0,
                            isBomb: false,
                            triggerMonsterInvincibleTimer: true,
                            who: Game1.player
                                        );
                            Utility.drawLightningBolt(closest_monster.Position, Game1.player.currentLocation);
                            Game1.playSound("thunder");

                        }
                       

                        /*
                        Monster closest_monster;
                        closest_monster = Utility.findClosestMonsterWithinRange(location, Game1.player.getStandingPosition(), 500, ignoreUntargetables: true);
                        if (closest_monster != null && !closest_monster.Name.Equals("Truffle Crab"))
                        {
                            {
                                Vector2 motion;
                                motion = Utility.getVelocityTowardPoint(Game1.player.getStandingPosition(), closest_monster.getStandingPosition(), 2f);
                                float projectile_rotation;
                                projectile_rotation = (float)Math.Atan2(motion.Y, motion.X) + (float)Math.PI / 2f;
                                BasicProjectile p;
                                p = new BasicProjectile(Game1.random.Next(13, 37), 99, 3, 3, 7f, motion.X, motion.Y, Game1.player.getStandingPosition() - new Vector2(32f, 48f), null, null, null, explode: true, damagesMonsters: true, location, Game1.player);
                                p.IgnoreLocationCollision = true;
                                p.ignoreObjectCollisions.Value = true;
                                p.acceleration.Value = motion;
                                p.maxVelocity.Value = 99f;
                                //p.projectileID.Value = 1;
                                p.startingRotation.Value = projectile_rotation;
                                p.alpha.Value = 0.001f;
                                p.alphaChange.Value = 0.05f;
                                p.light.Value = true;
                                // p.collisionSound.Value = "crit";
                                location.projectiles.Add(p);
                                // location.playSound("flameSpell");
                            }

                            //location?.explode(Game1.player.Tile, 7, Game1.player, damageFarmers: false, 23, false);
                            Utility.drawLightningBolt(closest_monster.Position, Game1.player.currentLocation);
                            Game1.playSound("thunder");

                        }
                            */
                    }
                }
            }
        }

        private static void Monster_focusedOnFarmer_Postfix(Monster __instance, ref bool __result)
        {
            if(Game1.player.isWearingRing("ApryllForever.MutantRings_ShadowCatRing"))
            {
                __result = __instance.netFocusedOnFarmers.Value = false;
               // return false;
            }
           // return true;
        } 
        

        private static void Ring_OnEquip_Postfix(Ring __instance, Farmer who)
        {
            GameLocation location;
            location = who.currentLocation;

            //var id = lightSource.GetValue(__instance);

            //lightSource.GetValue(__instance);

            // lightSource.SetValue(__instance, (int)__instance.uniqueID + (int)who.UniqueMultiplayerID);

            


            if (__instance.Name.Equals("Dark Phoenix Ring"))
                {
                int jewel = (int)__instance.uniqueID + (int)who.UniqueMultiplayerID;
                while (location.sharedLights.ContainsKey(jewel))
                {
                    jewel = jewel + 1; //Named after Jewel the singer!
                }

                location.sharedLights[jewel] = new LightSource(1, new Vector2(who.Position.X + 21f, who.Position.Y + 64f), 5f, new Color(0, 170, 0), (int)__instance.uniqueID + (int)who.UniqueMultiplayerID, LightSource.LightContext.None, who.UniqueMultiplayerID);

                fuckitall = jewel; //Otpusti, y zabud, novi den u kazhi put!!!!
             
                


                // if (who.isWearingRing("ApryllForever.MutantRings_DarkPhoenixRing"))
                // {
                //   location.sharedLights[goat] = new LightSource(1, new Vector2(who.Position.X + 21f, who.Position.Y + 64f), 5f, new Color(0, 50, 170), (int)__instance.uniqueID + (int)who.UniqueMultiplayerID, LightSource.LightContext.None, who.UniqueMultiplayerID);

                //  Game1.player.startGlowing(Color.OrangeRed, true, .25f);
                //  Game1.player.glowingTransparency = 1f;
                //}
            }


            if (__instance.Name.Equals("Goblin Queen Ring"))
            {
                int britney = (int)__instance.uniqueID + (int)who.UniqueMultiplayerID;
                while (location.sharedLights.ContainsKey(britney))
                {
                    britney = britney + 1; //Free Britney!!!
                }

                location.sharedLights[britney] = new LightSource(1, new Vector2(who.Position.X + 21f, who.Position.Y + 64f), 5f, new Color(124, 170, 0), (int)__instance.uniqueID + (int)who.UniqueMultiplayerID, LightSource.LightContext.None, who.UniqueMultiplayerID);

                fuckme = britney; //If U seek Amy
            }
        }


            private static void Ring_OnUnequip_Postfix(Ring __instance, Farmer who)
        {
            GameLocation location;
            location = who.currentLocation;

            if (__instance.Name.Equals("Dark Phoenix Ring"))
            {
                if (location.sharedLights.ContainsKey(fuckitall))
                {
                    who.currentLocation.removeLightSource(fuckitall);
                
                }
            }

            if (__instance.Name.Equals("Goblin Queen Ring"))
            {
                if (location.sharedLights.ContainsKey(fuckme))
                {
                    who.currentLocation.removeLightSource(fuckme);

                }
            }

        }


        private static void Farmer_takeDamage_Postfix(Farmer __instance, int damage, bool overrideParry, Monster damager)
        {
            GameLocation location = new GameLocation();
            location = Game1.player.currentLocation;

            if (Game1.player.isWearingRing("ApryllForever.MutantRings_DarkPhoenixRing"))
            {
                if (__instance.health <= 0 && !hasUsedDarkPhoenixRevive.Value)
                {
                    Game1.player.startGlowing(new Color(255, 255, 0), border: false, 0.25f);
                    DelayedAction.functionAfterDelay(delegate
                    {
                        Game1.player.stopGlowing();
                    }, 1500);
                    Game1.playSound("ApryllForever.MutantRings_DarkPhoenix");
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
                    location?.explode(Game1.player.Tile, 3, Game1.player, damageFarmers: false, 99);
                }
            }

                if (Game1.player.isWearingRing("ApryllForever.MutantRings_MadelyneRing"))
                {
                    if (__instance.health <= 0 && !hasUsedMadelyneRevive.Value)
                    {
                        Game1.player.startGlowing(new Color(255, 255, 0), border: false, 0.25f);
                        DelayedAction.functionAfterDelay(delegate
                        {
                            Game1.player.stopGlowing();
                        }, 1500);
                        Game1.playSound("ApryllForever.MutantRings_GoblinQueen");
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
                        hasUsedMadelyneRevive.Value = true;
                        location?.explode(Game1.player.Tile, 4, Game1.player, damageFarmers: false, 999);
                    }


                }
                Random rnd = new Random();
                int heal = rnd.Next(3,13);

                if (Game1.player.isWearingRing("ApryllForever.MutantRings_MystiqueRing"))
                { 
                    if(MystiqueHeal==false)
                Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + heal);
                }
                MystiqueHeal = true;
            if (Game1.player.isWearingRing("ApryllForever.MutantRings_ShadowCatRing"))
            {
                Game1.player.temporarilyInvincible = true;
                Game1.player.flashDuringThisTemporaryInvincibility = true;
                Game1.player.temporaryInvincibilityTimer = 0;
                Game1.player.currentTemporaryInvincibilityDuration = 4000;

            }

            if (Game1.player.isWearingRing("ApryllForever.MutantRings_StormRing"))
            {
               // Vector2 randomTile = new Vector2();
                //Random random = new Random();
               // int randox = random.Next(-7,7);
               // int randoy = random.Next(-7, 7);
                //randomTile = Game1.player.Position;
                //randomTile.X = randomTile.X + (float)randox;
                //randomTile.Y += (float)(randoy);

                //Vector2 rawrTile =  new Vector2();
               // rawrTile = Game1.player.Position;
                //rawrTile.X = Game1.player.Position.X +2;
                //rawrTile.Y = Game1.player.Position.Y +2;

                if (StormStrike == false)
                {
                    Monster closest_monster;
                    closest_monster = Utility.findClosestMonsterWithinRange(location, Game1.player.getStandingPosition(), 300, ignoreUntargetables: true);

                    if (closest_monster != null && !closest_monster.Name.Equals("Truffle Crab"))
                    {
                         location.damageMonster(
                    areaOfEffect: closest_monster.GetBoundingBox(),
                    minDamage: 13 * 2,
                    maxDamage: 37 * 2,
                    knockBackModifier: 1,
                    addedPrecision: 1,
                    critChance: 0,
                    critMultiplier: 0,
                    isBomb: false,
                    triggerMonsterInvincibleTimer: true,
                    who: Game1.player
                                );
                        Utility.drawLightningBolt(closest_monster.Position, Game1.player.currentLocation);
                        Game1.playSound("thunder");
                    }
                    /*
                                        Monster closest_monster;
                                        closest_monster = Utility.findClosestMonsterWithinRange(location, Game1.player.getStandingPosition(), 300, ignoreUntargetables: true);
                                        if (closest_monster != null && !closest_monster.Name.Equals("Truffle Crab"))
                                        {

                                        */
                    /*
                    {
                        Vector2 motion;
                        motion = Utility.getVelocityTowardPoint(Game1.player.getStandingPosition(), closest_monster.getStandingPosition(), 2f);
                        float projectile_rotation;
                        projectile_rotation = (float)Math.Atan2(motion.Y, motion.X) + (float)Math.PI / 2f;
                        BasicProjectile p;
                        p = new BasicProjectile(Game1.random.Next(13, 37), 99, 3, 3, 7f, motion.X, motion.Y, Game1.player.getStandingPosition() - new Vector2(32f, 48f), null, null, null, explode: true, damagesMonsters: true, location, Game1.player);
                        p.IgnoreLocationCollision = true;
                        p.ignoreObjectCollisions.Value = true;
                        p.acceleration.Value = motion;
                        p.maxVelocity.Value = 99f;
                        //p.projectileID.Value = 1;
                        p.startingRotation.Value = projectile_rotation;
                        p.alpha.Value = 0.001f;
                        p.alphaChange.Value = 0.05f;
                        p.light.Value = true;
                        // p.collisionSound.Value = "crit";
                        location.projectiles.Add(p);
                        // location.playSound("flameSpell");
                    }
                    */

                    /*
                        closest_monster.takeDamage(18,0,0,true,0,Game1.player);

                        //location?.explode(Game1.player.Tile, 7, Game1.player, damageFarmers: false, 23, false);
                        Utility.drawLightningBolt(closest_monster.Position, Game1.player.currentLocation);
                        Game1.playSound("thunder");
                    }
                    */


                    //location?.explode(Game1.player.Tile, 7, Game1.player, damageFarmers: false, 23,false);
                    /// Utility.drawLightningBolt(Game1.player.Position, Game1.player.currentLocation);
                    /// Game1.playSound("thunder");

                    StormStrike = true;
                }
            }


            if(Game1.player.isWearingRing("ApryllForever.MutantRings_JubileeRing"))
            {
                if (JubileeFirework == false)
                {


                   //Utility.addRainbowStarExplosion(location, Game1.player.getStandingPosition(),125 );// Game1.random.Next(6, 9));



                    // Game1.player.currentLocation.explode(Game1.player.Tile, 4, Game1.player, false, 17, false);
                    //location?.explode(Game1.player.Position, 7, Game1.player, false, 17, false);
                    {
                        Random random = new Random();
                        int fireworkType;
                        fireworkType = 1; //random.Next(0,3);
                        int spriteX;
                        spriteX = 256 + fireworkType * 16;

                        int idNum;
                        idNum = Game1.random.Next();
                        int idNumFirework;
                        idNumFirework = Game1.random.Next();
                        location.playSound("thudStep");

                        /*
                        Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(spriteX, 397, 16, 16), 2400f, 1, 1, __instance.Tile * 64f, flicker: false, flipped: false, -1f, 0f, Color.White, 4f, 0f, 0f, 0f)
                        {
                            shakeIntensity = 0.5f,
                            shakeIntensityChange = 0.002f,
                            extraInfoForEndBehavior = idNum,
                            endFunction = location.removeTemporarySpritesWithID,
                            layerDepth = (__instance.Tile.Y * 64f + 64f - 16f) / 10000f
                        });*/

                        Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors_1_6", new Microsoft.Xna.Framework.Rectangle(0, 432, 16, 16), 8f, 1, 0, __instance.Tile * 64f, flicker: false, flipped: false, -1f, 0f, Color.White, 4f, 0f, 0f, 0f)
                        {
                            fireworkType = fireworkType,
                            delayBeforeAnimationStart = 1,
                            acceleration = new Vector2(0f, -0.36f + (float)Game1.random.Next(2) / 100f),
                            drawAboveAlwaysFront = true,
                            startSound = "firework",
                            shakeIntensity = 0.5f,
                            shakeIntensityChange = 0.002f,
                            extraInfoForEndBehavior = idNumFirework,
                            endFunction = location.removeTemporarySpritesWithID,
                            id = Game1.random.Next(20, 31),
                            Parent = location,
                            bombDamage = 17,
                            bombRadius = 4,
                            owner = Game1.player

                        });
                        Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 40f, 5, 5, __instance.Tile * 64f + new Vector2(11f, 12f) * 4f, flicker: true, flipped: false, (float)(Game1.player.Tile.Y + 7) / 10000f, 0f, Color.Yellow, 4f, 0f, 0f, 0f)
                        {
                            id = idNum
                        });
                        Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 40f, 5, 5, __instance.Tile * 64f + new Vector2(11f, 12f) * 4f, flicker: true, flipped: true, (float)(Game1.player.Tile.Y + 7) / 10000f, 0f, Color.Orange, 4f, 0f, 0f, 0f)
                        {
                            delayBeforeAnimationStart = 50,
                            id = idNum
                        });
                        Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(598, 1279, 3, 4), 40f, 5, 5, __instance.Tile * 64f + new Vector2(11f, 12f) * 4f, flicker: true, flipped: false, (float)(Game1.player.Tile.Y + 7) / 10000f, 0f, Color.White, 3f, 0f, 0f, 0f)
                        {
                            delayBeforeAnimationStart = 100,
                            id = idNum
                        });
                    

                        JubileeFirework = true;

                    }
                }

            }

            if (Game1.player.isWearingRing("ApryllForever.MutantRings_DazzlerRing"))
            {
                if (DazzlerFlash == false)
                {
                    Utility.addRainbowStarExplosion(location, Game1.player.getStandingPosition(), 256);// Game1.random.Next(6, 9));

                    DazzlerFlash = true;
                }
            }

            if (Game1.player.isWearingRing("ApryllForever.MutantRings_AmaraRing"))
            {
                if (MagmaBlast == false)
                {

                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), Game1.player.getStandingPosition() + new Vector2(-48f, -48f), flipped: false, 0f, Color.White)
                    {
                        interval = 3000f,
                        totalNumberOfLoops = 99999,
                        animationLength = 4,
                        scale = 4f,
                        alphaFade = 0.01f
                    });
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), Game1.player.getStandingPosition() + new Vector2(-24f, -48f), flipped: false, 0f, Color.White)
                    {
                        interval = 3000f,
                        totalNumberOfLoops = 99999,
                        animationLength = 4,
                        scale = 4f,
                        alphaFade = 0.01f
                    });
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), Game1.player.getStandingPosition() + new Vector2(0f, -48f), flipped: false, 0f, Color.White)
                    {
                        interval = 3000f,
                        totalNumberOfLoops = 99999,
                        animationLength = 4,
                        scale = 4f,
                        alphaFade = 0.01f
                    });
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), Game1.player.getStandingPosition() + new Vector2(24f, -48f), flipped: false, 0f, Color.White)
                    {
                        interval = 3000f,
                        totalNumberOfLoops = 99999,
                        animationLength = 4,
                        scale = 4f,
                        alphaFade = 0.01f
                    });
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(276, 1985, 12, 11), Game1.player.getStandingPosition() + new Vector2(48f, -48f), flipped: false, 0f, Color.White)
                    {
                        interval = 3000f,
                        totalNumberOfLoops = 99999,
                        animationLength = 4,
                        scale = 4f,
                        alphaFade = 0.01f
                    });
                   
                    location?.explode(Game1.player.Tile, 5, Game1.player, damageFarmers: false, 33, false);

                    MagmaBlast = true;
                }
            }

            if (Game1.player.isWearingRing("ApryllForever.MutantRings_WandaRing"))
            {
                if (ScarletChaos = false)
                {






                    int idNum;
                    idNum = Game1.random.Next();
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(353, 100f, 1, 2400, Game1.player.getStandingPosition() * 64f, flicker: true, flipped: false, location, Game1.player)
                    {
                        shakeIntensity = 5f,
                        shakeIntensityChange = 0.2f,
                        extraInfoForEndBehavior = idNum,
                        endFunction = location.removeTemporarySpritesWithID
                    });
                    location?.explode(Game1.player.Tile, 7, Game1.player, damageFarmers: false, 33, false);
                    ScarletChaos = true;
                }
            }



            if (Game1.player.isWearingRing("ApryllForever.MutantRings_MagnetoRing"))
            {
                if (Magnetobomb = false)
                {
                    int idNum;
                    idNum = Game1.random.Next();
                    Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(353, 100f, 1, 2400, Game1.player.getStandingPosition() * 64f, flicker: true, flipped: false, location, Game1.player)
                    {
                        shakeIntensity = 5f,
                        shakeIntensityChange = 0.2f,
                        extraInfoForEndBehavior = idNum,
                        endFunction = location.removeTemporarySpritesWithID
                    });
                    location?.explode(Game1.player.Tile, 5, Game1.player, damageFarmers: false, 33, false);
                    Magnetobomb = true;
                }
            }



            }
        
        private static void Ring_Update_Postfix(GameTime time, GameLocation environment, Farmer who, Ring __instance)
        {
           
            
            if (__instance.Name.Equals("Dark Phoenix Ring"))
            {
                Vector2 offset;
                offset = Vector2.Zero;
                if (who.shouldShadowBeOffset)
                {
                    offset += who.drawOffset;
                }

                int goat = __instance.uniqueID + (int)who.UniqueMultiplayerID;

                environment.repositionLightSource(fuckitall, new Vector2(who.Position.X + 21f, who.Position.Y) + offset);
                if (!environment.isOutdoors && !(environment is MineShaft) && !(environment is VolcanoDungeon))
                {
                    LightSource i;
                    i = environment.getLightSource(fuckitall);
                    if (i != null)
                    {
                        i.radius.Value = 3f;
                    }
                }
            }


            if (__instance.Name.Equals("Goblin Queen Ring"))
            {
                Vector2 offset;
                offset = Vector2.Zero;
                if (who.shouldShadowBeOffset)
                {
                    offset += who.drawOffset;
                }

                //int goat = __instance.uniqueID + (int)who.UniqueMultiplayerID;

                environment.repositionLightSource(fuckme, new Vector2(who.Position.X + 21f, who.Position.Y) + offset);
                if (!environment.isOutdoors && !(environment is MineShaft) && !(environment is VolcanoDungeon))
                {
                    LightSource i;
                    i = environment.getLightSource(fuckme);
                    if (i != null)
                    {
                        i.radius.Value = 3f;
                    }
                }
            }



            if (__instance.Name.Equals("Scarlet Witch Ring"))
            {
                

                chaosPos = Game1.updateFloatingObjectPositionForMovement(current: new Vector2(Game1.viewport.X, Game1.viewport.Y), w: chaosPos, previous: Game1.previousViewportPosition, speed: -1f);
                chaosPos.X = (chaosPos.X + 0.5f) % 256f;
                chaosPos.Y = (chaosPos.Y + 0.5f) % 256f;
            }




        }
        

        private static void Ring_onNewLocation_Postfix(Farmer who, GameLocation environment,Ring __instance)
        {
            if (__instance.Name.Equals("Dark Phoenix Ring"))
            {
                environment.removeLightSource(fuckitall);

                environment.sharedLights[fuckitall] = new LightSource(1, new Vector2(who.Position.X + 21f, who.Position.Y + 64f), 10f, new Color(0, 170, 0), LightSource.LightContext.None, who.UniqueMultiplayerID);
            }

            if (__instance.Name.Equals("Goblin Queen Ring"))
            {
                environment.removeLightSource(fuckme);

                environment.sharedLights[fuckme] = new LightSource(1, new Vector2(who.Position.X + 21f, who.Position.Y + 64f), 10f, new Color(124, 170, 0), LightSource.LightContext.None, who.UniqueMultiplayerID);
            }

        }

        private static void Ring_onLeaveLocation_Postfix(Farmer who, GameLocation environment, Ring __instance)
        {



        }


        private static void Ring_onMonsterSlay_Postfix(Monster monster, GameLocation location, Farmer who)
        {
          

            if (Game1.player.isWearingRing("ApryllForever.MutantRings_DarkPhoenixRing") || Game1.player.isWearingRing("ApryllForever.MutantRings_MadelyneRing"))
            {
                location?.explode(monster.Tile, 2, who, damageFarmers: false, -1, !(location is Farm) && !(location is SlimeHutch) && !(location is FarmHouse));

            }

            if (Game1.player.isWearingRing("ApryllForever.MutantRings_RogueRing") && DataLoader.Monsters(Game1.content).TryGetValue(monster.Name, out var result))
            {
                IList<string> objects;
                objects = monster.objectsToDrop;
                string[] objectsSplit;
                objectsSplit = ArgUtility.SplitBySpace(result.Split('/')[6]);
                for (int l = 0; l < objectsSplit.Length; l += 2)
                {
                    if (Game1.random.NextDouble() < Convert.ToDouble(objectsSplit[l + 1]))
                    {
                        objects.Add(objectsSplit[l]);
                    }
                }
                who.health = Math.Min(who.maxHealth, who.health + 10);
                who.Stamina = Math.Min(who.MaxStamina, who.Stamina + 12);

            }
            if (Game1.player.isWearingRing("ApryllForever.MutantRings_EmmaRing"))
            {
                 Item item = ItemRegistry.Create("72");
                if (Game1.random.NextDouble() < 0.2)
                {
                    Game1.createItemDebris(item, Game1.player.getStandingPosition(), 1);
                }
                //location.debris.Add(new Debris(ItemRegistry.Create("(O)72"), monster.Position * 64f + new Vector2(32f, 32f)));
            }

            if (Game1.player.isWearingRing("ApryllForever.MutantRings_MagnetoRing"))
            {
                Item ironore = ItemRegistry.Create("380");
                Item ironbar = ItemRegistry.Create("335");
                Item copperore = ItemRegistry.Create("378");
                Item copperbar = ItemRegistry.Create("334");
                Item goldore = ItemRegistry.Create("384");
                Item goldbar = ItemRegistry.Create("336");
                Item iridiumore = ItemRegistry.Create("386");
                Item iridiumbar = ItemRegistry.Create("337");

                if (monster.Name.Equals("Dwarvish Sentry"))
                {
                    Game1.createItemDebris(ironbar, Game1.player.getStandingPosition(), 1);
                }

                if (monster.Name.Equals("Armored Bug") || monster.Name.Equals("Metal Head") || monster.Name.Equals("Iron Slime") || monster.Name.Equals("Shadow Sniper") || monster.Name.Equals("Carbon Ghost")) 
                {
                    Game1.createItemDebris(ironore, Game1.player.getStandingPosition(), 1);
                }

                if (monster.Name.Equals("Skeleton") || monster.Name.Equals("Copper Slime") || monster.Name.Equals("Rock Crab") || monster.Name.Equals("Skeleton Mage") )
                {
                    Game1.createItemDebris(copperore, Game1.player.getStandingPosition(), 1);
                }

                if (monster.Name.Equals("Hot Head") || monster.Name.Equals("Lava Lurk") || monster.Name.Equals("Lava Crab"))
                {
                    Game1.createItemDebris(goldore, Game1.player.getStandingPosition(), 1);
                }




                if (monster.Name.Equals("Iridium Golem") || monster.Name.Equals("Iridium Bat"))
                {
                    if (Game1.random.NextDouble() <= 0.5)
                    {
                        Game1.createItemDebris(iridiumore, Game1.player.getStandingPosition(), 1);
                    }
                    else
                    {
                        Game1.createItemDebris(goldore, Game1.player.getStandingPosition(), 1);
                    }
                }

            }

        }

        private static void Ring_AddEquipmentEffects_Postfix(BuffEffects effects,Ring __instance)
        {
            if (__instance.Name.Equals("Dark Phoenix Ring"))
            {
                effects.AttackMultiplier.Value += 0.4f;
                effects.KnockbackMultiplier.Value += 0.2f;
                effects.Defense.Value += 2;
                effects.Immunity.Value += 2;
            }

                if (__instance.Name.Equals("Rogue Ring"))
            {
                effects.AttackMultiplier.Value += 0.6f;
                effects.Defense.Value += 2;
                effects.Immunity.Value += 2;

            }

            if (__instance.Name.Equals("Scarlet Witch Ring"))
            {
                effects.AttackMultiplier.Value += 0.6f;
                effects.LuckLevel.Value += 3;
                effects.Speed.Value += 3;
                //effects.WeaponSpeedMultiplier.Value -= 6;

            }

            if (__instance.Name.Equals("Mystique Ring"))
            {
                effects.AttackMultiplier.Value += 0.6f;
                effects.LuckLevel.Value += 3;
                effects.Speed.Value += 3;
                //effects.WeaponSpeedMultiplier.Value -= 6;

            }
            if (__instance.Name.Equals("Goblin Queen Ring"))
            {
                effects.AttackMultiplier.Value += 0.7f;
                effects.CriticalChanceMultiplier.Value += 2;
            }
            if (__instance.Name.Equals("White Queen Ring"))
            {
                effects.AttackMultiplier.Value += 0.2f;
                effects.Defense.Value += 7;
                effects.Immunity.Value += 7;
                effects.CriticalChanceMultiplier.Value += 2;

            }

            if (__instance.Name.Equals("Shadow Cat Ring"))
            {
                effects.CriticalChanceMultiplier.Value += 1;
                effects.AttackMultiplier.Value += 0.1f;
                effects.KnockbackMultiplier.Value += 0.1f;
            }

            if (__instance.Name.Equals("Jubilee Ring"))
            {
                effects.Defense.Value += 1;
                effects.KnockbackMultiplier.Value += 0.1f;
            }

            if (__instance.Name.Equals("Wolverine Ring"))
            {
                effects.AttackMultiplier.Value += 0.6f;
                effects.Defense.Value += 4;
                effects.Immunity.Value += 2;
            }

            if (__instance.Name.Equals("Magneto Ring"))
            {
                effects.AttackMultiplier.Value += 0.3f;
                effects.MagneticRadius.Value += 1024;
                effects.KnockbackMultiplier.Value += 0.3f;
            }




        }

        private static void GameLocation_drawAboveAlwaysFrontLayer_Postfix(SpriteBatch b)
        {
            if (Game1.player.isWearingRing("ApryllForever.MutantRings_WandaRing"))
            // b.End();
            //   b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            {
                Vector2 v;
                v = default(Vector2);
                for (float x = -256 + (int)(chaosPos.X % 256f); x < (float)Game1.graphics.GraphicsDevice.Viewport.Width; x += 256f)
                {
                    for (float y = -256 + (int)(chaosPos.Y % 256f); y < (float)Game1.graphics.GraphicsDevice.Viewport.Height; y += 256f)
                    {
                        v.X = (int)x;
                        v.Y = (int)y;
                        b.Draw(Game1.mouseCursors, v, chaosSource, Color.Crimson, 0f, Vector2.Zero, 4.001f, SpriteEffects.None, 1f);
                    }
                }
            }
        }
          //  b.End();


        





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
