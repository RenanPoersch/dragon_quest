using UnityEngine;

namespace DragonQuest.Prototype
{
    // Atlas 4x2: madeira, tecido, latao, ceramica, cristal verde/violeta, almofada, piso.
    public static class MateriaShopArt
    {
        private static readonly Color White = new Color(1, 1, 1, 1);
        private static void Surface(ObliqueMeshData m, int tile) => m.DefaultSurface = (PixelSurface)tile;
        public static ObliqueMeshData Build()
        {
            var m = new ObliqueMeshData();
            Surface(m, 7); m.Plane(0, 0, 14, 10, 0.02f, White);
            Surface(m, 0);
            m.Box(0, 4.7f, 14, .6f, 0, 2.8f, White);
            m.Box(-6.7f, 0, .6f, 10, 0, 1.1f, White);
            m.Box(6.7f, 0, .6f, 10, 0, 1.1f, White);
            // Parede frontal baixa para nao esconder jogador e saida.
            m.Box(0, -4.7f, 14, .6f, 0, .22f, White);
            Surface(m, 2); m.Plane(0, -2, 3.2f, 4.1f, .035f, White);
            Surface(m, 1); m.Plane(0, -2, 3, 4, .04f, White);
            Surface(m, 0); m.Box(0, .15f, 4, .6f, 0, .82f, White);
            Surface(m, 2); m.Box(0, .15f, 4.15f, .78f, .82f, .91f, White);
            Surface(m, 1); m.Box(0, -.16f, 3.8f, .035f, .12f, .73f, White);
            Surface(m, 6); m.Box(0, 2.7f, 2.8f, 1, .12f, .32f, White);
            foreach (float x in new[] { -4.8f, 4.8f })
            {
                Surface(m, 0);
                m.Box(x, 2.93f, 1.5f, .12f, 0, 2.4f, White);
                foreach (float side in new[] { -.7f, .7f }) m.Box(x + side, 1.5f, .12f, 3, 0, 2.4f, White);
                for (int i = 0; i < 3; i++)
                {
                    float h = .3f + i * .72f;
                    Surface(m, 0); m.Box(x, 1.5f, 1.5f, 3, h, h + .1f, White);
                    Surface(m, 2); m.Box(x, -.02f, 1.52f, .08f, h, h + .14f, White);
                    Crystal(m, x - .25f, .65f + i * .7f, h + .1f, i % 2 == 0 ? 4 : 5, .32f);
                    Vase(m, x + .28f, 1.55f + i * .2f, h + .1f, .22f, i % 2 == 0 ? 3 : 2);
                    // Fileiras de pocoes azuis nas tres prateleiras de ambos os lados.
                    for (int bottle = 0; bottle < 4; bottle++)
                        BlueBottle(m, x - .49f + bottle * .32f, .19f, h + .1f,
                            bottle % 2 == 0 ? .115f : .095f);
                }
            }
            // Fundos vermelhos em paineis estreitos, preservando o mercador visivel.
            Surface(m, 1);
            foreach (float x in new[] { -3.2f, 3.2f }) m.Box(x, 4.32f, 1.6f, .08f, .15f, 2.7f, White);
            Surface(m, 2); m.Box(0, 4.25f, 12.3f, .14f, 2.65f, 2.8f, White);
            Vase(m, -2.65f, 3.1f, 0, .55f, 2);
            Vase(m, 2.75f, 3.25f, 0, .65f, 3);
            Vase(m, 3.55f, 3.35f, 0, .35f, 2);
            Crystal(m, -1.2f, .15f, .93f, 5, .25f);
            Crystal(m, 1.15f, .15f, .93f, 4, .25f);
            foreach (float x in new[] { -1.65f, -.65f, -.28f, .1f, 1.65f })
                BlueBottle(m, x, .08f, .93f, .12f);
            foreach (float x in new[] { -2.75f, 2.75f }) Candle(m, x, 3.8f, 1.35f);
            Candle(m, .65f, .15f, .93f);
            // Bonsai da referencia, sobre pedestal na lateral livre da loja.
            Surface(m, 0); m.Box(5.35f, -2.65f, 1.1f, .8f, 0, .55f, White);
            Surface(m, 3); m.Box(5.35f, -2.65f, .85f, .65f, .55f, .76f, White);
            Surface(m, 0); m.Cone(5.35f, -2.65f, .13f, .76f, 1.45f, White, 5, .06f);
            m.DefaultSurface = PixelSurface.None;
            foreach (float dx in new[] { -.33f, 0, .33f })
                m.Cone(5.35f + dx, -2.65f, .38f, 1.14f + dx * .3f, 1.6f + dx * .3f, ObliqueMeshData.Hex("355D37"), 7, .14f);
            return m;
        }
        private static void BlueBottle(ObliqueMeshData m, float x, float y, float h, float r)
        {
            // Silhueta facetada, gargalo e rolha; brilho em blocos acompanha o pixel art.
            m.DefaultSurface = PixelSurface.None;
            Color glass = ObliqueMeshData.Hex("236DD0"), rim = ObliqueMeshData.Hex("58ADFA");
            m.Cone(x, y, r * .72f, h, h + .04f, glass, 8, r);
            m.Cone(x, y, r, h + .04f, h + r * 1.9f, glass, 8, r * .92f);
            m.Cone(x, y, r * .92f, h + r * 1.9f, h + r * 2.4f, rim, 8, r * .4f);
            m.Cone(x, y, r * .4f, h + r * 2.4f, h + r * 3f, glass, 8, r * .4f);
            m.Cone(x, y, r * .47f, h + r * 2.9f, h + r * 3.08f, rim, 8, r * .47f);
            m.Cone(x, y, r * .34f, h + r * 3.08f, h + r * 3.4f,
                ObliqueMeshData.Hex("B78752"), 6, r * .34f);
            m.Box(x - r * .36f, y - r * .95f, r * .22f, .016f,
                h + r * .55f, h + r * 1.65f, ObliqueMeshData.Hex("B6E5FF"));
            m.Box(x, y - r * .97f, r * .95f, .018f,
                h + r * .6f, h + r * 1.15f, ObliqueMeshData.Hex("DBCEA6"));
        }
        private static void Vase(ObliqueMeshData m, float x, float y, float h, float r, int tile)
        {
            Surface(m, tile);
            m.Cone(x, y, r * .6f, h, h + r * .25f, White, 8, r);
            m.Cone(x, y, r, h + r * .25f, h + r * 1.35f, White, 8, r * .4f);
            m.Cone(x, y, r * .4f, h + r * 1.35f, h + r * 1.75f, White, 8, r * .48f);
            m.Cone(x, y, r * .5f, h + r * 1.75f, h + r * 1.85f, White, 8, r * .5f);
        }
        private static void Crystal(ObliqueMeshData m, float x, float y, float h, int tile, float r)
        {
            Surface(m, 2); m.Box(x, y, r * 2.2f, r * 1.7f, h, h + .08f, White);
            Surface(m, tile);
            foreach (float dx in new[] { -r * .6f, 0, r * .6f })
                m.Cone(x + dx, y, r * .42f, h + .08f, h + r * (dx == 0 ? 2.3f : 1.6f), White, 5);
        }
        private static void Candle(ObliqueMeshData m, float x, float y, float h)
        {
            Surface(m, 2); m.Cone(x, y, .13f, h, h + .08f, White, 8, .13f);
            m.DefaultSurface = PixelSurface.None;
            m.Box(x, y, .09f, .09f, h + .08f, h + .38f, ObliqueMeshData.Hex("E8C88D"));
            m.Cone(x, y, .075f, h + .38f, h + .57f, ObliqueMeshData.Hex("FFBF55"), 5);
            m.Cone(x, y, .035f, h + .39f, h + .5f, ObliqueMeshData.Hex("FFF2BC"), 5);
        }
    }
}
