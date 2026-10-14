using System;
using System.Collections.Generic;
using System.IO;
using DragonQuest.Equipment;
using DragonQuest.Progression;
using DragonQuest.Prototype;
using UnityEditor;
using UnityEngine;
using P = DragonQuest.Prototype.PrototypePartyFactory;

public static class InventorySpriteValidation
{
    [MenuItem("Tools/Dragon Quest/Validate Inventory Sprites")]
    public static void Run()
    {
        var keys = new HashSet<string>();
        foreach (var gear in new[] { P.InitialWeapon, P.InitialArmor, P.IronSword, P.RunicStaff, P.IronArmor, P.ArcaneRobe, DragonProgress.Armor })
        {
            Assert(InventoryPixelArt.Texture(gear) != null, "Missing equipment sprite: " + gear.Name);
            Assert(keys.Add(InventoryIconCatalog.Key(gear)), "Equipment must have distinct sprites.");
        }
        var materiaKeys = new HashSet<string>();
        var materias = new[] { P.ThunderMateria, P.FireMateria, P.IceMateria, P.CureMateria, P.CleanseMateria, P.ReviveMateria,
            P.SummonMateria, P.AllMateria, P.InfuseMateria, P.HeavyMateria, P.CleaveMateria, P.GuardianMateria,
            P.EarthMateria, P.ChaosMateria, P.BahamutMateria, P.TitanMateria, P.DefenseMateria, P.HpMateria, P.MpMateria };
        var typeTextures = new Dictionary<MateriaType, Texture2D>();
        foreach (var materia in materias)
        {
            Texture2D texture = InventoryPixelArt.Texture(materia);
            Assert(texture != null, "Missing materia sprite: " + materia.Name);
            if (typeTextures.TryGetValue(materia.Type, out var sameColor))
                Assert(texture == sameColor, "Materias of the same color must share a sprite.");
            else typeTextures.Add(materia.Type, texture);
            materiaKeys.Add(InventoryIconCatalog.Key(materia));
        }
        Assert(materiaKeys.SetEquals(new[] { "materia-green", "materia-blue", "materia-red", "materia-purple" }), "Exactly four materia colors.");
        keys.UnionWith(materiaKeys);
        foreach (var item in new[] { P.Potion, P.Ether, P.Antidote, P.PhoenixFeather })
        {
            Assert(InventoryPixelArt.Texture(item) != null, "Missing item sprite: " + item.Name);
            Assert(keys.Add(InventoryIconCatalog.Key(item)), "Items must have distinct sprites.");
        }
        Assert(keys.Count == 15 && Resources.LoadAll<Sprite>("PixelArt/Inventory").Length == 15, "Expected 15 single-sprite assets.");
        foreach (string key in keys)
        {
            string path = "Assets/Resources/PixelArt/Inventory/" + key + ".png";
            var texture = Resources.Load<Texture2D>("PixelArt/Inventory/" + key);
            var sprite = Resources.Load<Sprite>("PixelArt/Inventory/" + key);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert(texture.width == 64 && texture.height == 64, "Expected native 64x64 pixels: " + key);
            Assert(texture.filterMode == FilterMode.Point && texture.mipmapCount == 1, "Point filtering without mipmaps: " + key);
            Assert(importer.textureCompression == TextureImporterCompression.Uncompressed && importer.alphaIsTransparency, "Uncompressed alpha: " + key);
            Assert(sprite != null && sprite.rect.width == 64 && sprite.rect.height == 64, "Full square sprite: " + key);
            var pixels = texture.GetPixels32(); int visible = 0;
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                byte alpha = pixels[y * 64 + x].a;
                if (alpha > 16) visible++;
                if (x == 0 || x == 63 || y == 0 || y == 63)
                    Assert(alpha <= 8, "Artwork must fit inside transparent margins: " + key);
            }
            Assert(visible >= 200 && visible <= 3500, "Readable isolated silhouette: " + key);
        }
        string report = "Inventory sprite validation passed: 7 equipment sprites, 4 shared materia colors covering 19 definitions, 4 consumables; 15 RGBA 64x64 sprites, point filtering, no mipmaps, uncompressed alpha, transparent margins.";
        string output = Environment.GetEnvironmentVariable("DQ_INVENTORY_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/InventorySpriteValidation"));
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "validation.txt"), report);
        Debug.Log(report);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
