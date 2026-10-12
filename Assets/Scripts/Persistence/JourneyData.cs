using System;
using System.Runtime.Serialization;

namespace DragonQuest.Persistence
{
    // IDs referenciam o catalogo; o save nunca redefine atributos ou habilidades.
    [DataContract]
    public sealed class JourneyData
    {
        public const int CurrentVersion = 1;
        [DataMember(IsRequired = true)] public int Version;
        [DataMember(IsRequired = true)] public string SavedUtc;
        [DataMember(IsRequired = true)] public string Map;
        [DataMember(IsRequired = true)] public float X;
        [DataMember(IsRequired = true)] public float Y;
        [DataMember(IsRequired = true)] public int Ending;
        [DataMember(IsRequired = true)] public int Gold;
        [DataMember(IsRequired = true)] public string[] OpenedChests;
        [DataMember(IsRequired = true)] public SecretData Secrets;
        [DataMember(IsRequired = true)] public bool DragonDefeated;
        [DataMember(IsRequired = true)] public MemberData[] Members;
        [DataMember(IsRequired = true)] public GearData[] Gear;
        [DataMember(IsRequired = true)] public MateriaData[] Materias;
        [DataMember(IsRequired = true)] public StackData[] Items;
        [DataMember(IsRequired = true)] public StockData[] Stocks;
        [DataMember(IsRequired = true)] public int DeliveryStage;
        [DataMember(IsRequired = true)] public bool DeliveryRestocked;
    }
    [DataContract] public sealed class SecretData
    {
        [DataMember(IsRequired = true)] public bool KnowsPoem;
        [DataMember(IsRequired = true)] public bool AltarSolved;
        [DataMember(IsRequired = true)] public int MonolithStep;
        [DataMember(IsRequired = true)] public bool HasScale;
        [DataMember(IsRequired = true)] public bool PassageOpen;
    }
    [DataContract] public sealed class MemberData
    {
        [DataMember(IsRequired = true)] public string Id;
        [DataMember(IsRequired = true)] public string Weapon;
        [DataMember(IsRequired = true)] public string Armor;
        [DataMember(IsRequired = true)] public int Limit;
    }
    [DataContract] public sealed class GearData
    {
        [DataMember(IsRequired = true)] public string Id;
        [DataMember(IsRequired = true)] public string Definition;
        [DataMember(IsRequired = true)] public string[] Sockets;
    }
    [DataContract] public sealed class MateriaData
    {
        [DataMember(IsRequired = true)] public string Id;
        [DataMember(IsRequired = true)] public string Definition;
    }
    [DataContract] public sealed class StackData
    {
        [DataMember(IsRequired = true)] public string Definition;
        [DataMember(IsRequired = true)] public int Quantity;
    }
    [DataContract] public sealed class StockData
    {
        [DataMember(IsRequired = true)] public string Shop;
        [DataMember(IsRequired = true)] public string Product;
        [DataMember(IsRequired = true)] public int Quantity;
    }
}
