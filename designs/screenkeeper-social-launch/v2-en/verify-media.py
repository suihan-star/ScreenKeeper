from pathlib import Path
from PIL import Image
import json, hashlib
root=Path(__file__).resolve().parent
records=[]
assert len(list((root/'exports').glob('*.gif')))==4
assert len(list((root/'exports').glob('*.mp4')))==4
assert len(list((root/'posters').glob('*.png')))==4
for path in sorted((root/'exports').glob('*.gif')):
    with Image.open(path) as im:
        total=0; unique=set()
        for n in range(im.n_frames):
            im.seek(n); im.load()
            total+=im.info.get('duration',0)
            if n%12==0: unique.add(hashlib.sha256(im.convert('RGB').tobytes()).hexdigest())
        assert im.size==(540,720), (path,im.size)
        assert abs(total-18000)<=100, (path,total)
        assert im.n_frames==216 and len(unique)>5, (path,im.n_frames,len(unique))
        assert im.info.get('loop')==0, path
        records.append(dict(file=path.name,width=im.width,height=im.height,frames=im.n_frames,durationMs=total,loop=0,sampledDistinctFrames=len(unique),bytes=path.stat().st_size,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
for path in sorted((root/'posters').glob('*.png')):
    with Image.open(path) as im:
        im.load()
        if path.name.startswith('xhs'): assert im.width*4==im.height*3
        records.append(dict(file='posters/'+path.name,width=im.width,height=im.height,bytes=path.stat().st_size,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
(root/'qa'/'media-verification.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(records,ensure_ascii=False,indent=2))
