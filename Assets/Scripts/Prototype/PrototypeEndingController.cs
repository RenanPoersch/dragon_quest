using System.Collections.Generic;
using UnityEngine;
using DragonQuest.Input;
using UnityEngine.SceneManagement;
using DragonQuest.Exploration;
using DragonQuest.Progression;

namespace DragonQuest.Prototype
{
    public sealed class PrototypeEndingController : MonoBehaviour
    {
        private Dictionary<string,GameObject> maps;
        private PlayerMovement player;
        private Camera view;
        private CameraFollow follow;
        private ObliqueScenery scenery;
        private string[] pages;
        private int page,shownFrame;
        private GameEnding ending;
        private Texture2D pixel;
        private GUIStyle titleStyle,textStyle,smallStyle,buttonStyle;
        private CastleDestroyedPresentation aftermath;
        private HeroPossessedPresentation possession;
        private KingFreedPresentation liberation;

        public void Initialize(Dictionary<string,GameObject> roots,PlayerMovement hero,Camera camera,CameraFollow cameraFollow,ObliqueScenery models)
        {maps=roots;player=hero;view=camera;follow=cameraFollow;scenery=models;}
        public void Show(GameEnding result)
        {
            if(pages!=null||result==GameEnding.None) return;
            ending=result;pages=EndingNarrative.Pages(result);page=0;shownFrame=Time.frameCount;
            foreach(GameObject map in maps.Values) map.SetActive(false);
            player.SetMovementEnabled(false);player.gameObject.SetActive(false);follow.enabled=false;
            if(result==GameEnding.CastleDestroyed)
            {
                aftermath=new CastleDestroyedPresentation();
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
            scenery.Show("Cena final / "+result,ObliqueScenery.EndingSceneData(result),transform);
            view.orthographic=true;view.rect=new Rect(0,0,1,1);view.ResetAspect();
            view.transform.rotation=Quaternion.AngleAxis(ObliqueProjection.YawDegrees,Vector3.forward)
                *Quaternion.Euler(-ObliqueProjection.TiltDegrees,0,0);
            view.orthographicSize=5.8f;
            view.transform.position=-view.transform.forward*ObliqueProjection.Distance;
            view.backgroundColor=result==GameEnding.KingFreed?new Color(0.22f,0.33f,0.3f):new Color(0.13f,0.1f,0.17f);
        }
        private void Update()
        {
            if(liberation!=null)
            {
                liberation.Update(Time.unscaledDeltaTime);
                if(liberation.Sequence.IsComplete) SceneManager.LoadScene(SceneManager.GetActiveScene().path);
                return;
            }
            if(possession!=null)
            {
                possession.Update(Time.unscaledDeltaTime);
                if(possession.Sequence.IsComplete) SceneManager.LoadScene(SceneManager.GetActiveScene().path);
                return;
            }
            if(aftermath!=null)
            {
                aftermath.Update(Time.unscaledDeltaTime);
                if(aftermath.Sequence.IsComplete) SceneManager.LoadScene(SceneManager.GetActiveScene().path);
                return;
            }
            if(pages==null||Time.frameCount<=shownFrame||!GameInput.IsFocused) return;
            if(GameInput.ConfirmPressed)
            {
                GameInput.ConsumeGamepadFrame();
                if(page<pages.Length-1) page++;
                else SceneManager.LoadScene(SceneManager.GetActiveScene().path);
            }
        }
        private void OnGUI()
        {
            if(pages==null) return;
            if(liberation!=null) {liberation.Draw();return;}
            if(aftermath!=null) {aftermath.Draw();return;}
            if(possession!=null) {possession.Draw();return;}
            EnsureStyles();
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            Matrix4x4 previous=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            Panel(new Rect(28,25,630,78));GUI.Label(new Rect(46,38,590,52),EndingNarrative.Title(ending),titleStyle);
            Panel(new Rect(28,540,1224,153));GUI.Label(new Rect(48,558,1180,69),pages[page],textStyle);
            GUI.Label(new Rect(48,645,960,33),page<pages.Length-1?"E / Espaco / Enter ou Continuar / "+(page+1)+" de "+pages.Length:"Fim da demo. Seu desfecho fica registrado no slot da jornada.",smallStyle);
            bool enabled=GUI.enabled;GUI.enabled=Time.frameCount>shownFrame;
            if(GUI.Button(new Rect(1058,645,174,35),page<pages.Length-1?"Continuar":"Voltar ao menu",buttonStyle))
            {
                if(page<pages.Length-1) page++;
                else SceneManager.LoadScene(SceneManager.GetActiveScene().path);
            }
            GUI.enabled=enabled;GUI.matrix=previous;
        }
        private void Panel(Rect rect)
        {
            Color previous=GUI.color;GUI.color=new Color(0.055f,0.07f,0.1f,0.96f);GUI.DrawTexture(rect,pixel);GUI.color=previous;
        }
        private void EnsureStyles()
        {
            if(pixel!=null) return;
            pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();
            titleStyle=PrototypeTypography.Style(GUI.skin.label,28,true);titleStyle.wordWrap=true;titleStyle.normal.textColor=new Color(0.96f,0.8f,0.48f);
            textStyle=PrototypeTypography.Style(GUI.skin.label,24);textStyle.wordWrap=true;textStyle.normal.textColor=Color.white;
            smallStyle=PrototypeTypography.Style(textStyle,16);buttonStyle=PrototypeTypography.Style(GUI.skin.button,20);
        }
        private void OnDestroy() {aftermath?.Dispose();possession?.Dispose();liberation?.Dispose();if(pixel!=null) Destroy(pixel);}
    }
}
