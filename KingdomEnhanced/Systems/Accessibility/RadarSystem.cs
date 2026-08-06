using UnityEngine;
using System.Collections.Generic;
using KingdomEnhanced.UI;
using KingdomEnhanced.Utils;

namespace KingdomEnhanced.Systems.Accessibility
{
    /// <summary>Sonar-style radar that reports the nearest payable targets to the left and right of the player.</summary>
    public class RadarSystem
    {
        /// <summary>Radar detection range in world units.</summary>
        private float _range = 60f;
        /// <summary>Cached reference to the player.</summary>
        private Player _player;

        /// <summary>Creates a radar system bound to the given player.</summary>
        /// <param name="player">The player to scan around.</param>
        public RadarSystem(Player player)
        {
            _player = player;
        }

        /// <summary>Scans for the nearest payable targets left and right of the player and announces them via speech.</summary>
        public void Pulse()
        {
            if (_player == null) return;
            
            float playerX = _player.transform.position.x;
            Dictionary<string, float> closestLeft = new Dictionary<string, float>();
            Dictionary<string, float> closestRight = new Dictionary<string, float>();

            var payables = Managers.Inst?.payables;
            if (payables != null)
            {
                foreach (var p in payables.AllPayables)
                {
                    if (p == null) continue;
                    var mb = p as MonoBehaviour;
                    if (mb == null || !mb.gameObject.activeInHierarchy) continue;

                    ProcessRadarTarget(mb, playerX, _range, closestLeft, closestRight);
                }
            }
            
            List<string> results = new List<string>();
            foreach (var kvp in closestLeft) results.Add($"{kvp.Key} {Mathf.RoundToInt(kvp.Value)}m Left");
            foreach (var kvp in closestRight) results.Add($"{kvp.Key} {Mathf.RoundToInt(kvp.Value)}m Right");

            ModMenu.Speak(results.Count > 0 ? string.Join(", ", results) : "No targets found");
        }

        /// <summary>Processes a single target: resolves its type and keeps the closest distance per type on each side.</summary>
        /// <param name="mb">The target's MonoBehaviour.</param>
        /// <param name="playerX">The player's world X position.</param>
        /// <param name="range">Maximum detection range in world units.</param>
        /// <param name="left">Closest distances of targets to the left, keyed by type name.</param>
        /// <param name="right">Closest distances of targets to the right, keyed by type name.</param>
        private void ProcessRadarTarget(MonoBehaviour mb, float playerX, float range, Dictionary<string, float> left, Dictionary<string, float> right)
        {
            float dist = mb.transform.position.x - playerX;
            if (Mathf.Abs(dist) > range || Mathf.Abs(dist) < 3) return;

            string n = mb.name.ToLower();
            string type = "";

            var shopTag = mb.GetComponent<ShopTag>();
            if (shopTag != null)
            {
                type = PayableNameResolver.GetShopTypeName(shopTag.type);
            }
            else
            {
                if (n.Contains("beggar")) type = "Beggar";
                else if (n.Contains("chest")) type = "Chest";
                else if (n.Contains("portal")) type = "Portal";
                else if (n.Contains("merchant")) type = "Merchant";
                else if (n.Contains("statue")) type = "Statue";
                else if (n.Contains("dog")) type = "Dog";
                else if (n.Contains("hermit")) type = "Hermit";
            }

            if (type == "") return;

            float absDist = Mathf.Abs(dist);
            var dict = dist > 0 ? right : left;
            
            if (!dict.ContainsKey(type) || absDist < dict[type]) 
                dict[type] = absDist;
        }
    }
}
