using DragonQuest.Progression;
using DragonQuest.Presentation.World.Scenery;
using DragonQuest.Presentation.World.Rendering;
using DragonQuest.Presentation.World.Characters;
using DragonQuest.Presentation.World;
using DragonQuest.Presentation.Cinematics.Exploration;
using DragonQuest.Exploration.Layouts;
using DragonQuest.Content;
using DragonQuest.Combat;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InnRestValidation
{
    private static int checks;
    private static readonly BindingFlags Private = BindingFlags.NonPublic|BindingFlags.Instance;
    private static void Check(bool ok,string reason)
    { checks++; if (!ok) throw new Exception("Inn rest: "+reason); }
    private static void Advance(InnRestController rest,float seconds)
        => typeof(InnRestController).GetMethod("Advance",Private).Invoke(rest,new object[] { seconds });

    [MenuItem("Tools/Dragon Quest/Validate Inn Rest")]
    public static void Run()
    {
        InnValidation.Run();
        string output=Environment.GetEnvironmentVariable("DQ_INN_OUTPUT");
        if (string.IsNullOrEmpty(output)) output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/InnPreview"));
        Directory.CreateDirectory(output);
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("Inn / descanso");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            root.AddComponent<InnPixelScenery>().Build();
            var scenery=new ObliqueScenery();
            var hero=new GameObject("Heroi"); hero.transform.SetParent(root.transform,false);
            var movement=hero.AddComponent<PlayerMovement>();
            typeof(PlayerMovement).GetMethod("Awake",Private).Invoke(movement,null);
            var heroVisual=scenery.PixelPerson(hero.transform,"ArenWalk");
            var hidden=new GameObject("Renderer ja oculto"); hidden.transform.SetParent(hero.transform,false);
            hidden.AddComponent<SpriteRenderer>().enabled=false;
            var dialogue=root.AddComponent<DialogueController>();
            var rest=root.AddComponent<InnRestController>();
            PartyProgress party=PartySetup.Create();
            bool atInn=true, blocked=false;
            rest.Initialize(movement,()=>party,scenery,()=>atInn,()=>blocked||rest.IsPlaying);
            var service=root.AddComponent<InnInteraction>();
            int saves=0;
            service.Initialize(()=>saves++,rest.Begin);
            var context=new InteractionContext(dialogue,new ExplorationProgress(),_=>{},isBusy:()=>rest.IsPlaying);
            service.Interact(context);
            Check(dialogue.Choices.SequenceEqual(new[] { "Salvar","Passar a noite" }),"opcoes exatas da hospedeira");
            dialogue.Close();
            Check(!rest.IsPlaying && saves==0,"cancelar nao dorme nem salva");
            service.Interact(context); dialogue.Choose(0);
            Check(saves==1 && !rest.IsPlaying && !dialogue.IsOpen,"Salvar so executa o salvamento");

            var cameraObject=new GameObject("Camera"); cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<Camera>();
            camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            var follow=cameraObject.AddComponent<CameraFollow>();
            follow.SetArtworkFrame(true,true,InnLayout.ArtAspect);
            follow.Initialize(hero.transform,InnLayout.Bounds,InnLayout.CameraSize,true);

            var starts=new[] { InnLayout.Reception,InnLayout.Ground(.6f,.56f),
                InnLayout.Ground(.8f,.69f) };
            for (int run=0;run<starts.Length;run++)
            {
                movement.Teleport(starts[run]);
                foreach (PartyMember member in party.Members)
                {
                    typeof(PartyMember).GetMethod("RestoreVitals",Private).Invoke(member,
                        new object[] { member==party.Members[2]?0:1,0,true,2 });
                    typeof(DragonQuest.Combat.LimitGauge).GetMethod("Restore",Private).Invoke(member.Limit,new object[] { 47 });
                }
                service.Interact(context); dialogue.Choose(1);
                Check(rest.IsPlaying && saves==1 && !dialogue.IsOpen,"Passar a noite comeca sem salvar");
                Check(!heroVisual.GetComponent<Renderer>().enabled,"heroi real oculto durante a caminhada");
                int guests=root.transform.Cast<Transform>().Count(t=>t.name.EndsWith("/ descanso na pousada"));
                Check(guests==party.Members.Count,"toda a equipe aparece na caminhada");
                rest.Begin();
                Check(root.transform.Cast<Transform>().Count(t=>t.name.EndsWith("/ descanso na pousada"))==guests,
                    "cliques repetidos nao duplicam a equipe");
                Check(!(bool)typeof(PlayerMovement).GetField("movementEnabled",Private).GetValue(movement),
                    "controles bloqueados durante o descanso");
                bool dimmed=false,black=false,revealed=false;
                int steps=0,walkingFrames=0;
                while (rest.IsPlaying && steps++<250)
                {
                    float previous=rest.BlackOpacity;
                    Advance(rest,.05f);
                    foreach (Transform guest in root.transform.Cast<Transform>().Where(t=>t.name.EndsWith("/ descanso na pousada")))
                    {
                        Check(InnLayout.IsWalkable(guest.position),"rota permanece no piso livre");
                        var character=guest.GetComponentInChildren<PixelCharacter>(true);
                        typeof(PixelCharacter).GetField("view",Private).SetValue(character,camera);
                        typeof(PixelCharacter).GetMethod("LateUpdate",Private).Invoke(character,null);
                    }
                    if (rest.BlackOpacity>0 && rest.BlackOpacity<1)
                    {
                        if (rest.BlackOpacity>previous) dimmed=true;
                        if (rest.BlackOpacity<previous) revealed=true;
                    }
                    if (!black && rest.BlackOpacity>=1)
                    {
                        black=true;
                        Check(party.Members.All(m=>m.Hp==m.MaxHp && m.Mp==m.MaxMp && !m.IsPoisoned && m.BoundActions==0),
                            "recuperacao e limpeza apenas no preto completo");
                    }
                    else if (!black)
                        Check(party.Members[0].Hp==1,"caminhada e fade nao antecipam a cura");
                    if (run==0 && rest.BlackOpacity==0 && rest.IsPlaying && ++walkingFrames==25)
                        Capture(camera,Path.Combine(output,"inn-rest-walking.png"));
                    if (run==0 && rest.BlackOpacity>0 && rest.BlackOpacity<.6f && dimmed && !black)
                        Capture(camera,Path.Combine(output,"inn-rest-beds.png"));
                }
                Check(steps<250 && dimmed && black && revealed,"caminhada, fade, noite e retorno terminam");
                Check(rest.BlackOpacity==0 && heroVisual.GetComponent<Renderer>().enabled
                    && !hidden.GetComponent<Renderer>().enabled,"visibilidade original restaurada");
                Check(InnLayout.IsWalkable(movement.transform.position),"heroi acorda perto da cama em piso livre");
                Check((bool)typeof(PlayerMovement).GetField("movementEnabled",Private).GetValue(movement),"controles liberados ao terminar");
                Check(party.Members.All(m=>m.Limit.Charge==47),"descanso preserva Limit");
            }
            heroVisual.GetComponent<PixelCharacter>().Present(movement.FacingDirection,0,false);
            typeof(PixelCharacter).GetField("view",Private).SetValue(heroVisual.GetComponent<PixelCharacter>(),camera);
            typeof(PixelCharacter).GetMethod("LateUpdate",Private).Invoke(heroVisual.GetComponent<PixelCharacter>(),null);
            Capture(camera,Path.Combine(output,"inn-rest-morning.png"));
            blocked=true; rest.Begin(); Check(!rest.IsPlaying,"outros menus bloqueiam o inicio");
            blocked=false; atInn=false; rest.Begin(); Check(!rest.IsPlaying,"descanso restrito a pousada");
            atInn=true; rest.Begin(); rest.enabled=false;
            typeof(InnRestController).GetMethod("OnDisable",Private).Invoke(rest,null);
            Check(!rest.IsPlaying && heroVisual.GetComponent<Renderer>().enabled,"desativar limpa a sequencia");
            rest.enabled=true; rest.Begin(); movement.Teleport(InnLayout.Spawn); Advance(rest,.1f);
            Check(!rest.IsPlaying && heroVisual.GetComponent<Renderer>().enabled,"teleporte interrompe sem deixar heroi invisivel");
            Debug.Log("INN_REST_VALIDATION_OK: "+checks+" checks; escolhas, equipe, rotas, controles, recuperacao, fades e limpeza.");
            File.WriteAllText(Path.Combine(output,"rest-result.txt"),"INN_REST_VALIDATION_OK: "+checks+" checks.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void Capture(Camera camera,string path)
    {
        var target=new RenderTexture(1280,720,24);
        RenderTexture previous=RenderTexture.active;
        var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target; camera.aspect=1280f/720f; camera.Render();
            RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
            Check(texture.GetPixels32().Count(p=>p.r>30 || p.g>30 || p.b>30)>100000,"captura contem o quarto renderizado");
            File.WriteAllBytes(path,texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=null; RenderTexture.active=previous;
            UnityEngine.Object.DestroyImmediate(texture); target.Release(); UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
