namespace DragonQuest.Progression
{
    // Símbolos que podem ser escolhidos ao ativar um monólito.
    public enum MonolithSymbol { Sun, Moon, Star }
    // Resultado da tentativa: símbolo inválido, sequência reiniciada, etapa aceita, conclusão ou enigma já concluído.
    public enum MonolithResult { Invalid, Reset, Accepted, Completed, AlreadyCompleted }

    // Estado unico da sessao: viajar ou reabrir um dialogo nao recria os segredos.
    // Objeto de estado compartilhado pelo bootstrap; suas regras não dependem de um MonoBehaviour.
    public sealed class AncientSecrets
    {
        // As propriedades permitem leitura externa, mas só esta classe pode alterar seus valores.
        // Indica se o grupo já aprendeu o poema exigido para resolver o altar.
        public bool KnowsPoem { get; private set; }
        // Indica se o altar já foi resolvido com o verso correto.
        public bool AltarSolved { get; private set; }
        // Indica se a sequência completa dos três monólitos já foi acertada.
        public bool MonolithsSolved { get; private set; }
        // Indica se a escama já foi coletada.
        public bool HasScale { get; private set; }
        // Indica se a passagem antiga já foi aberta.
        public bool PassageOpen { get; private set; }
        // Quantidade de símbolos corretos na tentativa atual: de 0 a 3.
        public int MonolithStep { get; private set; }
        // Notifica os assinantes quando uma ação altera o progresso; ?.Invoke() só chama se houver assinantes.
        public event System.Action Changed;
        // Restaura os valores de uma partida salva sem emitir o evento Changed.
        internal void Restore(bool poem, bool altar, int monolithStep, bool scale, bool passage)
        {
            KnowsPoem = poem; AltarSolved = altar; MonolithStep = monolithStep;
            // O passo salvo também determina se a sequência dos monólitos já foi concluída.
            MonolithsSolved = monolithStep == 3; HasScale = scale; PassageOpen = passage;
        }
        // Conta os três objetivos: altar, monólitos e escama; aprender o poema não entra na contagem.
        public int CompletedCount => (AltarSolved ? 1 : 0) + (MonolithsSolved ? 1 : 0) + (HasScale ? 1 : 0);
        // Considera os segredos descobertos quando ao menos um dos três objetivos foi concluído.
        public bool IsDiscovered => CompletedCount > 0;
        // Permite abrir a passagem quando os três objetivos estão completos e ela ainda está fechada.
        public bool CanOpenPassage => CompletedCount == 3 && !PassageOpen;
        // Texto calculado a partir do progresso atual; fica vazio enquanto nenhum objetivo foi concluído.
        public string Summary => !IsDiscovered ? string.Empty : PassageOpen ? "A trilha antiga esta aberta."
            : "Sinais antigos: " + CompletedCount + "/3" + (CompletedCount == 3 ? " / A escama reage ao norte da ilha." : "");

        // Registra o aprendizado uma única vez e avisa os sistemas interessados.
        public void LearnPoem() { if (KnowsPoem) return; KnowsPoem = true; Changed?.Invoke(); }

        // Só resolve o altar se o poema for conhecido e o verso recebido corresponder a LegendPoem.Flame.
        public bool TrySolveAltar(string verse)
        {
            if (!KnowsPoem || AltarSolved || verse != LegendPoem.Flame) return false;
            AltarSolved = true; Changed?.Invoke(); return true;
        }

        // Processa a ativação de um monólito e informa o resultado usando MonolithResult.
        public MonolithResult Activate(MonolithSymbol symbol)
        {
            if (symbol < MonolithSymbol.Sun || symbol > MonolithSymbol.Star) return MonolithResult.Invalid;
            if (MonolithsSolved) return MonolithResult.AlreadyCompleted;
            // Próximo símbolo exigido: Lua no passo 0, Estrela no passo 1 e Sol no passo 2.
            MonolithSymbol expected = MonolithStep == 0 ? MonolithSymbol.Moon : MonolithStep == 1 ? MonolithSymbol.Star : MonolithSymbol.Sun;
            // Um erro zera toda a sequência; o símbolo errado não é aproveitado como novo início.
            if (symbol != expected) { MonolithStep = 0; Changed?.Invoke(); return MonolithResult.Reset; }
            // O acerto avança uma etapa; o terceiro acerto conclui o enigma.
            MonolithStep++;
            if (MonolithStep == 3) { MonolithsSolved = true; Changed?.Invoke(); return MonolithResult.Completed; }
            Changed?.Invoke();
            return MonolithResult.Accepted;
        }

        // Informa se o símbolo deve ficar aceso conforme a sequência atual, sem alterar o progresso.
        public bool IsLit(MonolithSymbol symbol) => symbol == MonolithSymbol.Moon ? MonolithStep >= 1
            : symbol == MonolithSymbol.Star ? MonolithStep >= 2 : symbol == MonolithSymbol.Sun && MonolithStep == 3;

        // Registra a coleta da escama uma única vez e informa se houve uma nova coleta.
        public bool TryCollectScale()
        {
            if (HasScale) return false;
            HasScale = true; Changed?.Invoke(); return true;
        }

        // Abre a passagem somente após concluir os três objetivos e se ela ainda estiver fechada.
        public bool TryOpenPassage()
        {
            if (!CanOpenPassage) return false;
            PassageOpen = true; Changed?.Invoke(); return true;
        }
    }
}
