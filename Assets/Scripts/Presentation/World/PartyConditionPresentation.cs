using System;
using DragonQuest.Equipment;
using DragonQuest.Exploration;
using DragonQuest.Presentation.Battle;
using DragonQuest.Prototype;
using UnityEngine;

namespace DragonQuest.Presentation.World
{
    public sealed class PartyConditionPresentation : IDisposable
    {
        private PlayerMovement movement;
        private PartyProgress Party;
        private SpriteRenderer explorationSprite;
        private GUIStyle poisonNumber;
        private readonly System.Collections.Generic.List<MapPoisonNumber> mapPoisonNumbers = new System.Collections.Generic.List<MapPoisonNumber>();
        private sealed class MapPoisonNumber
        {
            public string Name;
            public int Damage, Lane;
            public float Started;
            public Vector3 Position;
        }

        public void Bind(PlayerMovement player, PartyProgress party)
        {
            Dispose(); movement = player; Party = party;
            Party.PoisonDamage += ShowMapPoison;
            explorationSprite = player.GetComponentInChildren<PixelCharacter>()?.GetComponent<SpriteRenderer>();
        }
        public void RefreshTint()
        {
            if (explorationSprite == null || Party.Members.Count == 0) return;
            var leader = Party.Members[0];
            explorationSprite.color = leader.IsAlive ? BattleStatusColors.Tint(leader.IsPoisoned, leader.BoundActions) : new Color(.4f, .4f, .4f);
        }
        private void ShowMapPoison(PartyMember member, int damage)
        {
            mapPoisonNumbers.Add(new MapPoisonNumber { Name = member.Name, Damage = damage,
                Lane = mapPoisonNumbers.Count % Party.Members.Count, Started = Time.unscaledTime,
                Position = movement.transform.position });
        }
        public void Draw(bool blocked)
        {
            mapPoisonNumbers.RemoveAll(number => Time.unscaledTime - number.Started > 1.4f);
            if (blocked || Camera.main == null) return;
            if (poisonNumber == null)
            {
                poisonNumber = PrototypeTypography.Style(GUI.skin.label, 24, true);
                poisonNumber.alignment = TextAnchor.MiddleCenter;
            }
            foreach (var number in mapPoisonNumbers)
            {
                float elapsed = Time.unscaledTime - number.Started;
                Vector3 point = Camera.main.WorldToScreenPoint(number.Position);
                Rect rect = new Rect(point.x - 120, Screen.height - point.y - 75 - elapsed * 40 - number.Lane * 28, 240, 36);
                Color color = BattleStatusColors.DamageOverTime;
                color.a = Mathf.Clamp01((1.4f - elapsed) / .35f);
                string label = number.Name + " -" + number.Damage;
                poisonNumber.normal.textColor = new Color(0, 0, 0, color.a);
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), label, poisonNumber);
                poisonNumber.normal.textColor = color;
                GUI.Label(rect, label, poisonNumber);
            }
        }
        public void Dispose()
        {
            if (Party != null) Party.PoisonDamage -= ShowMapPoison;
            Party = null; mapPoisonNumbers.Clear();
        }
    }
}
