"""Convert supplied music to a chip arrangement without composing replacement notes.

Requires numpy, scipy, soundfile, imageio-ffmpeg. Automatic transcription also
requires basic-pitch with its ONNX runtime and dependencies. Use --notes to render
the committed note events without running transcription again.
"""
import argparse
import hashlib
import json
import logging
from pathlib import Path
import subprocess
import tempfile
import numpy as np
import soundfile as sf
from scipy.ndimage import uniform_filter1d

RATE = 44100


def decode(path):
    import imageio_ffmpeg
    result = subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-v', 'error', '-i', str(path),
        '-map_metadata', '-1', '-ac', '2', '-ar', str(RATE), '-f', 'f32le', 'pipe:1'],
        check=True, stdout=subprocess.PIPE)
    return np.frombuffer(result.stdout, dtype='<f4').reshape(-1, 2).copy()


def extract(path):
    logging.getLogger().setLevel(logging.ERROR)
    from basic_pitch.inference import predict
    model, _, _ = predict(path, onset_threshold=.55, frame_threshold=.35,
        minimum_note_length=65, minimum_frequency=50, maximum_frequency=4200, multiple_pitch_bends=False,
        melodia_trick=False)
    return extract_events(model)


def extract_events(model):
    from basic_pitch.note_creation import model_output_to_notes, model_frames_to_time
    _, notes = model_output_to_notes(model, .55, .35, infer_onsets=False, min_note_len=6,
        min_freq=50, max_freq=4200, include_pitch_bends=False, melodia_trick=False)
    times = model_frames_to_time(len(model['note']))
    result = []
    for start, end, pitch, amplitude, _ in sorted(notes):
        # Retain the recorded echo quietly; prefer real attacks over their reverberation.
        region = (times >= start - .035) & (times <= start + .055)
        strength = float(model['onset'][region, int(pitch) - 21].max()) if region.any() else .55
        result.append(dict(start=round(float(start), 6), end=round(float(end), 6),
                           pitch=int(pitch), amplitude=round(float(amplitude), 6), onset=round(strength, 6)))
    return result


def oscillator(pitch, count, kind):
    frequency = 440 * 2 ** ((pitch - 69) / 12)
    phase = 2 * np.pi * frequency * np.arange(count, dtype=np.float64) / RATE
    result = np.zeros(count, dtype=np.float64)
    max_partial = min(39, int(RATE * .44 / frequency))
    if kind == 'triangle':
        for h in range(1, max_partial + 1, 2):
            result += ((-1) ** ((h - 1) // 2)) * np.sin(phase * h) / h**2
        result *= 8 / np.pi**2
    else:
        # A band-limited 25% pulse, quantized like a small wavetable.
        for h in range(1, max_partial + 1):
            result += 2 * np.sin(np.pi * h * .25) / (np.pi * h) * np.cos(phase * h - np.pi * h * .25)
        result *= 1.5
    return (np.round(result * 127) / 127).astype(np.float32)


def render(events, source):
    mix = np.zeros_like(source, dtype=np.float32)
    active = np.zeros(len(source), np.float32)
    for event in events:
        start = max(0, round(event['start'] * RATE))
        duration = event['end'] - event['start']
        release = .055 if duration < .5 else .1
        length = min(round((duration + release) * RATE), len(source) - start)
        if length <= 0: continue
        t = np.arange(length, dtype=np.float32) / RATE
        pitch = event['pitch']
        sustained = duration > .42 and event['onset'] < .8
        kind = 'triangle' if pitch < 48 or sustained else 'pulse'
        attack = .027 if sustained else .003
        env = np.minimum(t / attack, 1) * np.exp(-np.maximum(t - duration, 0) * 7 / release)
        if not sustained: env *= .22 + .78 * np.exp(-t * 7)
        weight = np.clip((event['onset'] - .4) / .5, .08, 1)**1.5
        if sustained: weight = max(weight, .45)
        gain = event['amplitude']**1.5 * weight * (.25 if sustained else .36)
        signal = oscillator(pitch, length, kind) * env * gain
        pan = float(np.clip((pitch - 70) / 65, -.4, .4))
        mix[start:start + length, 0] += signal * np.sqrt((1-pan)/2)
        mix[start:start + length, 1] += signal * np.sqrt((1+pan)/2)
        active[start:start + length] += env * gain
    # Gentle voice balancing and the original performance's loudness contour.
    balance = np.sqrt(np.maximum(1, uniform_filter1d(active, round(RATE * .05)) * 3))
    mix /= balance[:, None]
    source_env = np.sqrt(np.maximum(0, uniform_filter1d(np.mean(source**2, axis=1), round(RATE * .08))))
    synth_env = np.sqrt(np.maximum(0, uniform_filter1d(np.mean(mix**2, axis=1), round(RATE * .08))))
    gain = np.clip(source_env / np.maximum(synth_env, .006), .15, 2.5)
    mix *= uniform_filter1d(gain, round(RATE * .03))[:, None]
    mix = np.tanh(mix * 1.5)
    mix *= min(.16 / max(float(np.sqrt(np.mean(mix**2))), .001),
               .82 / max(float(np.max(abs(mix))), .001))
    mix = np.round(mix * 127) / 127  # 8-bit amplitude resolution; OGG stores the result efficiently.
    edge = round(RATE * .008)
    mix[:edge] *= np.linspace(0, 1, edge)[:, None]
    mix[-edge:] *= np.linspace(1, 0, edge)[:, None]
    return mix


def write_ogg(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    # Bounded writes avoid a stack overflow in the Windows Vorbis encoder.
    with sf.SoundFile(path, 'w', samplerate=RATE, channels=2, format='OGG', subtype='VORBIS', compression_level=.85) as out:
        for offset in range(0, len(samples), 16384): out.write(samples[offset:offset + 16384])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('source', type=Path)
    parser.add_argument('--notes', type=Path)
    parser.add_argument('--prediction', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--metadata-dir', type=Path)
    parser.add_argument('--preview', type=Path)
    args = parser.parse_args()
    source = decode(args.source)
    if args.notes:
        record = json.loads(args.notes.read_text(encoding='utf-8'))
        expected = hashlib.sha256(args.source.read_bytes()).hexdigest()
        if record['source_sha256'] != expected: raise ValueError('Note events belong to a different recording')
        events = record['notes']
    else:
        logging.getLogger().setLevel(logging.ERROR)
        if args.prediction:
            events = extract_events(dict(np.load(args.prediction)))
        else:
            args.output.parent.mkdir(parents=True, exist_ok=True)
            with tempfile.TemporaryDirectory(prefix='dq-score-', dir=args.output.parent) as folder:
                wav = Path(folder) / 'source.wav'
                sf.write(wav, source, RATE, subtype='PCM_16')
                events = extract(wav)
        record = dict(source_sha256=hashlib.sha256(args.source.read_bytes()).hexdigest(),
                      source_file=args.source.name, seconds=len(source) / RATE, notes=events)
    samples = render(events, source)
    write_ogg(args.output, samples)
    decoded, rate = sf.read(args.output, dtype='float32')
    assert rate == RATE and decoded.shape == samples.shape and np.isfinite(decoded).all()
    assert float(np.max(abs(decoded))) < .98
    metadata_dir = args.metadata_dir or args.output.parent
    metadata_dir.mkdir(parents=True, exist_ok=True)
    (metadata_dir / (args.output.stem + '-notes.json')).write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8')
    stats = dict(seconds=len(decoded) / rate, notes=len(events), rate=rate, channels=2,
        peak_db=round(20 * np.log10(float(np.max(abs(decoded)))),2),
        rms_db=round(20 * np.log10(float(np.sqrt(np.mean(decoded**2)))),2),
        source_sha256=record['source_sha256'], output_sha256=hashlib.sha256(args.output.read_bytes()).hexdigest())
    (metadata_dir / (args.output.stem + '-conversion.json')).write_text(json.dumps(stats, indent=2) + '\n', encoding='utf-8')
    if args.preview:
        args.preview.parent.mkdir(parents=True, exist_ok=True)
        sf.write(args.preview, np.concatenate((samples[RATE:RATE*13], samples[RATE*58:RATE*70])), RATE, subtype='PCM_16')
    print(json.dumps(stats), flush=True)


if __name__ == '__main__': main()
