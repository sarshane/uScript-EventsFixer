﻿using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SDG.Unturned;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;
using UnityEngine;
using uScript.API.Attributes;
using uScript.Core;
using uScript.Module.Main.Classes;
using uScript.Unturned;

namespace uScript_EventsFix
{

    public class EventPatches
    {
        
        [UsedImplicitly]
        [HarmonyPatch]
        public static class Patches
        {
            
            public delegate void ExpUpdated(Player player);
            public static event ExpUpdated OnExperienceUpdated;
            
            public delegate void PreExpUpdated(Player player, ref uint cost, bool isPositive , ref bool shouldAllow);
            public static event PreExpUpdated OnPreExperienceUpdated;
        
            public delegate void ItemEquipped(Player player, ItemJar item, ref bool shouldAllow);
            public static event ItemEquipped OnPlayerEquippedFixed;

            public delegate void StanceUpdated(Player player, EPlayerStance newStance);
            public static event StanceUpdated OnPlayerStanceUpdatedFixed;
            
            public delegate void ReceiveUpgradeRequestHandler (Player player, ref byte speciality, ref byte index, ref bool force, uint cost,  ref bool shouldAllow);
            public static event ReceiveUpgradeRequestHandler OnPlayerSkillUpdated;
            

            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askSpend))]
            [HarmonyPrefix]
            public static bool askSpend(PlayerSkills __instance, ref uint cost)
            {
                var shouldAllow = false;
                OnPreExperienceUpdated?.Invoke(__instance.player, ref cost, false, ref shouldAllow);
                return !shouldAllow;
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askSpend))]
            [HarmonyPostfix]
            public static void askSpendPostfix(PlayerSkills __instance, uint cost)
            {
                OnExperienceUpdated?.Invoke(__instance.player);
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askAward))]
            [HarmonyPrefix]
            public static bool askAward(PlayerSkills __instance, ref uint award)
            {
                var shouldAllow = false;
                OnPreExperienceUpdated?.Invoke(__instance.player, ref award, true, ref shouldAllow);
                return !shouldAllow;
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askAward))]
            [HarmonyPostfix]
            public static void askAwardPostfix(PlayerSkills __instance, uint award)
            {
                OnExperienceUpdated?.Invoke(__instance.player);
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askPay))]
            [HarmonyPrefix]
            public static bool askPay(PlayerSkills __instance, ref uint pay)
            {
                var shouldAllow = false;
                OnPreExperienceUpdated?.Invoke(__instance.player, ref pay, true, ref shouldAllow);
                return !shouldAllow;
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askPay))]
            [HarmonyPostfix]
            public static void askPayPostfix(PlayerSkills __instance, uint pay)
            {
                OnExperienceUpdated?.Invoke(__instance.player);
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.modXp))]
            [HarmonyPrefix]
            public static bool modXp(PlayerSkills __instance, ref uint xp)
            {
                var shouldAllow = false;
                OnPreExperienceUpdated?.Invoke(__instance.player, ref xp, true, ref shouldAllow);
                return !shouldAllow;
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.modXp))]
            [HarmonyPostfix]
            public static void modXpPostfix(PlayerSkills __instance, uint xp)
            {
                OnExperienceUpdated?.Invoke(__instance.player);
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.modXp2))]
            [HarmonyPrefix]
            public static bool modXp2(PlayerSkills __instance, ref uint xp)
            {
                var shouldAllow = false;
                OnPreExperienceUpdated?.Invoke(__instance.player, ref xp, false, ref shouldAllow);
                return !shouldAllow;
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.modXp2))]
            [HarmonyPostfix]
            public static void modXp2Postfix(PlayerSkills __instance, uint xp)
            {
                OnExperienceUpdated?.Invoke(__instance.player);
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.ReceiveUpgradeRequest))]
            [HarmonyPrefix]
            public static bool ReceiveUpgradeRequest(PlayerSkills __instance, ref byte speciality, ref byte index, ref bool force)
            {
                var cost = __instance.cost(speciality, index);
                var shouldAllow = false;
                OnPlayerSkillUpdated?.Invoke(__instance.player, ref speciality, ref index, ref force, cost, ref shouldAllow);
                return !shouldAllow;
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.ReceiveUpgradeRequest))]
            [HarmonyPostfix]
            public static void ReceiveUpgradeRequestPostfix(PlayerSkills __instance, byte speciality, byte index, bool force)
            {
                 OnExperienceUpdated?.Invoke(__instance.player);
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.ReceiveBoostRequest))]
            [HarmonyPostfix]
            public static void ReceiveUpgradeRequestPostfix(PlayerSkills __instance)
            {
                OnExperienceUpdated?.Invoke(__instance.player);
            }
            
            [HarmonyPatch(typeof(uScript.Module.Main.Events.VehicleDamagedEvent), nameof(uScript.Module.Main.Events.VehicleDamagedEvent.VehicleDamaged))]
            [HarmonyPrefix]
            public static bool VehicleDamagedEvent(ScriptEvent __instance, InteractableVehicle vehicle, Player player, EDamageOrigin cause, ref ushort damage, ref bool allow)
            {
                ExpressionValue[] args = new ExpressionValue[]
                {
                    (vehicle != null) ? ExpressionValue.CreateObject(new VehicleClass(vehicle)) : null,
                    (player != null) ? ExpressionValue.CreateObject(new PlayerClass(player)) : null,
                    (cause != null) ? cause.ToString() : null,
                    (double)damage,
                    !allow
                };
                
                Type type = __instance.GetType();
                MethodInfo methodInfo = type.GetMethod("RunEvent", BindingFlags.NonPublic | BindingFlags.Instance);
                methodInfo?.Invoke(__instance, new object[] { __instance, args });
                
                damage = Convert.ToUInt16((double) args[3]);
                allow = !args[4];

                return false;
            }
            
            
            

            [HarmonyPatch(typeof(PlayerEquipment), nameof(PlayerEquipment.ServerEquip))]
            [HarmonyPrefix]
            public static bool ServerEquip(PlayerEquipment __instance, byte page, byte x, byte y)
            {
                
                if (__instance.isBusy || !__instance.canEquip || __instance.player.life.isDead || __instance.player.stance.stance == EPlayerStance.CLIMB || __instance.player.stance.stance == EPlayerStance.DRIVING || __instance.HasValidUseable && !__instance.IsEquipAnimationFinished || __instance.isTurret)
                    return true;
                if ((int) page == (int) __instance.equippedPage && (int) x == (int) __instance.equipped_x && (int) y == (int) __instance.equipped_y || page == byte.MaxValue)
                {
                        return true;
                }

                if (page < (byte) 0 || (int) page >= (int) PlayerInventory.PAGES - 2)
                    return true;
                byte index = __instance.player.inventory.getIndex(page, x, y);
                if (index == byte.MaxValue)
                    return true;
            
            
                var shouldAllow = false;

                ItemJar jar =
                    __instance.player.inventory.getItem(page, __instance.player.inventory.getIndex(page, x, y));

                OnPlayerEquippedFixed?.Invoke(__instance.player, jar, ref shouldAllow);
                return !shouldAllow;
            }


            [HarmonyPatch(typeof(PlayerStance), "checkStance", new[] {typeof(EPlayerStance), typeof(bool)})]
            [HarmonyPrefix]
            public static bool OnPrePlayerChangedStanceInvoker(PlayerStance __instance, ref EPlayerStance newStance,
                ref bool all)
            {
                // Debug.Log("CheckStance");
                var shouldAllow = true;
                OnPlayerStanceUpdatedFixed?.Invoke(__instance.player, newStance);
                return shouldAllow;
            }
            
        }
    }
    
    [ScriptEvent("OnPlayerSkillUpdated", "player, *speciality, *index, *force, cost, *cancel")]
    public class PlayerSkillUpdated : ScriptEvent
    {
        public override EventInfo EventHook(out object instance)
        {
            instance = null;
            Debug.Log($"Event info: {typeof(EventPatches.Patches).GetEvent("OnPlayerSkillUpdated", BindingFlags.Public | BindingFlags.Static)?.Name}");
            return typeof(EventPatches.Patches).GetEvent("OnPlayerSkillUpdated", BindingFlags.Public | BindingFlags.Static);
        }
    
        [ScriptEventSubscription]
        public void OnPlayerSkillUpdated(Player player, ref byte speciality, ref byte index, ref bool force, uint cost,  ref bool shouldAllow)
        {
            var args = new ExpressionValue[]
            {
                ExpressionValue.CreateObject(new PlayerClass(player)),
                speciality,
                index,
                force,
                cost,
                shouldAllow
            };
            RunEvent(this, args);
            speciality = (byte) args[1];
            index = (byte) args[2];
            force = args[3];
            shouldAllow = args[5];
        }
    }
    
    [ScriptEvent("onPrePlayerExperienceUpdated", "player, *cost, isPositive, *cancel")]
    public class PreExperienceUpdated : ScriptEvent
    {
        public override EventInfo EventHook(out object instance)
        {
            instance = null;
            Debug.Log($"Event info: {typeof(EventPatches.Patches).GetEvent("OnPreExperienceUpdated", BindingFlags.Public | BindingFlags.Static)?.Name}");
            return typeof(EventPatches.Patches).GetEvent("OnPreExperienceUpdated", BindingFlags.Public | BindingFlags.Static);
        }
    
        [ScriptEventSubscription]
        public void OnPreExperienceUpdated(Player player, ref uint cost, bool isPositive, ref bool shouldAllow)
        {
            var args = new ExpressionValue[]
            {
                ExpressionValue.CreateObject(new PlayerClass(player)),
                cost,
                isPositive,
                shouldAllow
            };
            RunEvent(this, args);
            shouldAllow = args[3];
            cost = (uint) args[1];
        }
    }
    
    [ScriptEvent("onPlayerExperienceUpdated", "player")]
    public class ExperienceUpdated : ScriptEvent
    {
        public override EventInfo EventHook(out object instance)
        {
            instance = null;
            Debug.Log($"Event info: {typeof(EventPatches.Patches).GetEvent("OnExperienceUpdated", BindingFlags.Public | BindingFlags.Static)?.Name}");
            return typeof(EventPatches.Patches).GetEvent("OnExperienceUpdated", BindingFlags.Public | BindingFlags.Static);
        }
    
        [ScriptEventSubscription]
        public void OnExperienceUpdated(Player player)
        {
            var args = new ExpressionValue[]
            {
                ExpressionValue.CreateObject(new PlayerClass(player))
            };
            RunEvent(this, args);
        }
    }
    
    
    
    [ScriptEvent("onPlayerEquipped", "player, item, *cancel")]
    public class ItemEquipped : ScriptEvent
    {
        public override EventInfo EventHook(out object instance)
        {
            instance = null; 
            Debug.Log($"Event info: {typeof(EventPatches.Patches).GetEvent("OnPlayerEquippedFixed", BindingFlags.Public | BindingFlags.Static)?.Name}");
            return typeof(EventPatches.Patches).GetEvent("OnPlayerEquippedFixed", BindingFlags.Public | BindingFlags.Static);
        }
    
        [ScriptEventSubscription]
        public void OnItemEquipped(Player player, ItemJar item, ref bool shouldAllow)
        {
            var args = new ExpressionValue[]
            {
                ExpressionValue.CreateObject(new PlayerClass(player)),
                ExpressionValue.CreateObject(new ItemClass(item)),
                shouldAllow
            };
            
            RunEvent(this, args);
            shouldAllow = args[2];
        }
    }
    
    [ScriptEvent("onPlayerStanceUpdated", "player, stance")]
    public class onPlayerStanceUpdated : ScriptEvent
    {
        public override EventInfo EventHook(out object instance)
        {
            instance = null;
            Debug.Log($"Event info: {typeof(EventPatches.Patches).GetEvent("OnPlayerStanceUpdatedFixed", BindingFlags.Public | BindingFlags.Static)?.Name}");
            return typeof(EventPatches.Patches).GetEvent("OnPlayerStanceUpdatedFixed", BindingFlags.Public | BindingFlags.Static);
        }
    
        [ScriptEventSubscription]
        public void PlayerStanceUpdated(Player player, EPlayerStance newStance)
        {
            var args = new ExpressionValue[]
            {
                ExpressionValue.CreateObject(new PlayerClass(player)),
                newStance.ToString()
            };
            
            RunEvent(this, args);
        }
    }
    
    
    
    public class ModuleEvents : ScriptModuleBase
    {
        
        protected override void OnModuleLoaded()
        {
            Debug.Log("uScriptEventsFixer: Using Harmony...");
            
            var HarmonyInstance = new Harmony("uScript_EventsFixer");
            Debug.Log("uScriptEventsFixer: Trying to Patch all methods...");
            HarmonyInstance.PatchAll();
            

            Debug.Log("uScriptEventsFixer: Patched methods list:");
            foreach (var patch in HarmonyInstance.GetPatchedMethods())
            {
                Debug.Log($"uScriptEventsFixer: Patched: {patch.Name}, {patch.ToString()}");
            }
            
            
            Debug.Log("uScriptEventsFixer: Disabling bugged events...");
            
            MethodInfo transpilerMethod = new Func<IEnumerable<CodeInstruction>, IEnumerable<CodeInstruction>>(Transpiler).Method;
            HarmonyInstance.Patch(typeof(uScript.Module.Main.PlayerEvents).GetMethod("Awake",
                BindingFlags.Instance | BindingFlags.NonPublic), transpiler: new HarmonyMethod(transpilerMethod));
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> old) => new CodeInstruction[] { new CodeInstruction(OpCodes.Ret) }.Concat(old);
            Debug.LogWarning("uScript bugged events are successfully fixed!");
        }
    }
    

    
    
}