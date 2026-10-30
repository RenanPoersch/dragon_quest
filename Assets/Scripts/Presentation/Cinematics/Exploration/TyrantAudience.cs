using DragonQuest.Content;
using System;
using System.Collections.Generic;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Interactions;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // Os companheiros so se separam do heroi durante a audiencia; nao alteram a party salva.
    public sealed class TyrantAudience : MonoBehaviour
    {
        private sealed class Companion
        {
            public GameObject Root;
            public PixelCharacter Character;
            public ThroneRoomDepth Depth;
            public Vector2 Destination;
            public float Delay, WalkDistance;
        }

        private readonly List<Companion> companions = new List<Companion>();
        private PlayerMovement hero;
        private Func<PartyProgress> getParty;
        private ObliqueScenery scenery;
        private Transform tyrant;
        private DialogueController dialogue;
        private int teleportRevision;
        private float elapsed;

        public void Initialize(PlayerMovement player, Func<PartyProgress> party, ObliqueScenery models)
        { hero = player; getParty = party; scenery = models; }

        public void Begin(Transform target, DialogueController conversation)
        {
            Hide();
            PartyProgress party = getParty?.Invoke();
            if (hero == null || target == null || conversation == null || !conversation.IsOpen || party == null) return;
            tyrant = target; dialogue = conversation; elapsed = 0; teleportRevision = hero.TeleportRevision;
            hero.SetMovementEnabled(false);
            hero.FaceTowards(tyrant.position);
            Vector2 origin = hero.transform.position;
            for (int i = 1; i < party.Members.Count; i++)
            {
                if (!TryPosition(origin, out Vector2 destination)) break;
                PartyMember member = party.Members[i];
                var root = new GameObject(member.Name + " / audiencia");
                root.transform.SetParent(transform, false); root.transform.position = origin;
                string atlas = member.Id == "lia" ? "LiaWalk" : member.Id == "bram" ? "BramWalk" : "ArenWalk";
                var visual = scenery.PixelPerson(root.transform, atlas);
                var companion = new Companion { Root = root, Character = visual.GetComponent<PixelCharacter>(),
                    Depth = root.AddComponent<ThroneRoomDepth>(), Destination = destination, Delay = (i - 1) * .16f };
                companion.Character.Present(destination - origin, 0, false, frontalView: true);
                companion.Depth.Refresh(); root.SetActive(false);
                companions.Add(companion);
            }
        }

        private bool TryPosition(Vector2 origin, out Vector2 destination)
        {
            // Tenta os dois lados e depois o lado mais livre, mantendo todos sobre o piso do patamar.
            for (int row = 0; row < 3; row++)
                foreach (int column in new[] { -1, 1, -2, 2, 0 })
                {
                    destination = origin + new Vector2(column * 1.2f, -.65f - row * .9f);
                    if (!ThroneRoomLayout.IsWalkable(destination) || Vector2.Distance(destination, origin) < 1.05f) continue;
                    bool clear = true;
                    foreach (Companion other in companions)
                        if (Vector2.Distance(destination, other.Destination) < 1.05f) clear = false;
                    for (int step = 1; step <= 20 && clear; step++)
                        clear = ThroneRoomLayout.IsWalkable(Vector2.Lerp(origin, destination, step / 20f));
                    if (clear) return true;
                }
            destination = origin; return false;
        }

        private void Update() => Advance(Time.deltaTime);

        private void Advance(float deltaTime)
        {
            if (dialogue == null) return;
            if (!dialogue.IsOpen || dialogue.DialogueId != DialogueCatalog.KingConfrontation.Id || tyrant == null || !tyrant.gameObject.activeInHierarchy
                || hero == null || !hero.gameObject.activeInHierarchy || hero.TeleportRevision != teleportRevision)
            { Hide(); return; }
            float previousTime = elapsed; elapsed += deltaTime;
            foreach (Companion companion in companions)
            {
                if (elapsed <= companion.Delay) continue;
                companion.Root.SetActive(true);
                Vector2 previous = companion.Root.transform.position;
                float movingTime = elapsed - Mathf.Max(previousTime, companion.Delay);
                Vector2 next = Vector2.MoveTowards(previous, companion.Destination, movingTime * 2.8f);
                companion.Root.transform.position = next;
                bool walking = Vector2.Distance(next, companion.Destination) > .001f;
                companion.WalkDistance += Vector2.Distance(previous, next);
                Vector2 direction = walking ? companion.Destination - previous : (Vector2)tyrant.position - next;
                companion.Character.Present(direction, companion.WalkDistance, walking, frontalView: true);
                companion.Depth.Refresh();
            }
        }

        private void Hide()
        {
            foreach (Companion companion in companions)
            {
                companion.Root.SetActive(false);
                if (Application.isPlaying) Destroy(companion.Root);
                else DestroyImmediate(companion.Root);
            }
            companions.Clear(); dialogue = null; tyrant = null;
        }

        private void OnDisable() => Hide();
    }
}
