using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using KingdomEnhanced.Core;
using KingdomEnhanced.UI; 

namespace KingdomEnhanced.Utils
{
    /// <summary>
    /// Resolves stable canonical English names for game objects and shops.
    /// </summary>
    public static class PayableNameResolver
    {
        // Maps raw in-game object names to stable canonical English names
        private static readonly Dictionary<string, string> _nameMapping = new Dictionary<string, string>
        {
            { "P1", "Peasant" },
            { "P2", "Worker" }, 
            { "Griffin", "Griffin Mount" },
            { "Stag", "Stag Mount" },
            { "Warhorse", "Warhorse Mount" },
            { "Unicorn", "Unicorn Mount" },
            { "Lizard", "Lizard Mount" },
            { "Bear", "Bear Mount" },
            { "Beetle", "Beetle Mount" },
            { "Scaffold", "Construction" },
            { "Boat Sail Position", "Boat" },
            { "Boat Sale Position", "Boat" },
            { "Border", "Portal" },
            { "Tower B", "Tower" },
            { "Tower A", "Tower" },
            { "Tower C", "Tower" },
            { "Tree Pin", "Tree" },
            { "Shop Hammer", "Builder Shop" },
            { "Shop Bow", "Archer Shop" },
            { "Shop Hammer Deadlands", "Builder Shop" },
            { "Shop Bow Deadlands", "Archer Shop" },
            { "Shop Hammer Dead Lands", "Builder Shop" },
            { "Shop Bow Dead Lands", "Archer Shop" },
            { "Tree Deadlands", "Dead Tree" },
            { "Tree Noleaves Deadlands", "Bare Dead Tree" },
            { "Tree Dead Lands", "Dead Tree" },
            { "Tree Noleaves Dead Lands", "Bare Dead Tree" },
            { "Castle Deadlands", "Castle" },
            { "Castle Dead Lands", "Castle" },
            { "Hermes Shade", "Hermes Statue" },
            { "Statue Pike", "Pikeman Statue" },
            { "Tree Cypress", "Tree" },
            { "Tree Pine", "Tree" },
            { "Tree Wild Pear", "Tree" },
            { "Tree Olive", "Tree" },
            { "Statue Archer", "Archer Statue" },
            { "Statue Builder", "Builder Statue" },
            { "Statue Farmer", "Farmer Statue" },
            { "Statue Knight", "Knight Statue" },
            { "BBB", "Banner" },
            { "Shop Scythe", "Farmer Shop" },
            { "Beggar", "Vagrant" },
            { "Villager", "Citizen" },
            { "Ronin", "Ronin" },
            { "Hoplite", "Hoplite" },
            { "Slinger", "Slinger" },
            { "Gamigin", "Gamigin Mount" },
            { "Gined", "Gined Mount" },
            { "Wolf", "Fenrir Mount" },
            { "Reindeer", "Reindeer Mount" },
            { "Sleipnir", "Sleipnir Mount" },
            { "Cat Chariot", "Cat Chariot" },
            { "Kelpie", "Kelpie Mount" },
            { "Hippocampus", "Hippocampus Mount" },
            { "Cerberus", "Cerberus Mount" },
            { "Pegasus", "Pegasus Mount" },
            { "Donkey", "Donkey Mount" },
            { "Quarry", "Stone Quarry" },
            { "Mine", "Iron Mine" },
            { "Dojo", "Dojo" },
            { "Ballista", "Ballista Tower" },
            { "Bakery", "Bakery" },
            { "Stable", "Stable" },
            { "Horn", "Horn Wall" },
            { "Lighthouse", "Lighthouse" },
            { "Lighthouse undeveloped", "Lighthouse" },
            { "Citizen House", "Citizen House" },
            { "Forge", "Forge" }
        };

        // Removes digits and dashes (runtime instance markers)
        private static readonly Regex _digitDashRegex = new Regex(@"[\d-]", RegexOptions.Compiled);
        // Removes a trailing space followed by a single capital letter (instance marker)
        private static readonly Regex _trailingUpperRegex = new Regex(@"\s[A-Z]$", RegexOptions.Compiled);
        // Removes " P#" tokens such as the worker marker "P2"
        private static readonly Regex _pNumberRegex = new Regex(@"\sP\d+", RegexOptions.Compiled);
        // Inserts a space between camelCase words
        private static readonly Regex _camelCaseRegex = new Regex("([a-z])([A-Z])", RegexOptions.Compiled);
        // Collapses runs of whitespace into a single space
        private static readonly Regex _multiSpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
        // Removes parenthesized suffixes such as "(Clone)"
        private static readonly Regex _parenRegex = new Regex(@"\s*\(.*?\)", RegexOptions.Compiled);
        // Strips biome/landscape keywords when name simplification is enabled
        private static readonly Regex _biomeRegex = new Regex(
            @"(?i)\b(bamboo|iron|stone|dead|lands|scaffold|wreck|grove|grace|pin|sale|jade|norse|norselands|shogun|dire|plague|europe|greece|cypress|pine|olive|wild|pear|p2|olympus|dynasty|viking|challenge|hickory|oak|birch|apple|cherry|palm|spruce|fir|willow|maple|walnut|chestnut)\b", RegexOptions.Compiled);

        /// <summary>
        /// Cleans a game object name into a stable canonical English name for internal object type and interaction rule checks.
        /// </summary>
        /// <param name="original">Original or prefab name of the game object.</param>
        /// <returns>Canonical English name with runtime suffixes removed and known mappings applied.</returns>
        public static string CleanName(string original)
        {
            return GetCanonicalName(original);
        }

        /// <summary>
        /// Converts a game object name into an accessibility display name in the current language.
        /// </summary>
        /// <param name="original">Original or prefab name of the game object.</param>
        /// <returns>Localized display name; the canonical English name for unknown objects.</returns>
        public static string GetLocalizedDisplayName(string original)
        {
            string canonicalName = CleanName(original);
            switch (canonicalName)
            {
                case "Peasant": return LocalizationService.Get("payable.name.peasant");
                case "Worker": return LocalizationService.Get("payable.name.worker");
                case "Griffin Mount": return LocalizationService.Get("payable.name.griffin_mount");
                case "Stag Mount": return LocalizationService.Get("payable.name.stag_mount");
                case "Warhorse Mount": return LocalizationService.Get("payable.name.warhorse_mount");
                case "Unicorn Mount": return LocalizationService.Get("payable.name.unicorn_mount");
                case "Lizard Mount": return LocalizationService.Get("payable.name.lizard_mount");
                case "Bear Mount": return LocalizationService.Get("payable.name.bear_mount");
                case "Beetle Mount": return LocalizationService.Get("payable.name.beetle_mount");
                case "Construction": return LocalizationService.Get("payable.name.construction");
                case "Boat": return LocalizationService.Get("payable.name.boat");
                case "Portal": return LocalizationService.Get("payable.name.portal");
                case "Tower": return LocalizationService.Get("payable.name.tower");
                case "Tree": return LocalizationService.Get("payable.name.tree");
                case "Builder Shop": return LocalizationService.Get("payable.name.builder_shop");
                case "Archer Shop": return LocalizationService.Get("payable.name.archer_shop");
                case "Dead Tree": return LocalizationService.Get("payable.name.dead_tree");
                case "Bare Dead Tree": return LocalizationService.Get("payable.name.bare_dead_tree");
                case "Castle": return LocalizationService.Get("payable.name.castle");
                case "Hermes Statue": return LocalizationService.Get("payable.name.hermes_statue");
                case "Pikeman Statue": return LocalizationService.Get("payable.name.pikeman_statue");
                case "Archer Statue": return LocalizationService.Get("payable.name.archer_statue");
                case "Builder Statue": return LocalizationService.Get("payable.name.builder_statue");
                case "Farmer Statue": return LocalizationService.Get("payable.name.farmer_statue");
                case "Knight Statue": return LocalizationService.Get("payable.name.knight_statue");
                case "Banner": return LocalizationService.Get("payable.name.banner");
                case "Farmer Shop": return LocalizationService.Get("payable.name.farmer_shop");
                case "Vagrant": return LocalizationService.Get("payable.name.vagrant");
                case "Citizen": return LocalizationService.Get("payable.name.citizen");
                case "Ronin": return LocalizationService.Get("payable.name.ronin");
                case "Hoplite": return LocalizationService.Get("payable.name.hoplite");
                case "Slinger": return LocalizationService.Get("payable.name.slinger");
                case "Gamigin Mount": return LocalizationService.Get("payable.name.gamigin_mount");
                case "Gined Mount": return LocalizationService.Get("payable.name.gined_mount");
                case "Fenrir Mount": return LocalizationService.Get("payable.name.fenrir_mount");
                case "Reindeer Mount": return LocalizationService.Get("payable.name.reindeer_mount");
                case "Sleipnir Mount": return LocalizationService.Get("payable.name.sleipnir_mount");
                case "Cat Chariot": return LocalizationService.Get("payable.name.cat_chariot");
                case "Kelpie Mount": return LocalizationService.Get("payable.name.kelpie_mount");
                case "Hippocampus Mount": return LocalizationService.Get("payable.name.hippocampus_mount");
                case "Cerberus Mount": return LocalizationService.Get("payable.name.cerberus_mount");
                case "Pegasus Mount": return LocalizationService.Get("payable.name.pegasus_mount");
                case "Donkey Mount": return LocalizationService.Get("payable.name.donkey_mount");
                case "Stone Quarry": return LocalizationService.Get("payable.name.stone_quarry");
                case "Iron Mine": return LocalizationService.Get("payable.name.iron_mine");
                case "Dojo": return LocalizationService.Get("payable.name.dojo");
                case "Ballista Tower": return LocalizationService.Get("payable.name.ballista_tower");
                case "Bakery": return LocalizationService.Get("payable.name.bakery");
                case "Stable": return LocalizationService.Get("payable.name.stable");
                case "Horn Wall": return LocalizationService.Get("payable.name.horn_wall");
                case "Lighthouse": return LocalizationService.Get("payable.name.lighthouse");
                case "Citizen House": return LocalizationService.Get("payable.name.citizen_house");
                case "Forge": return LocalizationService.Get("payable.name.forge");
                default: return canonicalName;
            }
        }

        /// <summary>
        /// Cleans a game object name into a stable canonical English name for internal object type and interaction rule checks.
        /// </summary>
        /// <param name="original">Original or prefab name of the game object.</param>
        /// <returns>Canonical English name with runtime suffixes removed and known mappings applied.</returns>
        public static string GetCanonicalName(string original)
        {
            if (string.IsNullOrEmpty(original)) return "";

            string s = original.Trim();

            s = s.Replace("(Clone)", "").Replace("_", " ");

            s = _parenRegex.Replace(s, "");
            s = _pNumberRegex.Replace(s, "");
            s = _digitDashRegex.Replace(s, "");
            s = _trailingUpperRegex.Replace(s, "");
            s = _camelCaseRegex.Replace(s, "$1 $2");
            s = _multiSpaceRegex.Replace(s, " ").Trim();

            if (_nameMapping.TryGetValue(s, out string mapped)) return mapped;

            if (ModMenu.SimplifyNames)
            {
                s = _biomeRegex.Replace(s, "");
                s = _multiSpaceRegex.Replace(s, " ").Trim();
            }

            if (_nameMapping.TryGetValue(s, out string mappedAfterStrip)) return mappedAfterStrip;

            return s;
        }
        
        /// <summary>
        /// Gets the canonical English shop name from a raycast hit.
        /// </summary>
        /// <param name="hit">The raycast hit result.</param>
        /// <returns>An empty string when no shop can currently be resolved.</returns>
        public static string GetShopTypeName(RaycastHit hit) 
        {
             return "";
        }
        
        /// <summary>
        /// Gets a stable canonical English name from a shop type.
        /// </summary>
        /// <param name="type">In-game shop type enum.</param>
        /// <returns>Canonical English shop name; an empty string for unknown types.</returns>
        public static string GetShopTypeName(PayableShop.ShopType type)
        {
            switch(type)
            {
                case PayableShop.ShopType.Bow: return "Archer Shop";
                case PayableShop.ShopType.Hammer: return "Builder Shop";
                case PayableShop.ShopType.Scythe: return "Farmer Shop";
                case PayableShop.ShopType.PikeLeft: 
                case PayableShop.ShopType.PikeRight: return "Pikeman Shop";
                case PayableShop.ShopType.ShieldShopLeft:
                case PayableShop.ShopType.ShieldShopRight: return "Shield Shop";
                case PayableShop.ShopType.Forge: return "Forge";
                case PayableShop.ShopType.NinjaLeft:
                case PayableShop.ShopType.NinjaRight: return "Ninja House";
                case PayableShop.ShopType.WorkshopLeft:
                case PayableShop.ShopType.WorkshopRight: return "Catapult Workshop";
                default: return "";
            }
        }
    }
}
