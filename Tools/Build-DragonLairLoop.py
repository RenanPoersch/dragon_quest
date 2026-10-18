"""Rebuild the initial bass loop from the user's nineteenth recording.

Usage: python Tools/Build-DragonLairLoop.py source.m4a output.wav --ffmpeg ffmpeg
Only the 40 ms circular splice is blended; the remainder is decoded source PCM.
The analysis/cut does not change pitch, tempo, EQ, or stereo placement.
"""
import argparse
from array import array
import hashlib
import io
import json
import math
from pathlib import Path
import subprocess
import sys
import wave

RATE = 44100
START_FRAME = 373968  # 8.480000 s, the rising bass attack of the second cycle.
END_FRAME = 738552    # 16.747211 s, the equivalent point of the next attack.
BLEND_FRAMES = 1764   # 40 ms; keeps the complete 8.267211 s cycle length.


def save(path, samples):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = array('h', samples)
    if sys.byteorder != 'little':
        pcm.byteswap()
    with wave.open(str(path), 'wb') as target:
        target.setnchannels(2)
        target.setsampwidth(2)
        target.setframerate(RATE)
        target.writeframes(pcm.tobytes())


def build(source, output, ffmpeg='ffmpeg', preview=None, report=None):
    source, output = Path(source), Path(output)
    decoded = subprocess.run([ffmpeg, '-v', 'error', '-nostdin', '-i', str(source),
                              '-map', '0:a:0', '-map_metadata', '-1', '-c:a', 'pcm_s16le',
                              '-fflags', '+bitexact', '-flags:a', '+bitexact', '-f', 'wav', 'pipe:1'],
                             check=True, stdout=subprocess.PIPE).stdout
    with wave.open(io.BytesIO(decoded), 'rb') as audio:
        if (audio.getnchannels(), audio.getsampwidth(), audio.getframerate()) != (2, 2, RATE):
            raise ValueError('The supplied recording must be stereo PCM16 at 44100 Hz after decoding.')
        pcm = array('h', audio.readframes(audio.getnframes()))
        if sys.byteorder != 'little':
            pcm.byteswap()
    if len(pcm) < (END_FRAME + BLEND_FRAMES) * 2:
        raise ValueError('The recording is too short for the selected initial cycle.')
    clip = array('h', pcm[START_FRAME * 2:END_FRAME * 2])
    # The first sample follows the original end sample exactly. Over the next
    # 40 ms, smoothly return from that continuation to the selected cycle.
    # This preserves the bass pulse spacing, unlike shortening the loop with overlap.
    for frame in range(BLEND_FRAMES):
        mix = (1 - math.cos(math.pi * frame / (BLEND_FRAMES - 1))) / 2
        for channel in range(2):
            i = frame * 2 + channel
            clip[i] = round(pcm[(END_FRAME + frame) * 2 + channel] * (1 - mix) + clip[i] * mix)
    save(output, clip)
    if preview:
        save(preview, clip * 3)
    seam = [clip[channel] - clip[-2 + channel] for channel in range(2)]
    original_step = [pcm[END_FRAME * 2 + channel] - pcm[(END_FRAME-1) * 2 + channel] for channel in range(2)]
    assert seam == original_step, 'The loop splice must follow a real adjacent source sample.'
    assert clip[BLEND_FRAMES*2:] == pcm[(START_FRAME+BLEND_FRAMES)*2:END_FRAME*2]
    details = {
        'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
        'output_sha256': hashlib.sha256(output.read_bytes()).hexdigest(),
        'rate': RATE, 'channels': 2, 'sample_width': 2,
        'start_frame': START_FRAME, 'end_frame': END_FRAME,
        'start_seconds': START_FRAME/RATE, 'end_seconds': END_FRAME/RATE,
        'frames': END_FRAME-START_FRAME, 'duration_seconds': (END_FRAME-START_FRAME)/RATE,
        'circular_blend_frames': BLEND_FRAMES, 'circular_blend_seconds': BLEND_FRAMES/RATE,
        'join_step_pcm': seam, 'join_step_matches_adjacent_source': seam == original_step,
        'unmodified_source_percent': 100*(1-BLEND_FRAMES/(END_FRAME-START_FRAME)),
    }
    if report:
        Path(report).write_text(json.dumps(details, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(details, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source')
    parser.add_argument('output')
    parser.add_argument('--ffmpeg', default='ffmpeg')
    parser.add_argument('--preview', help='Optional WAV containing three consecutive cycles.')
    parser.add_argument('--report', help='Optional analysis/provenance JSON.')
    args = parser.parse_args()
    build(args.source, args.output, args.ffmpeg, args.preview, args.report)
