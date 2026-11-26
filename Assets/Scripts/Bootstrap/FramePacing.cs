using System;
using UnityEngine;

namespace DragonQuest.Bootstrap
{
    // O Game view tem sincronizacao propria; limita-lo evita renderizar centenas de quadros/s no Editor.
    // O executavel usa sincronizacao vertical com a tela, sem um segundo limitador por software.
    // Aplica configurações temporárias de renderização; IDisposable permite restaurá-las ao encerrar o bootstrap.
    public sealed class FramePacing : IDisposable
    {
        // Guarda a meta de FPS que estava ativa antes de criar esta instância.
        private readonly int previousFrameRate = Application.targetFrameRate;
        // Guarda o VSync anterior para restaurá-lo ao encerrar o bootstrap.
        private readonly int previousVsync = QualitySettings.vSyncCount;
        // Valores escolhidos no construtor: meta de FPS e intervalo de sincronização vertical, respectivamente.
        // readonly permite atribuir na declaração ou no construtor e impede reatribuições posteriores.
        private readonly int appliedFrameRate, appliedVsync;
        // Indica se Dispose já executou; evita repetir a restauração.
        private bool disposed;

        // Ao criar a instância, escolhe as configurações para o Editor ou para o jogo compilado.
        public FramePacing()
        {
            // No Editor, usa uma meta de 60 FPS; no executável para computador, mantém -1 e deixa o controle para o VSync.
            appliedFrameRate = Application.isEditor ? 60 : -1;
            // 0 desativa o VSync no Editor; 1 sincroniza o executável para computador a cada atualização da tela.
            appliedVsync = Application.isEditor ? 0 : 1;
            // Aplica os valores às configurações globais do Unity, que são compartilhadas pelos sistemas do jogo.
            QualitySettings.vSyncCount = appliedVsync;
            Application.targetFrameRate = appliedFrameRate;
        }

        // Chamado por GameBootstrap.Dispose para restaurar as configurações que esta instância substituiu.
        public void Dispose()
        {
            // Chamadas posteriores encerram aqui, sem alterar as configurações novamente.
            if (disposed) return;
            // Marca o descarte antes de restaurar os valores.
            disposed = true;
            // Conserva uma configuracao que outro sistema possa ter instalado depois desta cena.
            // Restaura a meta de FPS somente se ela ainda coincide com o valor aplicado por esta instância.
            if (Application.targetFrameRate == appliedFrameRate) Application.targetFrameRate = previousFrameRate;
            // Verifica o VSync de forma independente e mantém um valor diferente que outro sistema tenha aplicado.
            if (QualitySettings.vSyncCount == appliedVsync) QualitySettings.vSyncCount = previousVsync;
        }
    }
}
