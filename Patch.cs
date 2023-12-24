using System;
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
            
            public delegate void ExpUpdated(Player player, ref uint cost, bool isPositive , ref bool shouldAllow);
            public static event ExpUpdated OnExperienceUpdatedFixed;
        
            public delegate void ItemEquipped(Player player, ItemJar item, ref bool shouldAllow);
            public static event ItemEquipped OnPlayerEquippedFixed;

            public delegate void StanceUpdated(Player player, EPlayerStance newStance);
            public static event StanceUpdated OnPlayerStanceUpdatedFixed;
            

            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askSpend))]
            [HarmonyPrefix]
            public static bool AskSpend(PlayerSkills __instance, ref uint cost)
            {
                Debug.Log("AskSpend");
                var shouldAllow = false;
                OnExperienceUpdatedFixed?.Invoke(__instance.player, ref cost, false, ref shouldAllow);
                return !shouldAllow;
            }

            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askAward))]
            [HarmonyPrefix]
            public static bool AskAward(PlayerSkills __instance, ref uint award)
            {
                Debug.Log("AskAward");
                var shouldAllow = false;
                OnExperienceUpdatedFixed?.Invoke(__instance.player, ref award, true, ref shouldAllow);
                return !shouldAllow;
            }

            [HarmonyPatch(typeof(PlayerEquipment), nameof(PlayerEquipment.ServerEquip))]
            [HarmonyPrefix]
            public static bool ServerEquip(PlayerEquipment __instance, byte page, byte x, byte y)
            {
                Debug.Log("ServerEquip");
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
                Debug.Log("CheckStance");
                var shouldAllow = true;
                OnPlayerStanceUpdatedFixed?.Invoke(__instance.player, newStance);
                return shouldAllow;
            }
        }
    }
    
    [ScriptEvent("onPlayerExperienceUpdated", "player, *cost, isPositive, *cancel")]
    public class ExperienceUpdatedFixed : ScriptEvent
    {
        public override EventInfo EventHook(out object instance)
        {
            instance = null;
            Debug.Log($"Event info: {typeof(EventPatches.Patches).GetEvent("OnExperienceUpdatedFixed", BindingFlags.Public | BindingFlags.Static)?.Name}");
            return typeof(EventPatches.Patches).GetEvent("OnExperienceUpdatedFixed", BindingFlags.Public | BindingFlags.Static);
        }
    
        [ScriptEventSubscription]
        public void OnExperienceUpdated(Player player, ref uint cost, bool isPositive, ref bool shouldAllow)
        {
            var args = new ExpressionValue[]
            {
                ExpressionValue.CreateObject(new PlayerClass(player)),
                cost,
                isPositive,
                shouldAllow
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