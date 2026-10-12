using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    // O Game view tem sincronizacao propria; limita-lo evita renderizar centenas de quadros/s no Editor.
    // O executavel usa sincronizacao vertical com a tela, sem um segundo limitador por software.
    public sealed class PrototypeFramePacing : IDisposable
    {
        private readonly int previousFrameRate = Application.targetFrameRate;
        private readonly int previousVsync = QualitySettings.vSyncCount;
        private readonly int appliedFrameRate, appliedVsync;
        private bool disposed;

        public PrototypeFramePacing()
        {
            appliedFrameRate = Application.isEditor ? 60 : -1;
            appliedVsync = Application.isEditor ? 0 : 1;
            QualitySettings.vSyncCount = appliedVsync;
            Application.targetFrameRate = appliedFrameRate;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            // Conserva uma configuracao que outro sistema possa ter instalado depois desta cena.
            if (Application.targetFrameRate == appliedFrameRate) Application.targetFrameRate = previousFrameRate;
            if (QualitySettings.vSyncCount == appliedVsync) QualitySettings.vSyncCount = previousVsync;
        }
    }
}
