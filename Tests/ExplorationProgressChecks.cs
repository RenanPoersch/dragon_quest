using System;
using DragonQuest.Exploration;

// Verificação executável do estado de exploração, sem depender do Editor Unity.
internal static class ExplorationProgressChecks
{
    private static int checks;

    private static void Assert(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception(description);
    }

    private static void AssertThrows<T>(Action action) where T : Exception
    {
        bool thrown = false;
        try { action(); }
        catch (T) { thrown = true; }
        Assert(thrown, "Excecao esperada: " + typeof(T).Name);
    }

    public static int Main()
    {
        try
        {
            var progress = new ExplorationProgress();
            Assert(progress.Gold == 0, "Nova sessao deve iniciar sem ouro.");
            Assert(progress.TryOpenChest("praca", 25), "Primeira abertura deve conceder recompensa.");
            Assert(progress.Gold == 25 && progress.HasOpenedChest("praca"), "Recompensa e ID devem ser registrados.");
            Assert(!progress.TryOpenChest("praca", 25), "Reabertura deve ser rejeitada.");
            Assert(progress.Gold == 25, "Reabertura nao pode duplicar ouro.");
            Assert(progress.TryOpenChest("outro-bau", 10), "Baus diferentes devem ter recompensas independentes.");
            Assert(progress.Gold == 35, "Recompensas devem acumular.");
            AssertThrows<ArgumentException>(() => progress.TryOpenChest("", 10));
            AssertThrows<ArgumentException>(() => progress.TryOpenChest(null, 10));
            AssertThrows<ArgumentOutOfRangeException>(() => progress.TryOpenChest("negativo", -1));
            Assert(!progress.HasOpenedChest("negativo") && progress.Gold == 35, "Entrada invalida nao pode alterar estado.");
            Assert(progress.TryOpenChest("negativo", 5), "Tentativa invalida nao pode consumir o ID do bau.");
            var maximum = new ExplorationProgress();
            maximum.TryOpenChest("limite", int.MaxValue);
            AssertThrows<OverflowException>(() => maximum.TryOpenChest("excesso", 1));
            Assert(!maximum.HasOpenedChest("excesso") && maximum.Gold == int.MaxValue, "Overflow nao pode consumir o bau ou corromper ouro.");
            Assert(new ExplorationProgress().Gold == 0, "Estado nao deve vazar entre sessoes.");
            Console.WriteLine(checks + " verificacoes de progresso passaram.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }
}
