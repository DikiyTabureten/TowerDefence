using System;

namespace TowerDefense
{
    /// <summary>Presenter of the sound switch: button click -> model, model change -> button label.</summary>
    public sealed class SoundTogglePresenter : IDisposable
    {
        private readonly SoundSettings _settings;
        private readonly SoundToggleView _view;

        public SoundTogglePresenter(SoundSettings settings, SoundToggleView view)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _view = view ? view : throw new ArgumentNullException(nameof(view));
        }

        public void Initialize()
        {
            _view.Clicked += _settings.Toggle;
            _settings.Changed += _view.ShowMuted;
            _view.ShowMuted(_settings.IsMuted);
        }

        public void Dispose()
        {
            _settings.Changed -= _view.ShowMuted;
            if (_view != null) _view.Clicked -= _settings.Toggle;
        }
    }
}
