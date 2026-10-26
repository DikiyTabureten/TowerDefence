using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TowerDefense.EditorTools.Rhythm
{
    /// <summary>A note read from a MIDI file, in ticks.</summary>
    internal struct MidiNote
    {
        public long StartTick;
        public long EndTick;
        public int Channel;   // 0..15 (9 = drums)
        public int Pitch;     // 0..127
        public int Velocity;  // 1..127
    }

    /// <summary>All notes of one MIDI track on one channel — one row in the import window.</summary>
    internal sealed class MidiPart
    {
        public int TrackIndex;
        public string TrackName;
        public int Channel;
        public readonly List<MidiNote> Notes = new List<MidiNote>();

        public bool IsDrums => Channel == 9;
        public int MinPitch => Notes.Count == 0 ? 0 : Notes.Min(n => n.Pitch);
        public int MaxPitch => Notes.Count == 0 ? 0 : Notes.Max(n => n.Pitch);
    }

    internal sealed class MidiFile
    {
        public int Format;
        public int TicksPerQuarter;
        public readonly List<MidiPart> Parts = new List<MidiPart>();
        public readonly List<(long tick, int microsecondsPerQuarter)> Tempos = new List<(long, int)>();
        public int TimeSignatureNumerator = 4;
        public int TimeSignatureDenominator = 4;
        public bool HasTimeSignature;

        /// <summary>BPM of the first tempo event (120 if the file has none).</summary>
        public float Bpm => Tempos.Count > 0 ? 60_000_000f / Tempos[0].microsecondsPerQuarter : 120f;

        public bool HasTempoChanges
        {
            get
            {
                for (int i = 1; i < Tempos.Count; i++)
                    if (Tempos[i].microsecondsPerQuarter != Tempos[0].microsecondsPerQuarter) return true;
                return false;
            }
        }

        /// <summary>Beats (quarter notes) per bar from the time signature, e.g. 4/4 = 4, 6/8 = 3.</summary>
        public float BeatsPerBar => TimeSignatureNumerator * 4f / TimeSignatureDenominator;
    }

    /// <summary>
    /// Minimal Standard MIDI File reader (formats 0 and 1). Reads note on/off, tempo, time signature
    /// and track names; skips SysEx and every other meta event. No external libraries.
    /// </summary>
    internal static class MidiFileReader
    {
        public static MidiFile Read(byte[] data)
        {
            var reader = new ByteReader(data);
            var file = new MidiFile();

            // Header chunk
            if (reader.ReadChunkId() != "MThd") throw new FormatException("Not a MIDI file: 'MThd' header is missing.");
            int headerLength = (int)reader.ReadUInt32();
            int headerEnd = reader.Position + headerLength;
            file.Format = reader.ReadUInt16();
            int trackCount = reader.ReadUInt16();
            int division = reader.ReadUInt16();
            reader.Position = headerEnd;

            if (file.Format > 1)
                throw new FormatException($"MIDI format {file.Format} is not supported (only 0 and 1).");
            if ((division & 0x8000) != 0)
                throw new FormatException("This MIDI file uses SMPTE time code instead of ticks per quarter note. It is not supported.");
            if (division == 0)
                throw new FormatException("The MIDI file has 0 ticks per quarter note.");
            file.TicksPerQuarter = division;

            for (int track = 0; track < trackCount && !reader.AtEnd; track++)
            {
                string id = reader.ReadChunkId();
                int length = (int)reader.ReadUInt32();
                int end = Math.Min(reader.Position + length, data.Length);
                if (id != "MTrk") { reader.Position = end; continue; } // unknown chunk: skip

                ReadTrack(reader, end, track, file);
                reader.Position = end;
            }

            file.Tempos.Sort((a, b) => a.tick.CompareTo(b.tick));
            file.Parts.RemoveAll(p => p.Notes.Count == 0);
            return file;
        }

        private static void ReadTrack(ByteReader reader, int end, int trackIndex, MidiFile file)
        {
            long tick = 0;
            int runningStatus = 0;
            string trackName = null;
            var parts = new Dictionary<int, MidiPart>();                 // channel -> part
            var open = new Dictionary<int, Stack<int>>();               // (channel << 8 | pitch) -> indices of unfinished notes

            while (reader.Position < end)
            {
                tick += reader.ReadVariableLength();
                int status = reader.PeekByte();

                if (status < 0x80)
                {
                    // Running status: reuse the previous channel message status, the byte is data.
                    if (runningStatus == 0) throw new FormatException($"Running status without a previous status byte (track {trackIndex}).");
                    status = runningStatus;
                }
                else
                {
                    reader.Position++;
                }

                if (status == 0xFF)
                {
                    int type = reader.ReadByte();
                    int length = (int)reader.ReadVariableLength();
                    int payload = reader.Position;
                    runningStatus = 0;

                    switch (type)
                    {
                        case 0x51 when length >= 3: // tempo: microseconds per quarter note
                            file.Tempos.Add((tick, (reader.ByteAt(payload) << 16) | (reader.ByteAt(payload + 1) << 8) | reader.ByteAt(payload + 2)));
                            break;
                        case 0x58 when length >= 2: // time signature: numerator, log2(denominator)
                            if (!file.HasTimeSignature)
                            {
                                file.TimeSignatureNumerator = Math.Max(1, (int)reader.ByteAt(payload));
                                file.TimeSignatureDenominator = 1 << Math.Min(6, (int)reader.ByteAt(payload + 1));
                                file.HasTimeSignature = true;
                            }
                            break;
                        case 0x03: // track name
                            trackName = Encoding.ASCII.GetString(reader.Slice(payload, length)).Trim();
                            break;
                    }

                    reader.Position = payload + length;
                    if (type == 0x2F) break; // end of track
                    continue;
                }

                if (status == 0xF0 || status == 0xF7)
                {
                    // SysEx: length-prefixed, skip it.
                    int length = (int)reader.ReadVariableLength();
                    reader.Position += length;
                    runningStatus = 0;
                    continue;
                }

                if (status >= 0xF0)
                {
                    // Other system messages are not allowed in MIDI files; skip the byte.
                    continue;
                }

                runningStatus = status;
                int kind = status & 0xF0;
                int channel = status & 0x0F;
                int data1 = reader.ReadByte();
                int data2 = kind == 0xC0 || kind == 0xD0 ? 0 : reader.ReadByte();

                bool noteOn = kind == 0x90 && data2 > 0;
                bool noteOff = kind == 0x80 || (kind == 0x90 && data2 == 0); // note on with velocity 0 = note off
                int key = (channel << 8) | data1;

                if (noteOn)
                {
                    if (!parts.TryGetValue(channel, out MidiPart part))
                    {
                        part = new MidiPart { TrackIndex = trackIndex, Channel = channel };
                        parts[channel] = part;
                    }
                    part.Notes.Add(new MidiNote { StartTick = tick, EndTick = tick, Channel = channel, Pitch = data1, Velocity = data2 });
                    if (!open.TryGetValue(key, out Stack<int> stack)) open[key] = stack = new Stack<int>();
                    stack.Push(part.Notes.Count - 1);
                }
                else if (noteOff && parts.TryGetValue(channel, out MidiPart offPart) &&
                         open.TryGetValue(key, out Stack<int> openNotes) && openNotes.Count > 0)
                {
                    int index = openNotes.Pop();
                    MidiNote note = offPart.Notes[index];
                    note.EndTick = tick;
                    offPart.Notes[index] = note;
                }
            }

            // Notes never switched off end where the track ends.
            foreach (KeyValuePair<int, Stack<int>> pair in open)
            {
                MidiPart part = parts[pair.Key >> 8];
                foreach (int index in pair.Value)
                {
                    MidiNote note = part.Notes[index];
                    note.EndTick = Math.Max(note.EndTick, tick);
                    part.Notes[index] = note;
                }
            }

            foreach (MidiPart part in parts.Values)
            {
                part.TrackName = string.IsNullOrEmpty(trackName) ? $"Track {trackIndex + 1}" : trackName;
                file.Parts.Add(part);
            }
        }

        private sealed class ByteReader
        {
            private readonly byte[] _data;
            public int Position;

            public ByteReader(byte[] data) => _data = data;

            public bool AtEnd => Position >= _data.Length;

            public int PeekByte()
            {
                EnsureAvailable(1);
                return _data[Position];
            }

            public int ReadByte()
            {
                EnsureAvailable(1);
                return _data[Position++];
            }

            public int ByteAt(int index)
            {
                if (index < 0 || index >= _data.Length) throw new FormatException("Unexpected end of MIDI file.");
                return _data[index];
            }

            public byte[] Slice(int start, int length)
            {
                if (start < 0 || length < 0 || start + length > _data.Length) throw new FormatException("Unexpected end of MIDI file.");
                var result = new byte[length];
                Array.Copy(_data, start, result, 0, length);
                return result;
            }

            public int ReadUInt16() => (ReadByte() << 8) | ReadByte();

            public uint ReadUInt32() => ((uint)ReadByte() << 24) | ((uint)ReadByte() << 16) | ((uint)ReadByte() << 8) | (uint)ReadByte();

            public string ReadChunkId() => Encoding.ASCII.GetString(new[] { (byte)ReadByte(), (byte)ReadByte(), (byte)ReadByte(), (byte)ReadByte() });

            /// <summary>MIDI variable-length quantity: 7 bits per byte, high bit = "more bytes follow".</summary>
            public long ReadVariableLength()
            {
                long value = 0;
                for (int i = 0; i < 4; i++)
                {
                    int b = ReadByte();
                    value = (value << 7) | (uint)(b & 0x7F);
                    if ((b & 0x80) == 0) return value;
                }
                throw new FormatException("Invalid variable-length number in MIDI file.");
            }

            private void EnsureAvailable(int count)
            {
                if (Position + count > _data.Length) throw new FormatException("Unexpected end of MIDI file.");
            }
        }
    }
}
