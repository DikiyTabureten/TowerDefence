namespace TowerDefense
{
    /// <summary>Something that plays the sound of a shot on a note at an exact audio time (e.g. ShotNotePlayer).</summary>
    public interface IShotNoteSink
    {
        /// <summary>
        /// Schedules the shot sound of note <paramref name="noteIndex"/> at <paramref name="dspTime"/>.
        /// Several towers shooting on the same note share one sound; it just gets louder.
        /// </summary>
        void Schedule(int noteIndex, RhythmNote note, double dspTime);
    }
}
