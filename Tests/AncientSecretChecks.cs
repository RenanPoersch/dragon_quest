using System;
using System.Linq;
using DragonQuest.Progression;
using DragonQuest.Interactions;

internal static class AncientSecretChecks
{
    private static int checks;
    private static void Assert(bool condition,string text) {checks++;if(!condition)throw new Exception(text);}
    private static void Cycle(AncientSecrets secrets)
    {
        Assert(secrets.Activate(MonolithSymbol.Moon)==MonolithResult.Accepted,"Lua inicia o ciclo.");
        Assert(secrets.Activate(MonolithSymbol.Star)==MonolithResult.Accepted,"Estrela segue a Lua.");
        Assert(secrets.Activate(MonolithSymbol.Sun)==MonolithResult.Completed,"Sol conclui o ciclo.");
    }

    private static void CheckAltar()
    {
        var secrets=new AncientSecrets();
        Assert(LegendPoem.Verses.Count==3&&LegendPoem.Verses.Distinct().Count()==3,"Poema deve ter tres versos distintos.");
        Assert(LegendPoem.AltarChoices.OrderBy(x=>x).SequenceEqual(LegendPoem.Verses.OrderBy(x=>x)),"Altar deve oferecer os tres versos originais sem trocar seu texto.");
        Assert(!secrets.IsDiscovered&&secrets.Summary==string.Empty,"Inicio nao pode revelar missao ou contador de segredos.");
        foreach(var verse in LegendPoem.Verses)
            Assert(!secrets.TrySolveAltar(verse)&&secrets.CompletedCount==0,"Sem conhecer o poema, altar nao registra solucao.");
        secrets.LearnPoem();secrets.LearnPoem();
        Assert(secrets.KnowsPoem&&!secrets.IsDiscovered,"Ouvir o poema nao conta como segredo concluido.");
        foreach(var verse in new[]{LegendPoem.Sky,LegendPoem.Forest,null,"",LegendPoem.Flame.ToLowerInvariant()})
            Assert(!secrets.TrySolveAltar(verse)&&!secrets.AltarSolved,"Resposta errada nao pode quebrar o selo nem bloquear nova tentativa.");
        Assert(secrets.TrySolveAltar(LegendPoem.Flame)&&secrets.CompletedCount==1,"Verso de chama e ruina deve resolver o altar.");
        Assert(secrets.IsDiscovered&&secrets.Summary.Contains("1/3"),"Primeiro segredo revela o contador.");
        Assert(!secrets.TrySolveAltar(LegendPoem.Flame)&&secrets.CompletedCount==1,"Repeticao do altar nao duplica progresso.");
        Assert(!secrets.TryOpenPassage(),"Somente altar nao pode abrir a passagem.");
    }

    private static void CheckMonoliths()
    {
        var secrets=new AncientSecrets();
        Assert(secrets.Activate((MonolithSymbol)999)==MonolithResult.Invalid&&secrets.MonolithStep==0,"Simbolo invalido nao pode alterar estado.");
        Assert(secrets.Activate(MonolithSymbol.Sun)==MonolithResult.Reset&&!secrets.IsDiscovered,"Sol antes da Lua nao conclui segredo.");
        secrets.Activate(MonolithSymbol.Moon);
        Assert(secrets.IsLit(MonolithSymbol.Moon)&&!secrets.IsLit(MonolithSymbol.Star)&&!secrets.IsLit(MonolithSymbol.Sun),"Apenas Lua acende no primeiro passo.");
        Assert(secrets.Activate((MonolithSymbol)(-1))==MonolithResult.Invalid&&secrets.MonolithStep==1,"Entrada invalida deve preservar sequencia em andamento.");
        Assert(secrets.Activate(MonolithSymbol.Moon)==MonolithResult.Reset&&secrets.MonolithStep==0&&!secrets.IsLit(MonolithSymbol.Moon),"Repetir Lua apaga tudo e reinicia.");
        secrets.Activate(MonolithSymbol.Moon);secrets.Activate(MonolithSymbol.Star);
        Assert(secrets.IsLit(MonolithSymbol.Star)&&!secrets.IsLit(MonolithSymbol.Sun),"Segundo passo acende Estrela, nao Sol.");
        Assert(secrets.Activate(MonolithSymbol.Star)==MonolithResult.Reset&&!secrets.IsLit(MonolithSymbol.Star),"Erro no ultimo passo precisa apagar todas as luzes.");
        Cycle(secrets);
        Assert(secrets.MonolithsSolved&&secrets.CompletedCount==1&&secrets.Summary.Contains("1/3"),"Acertar ciclo registra apenas um segredo.");
        foreach(var symbol in new[]{MonolithSymbol.Sun,MonolithSymbol.Moon,MonolithSymbol.Star})
            Assert(secrets.Activate(symbol)==MonolithResult.AlreadyCompleted&&secrets.IsLit(symbol)&&secrets.CompletedCount==1,"Monolitos resolvidos nao podem apagar nem duplicar progresso.");
        Assert(!secrets.KnowsPoem,"Resolver o enigma por deducao nao deve falsificar conversa com a camponesa.");
    }

    private static void CheckOrders()
    {
        foreach(var order in new[]{new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}})
        {
            var secrets=new AncientSecrets();secrets.LearnPoem();
            Assert(!secrets.TryOpenPassage(),"Passagem inicia fechada.");
            for(int i=0;i<order.Length;i++)
            {
                if(order[i]==0)Assert(secrets.TrySolveAltar(LegendPoem.Flame),"Altar pode ser resolvido em qualquer ordem.");
                else if(order[i]==1)Cycle(secrets);
                else Assert(secrets.TryCollectScale(),"Escama pode ser encontrada em qualquer ordem.");
                Assert(secrets.CompletedCount==i+1,"Contador deve contar objetivos distintos.");
                Assert(!secrets.PassageOpen,"Concluir objetivos nao abre as rochas automaticamente.");
                if(i<2)Assert(!secrets.TryOpenPassage(),"Dois segredos nao bastam para abrir.");
            }
            Assert(secrets.HasScale&&secrets.CanOpenPassage,"Todos os segredos tornam a escama apta a abrir a trilha.");
            Assert(secrets.TryOpenPassage()&&secrets.PassageOpen&&!secrets.CanOpenPassage,"Interagir deve abrir a passagem uma unica vez.");
            Assert(secrets.HasScale&&!secrets.TryCollectScale(),"Escama nao e consumida nem pode ser duplicada.");
            Assert(!secrets.TryOpenPassage()&&secrets.CompletedCount==3,"Reabrir passagem nao repete a mutacao.");
            Assert(secrets.Summary.Contains("aberta"),"Resumo deve refletir trilha aberta.");
        }
        var scaleOnly=new AncientSecrets();scaleOnly.TryCollectScale();
        Assert(!scaleOnly.TryOpenPassage()&&scaleOnly.HasScale,"Escama sozinha nao desfaz os tres selos.");
        var fresh=new AncientSecrets();
        Assert(!fresh.KnowsPoem&&!fresh.HasScale&&!fresh.PassageOpen&&fresh.CompletedCount==0&&fresh.MonolithStep==0,"Sessoes novas nao herdam progresso.");
    }

    private static void CheckDialogues()
    {
        var secrets=new AncientSecrets();var session=new DialogueSession();
        var pages=LegendPoem.ExplainedPages();
        session.Open("Camponesa",secrets.LearnPoem,pages);pages[0]="alterado";
        Assert(session.Text==LegendPoem.Text,"Dialogo deve guardar uma copia de suas paginas.");
        session.Advance();session.Close();
        Assert(!secrets.KnowsPoem&&!session.IsOpen,"Cancelar explicacao nao pode ensinar versos nao lidos.");
        session.Open("Camponesa",secrets.LearnPoem,LegendPoem.ExplainedPages());
        while(session.IsOpen)session.Advance();
        Assert(secrets.KnowsPoem&&!secrets.IsDiscovered,"Concluir explicacoes permite consulta do poema sem revelar segredos.");
        int callbacks=0;
        session.OpenChoices("Altar","Verso?",LegendPoem.AltarChoices,index=>
        {
            callbacks++;secrets.TrySolveAltar(LegendPoem.AltarChoices[index]);session.Open("Resposta",null,"A pedra reage.");
        });
        session.Advance();
        Assert(session.HasChoices,"E/Enter nao devem escolher uma resposta automaticamente.");
        Assert(!session.Choose(-1)&&!session.Choose(3)&&session.HasChoices&&callbacks==0,"Indices invalidos nao fecham nem executam escolhas.");
        session.Close();
        Assert(callbacks==0&&!secrets.AltarSolved,"Esc cancela sem resolver o altar.");
        session.OpenChoices("Altar","Verso?",LegendPoem.AltarChoices,index=>
        {callbacks++;secrets.TrySolveAltar(LegendPoem.AltarChoices[index]);session.Open("Resposta",null,"A pedra reage.");});
        Assert(session.Choose(1)&&secrets.AltarSolved&&callbacks==1,"Escolha correta executa exatamente uma resposta.");
        Assert(session.IsOpen&&!session.HasChoices&&session.Title=="Resposta","Fechamento da escolha nao pode apagar dialogo aberto por seu callback.");
        Assert(!session.Choose(1)&&callbacks==1,"Selecao repetida apos resposta nao executa callback outra vez.");
        int completed=0;session.Open("Antigo",()=>completed++,"Pagina");session.Open("Novo",null,"Nova pagina");session.Advance();
        Assert(completed==0,"Substituir um dialogo deve cancelar conclusao anterior.");
        session.Open("Fim",()=>{completed++;session.Open("Seguinte",null,"Outra pagina");},"Ultima");session.Advance();
        Assert(completed==1&&session.IsOpen&&session.Title=="Seguinte","Conclusao pode abrir outro dialogo com seguranca.");
        session.Close();session.Advance();session.Close();
        Assert(!session.IsOpen&&session.PageNumber==0&&session.PageCount==0,"Avancar/fechar dialogo fechado deve ser seguro.");
    }

    private static void CheckTextReveal()
    {
        var reveal = new DialogueTextReveal();
        reveal.Begin(null);
        Assert(reveal.IsComplete && reveal.VisibleText == "", "Texto vazio termina sem esperar.");
        reveal.Begin("A\u0301\U0001F31FB");
        Assert(!reveal.IsComplete && reveal.VisibleText == "", "Nova fala inicia sem texto revelado.");
        reveal.Tick(1f/DialogueTextReveal.CharactersPerSecond);
        Assert(reveal.VisibleText == "A\u0301", "Acento combinante aparece com a letra inteira.");
        reveal.Tick(1f/DialogueTextReveal.CharactersPerSecond);
        Assert(reveal.VisibleText == "A\u0301\U0001F31F", "Par substituto aparece inteiro.");
        foreach (float invalid in new[]{-1f,0f,float.NaN,float.PositiveInfinity}) reveal.Tick(invalid);
        Assert(reveal.VisibleText == "A\u0301\U0001F31F", "Tempos invalidos nao alteram a escrita.");
        reveal.Finish();
        Assert(reveal.IsComplete && reveal.VisibleText == "A\u0301\U0001F31FB", "Concluir revela o restante da fala.");
        reveal.Tick(10);
        Assert(reveal.IsComplete && reveal.VisibleText == "A\u0301\U0001F31FB", "Fala concluida nao reinicia pelo tempo.");
        string longText = new string('x',200);
        var split = new DialogueTextReveal();
        reveal.Begin(longText); split.Begin(longText);
        reveal.Tick(1);
        for(int i=0;i<60;i++) split.Tick(1f/60);
        Assert(reveal.VisibleText == split.VisibleText && reveal.VisibleText.Length == 48,
            "Escrita independe da quantidade de frames.");
        reveal.Begin(longText);
        Assert(reveal.VisibleText == "" && !reveal.IsComplete, "Texto identico em outra pagina reinicia a escrita.");
        reveal.Tick(1000);
        Assert(reveal.IsComplete && reveal.VisibleText == longText, "Frame longo termina no limite exato da fala.");
    }

    public static void Main()
    {CheckAltar();CheckMonoliths();CheckOrders();CheckDialogues();CheckTextReveal();Console.WriteLine(checks+" verificacoes de segredos, escolhas e escrita passaram.");}
}
