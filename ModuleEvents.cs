using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace uScript_EventsFixer
{
    public class EventsPatches
    {
        public delegate void ExpUpdated(Player player, ref uint cost, bool isPositive , ref bool shouldAllow);
        public static event ExpUpdated OnExperienceUpdatedFixed;
        
        
        public delegate void ItemEquipped(Player player, ItemJar item, ref bool shouldAllow);
        public static event ItemEquipped OnPlayerEquippedFixed;
        
        
        public delegate void StanceUpdated(Player player, ref EPlayerStance newStance, ref bool shouldAllow);
        public static event StanceUpdated OnPlayerStanceUpdatedFixed;
        
        
        
        
        
        [UsedImplicitly]
        [HarmonyPatch]
        public static class Patches
        {
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askSpend))]
            [HarmonyPrefix]
            public static bool AskSpend(PlayerSkills __instance, ref uint cost)
            {
                var shouldAllow = false;
                OnExperienceUpdatedFixed?.Invoke(__instance.player, ref cost, false ,ref shouldAllow);
                return !shouldAllow;
            }
            
            [HarmonyPatch(typeof(PlayerSkills), nameof(PlayerSkills.askAward))]
            [HarmonyPrefix]
            public static bool AskAward(PlayerSkills __instance, ref uint award)
            {
                var shouldAllow = false;
                OnExperienceUpdatedFixed?.Invoke(__instance.player, ref award, true , ref shouldAllow);
                return !shouldAllow;
            }
            
            [HarmonyPatch(typeof(PlayerEquipment), nameof(PlayerEquipment.ServerEquip))]
            [HarmonyPrefix]
            public static bool ServerEquip(PlayerEquipment __instance, byte page, byte x, byte y)
            {
                var shouldAllow = false;
                
                ItemJar jar = __instance.player.inventory.getItem(page, __instance.player.inventory.getIndex(page, x, y));
                
                OnPlayerEquippedFixed?.Invoke(__instance.player, jar, ref shouldAllow);
                return !shouldAllow;
            }
            
            [HarmonyPatch(typeof(PlayerStance), "checkStance", new[] {typeof(EPlayerStance), typeof(bool)})]
            [HarmonyPrefix]
            internal static bool CheckStance(PlayerStance __instance, ref EPlayerStance newStance, bool all)
            {
                var shouldAllow = false;
                OnPlayerStanceUpdatedFixed?.Invoke(__instance.player, ref newStance, ref shouldAllow);
                return !shouldAllow;
            }
            
        }
        
    }
    
}