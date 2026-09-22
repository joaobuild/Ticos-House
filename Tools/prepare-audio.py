"""Build-time audio preparation. Game playback is entirely offline."""
from pathlib import Path
import sys, asyncio, json
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / '.tools/audio-deps'))
import numpy as np
import soundfile as sf
import edge_tts

OUT = ROOT / 'Assets/Resources/AudioReal'
OUT.mkdir(parents=True, exist_ok=True)
def master(data, rate, target=.10, peak=.60):
    if data.ndim > 1: data = data.mean(axis=1)
    data = data.astype(np.float64); data -= data.mean()
    # Remove DC and very low rumble with a one-pole high-pass.
    previous=0.; filtered=0.; a=np.exp(-2*np.pi*65/rate)
    for i,x in enumerate(data):
        filtered=a*(filtered+x-previous);previous=x;data[i]=filtered
    rms=np.sqrt(np.mean(data*data));data*=min(target/max(rms,1e-8),peak/max(abs(data).max(),1e-8))
    fade=min(int(rate*.045),len(data)//4)
    data[:fade]*=np.linspace(0,1,fade);data[-fade:]*=np.linspace(1,0,fade)
    return data

def snores():
    data,rate=sf.read(ROOT/'.tools/AudioSource/snoring-original.ogg')
    if data.ndim>1:data=data.mean(axis=1)
    # Select a separate natural breathing cycle from each quarter of the recording.
    for index in range(4):
        start=int((index*12+1)*rate);section=data[start:start+int(10*rate)]
        block=int(rate*.1);energy=np.array([np.mean(section[i:i+block]**2) for i in range(0,len(section)-block,block)])
        center=int(energy.argmax())*block
        offset=max(0,min(center-int(.8*rate),len(section)-int(3.15*rate)))
        clip=master(section[offset:offset+int(3.15*rate)],rate,.085,.42)
        sf.write(OUT/f'snore{index}.wav',clip,rate,subtype='PCM_16')
        print(f'snore{index}: peak={abs(clip).max():.3f}',flush=True)

LINES={
 'brother_enter':('Calma, sou eu! Achou que era o Tico? Pode jogar. Eu fico olhando.', '+3%', '+2Hz'),
 'brother_warning':('Gust! Desliga isso! Ele tá subindo!', '+10%', '+4Hz'),
 'brother_leave':('Vou dormir. Boa sorte aí.', '-3%', '+2Hz'),
 'brother_false':('Acho que ouvi ele... Não, era a geladeira.', '+0%', '+2Hz'),
 'brother_hunt':('Gust... fudeu. Ele tá vindo!', '+5%', '+2Hz'),
 'carlos':('Foi mal, tava olhando o outro monitor.', '-2%', '-2Hz'),
 'munhak':('Esse cara tá muito estranho, mano.', '+6%', '+3Hz'),
 'tico':('Gustavo? Você ainda tá acordado?', '-12%', '-6Hz'),
 'gust_shout':('Carlos! Como você perdeu isso?', '+9%', '+1Hz'),
 'gust_muted':('Tô falando sozinho no mute de novo...', '-4%', '+0Hz'),
 'queue':('Partida encontrada.', '+0%', '+0Hz'),
 'round_win':('Round vencido.', '+0%', '+0Hz'),
 'round_loss':('Round perdido.', '-3%', '+0Hz')}
async def voices():
    destination=ROOT/'Assets/Resources/Voices'
    for name,(line,rate,pitch) in LINES.items():
        temp=ROOT/'.tools/AudioSource'/f'{name}.mp3'
        await asyncio.wait_for(edge_tts.Communicate(line,'pt-BR-AntonioNeural',rate=rate,pitch=pitch).save(str(temp)),60)
        samples,hz=sf.read(temp);samples=master(samples,hz,.12,.60)
        sf.write(destination/f'{name}.wav',samples,hz,subtype='PCM_16')
        print(f'voice {name}: {len(samples)/hz:.2f}s peak={abs(samples).max():.3f}',flush=True)
if __name__=='__main__':
    snores()
    if '--voices' in sys.argv:asyncio.run(voices())
