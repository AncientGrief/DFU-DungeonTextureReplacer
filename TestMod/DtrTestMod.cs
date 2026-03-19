using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using UnityEngine;
using Game.Mods.DungeonTextures;

namespace Game.Mods.DTR.TestMod
{
    public class DtrTestMod : MonoBehaviour
    {
        private static Mod _mod;
        private DungeonTextureReplacer _replacer;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            _mod = initParams.Mod;
            new GameObject(_mod.Title).AddComponent<DtrTestMod>();
        }

        private void Awake()
        {
            PlayerEnterExit.OnTransitionDungeonInterior += OnEnterDungeon;
            PlayerEnterExit.OnTransitionDungeonExterior += OnExitDungeon;
            _mod.IsReady = true;
        }

        private void OnDestroy()
        {
            PlayerEnterExit.OnTransitionDungeonInterior -= OnEnterDungeon;
            PlayerEnterExit.OnTransitionDungeonExterior -= OnExitDungeon;
        }

        private void OnEnterDungeon(PlayerEnterExit.TransitionEventArgs args)
        {
            _replacer?.Revert();
            _replacer = new DungeonTextureReplacer();

            //Example for Privateers' Hold
            DungeonTexturePack pack = new DungeonTexturePack();
            Texture2D wall1 = _mod.GetAsset<Texture2D>("0-0");
            Texture2D wall2 = _mod.GetAsset<Texture2D>("1-0");
            Texture2D cavestone = _mod.GetAsset<Texture2D>("2-0");
            Texture2D squareThingy = _mod.GetAsset<Texture2D>("3-0");
            Texture2D stone = _mod.GetAsset<Texture2D>("4-0");

            pack.AddArchiveRule(19,1, wall1);
            pack.AddArchiveRule(19,0, wall2);
            pack.AddArchiveRule(19,3, squareThingy);
            pack.AddArchiveRule(368,0, stone);
            pack.AddArchiveRule(147,1, cavestone);
            pack.AddArchiveRule(20,2, squareThingy); //Tiled stone floor in Privateer's Hold

            // Alternative and fallback (Will replace every non Rule-Texture with one of these
            /*
            pack.SetTextures(
                _mod.GetAsset<Texture2D>("0-0"),
                _mod.GetAsset<Texture2D>("1-0"),
                _mod.GetAsset<Texture2D>("2-0"),
                _mod.GetAsset<Texture2D>("3-0"),
                _mod.GetAsset<Texture2D>("4-0")
                );
            */

            // Door textures are assigned randomly to action doors.
            pack.RegisterDoorTextures(
                _mod.GetAsset<Texture2D>("Door1"),
                _mod.GetAsset<Texture2D>("Door2")
            );

            _replacer.RegisterTexturePack(pack);

            if (args.DaggerfallDungeon)
            {
                _replacer.Apply(args.DaggerfallDungeon);
                // _replacer.DebugDumpTextures(); //Log only
                //_replacer.DebugDumpTextures(@"C:\Temp\DTRDump"); //Dump all texture from the dungeon to a folder for Testing
            }

        }

        private void OnExitDungeon(PlayerEnterExit.TransitionEventArgs args)
        {
            _replacer?.Revert();
            _replacer = null;
        }
    }
}
