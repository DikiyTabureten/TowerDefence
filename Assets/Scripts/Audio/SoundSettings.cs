using System;
using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// Model of the sound on/off switch. Muting sets AudioListener.volume, so every sound
    /// (cannons, bells, music) goes quiet at once; the choice is remembered in PlayerPrefs.
    /// </summary>
    public sealed class SoundSettings
    {
        private const string MutedKey = "SoundMuted";

        public bool IsMuted { get; private set; }

        public event Action<bool> Changed;

        public SoundSettings()
        {
            IsMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            Apply();
        }

        public void Toggle()
        {
            IsMuted = !IsMuted;
            PlayerPrefs.SetInt(MutedKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke(IsMuted);
        }

        private void Apply() => AudioListener.volume = IsMuted ? 0f : 1f;
    }
}
