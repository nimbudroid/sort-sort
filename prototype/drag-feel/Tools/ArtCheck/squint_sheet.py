"""Phone-size readability check ("squint test").

    python3 squint_sheet.py <dirA[,dirB...]> <out.png> <id,id,...> [size=120]

For each ArtPreview output folder: one row of objects at phone size (with the baked soft shadow when present),
then the same row downscaled 4x, blurred and scaled back up. If an object still reads as a 3D toy in the blurred
row, its form survives on a phone; if it turns into a flat icon there, it fails.
"""
import sys, math, os
from PIL import Image, ImageFilter
# rows: each folder; objects at phone size (T px) on play-zone colour, then a squint row (blurred).
dirs=sys.argv[1].split(','); out=sys.argv[2]; ids=sys.argv[3].split(','); T=int(sys.argv[4]) if len(sys.argv)>4 else 120
BG=(214,236,247,255); pad=8
im=Image.new('RGBA',(len(ids)*(T+pad)+pad,(len(dirs)*2)*(T+pad)+pad),BG)
def load(d,o,suf=''):
    p=f'{d}/{o}{suf}.rgba'
    if not os.path.exists(p): return None
    raw=open(p,'rb').read(); n=int(math.isqrt(len(raw)//4)); return Image.frombytes('RGBA',(n,n),raw).transpose(Image.FLIP_TOP_BOTTOM)
for r,d in enumerate(dirs):
    row=Image.new('RGBA',(im.width,T+2*pad),BG)
    for c,o in enumerate(ids):
        x=pad+c*(T+pad)
        sh=load(d,o,'.shadow')
        if sh is not None:
            s=int(T*1.3); sh=sh.resize((s,s),Image.LANCZOS); a=sh.split()[3].point(lambda v:int(v*0.3))
            L=Image.new('RGBA',sh.size,(41,26,77,0)); L.putalpha(a); row.alpha_composite(L,(int(x+T*0.06-(s-T)/2),int(pad+T*0.08-(s-T)/2)))
        ob=load(d,o).resize((T,T),Image.LANCZOS); row.alpha_composite(ob,(x,pad))
    im.alpha_composite(row.crop((0,0,row.width,T+pad)),(0,2*r*(T+pad)))
    sq=row.resize((row.width//4,row.height//4),Image.LANCZOS).filter(ImageFilter.GaussianBlur(0.8)).resize(row.size,Image.LANCZOS)
    im.alpha_composite(sq.crop((0,0,row.width,T+pad)),(0,(2*r+1)*(T+pad)))
im.convert('RGB').save(out); print(out, im.size)
