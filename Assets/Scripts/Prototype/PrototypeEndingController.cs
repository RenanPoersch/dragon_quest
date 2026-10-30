using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DragonQuest.Exploration;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeEndingController : MonoBehaviour
    {
        private Dictionary<string,GameObject> maps;
        private PlayerMovement player;
        private CameraFollow follow;
        private bool started;
        private GameEnding ending;
        private CastleDestroyedEndingPresentation aftermath;
        private HeroPossessedPresentation possession;
        private KingFreedPresentation liberation;
        private readonly BattleExitSequence battleExit = new BattleExitSequence();
        public bool IsFadingBattle => battleExit.IsPlaying;
        public float BattleBlackOpacity => battleExit.BlackOpacity;
        public float BattleMusicGain => IsFadingBattle ? battleExit.MusicGain : 1;
        public GameEnding CurrentEnding => started && !IsFadingBattle ? ending : GameEnding.None;
        public float MusicGain => liberation?.Sequence.MusicGain ?? possession?.Sequence.MusicGain
            ?? (aftermath != null ? aftermath.Impact.Sequence.MusicGain * aftermath.Aftermath.Sequence.MusicGain : 0);

        public void Initialize(Dictionary<string,GameObject> roots,PlayerMovement hero,Camera camera,CameraFollow cameraFollow,ObliqueScenery models)
        {maps=roots;player=hero;follow=cameraFollow;}
        public void Show(GameEnding result)
        {
            if(started||(result!=GameEnding.KingFreed&&result!=GameEnding.CastleDestroyed&&result!=GameEnding.HeroPossessed)) return;
            ending=result;started=true;
            player.SetMovementEnabled(false);
            // Keep the live battle and its music until a fully black frame has been painted.
            var battle = GetComponent<PrototypeBattleController>();
            if (battle != null && battle.IsActive && battle.IsCastleEncounter) battleExit.Begin();
            else BeginPresentation();
        }
        private void BeginPresentation()
        {
            GameEnding result = ending;
            foreach(GameObject map in maps.Values) map.SetActive(false);
            player.SetMovementEnabled(false);player.gameObject.SetActive(false);follow.enabled=false;
            if(result==GameEnding.CastleDestroyed)
            {
                aftermath=new CastleDestroyedEndingPresentation();
                return;
            }
            if(result==GameEnding.HeroPossessed)
            {
                possession=new HeroPossessedPresentation();
                return;
            }
            if(result==GameEnding.KingFreed)
            {
                liberation=new KingFreedPresentation();
                return;
            }
        }
        private void ReturnToMenu()
        {
            PrototypeAchievements.Complete(ending);
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        }
        private void Update()
        {
            if (IsFadingBattle) { AdvanceBattleFade(Time.unscaledDeltaTime); return; }
            if(liberation!=null)
            {
                liberation.Update(Time.unscaledDeltaTime);
                if(liberation.Sequence.IsComplete) ReturnToMenu();
                return;
            }
            if(possession!=null)
            {
                possession.Update(Time.unscaledDeltaTime);
                if(possession.Sequence.IsComplete) ReturnToMenu();
                return;
            }
            if(aftermath!=null)
            {
                aftermath.Update(Time.unscaledDeltaTime);
                if(aftermath.IsComplete) ReturnToMenu();
                return;
            }
        }
        private void OnGUI()
        {
            if(!started) return;
            if (IsFadingBattle) { DrawBattleFade(); return; }
            if(liberation!=null) {liberation.Draw();return;}
            if(aftermath!=null) {aftermath.Draw();return;}
            if(possession!=null) {possession.Draw();return;}
        }
        private void AdvanceBattleFade(float delta)
        {
            if (battleExit.Advance(delta)) BeginPresentation();
        }
        private void DrawBattleFade()
        {
            if (Event.current.isKey || Event.current.isMouse) Event.current.Use();
            if (Event.current.type != EventType.Repaint) return;
            battleExit.MarkDrawn();
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            try
            {
                GUI.depth = -10000; GUI.matrix = Matrix4x4.identity;
                GUI.color = new Color(0, 0, 0, BattleBlackOpacity);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }
        private void OnDestroy() {aftermath?.Dispose();possession?.Dispose();liberation?.Dispose();}
    }
}
