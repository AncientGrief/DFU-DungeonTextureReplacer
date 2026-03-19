using System.Collections.Generic;
using UnityEngine;

namespace Game.Mods.DungeonTextures
{
    /// <summary>
    /// A set of textures to apply to dungeon geometry and action doors.
    /// </summary>
    public class DungeonTexturePack
    {
        /// <summary>
        /// Textures cycled across material slots on wall/floor/ceiling geometry.
        /// Used as global fallback when no archive rule matches.
        /// </summary>
        public Texture2D[] WallTextures { get; private set; }

        /// <summary>
        /// Textures randomly assigned to action doors.
        /// </summary>
        public Texture2D[] DoorTextures { get; private set; }

        private readonly HashSet<int> _excludedSlots = new HashSet<int>();
        private readonly Dictionary<string, Texture2D[]> _archiveRules = new Dictionary<string, Texture2D[]>();

        public void SetTextures(params Texture2D[] textures) => WallTextures = textures;

        public void RegisterDoorTextures(params Texture2D[] textures) => DoorTextures = textures;

        /// <summary>
        /// Material slots in this set will keep their original texture.
        /// </summary>
        public void ExcludeSlots(params int[] slots)
        {
            foreach (int slot in slots)
                _excludedSlots.Add(slot);
        }

        /// <summary>
        /// Add a texture rule for all records in an archive.
        /// </summary>
        public void AddArchiveRule(int archive, params Texture2D[] textures) => _archiveRules[ArchiveKey(archive)] = textures;

        /// <summary>
        /// Add a texture rule for a specific archive + record
        /// </summary>
        public void AddArchiveRule(int archive, int index, params Texture2D[] textures) => _archiveRules[ArchiveKey(archive, index)] = textures;

        private static string ArchiveKey(int archive) => $"TEXTURE.{archive:D3}";
        private static string ArchiveKey(int archive, int index) => $"TEXTURE.{archive:D3} [Index={index}]";

        /// <summary>
        /// Returns true if any wall textures or archive rules are configured.
        /// </summary>
        public bool HasWallTextures => (WallTextures != null && WallTextures.Length > 0) || _archiveRules.Count > 0;

        /// <summary>
        /// Returns true if the given slot should keep its original texture.
        /// </summary>
        public bool IsSlotExcluded(int slot) => _excludedSlots.Contains(slot);

        /// <summary>
        /// Returns the archive rule textures for the given material name, or null if no rule matches.
        /// </summary>
        public Texture2D[] GetArchiveTextures(string materialName)
        {
            foreach (KeyValuePair<string, Texture2D[]> rule in _archiveRules)
                if (materialName.StartsWith(rule.Key))
                    return rule.Value;

            return null;
        }
    }
}
