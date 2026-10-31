using DragonQuest.Progression;
using DragonQuest.Presentation.World.Rendering;
using DragonQuest.Presentation.World.Characters;
using DragonQuest.Exploration.Layouts;
using System;
using System.Collections.Generic;
using DragonQuest.Exploration;
using DragonQuest.Input;
using UnityEngine;

namespace DragonQuest.Presentation.Cinematics.Exploration
{
    // A caminhada usa o corredor livre diante das camas da pintura.
    public sealed class InnRestController : MonoBehaviour
    {
        private enum Phase { Walking, FadeOut, Night, FadeIn }
        private sealed class Guest
        {
            public GameObject Root;
            public PixelCharacter Character;
            public Vector2[] Route;
            public int Waypoint;
            public float Delay, Distance;
        }

        public const float WalkSpeed = 2.6f, FadeDuration = 1.2f, NightDuration = .8f;
        private static readonly Vector2[] BedFronts = {
            InnLayout.Ground(.275f,.55f), InnLayout.Ground(.465f,.55f),
            InnLayout.Ground(.37f,.55f)
        };
        private readonly List<Guest> guests = new List<Guest>();
        private readonly Dictionary<Renderer,bool> heroRenderers = new Dictionary<Renderer,bool>();
        private PlayerMovement player;
        private Func<PartyProgress> getParty;
        private Func<bool> atInn, blocked;
        private ObliqueScenery scenery;
        private PartyProgress restingParty;
        private Phase phase;
        private float elapsed;
        private int teleportRevision;
        public bool IsPlaying { get; private set; }
        public float BlackOpacity { get; private set; }
        public bool IsSleeping => IsPlaying && (phase == Phase.FadeOut || phase == Phase.Night);
        public float SleepMusicSeconds { get; private set; }
        public void ConfigureSleepMusic(float seconds)
            => SleepMusicSeconds = float.IsNaN(seconds) || float.IsInfinity(seconds) ? 0 : Mathf.Max(0,seconds);

        public void Initialize(PlayerMovement hero, Func<PartyProgress> party, ObliqueScenery models,
            Func<bool> isAtInn, Func<bool> isBlocked)
        { player=hero; getParty=party; scenery=models; atInn=isAtInn; blocked=isBlocked; }

        public void Begin()
        {
            if (IsPlaying || player==null || atInn?.Invoke()!=true || blocked?.Invoke()==true) return;
            restingParty=getParty?.Invoke();
            if (restingParty==null || restingParty.Members.Count==0) return;
            IsPlaying=true; phase=Phase.Walking; elapsed=0; BlackOpacity=0;
            teleportRevision=player.TeleportRevision;
            player.SetMovementEnabled(false);
            Vector2 origin=player.transform.position;
            Vector2 image=InnLayout.ImagePoint(origin);
            for (int i=0;i<restingParty.Members.Count;i++)
            {
                PartyMember member=restingParty.Members[i];
                Vector2 destination=BedFronts[i%BedFronts.Length];
                Vector2 bedImage=InnLayout.ImagePoint(destination);
                var root=new GameObject(member.Name+" / descanso na pousada");
                root.transform.SetParent(transform,false); root.transform.position=origin;
                string atlas=member.Id=="lia"?"LiaWalk":member.Id=="bram"?"BramWalk":"ArenWalk";
                var visual=scenery.PixelPerson(root.transform,atlas);
                var guest=new Guest { Root=root, Character=visual.GetComponent<PixelCharacter>(),
                    Route=new[] { InnLayout.Ground(image.x,.61f),
                        InnLayout.Ground(bedImage.x,.61f), destination }, Delay=i*.22f };
                guest.Character.Present(guest.Route[0]-origin,0,false);
                root.SetActive(i==0); guests.Add(guest);
            }
            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
            { heroRenderers.Add(renderer,renderer.enabled); renderer.enabled=false; }
            GameInput.ConsumeFrame();
        }

        private void Update()
        {
            if (IsPlaying && GameInput.IsFocused) Advance(Time.unscaledDeltaTime);
        }

        private void Advance(float deltaTime)
        {
            if (!IsPlaying) return;
            if (player==null || atInn?.Invoke()!=true || player.TeleportRevision!=teleportRevision)
            { Finish(); return; }
            player.SetMovementEnabled(false);
            elapsed+=Mathf.Max(0,deltaTime);
            if (phase==Phase.Walking)
            {
                bool arrived=true;
                foreach (Guest guest in guests)
                {
                    if (elapsed<guest.Delay) { arrived=false; continue; }
                    guest.Root.SetActive(true);
                    Vector2 previous=guest.Root.transform.position;
                    float budget=WalkSpeed*Mathf.Min(deltaTime,elapsed-guest.Delay);
                    Vector2 direction=Vector2.zero;
                    while (guest.Waypoint<guest.Route.Length)
                    {
                        Vector2 target=guest.Route[guest.Waypoint];
                        Vector2 current=guest.Root.transform.position;
                        float distance=Vector2.Distance(current,target);
                        if (distance>.001f) direction=target-current;
                        Vector2 next=Vector2.MoveTowards(current,target,budget);
                        float step=Vector2.Distance(current,next);
                        guest.Root.transform.position=next; guest.Distance+=step; budget-=step;
                        if (Vector2.Distance(next,target)>.001f) break;
                        guest.Waypoint++;
                    }
                    bool walking=Vector2.Distance(previous,guest.Root.transform.position)>.0001f;
                    guest.Character.Present(walking?direction:BedFacing(),guest.Distance,walking);
                    if (guest.Waypoint<guest.Route.Length) arrived=false;
                }
                if (arrived) { phase=Phase.FadeOut; elapsed=0; }
                return;
            }
            if (phase==Phase.FadeOut)
            {
                BlackOpacity=Mathf.SmoothStep(0,1,elapsed/FadeDuration);
                if (BlackOpacity<1) return;
                // Somente o preto completo encobre a reuniao do grupo e a recuperacao.
                restingParty.RecoverAtInn();
                player.Teleport(BedFronts[0]); teleportRevision=player.TeleportRevision;
                player.FaceTowards(BedFronts[0]-BedFacing());
                ClearGuests(); RestoreHero();
                phase=Phase.Night; elapsed=0;
            }
            else if (phase==Phase.Night)
            {
                if (elapsed<Mathf.Max(NightDuration,SleepMusicSeconds-FadeDuration)) return;
                phase=Phase.FadeIn; elapsed=0;
            }
            else
            {
                BlackOpacity=Mathf.SmoothStep(1,0,elapsed/FadeDuration);
                if (elapsed>=FadeDuration) Finish();
            }
        }

        private static Vector2 BedFacing() => ObliqueProjection.GroundDirection(Vector2.up);

        private void OnGUI()
        {
            if (!IsPlaying || BlackOpacity<=0) return;
            int depth=GUI.depth; Color color=GUI.color;
            try
            {
                GUI.depth=-1000; GUI.color=new Color(0,0,0,BlackOpacity);
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            }
            finally { GUI.depth=depth; GUI.color=color; }
        }

        private void ClearGuests()
        {
            foreach (Guest guest in guests)
            {
                guest.Root.SetActive(false);
                if (Application.isPlaying) Destroy(guest.Root); else DestroyImmediate(guest.Root);
            }
            guests.Clear();
        }
        private void RestoreHero()
        {
            foreach (var entry in heroRenderers) if (entry.Key!=null) entry.Key.enabled=entry.Value;
            heroRenderers.Clear();
        }
        private void Finish()
        {
            ClearGuests(); RestoreHero(); restingParty=null; BlackOpacity=0; IsPlaying=false;
            if (player!=null) player.SetMovementEnabled(blocked?.Invoke()!=true);
        }
        private void OnDisable() => Finish();
    }
}
