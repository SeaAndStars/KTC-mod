using HarmonyLib;
using KingdomEnhanced.UI; 

namespace KingdomEnhanced.Hooks
{
    
    /// <summary>Prevents coins from dropping into water when the "Coins Stay Dry" option is enabled.</summary>
    [HarmonyPatch(typeof(CurrencyManagerExt), "CanDropInWater")]
    public class CoinBuoyancyPatch
    {
        
        
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            
            if (ModMenu.CoinsStayDry)
            {
                
                __result = false; 
            }
        }
    }
}