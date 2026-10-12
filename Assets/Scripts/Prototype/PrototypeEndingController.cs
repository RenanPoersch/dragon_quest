using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

        public void Initialize(Dictionary<string,GameObject> roots,PlayerMovement hero,Camera camera,CameraFollow cameraFollow,ObliqueScenery models)
        {maps=roots;player=hero;view=camera;follow=cameraFollow;scenery=models;}
        public void Show(GameEnding result)
        {
            if(pages!=null||result==GameEnding.None) return;
            ending=result;pages=EndingNarrative.Pages(result);page=0;shownFrame=Time.frameCount;
            foreach(GameObject map in maps.Values) map.SetActive(false);
            player.SetMovementEnabled(false);player.gameObject.SetActive(false);follow.enabled=false;
            scenery.Show("Cena final / "+result,ObliqueScenery.EndingSceneData(result),transform);
            view.orthographicSize=5.8f;
            view.transform.position=-view.transform.forward*ObliqueProjection.Distance;
            view.backgroundColor=result==GameEnding.KingFreed?new Color(0.22f,0.33f,0.3f):new Color(0.13f,0.1f,0.17f);
        }
        private void Update()
        {
            if(pages==null||page>=pages.Length-1||Time.frameCount<=shownFrame||!Application.isFocused) return;
            Keyboard keyboard=Keyboard.current;
            if(keyboard!=null&&(keyboard.eKey.wasPressedThisFrame||keyboard.spaceKey.wasPressedThisFrame||keyboard.enterKey.wasPressedThisFrame)) page++;
        }
        private void OnGUI()
        {
            if(pages==null) return;
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
            titleStyle=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold,wordWrap=true};titleStyle.normal.textColor=new Color(0.96f,0.8f,0.48f);
            textStyle=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true};textStyle.normal.textColor=Color.white;
            smallStyle=new GUIStyle(textStyle){fontSize=14};buttonStyle=new GUIStyle(GUI.skin.button){fontSize=15};
        }
        private void OnDestroy() {if(pixel!=null) Destroy(pixel);}
    }
}
