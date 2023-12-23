using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SDG.Unturned;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using uScript.API.Attributes;
using uScript.Core;
using uScript.Module.Main.Classes;
using uScript.Unturned;

namespace uScript_EventsFixer
{


    [ScriptEvent("onPlayerExperienceUpdated", "player, *cost, isPositive, *cancel")]
    public class ExperienceUpdatedFixed : ScriptEvent
    {
        public override EventInfo EventHook(out object instance)
        {
            instance = null;
            return typeof(EventsPatches).GetEvent("OnExperienceUpdatedFixed", BindingFlags.Public | BindingFlags.Static);
        }
    
        [ScriptEventSubscription]
        public void OnExperienceUpdatedFixed(Player player, ref uint cost, bool isPositive,  ref bool shouldAllow)
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
    
    
    [ScriptEvent("onPlayerEquipped", "player, item, *cancel")]
    public class ItemEquipped : ScriptEvent
    {
        public override EventInfo EventHook(out object instance)
        {
            instance = null;
            return typeof(EventsPatches).GetEvent("OnPlayerEquippedFixed", BindingFlags.Public | BindingFlags.Static);
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
    
    [ScriptEvent("onPlayerStanceUpdated", "player, *stance, *cancel")]
    public class OnPlayerStanceUpdatedFixed : ScriptEvent
    {
        public override EventInfo EventHook(out object instance)
        {
            instance = null;
            return typeof(EventsPatches).GetEvent("onPlayerStanceUpdatedFixed", BindingFlags.Public | BindingFlags.Static);
        }
    
        [ScriptEventSubscription]
        public void OnItemEquipped(Player player, ref EPlayerStance newStance, ref bool shouldAllow)
        {
            var args = new ExpressionValue[]
            {
                ExpressionValue.CreateObject(new PlayerClass(player)),
                newStance.ToString(),
                shouldAllow
            };
            
            RunEvent(this, args);
            shouldAllow = args[2];
            Enum.TryParse(args[1], out newStance);
        }
    }

    public class ModuleEvents : ScriptModuleBase
    {
        private Harmony _harmony;
        protected override void OnModuleLoaded()
        {
            Patching();
        }

        void Patching()
        {            
            _harmony = new Harmony("PingFix");
            MethodInfo transpilerMethod = new Func<IEnumerable<CodeInstruction>, IEnumerable<CodeInstruction>>(Transpiler).Method;
            _harmony.Patch(typeof(uScript.Module.Main.PlayerEvents).GetMethod("Awake",
                BindingFlags.Instance | BindingFlags.NonPublic), transpiler: new HarmonyMethod(transpilerMethod));
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> old) => new CodeInstruction[] { new CodeInstruction(OpCodes.Ret) }.Concat(old);
            Debug.Log("uScript bugged events are successfully fixed!");
            _harmony.PatchAll();
        }
        
    }
    
}