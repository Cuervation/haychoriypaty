#!/usr/bin/env python3
"""Synthesizes the original reward-shutter sound effects (pure stdlib, deterministic).

Usage: python3 tools/audio/generate_reward_shutter_sounds.py [output_dir]
Default output: Assets/Resources/Audio. Writes mono 16-bit 44.1 kHz WAV files and, when missing,
Unity .meta files so the GUIDs stay stable once committed.
"""
import math
import os
import random
import struct
import sys
import uuid
import wave

SR = 44100


def buf(seconds):
    return [0.0] * int(seconds * SR)


def add(dst, src, start_s, gain=1.0):
    start = int(start_s * SR)
    for i, v in enumerate(src):
        j = start + i
        if 0 <= j < len(dst):
            dst[j] += v * gain


def one_pole_lp(x, cutoff):
    a = 1.0 - math.exp(-2.0 * math.pi * cutoff / SR)
    y, s = [], 0.0
    for v in x:
        s += a * (v - s)
        y.append(s)
    return y


def one_pole_hp(x, cutoff):
    lp = one_pole_lp(x, cutoff)
    return [v - l for v, l in zip(x, lp)]


def noise(seconds, rng):
    return [rng.uniform(-1.0, 1.0) for _ in range(int(seconds * SR))]


def env_exp(n, decay_s):
    return [math.exp(-i / SR / decay_s) for i in range(n)]


def ping(freq, seconds, decay_s, partials=((1.0, 1.0),)):
    n = int(seconds * SR)
    env = env_exp(n, decay_s)
    out = [0.0] * n
    for ratio, amp in partials:
        w = 2.0 * math.pi * freq * ratio / SR
        for i in range(n):
            out[i] += math.sin(w * i) * amp * env[i]
    return out


METAL = ((1.0, 1.0), (2.76, 0.55), (5.4, 0.3), (8.93, 0.12))


def slat_click(rng, freq):
    """One persiana slat: a bandpassed noise tick plus a damped metallic ping."""
    tick = one_pole_hp(one_pole_lp(noise(0.012, rng), 6500), 1800)
    tick = [v * e for v, e in zip(tick, env_exp(len(tick), 0.003))]
    body = ping(freq, 0.05, 0.012, METAL)
    out = [0.0] * len(body)
    add(out, tick, 0.0, 1.4)
    add(out, body, 0.0, 0.35)
    return out


def rattle(rng, seconds, times, f0, f1, rumble):
    out = buf(seconds)
    n = len(times)
    for k, t in enumerate(times):
        freq = f0 + (f1 - f0) * (k / max(1, n - 1))
        add(out, slat_click(rng, freq * rng.uniform(.94, 1.06)), t, rng.uniform(.7, 1.0))
    # Low rolling rumble of the coil, swelling or fading with the motion.
    body = one_pole_lp(noise(seconds, rng), 520)
    total = len(out)
    for i in range(total):
        p = i / total
        out[i] += body[i] * rumble(p)
    return out


def close_clip(rng):
    seconds = .54
    times, t, gap = [], .01, .05
    while t < seconds - .04:
        times.append(t)
        t += gap
        gap = max(.024, gap * .9)
    return rattle(rng, seconds, times, 900, 1500, lambda p: .5 * math.sin(math.pi * min(1.0, p * 1.1)))


def open_clip(rng):
    seconds = .56
    times, t, gap = [], .01, .022
    while t < .40:
        times.append(t)
        t += gap
        gap = min(.045, gap * 1.1)
    out = rattle(rng, seconds, times, 1250, 2050, lambda p: .45 * (1.0 - p))
    clank = ping(540, .2, .06, ((1.0, 1.0), (2.41, .6), (4.1, .35), (6.7, .2)))
    add(out, clank, .41, .7)
    return out


def slam_clip(rng):
    seconds = .38
    out = buf(seconds)
    n = int(.22 * SR)
    phase = 0.0
    for i in range(n):
        t = i / SR
        freq = 45.0 + 95.0 * math.exp(-t / .03)
        phase += 2.0 * math.pi * freq / SR
        out[i] += math.sin(phase) * math.exp(-t / .07) * 1.1
    crack = one_pole_lp(noise(.08, rng), 2600)
    add(out, [v * e for v, e in zip(crack, env_exp(len(crack), .018))], 0.0, .9)
    add(out, ping(310, .3, .09, ((1.0, 1.0), (2.51, .7), (4.2, .45), (6.9, .25))), 0.0, .5)
    return out


def bell(freq, seconds=.5, decay=.14):
    return ping(freq, seconds, decay, ((1.0, 1.0), (2.0, .35), (3.0, .12), (4.07, .06)))


def coin(rng, freq):
    n = .22
    p = ping(freq, n, .05, ((1.0, 1.0), (1.51, .5), (2.32, .35), (3.7, .2)))
    tick = one_pole_hp(noise(.006, rng), 3000)
    out = [0.0] * len(p)
    add(out, p, 0.0, .7)
    add(out, [v * e for v, e in zip(tick, env_exp(len(tick), .002))], 0.0, .5)
    return out


def register_clip(rng, bright):
    seconds = .85 if bright else .6
    out = buf(seconds)
    # Mechanical drawer click, then the two-note register bell.
    click = one_pole_hp(one_pole_lp(noise(.02, rng), 3500), 700)
    add(out, [v * e for v, e in zip(click, env_exp(len(click), .006))], 0.0, .9)
    notes = (1568.0, 2093.0) if bright else (1318.5, 1760.0)
    add(out, bell(notes[0], .35, .1), .03, .55)
    add(out, bell(notes[1], .6, .16), .11, .6)
    coins = 10 if bright else 5
    base = 2600.0 if bright else 2200.0
    for k in range(coins):
        t = .12 + k * (.045 if bright else .06) + rng.uniform(0, .015)
        add(out, coin(rng, base * rng.uniform(.8, 1.35)), t, rng.uniform(.35, .6))
    if bright:
        for k, f in enumerate((2093.0, 2637.0, 3136.0, 4186.0)):
            add(out, bell(f, .3, .07), .3 + k * .05, .22)
    return out


def finalize(samples, peak=.8, fade_ms=6):
    top = max(1e-9, max(abs(v) for v in samples))
    gain = peak / top
    out = [v * gain for v in samples]
    fade = int(fade_ms / 1000.0 * SR)
    for i in range(min(fade, len(out))):
        out[-1 - i] *= i / fade
    for i in range(min(48, len(out))):
        out[i] *= i / 48.0
    return out


def write_wav(path, samples):
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, v)) * 32767)) for v in samples))


META = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 0
    quality: 1
    conversionMode: 0
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 0
  preloadAudioData: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def main():
    out_dir = sys.argv[1] if len(sys.argv) > 1 else os.path.join("Assets", "Resources", "Audio")
    os.makedirs(out_dir, exist_ok=True)
    clips = {
        "reward-shutter-close": close_clip(random.Random(11)),
        "reward-shutter-slam": slam_clip(random.Random(12)),
        "reward-shutter-open": open_clip(random.Random(13)),
        "reward-claim-coins": register_clip(random.Random(14), False),
        "reward-claim-coins-bright": register_clip(random.Random(15), True),
    }
    for name, samples in clips.items():
        path = os.path.join(out_dir, name + ".wav")
        write_wav(path, finalize(samples))
        meta = path + ".meta"
        if not os.path.exists(meta):
            with open(meta, "w") as f:
                f.write(META.format(guid=uuid.uuid4().hex))
        print("%s  %.2fs" % (path, len(samples) / SR))


if __name__ == "__main__":
    main()
