#!/usr/bin/env python3
"""Original qiaopi theme: deterministic score, modelled instruments, circular room.
Requirements: Python 3, numpy, ffmpeg. No samples, external songs, or network calls.
Run: python3 generate_score.py [--output-dir PATH] [--work-dir PATH]
"""
from pathlib import Path
import argparse, json, math, re, struct, subprocess, wave, shutil
import numpy as np

SR=44100
SOURCE=Path(__file__).resolve().parent
PROJECT=SOURCE.parent
parser=argparse.ArgumentParser()
parser.add_argument('--output-dir',type=Path,default=PROJECT/'Assets/Resources/Music')
parser.add_argument('--work-dir',type=Path,default=SOURCE/'render-work')
args=parser.parse_args()
args.output_dir.mkdir(parents=True,exist_ok=True);args.work_dir.mkdir(parents=True,exist_ok=True)

# Eight breathing phrases, each eight quarter-note beats, D minor pentatonic.
# Tuple: offset in phrase, MIDI pitch, sounding duration in beats, expression.
PHRASES=[
 [(0,62,1.42,.83),(1.75,65,.56,.70),(2.55,67,.99,.78),(4.0,69,1.80,.93),(6.27,67,.50,.70),(7.0,65,.67,.68)],
 [(0,67,.95,.79),(1.30,65,.76,.69),(2.35,62,2.17,.86),(5.13,60,.67,.63),(6.25,62,1.49,.73)],
 [(0,65,1.22,.78),(1.58,67,.73,.72),(2.56,69,1.51,.89),(4.55,72,1.26,.87),(6.27,69,1.43,.74)],
 [(0,67,1.52,.80),(2.03,65,.83,.71),(3.27,62,2.58,.85)],
 [(0,62,1.39,.85),(1.62,65,.70,.74),(2.71,67,.82,.80),(4.02,69,1.48,.94),(6.10,72,.56,.73),(7.03,69,.61,.69)],
 [(0,74,1.65,.85),(2.10,72,.76,.76),(3.25,69,1.32,.89),(5.23,67,.76,.70),(6.48,65,1.02,.73)],
 [(0,67,1.34,.83),(1.77,69,.76,.78),(3.06,72,1.33,.86),(5.10,69,.73,.76),(6.24,67,1.43,.75)],
 [(0,65,1.47,.77),(2.01,67,.61,.70),(3.02,65,.73,.68),(4.26,62,2.77,.86)]
]
FOUNDATIONS=[(50,57),(48,55),(53,60),(50,57),(50,57),(53,60),(48,55),(50,57)]
CONFIGS={
 'HomeLetter':dict(title='纸短情长 · 泉州思乡',bpm=48,seed=1820,flute=.165,pipa=.092,sanxian=.063,bow=.024,clap=.036,room=.20,pan=.28),
 'SeaLetter':dict(title='纸短情长 · 海路南洋',bpm=50,seed=1831,flute=.153,pipa=.105,sanxian=.070,bow=.031,clap=.032,room=.27,pan=.36),
 'ReturnLetter':dict(title='纸短情长 · 回批归途',bpm=46,seed=1842,flute=.158,pipa=.086,sanxian=.056,bow=.039,clap=.031,room=.22,pan=.29)
}

def envelope(t,duration,attack,release):
    a=np.sin(np.minimum(t/max(attack,1e-5),1)*np.pi/2)**2
    r=np.sin(np.minimum(np.maximum(duration-t,0)/max(release,1e-5),1)*np.pi/2)**2
    return a*r

def band_noise(n,rng,lo,hi):
    white=rng.normal(0,1,n)
    freq=np.fft.rfftfreq(n,1/SR)
    filt=(1-np.exp(-(freq/lo)**4))*np.exp(-(freq/hi)**4)
    out=np.fft.irfft(np.fft.rfft(white)*filt,n)
    return out/(np.std(out)+1e-9)

def freq(midi):return 440*2**((midi-69)/12)

def flute(midi,duration,rng,expression=1):
    # Breath-shaped low woodwind with harmonic saturation, delayed vibrato,
    # unvoiced breath and a gentle entry scoop, rather than a fixed sine beep.
    n=int((duration+.30)*SR);t=np.arange(n)/SR;f=freq(midi)
    attack=.105+rng.uniform(0,.065);release=min(.34,duration*.23)
    vibrato_depth=(1-np.exp(-np.maximum(t-.30,0)*2.4))*.0024
    pitch=1+vibrato_depth*np.sin(2*np.pi*(4.5+rng.uniform(-.25,.25))*t)+.0008*np.sin(2*np.pi*.83*t)
    pitch-=.007*np.exp(-t/.046)
    phase=2*np.pi*np.cumsum(f*pitch)/SR
    airy=band_noise(n,rng,700,4800)
    body=np.sin(phase)+.26*np.sin(2*phase+.12)+.075*np.sin(3*phase+.31)+.035*np.sin(4*phase)
    body+=.014*np.sin(5*phase)
    breath=.026*airy*(1+.65*np.exp(-t/.07))
    phrasing=1+.075*np.sin(np.pi*t/max(duration,.1))+.022*np.sin(2*np.pi*2.4*t)
    return ((body+breath)*envelope(t,duration+.19,attack,release+.19)*phrasing*.69*expression).astype(np.float32)

def pluck(midi,duration,rng,kind='pipa'):
    # Modal vibrating string, frequency-dependent decay and body resonances.
    tail=2.2 if kind=='pipa' else 1.8;n=int((duration+tail)*SR);t=np.arange(n)/SR;f=freq(midi)
    pick=.20 if kind=='pipa' else .27
    signal=np.zeros(n,np.float64)
    for h in range(1,19 if kind=='pipa' else 13):
        hh=h*np.sqrt(1+.000035*h*h)
        strength=abs(np.sin(np.pi*h*pick))/(h**1.12)
        decay=(1.65 if kind=='pipa' else 1.25)/(1+.115*h**1.4)
        body=.65+.38*np.exp(-((f*h-640)/380)**2)
        detune=1+rng.uniform(-.00045,.00045)
        signal+=strength*body*np.exp(-t/decay)*np.sin(2*np.pi*f*hh*detune*t)
    # Soft paired-course beating, with a restrained wooden pick transient.
    signal+=.12*np.exp(-t/.90)*np.sin(2*np.pi*f*1.0022*t)
    click=band_noise(n,rng,350,3600)*np.exp(-t/.014)*.035
    attack=1-np.exp(-t/.004)
    end=envelope(t,duration+tail,0.002,.30)
    return ((signal+click)*attack*end*.86).astype(np.float32)

def bowed(midi,duration,rng):
    n=int((duration+.55)*SR);t=np.arange(n)/SR;f=freq(midi)
    vibrato=.0018*np.sin(2*np.pi*4.3*t)*(1-np.exp(-t*1.8))
    phase=2*np.pi*np.cumsum(f*(1+vibrato+.0004*np.sin(t*4)))/SR
    signal=np.zeros(n)
    for h in range(1,15):
        formant=.6+.35*np.exp(-((f*h-800)/450)**2)
        signal+=(1/h**1.45)*formant*np.sin(h*phase+.1*h)
    noise=band_noise(n,rng,450,2400)*.016
    return ((signal+noise)*envelope(t,duration+.35,.55,.65)*.53*(1+.025*np.sin(t*7.5))).astype(np.float32)

def clapper(rng):
    t=np.arange(int(.23*SR))/SR
    # Two small wooden slats; no drum or bright electronic click.
    signal=np.zeros(len(t))
    for hz,weight,decay in [(760,1,.022),(1187,.63,.013),(1923,.29,.009),(2911,.11,.006)]:
        signal+=weight*np.sin(2*np.pi*hz*t)*np.exp(-t/decay)
    signal+=band_noise(len(t),rng,650,3500)*np.exp(-t/.005)*.12
    return (signal*(1-np.exp(-t/.0005))*.65).astype(np.float32)

def add_wrap(track,signal,start,level,pan):
    start=int(round(start*SR))%len(track);length=min(len(signal),len(track))
    signal=signal[:length]*level
    gains=np.array([math.cos((pan+1)*np.pi/4),math.sin((pan+1)*np.pi/4)],np.float32)
    split=min(length,len(track)-start)
    track[start:start+split]+=signal[:split,None]*gains
    if split<length:track[:length-split]+=signal[split:,None]*gains

def circular_room(dry,wet,seed):
    rng=np.random.default_rng(seed);n=len(dry);m=int(1.72*SR);t=np.arange(m)/SR
    room=np.zeros((m,2),np.float64)
    for c in range(2):
        noise=band_noise(m,rng,180,3200)
        # Diffuse tail begins behind early reflections, never a huge hall wash.
        room[:,c]=noise*np.exp(-t/0.33)*(1-np.exp(-np.maximum(t-.035,0)/.045))*.009
        for delay,amp in [(.037,.33),(.068,.24),(.097,.18),(.143,.13),(.211,.08)]:
            room[int((delay+c*.006)*SR),c]+=amp
        room[:,c]/=max(1,np.sqrt(np.sum(room[:,c]**2))*2.3)
    output=dry.astype(np.float64).copy()
    for c in range(2):
        source=.79*dry[:,c]+.21*dry[:,1-c]
        response=np.fft.rfft(room[:,c],n)
        output[:,c]+=wet*np.fft.irfft(np.fft.rfft(source)*response,n)
    # Circular DC/rumble removal and gentle air roll-off preserve the loop seam.
    frequencies=np.fft.rfftfreq(n,1/SR)
    eq=(1-np.exp(-(frequencies/48)**4))*np.exp(-(frequencies/8600)**6)
    for c in range(2):output[:,c]=np.fft.irfft(np.fft.rfft(output[:,c])*eq,n)
    return output

def write_wav(path,data):
    pcm=np.round(np.clip(data,-1,1)*32767).astype('<i2')
    with wave.open(str(path),'wb') as f:
        f.setnchannels(2);f.setsampwidth(2);f.setframerate(SR);f.writeframes(pcm.tobytes())

def loudness(path):
    result=subprocess.run(['ffmpeg','-hide_banner','-nostats','-i',str(path),'-af','loudnorm=I=-22:TP=-3:LRA=11:print_format=json','-f','null','-'],capture_output=True,text=True,check=True)
    return json.loads(re.findall(r'\{[^{}]+\}',result.stderr,re.S)[-1])

def varlen(value):
    buf=[value&127];value>>=7
    while value:buf.insert(0,(value&127)|128);value>>=7
    return bytes(buf)
def midi_track(events):
    events.sort(key=lambda x:(x[0],x[1][0]&0xf0!=0x80))
    prev=0;body=bytearray()
    for tick,data in events:body+=varlen(tick-prev)+data;prev=tick
    body+=b'\x00\xff\x2f\x00'
    return b'MTrk'+struct.pack('>I',len(body))+body
def write_midi(path,events,bpm,title):
    tracks=[];tempo=round(60_000_000/bpm);name=title.encode('utf8')
    tracks.append(midi_track([(0,b'\xff\x03'+varlen(len(name))+name),(0,b'\xff\x51\x03'+tempo.to_bytes(3,'big')),(0,b'\xff\x58\x04\x04\x02\x18\x08')]))
    for channel,(inst,program) in enumerate([('flute',73),('pipa',24),('sanxian',105),('bow',110),('clap',115)]):
        label=inst.encode();msgs=[(0,b'\xff\x03'+varlen(len(label))+label),(0,bytes([0xc0+channel,program]))]
        for ev in events:
            if ev['instrument']!=inst:continue
            start=max(0,round(ev['beat']*480));end=round((ev['beat']+ev['duration_beats'])*480)
            pitch=ev['pitch'];velocity=min(110,max(25,int(ev['expression']*88)))
            msgs.extend([(start,bytes([0x90+channel,pitch,velocity])),(end,bytes([0x80+channel,pitch,0]))])
        tracks.append(midi_track(msgs))
    path.write_bytes(b'MThd'+struct.pack('>IHHH',6,1,len(tracks),480)+b''.join(tracks))

def render(name,config):
    rng=np.random.default_rng(config['seed']);beat=60/config['bpm'];duration=64*beat
    n=round(duration*SR);dry=np.zeros((n,2),np.float32);events=[]
    def place(inst,pitch,start,dur,expression=1,pan=0):
        on=max(.016,start*beat+rng.uniform(-.014,.014))
        sec=dur*beat
        if inst=='flute':sig=flute(pitch,sec,rng,expression)
        elif inst in ('pipa','sanxian'):sig=pluck(pitch,sec,rng,inst)*expression
        elif inst=='bow':sig=bowed(pitch,sec,rng)*expression
        else:sig=clapper(rng)*expression
        add_wrap(dry,sig,on,config[inst],pan)
        events.append(dict(instrument=inst,pitch=pitch,beat=start,duration_beats=dur,expression=expression))
    for phrase_idx,phrase in enumerate(PHRASES):
        base=phrase_idx*8;bass,fifth=FOUNDATIONS[phrase_idx]
        for offset,pitch,dur,expression in phrase:
            place('flute',pitch,base+offset+.10,dur,expression,-.075)
        # Low strings articulate the phrase, not a repeated Western beat.
        place('sanxian',bass,base+.05,1.0,.80,-config['pan'])
        place('sanxian',fifth,base+4.35,.60,.48,-config['pan'])
        # Open fifth support; no piano, chord-pad swells or bass-drum pattern.
        place('bow',bass+12,base+.47,5.72,.78,.23)
        if phrase_idx in (2,5,6) or name=='ReturnLetter':
            place('bow',fifth+12,base+1.16,4.26,.38,.30)
        # Plucked replies make a second voice in gaps of the breathing melody.
        reply=[phrase[-1][1],phrase[-1][1]-2 if phrase[-1][1] in (67,69,74) else 62,bass+12]
        for off,pitch,vel in [(1.03,bass+12,.64),(3.68,fifth+12,.60),(6.02,reply[0],.63),(7.46,reply[-1],.55)]:
            place('pipa',pitch,base+off,.36,vel,config['pan'])
        if phrase_idx in (1,3,7):
            place('pipa',62,base+6.64,.45,.63,config['pan'])
            place('pipa',57,base+7.14,.42,.42,config['pan'])
        if name=='SeaLetter':
            for off,pitch in [(2.18,fifth+12),(5.43,bass+19)]:place('pipa',pitch,base+off,.27,.42,config['pan'])
        if (name=='HomeLetter' and phrase_idx in (2,6)) or (name=='SeaLetter' and phrase_idx in (1,5)) or (name=='ReturnLetter' and phrase_idx in (0,4,7)):
            for k in range(5):place('pipa',phrase[-1][1],base+6.67+k*.115,.15,.24+.10*math.sin(k*math.pi/4),config['pan'])
        if phrase_idx%2==0:place('clap',76,base+.015,.12,.72,-.21)
        if name=='SeaLetter' and phrase_idx in (1,5):place('clap',76,base+4.06,.12,.45,-.21)
        if name=='ReturnLetter' and phrase_idx==7:place('clap',76,base+.02,.12,.48,-.21)
    mix=circular_room(dry,config['room'],config['seed']+200)
    mix*=.80/max(np.max(np.abs(mix)),.001)
    raw=args.work_dir/(name+'-premaster.wav');write_wav(raw,mix)
    measured=loudness(raw)
    gain=10**((-22-float(measured['input_i']))/20)
    peak_after=float(measured['input_tp'])+20*math.log10(gain)
    if peak_after>-3.05:gain*=10**((-3.05-peak_after)/20)
    mix*=gain
    path=args.output_dir/(name+'.wav');write_wav(path,mix)
    final=loudness(path)
    write_midi(SOURCE/(name+'.mid'),events,config['bpm'],config['title'])
    (SOURCE/(name+'-score.json')).write_text(json.dumps(dict(title=config['title'],bpm=config['bpm'],meter='4/4',bars=16,scale='D F G A C — original modal theme, not a traditional qupai',seed=config['seed'],events=events),ensure_ascii=False,indent=2))
    pcm=np.round(np.clip(mix,-1,1)*32767).astype(np.int16).astype(float)/32768
    seam=np.max(np.abs(pcm[0]-pcm[-1]));diff=np.abs(np.diff(pcm,axis=0))
    frames=pcm[:(len(pcm)//SR)*SR].reshape(-1,SR,2)
    rms=np.sqrt(np.mean(frames**2,axis=(1,2)))
    active=np.flatnonzero(np.max(np.abs(pcm),axis=1)>.0003)
    # Seam cut-out is last 3 s + first 3 s, repeated once for inspection.
    seamclip=np.concatenate([pcm[-3*SR:],pcm[:3*SR],pcm[-3*SR:],pcm[:3*SR]])
    write_wav(args.work_dir/(name+'-loop-seam.wav'),seamclip)
    stat=dict(file=str(path),seconds=len(mix)/SR,sample_rate=SR,channels=2,bit_depth=16,integrated_lufs=float(final['input_i']),true_peak_dbtp=float(final['input_tp']),loudness_range_lu=float(final['input_lra']),seam_sample_delta=float(seam),seam_delta_dbfs=20*math.log10(max(seam,1e-12)),normal_adjacent_step_99pct=float(np.percentile(diff,99)),first_audible_seconds=float(active[0]/SR),quietest_one_second_rms_dbfs=float(20*np.log10(min(rms))),peak_one_second_rms_dbfs=float(20*np.log10(max(rms))),notes_by_instrument={inst:sum(ev['instrument']==inst for ev in events) for inst in ('flute','pipa','sanxian','bow','clap')})
    assert 64<=stat['seconds']<=96 and stat['true_peak_dbtp']<=-3 and abs(stat['integrated_lufs']+22)<.35
    # A periodic sinusoid normally has different adjacent sample values; the
    # boundary step must remain below ordinary in-track steps, not equal zero.
    assert seam<.008 and seam<stat['normal_adjacent_step_99pct'] and stat['first_audible_seconds']<.30
    print(json.dumps(stat,ensure_ascii=False),flush=True)
    return stat,mix

report=[];mixes={}
for name,config in CONFIGS.items():
    stat,mix=render(name,config);report.append(stat);mixes[name]=mix
(SOURCE/'audio-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
# User-facing stand-alone theme audition, with graceful start/end instead of a hard stop.
audition=mixes['HomeLetter'].copy();fade=int(SR*1.5);audition[:int(.035*SR)]*=np.linspace(0,1,int(.035*SR))[:,None];audition[-fade:]*=np.linspace(1,0,fade)[:,None]
preview=PROJECT.parent/'泉州侨批-主题配乐.wav';write_wav(preview,audition)
print('SHAREABLE_AUDITION',preview,flush=True)
