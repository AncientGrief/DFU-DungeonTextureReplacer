using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DaggerfallWorkshop;
using UnityEngine;

namespace Game.Mods.DungeonTextures
{
    /// <summary>
    /// Texture Replacer to exchange textures in a dungeon.
    /// </summary>
    public class DungeonTextureReplacer
    {
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private readonly List<(DungeonTexturePack pack, Vector2? range, string blockFilter)> _packs = new List<(DungeonTexturePack, Vector2?, string)>();
        private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
        private readonly List<Material> _createdMaterials = new List<Material>();
        private HashSet<Renderer> _excludedRenderers = new HashSet<Renderer>();

        /// <summary>
        /// Called for each created material. Can be used to apply shaders.
        /// </summary>
        public Action<Material> OnMaterialCreated;

        /// <summary>
        /// Register a pack as global fallback, or limit it to a specific RDB block name (e.g. "N0000012").
        /// Block-specific packs take priority over global ones.
        /// </summary>
        public void RegisterTexturePack(DungeonTexturePack pack, string blockFilter = null) => _packs.Add((pack, null, blockFilter));

        //TODO: Y-Based dungeon texture blending with multiple packs...
        //public void RegisterTexturePack(DungeonTexturePack pack, Vector2 range) => _packs.Add((pack, range, null));

        /// <summary>
        /// Renderers in this set will be skipped during Apply.
        /// </summary>
        public void SetExcludedRenderers(HashSet<Renderer> excluded)
        {
            _excludedRenderers = excluded ?? new HashSet<Renderer>();
        }

        /// <summary>
        /// Apply all registered packs to the dungeon.
        /// </summary>
        public void Apply(DaggerfallDungeon dungeon)
        {
            if (!dungeon || _packs.Count == 0)
                return;

            _originalMaterials.Clear();
            _createdMaterials.Clear();

            Vector3 startPos = dungeon.StartMarker ? dungeon.StartMarker.transform.position : dungeon.transform.position;

            foreach (Transform child in dungeon.transform)
            {
                if (!child.name.StartsWith("DaggerfallBlock")) continue;

                string blockName = ExtractBlockName(child.name);
                float dist = Vector3.Distance(child.position, startPos);
                DungeonTexturePack pack = FindPack(dist, blockName);

                if (pack == null)
                    continue;

                DaggerfallMesh[]      meshes = child.GetComponentsInChildren<DaggerfallMesh>();
                DaggerfallActionDoor[] doors = child.GetComponentsInChildren<DaggerfallActionDoor>();

                ApplyWallTextures(meshes, pack);
                ApplyDoorTextures(doors, pack);
            }
        }

        /// <summary>
        /// Restore all original materials and destroy instances.
        /// </summary>
        public void Revert()
        {
            foreach (KeyValuePair<Renderer, Material[]> pair in _originalMaterials)
            {
                if (pair.Key)
                    pair.Key.sharedMaterials = pair.Value;
            }

            _originalMaterials.Clear();

            foreach (Material mat in _createdMaterials)
            {
                if (mat)
                    UnityEngine.Object.Destroy(mat);
            }

            _createdMaterials.Clear();
        }

        /// <summary>
        /// Logs all original textures per renderer slot. FOR DEBUGGING ONLY
        /// <c>CALL AFTER .Apply()</c>.
        /// </summary>
        /// <param name="outputPath">if set, will dump textures in the given path</param>
        public void DebugDumpTextures(string outputPath = null)
        {
            if (_originalMaterials.Count == 0)
            {
                Debug.Log("DebugDumpTextures: nothing replaced yet (call after Apply)");
                return;
            }

            bool dumpFiles = !string.IsNullOrEmpty(outputPath);

            if (dumpFiles && !Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"DebugDumpTextures: {_originalMaterials.Count} renderers replaced:");

            var dumped = new HashSet<string>();

            foreach (KeyValuePair<Renderer, Material[]> pair in _originalMaterials)
            {
                if (!pair.Key) continue;
                if (pair.Key.GetComponent<DaggerfallActionDoor>()) continue;

                Material[] origMats = pair.Value;
                sb.AppendLine($"  {pair.Key.name} ({origMats.Length} slots):");

                for (int i = 0; i < origMats.Length; i++)
                {
                    if (!origMats[i])
                    {
                        sb.AppendLine($"    [{i}] null");
                        continue;
                    }

                    string matName = origMats[i].name;
                    Texture2D tex = origMats[i].GetTexture(MainTex) as Texture2D;
                    sb.AppendLine($"    [{i}] {matName}  →  {(tex ? "texture found" : "no texture")}");

                    if (!dumpFiles || !tex || !dumped.Add(matName)) continue;

                    try
                    {
                        byte[] png = tex.EncodeToPNG();

                        string safeName = string.Concat(matName.Split(Path.GetInvalidFileNameChars()));
                        string filePath = Path.Combine(outputPath, $"{i} - {safeName}.png");

                        File.WriteAllBytes(filePath, png);
                        sb.AppendLine($"      dumped => {filePath}");
                    }
                    catch (Exception e)
                    {
                        sb.AppendLine($"      dump failed: {e.Message}");
                    }
                }
            }

            Debug.Log(sb.ToString());
        }

        // DaggerfallBlock e.g. "[N0000012]"
        private static string ExtractBlockName(string transformName)
        {
            int start = transformName.IndexOf('[');
            int end   = transformName.IndexOf(']');

            if (start >= 0 && end > start)
                return transformName.Substring(start + 1, end - start - 1);

            return null;
        }

        private DungeonTexturePack FindPack(float dist, string blockName)
        {
            DungeonTexturePack rangePack    = null;
            DungeonTexturePack globalFallback = null;

            foreach ((DungeonTexturePack pack, Vector2? range, string blockFilter) in _packs)
            {
                if (blockFilter != null)
                {
                    if (blockFilter == blockName) return pack;
                    continue;
                }

                if (range != null)
                {
                    if (rangePack == null && dist >= range.Value.x && dist < range.Value.y)
                        rangePack = pack;
                    continue;
                }

                if (globalFallback == null) globalFallback = pack;
            }

            return rangePack ?? globalFallback;
        }

        private void ApplyWallTextures(DaggerfallMesh[] meshes, DungeonTexturePack pack)
        {
            if (!pack.HasWallTextures) return;

            foreach (DaggerfallMesh mesh in meshes)
            {
                if (IsInsideModelsNode(mesh.transform))
                    continue;

                if (mesh.GetComponent<DaggerfallActionDoor>())
                    continue;

                foreach (Renderer rend in mesh.GetComponents<Renderer>())
                {
                    if (_excludedRenderers.Contains(rend)) continue;

                    Material[] origMats = rend.sharedMaterials;
                    _originalMaterials[rend] = origMats;

                    var newMats = new Material[origMats.Length];
                    for (int i = 0; i < origMats.Length; i++)
                    {
                        if (!origMats[i])
                        {
                            newMats[i] = null;
                            continue;
                        }

                        if (pack.IsSlotExcluded(i))
                        {
                            newMats[i] = origMats[i];
                            continue;
                        }

                        Texture2D tex = ResolveWallTexture(pack, origMats[i].name, i);

                        if (!tex)
                        {
                            newMats[i] = origMats[i];
                            continue;
                        }

                        newMats[i] = new Material(origMats[i]);
                        _createdMaterials.Add(newMats[i]);
                        newMats[i].mainTexture = tex;
                        OnMaterialCreated?.Invoke(newMats[i]);
                    }
                    rend.materials = newMats;
                }
            }
        }

        private void ApplyDoorTextures(DaggerfallActionDoor[] doors, DungeonTexturePack pack)
        {
            foreach (DaggerfallActionDoor door in doors)
            {
                Renderer renderer = door.GetComponent<Renderer>();

                if (!renderer)
                    continue;

                if (IsHiddenDoor(renderer))
                {
                    if (!pack.HasWallTextures)
                        continue;

                    ApplyMaterials(renderer, (i, matName) => pack.IsSlotExcluded(i) ? null : ResolveWallTexture(pack, matName, i));
                }
                else
                {
                    if (pack.DoorTextures == null || pack.DoorTextures.Length == 0) continue;

                    Texture2D chosen = pack.DoorTextures[UnityEngine.Random.Range(0, pack.DoorTextures.Length)];

                    ApplyMaterials(renderer, (i, _) => (i == 0 || pack.IsSlotExcluded(i)) ? null : chosen);
                }
            }
        }

        private void ApplyMaterials(Renderer rend, Func<int, string, Texture2D> textureSelector)
        {
            Material[] origMats = rend.sharedMaterials;
            _originalMaterials[rend] = origMats;

            var newMats = new Material[origMats.Length];

            for (int i = 0; i < origMats.Length; i++)
            {
                if (!origMats[i])
                {
                    newMats[i] = null;
                    continue;
                }

                Texture2D tex = textureSelector(i, origMats[i].name);

                if (!tex)
                {
                    newMats[i] = origMats[i];
                    continue;
                }

                newMats[i] = new Material(origMats[i]);
                _createdMaterials.Add(newMats[i]);
                newMats[i].mainTexture = tex;
                OnMaterialCreated?.Invoke(newMats[i]);
            }

            rend.materials = newMats;
        }

        // Archive rule takes priority over WallTextures fallback.
        private static Texture2D ResolveWallTexture(DungeonTexturePack pack, string materialName, int slotIndex)
        {
            Texture2D[] archiveTexs = pack.GetArchiveTextures(materialName);

            if (archiveTexs != null && archiveTexs.Length > 0)
                return archiveTexs[slotIndex % archiveTexs.Length];

            if (pack.WallTextures != null && pack.WallTextures.Length > 0)
                return pack.WallTextures[slotIndex % pack.WallTextures.Length];

            return null;
        }

        /// <summary>
        /// Returns true if this is a hidden door.
        /// Hidden doors use wall textures
        /// visible doors use TEXTURE.374, .074, or .332.
        /// </summary>
        private static bool IsHiddenDoor(Renderer rend)
        {
            foreach (Material mat in rend.sharedMaterials)
            {
                if (!mat) continue;

                if (mat.name.Contains("TEXTURE.374") ||
                    mat.name.Contains("TEXTURE.074") ||
                    mat.name.Contains("TEXTURE.332"))
                    return false;
            }

            return true;
        }

        private static bool IsInsideModelsNode(Transform t)
        {
            while (t)
            {
                if (t.name == "CombinedModels")
                    return false; // Dungeon geometry

                if (t.name == "Models")
                    return true;  // exclude everything else

                t = t.parent;
            }

            return false;
        }
    }
}
