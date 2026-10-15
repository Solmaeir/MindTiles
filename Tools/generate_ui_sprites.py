# Generates the MindTiles UI sprites (palette from the design sheet).
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import math, os

OUT = "/home/ZeynOS/Github/MindTiles/Assets/MindTiles/UI/Sprites"
FONTS = "/home/ZeynOS/Github/MindTiles/Assets/MindTiles/Fonts/Font Sources/static"
SS = 4  # supersampling

NIGHT=(0x1A,0x13,0x38); INK=(0x2A,0x1A,0x5E); VIOLET=(0x5B,0x3F,0xB8)
LAVENDER=(0xB2,0x9A,0xFF); PINK=(0xFF,0x94,0xC6); PEACH=(0xFF,0xC7,0x9A)
OUTLINE=(0x2A,0x17,0x52)

def lerp(a,b,t): return tuple(int(round(a[i]+(b[i]-a[i])*t)) for i in range(len(a)))
def lighten(c,t): return lerp(c,(255,255,255),t)
def darken(c,t): return lerp(c,(0,0,0),t)

def finish(img, w, h):
    return img.resize((w,h), Image.LANCZOS)

def rr_mask(w,h,r):
    m = Image.new("L",(w*SS,h*SS),0)
    ImageDraw.Draw(m).rounded_rectangle([0,0,w*SS-1,h*SS-1], r*SS, fill=255)
    return m

def vgrad(w,h,top,bottom):
    g = Image.new("RGBA",(w,h))
    px = g.load()
    for y in range(h):
        c = lerp(top,bottom,y/max(1,h-1)) + (255,)
        for x in range(w): px[x,y]=c
    return g

def save(img,name):
    img.save(os.path.join(OUT,name)); print("wrote",name,img.size)

# ---- plain shapes (white, tinted in Unity) ----
def rounded(name, w, h, r):
    m = rr_mask(w,h,r)
    img = Image.new("RGBA",(w*SS,h*SS),(255,255,255,0)); img.putalpha(m)
    save(finish(img,w,h), name)

def outline(name, w, h, r, stroke):
    m = Image.new("L",(w*SS,h*SS),0); d=ImageDraw.Draw(m)
    d.rounded_rectangle([0,0,w*SS-1,h*SS-1], r*SS, fill=255)
    s=stroke*SS
    d.rounded_rectangle([s,s,w*SS-1-s,h*SS-1-s], max(1,(r-stroke))*SS, fill=0)
    img = Image.new("RGBA",(w*SS,h*SS),(255,255,255,0)); img.putalpha(m)
    save(finish(img,w,h), name)

def circle(name, d):
    m = Image.new("L",(d*SS,d*SS),0); ImageDraw.Draw(m).ellipse([0,0,d*SS-1,d*SS-1], fill=255)
    img = Image.new("RGBA",(d*SS,d*SS),(255,255,255,0)); img.putalpha(m)
    save(finish(img,d,d), name)

rounded("ui_rounded.png", 128, 128, 40)
rounded("ui_rounded_small.png", 64, 64, 16)
outline("ui_outline.png", 128, 128, 40, 4)
rounded("ui_pill.png", 128, 64, 32)
circle("ui_circle.png", 128)

# ---- gradient button faces (pre-colored), with a light inner rim at the top ----
def button_face(name, top, bottom, w=256, h=128, r=44):
    W,H=w*SS,h*SS
    g = vgrad(W,H,top,bottom)
    rim = Image.new("L",(W,H),0); d=ImageDraw.Draw(rim)
    d.rounded_rectangle([0,0,W-1,H-1], r*SS, outline=255, width=3*SS)
    rimcol = Image.new("RGBA",(W,H),(255,255,255,170))
    g = Image.composite(rimcol, g, rim)
    g.putalpha(rr_mask(w,h,r))
    save(finish(g,w,h), name)

button_face("btn_pink.png", (0xFF,0xC6,0xE0), PINK)
button_face("btn_lavender.png", (0xE6,0xDE,0xFF), LAVENDER)

# ---- tiles (logo letters + loading bar): gradient face, dark outline, darker lip ----
def tile(name, color, size=128, r=30, outline_px=7, lip=10):
    W=size*SS; o=outline_px*SS; L=lip*SS
    img = Image.new("RGBA",(W,W),(0,0,0,0)); d=ImageDraw.Draw(img)
    d.rounded_rectangle([0,0,W-1,W-1], r*SS, fill=OUTLINE+(255,))
    d.rounded_rectangle([o,o+L,W-1-o,W-1-o], (r-outline_px)*SS, fill=darken(color,0.28)+(255,))
    face = vgrad(W-2*o, W-2*o-L, lighten(color,0.45), color)
    fm = Image.new("L",face.size,0); ImageDraw.Draw(fm).rounded_rectangle([0,0,face.size[0]-1,face.size[1]-1],(r-outline_px)*SS,fill=255)
    img.paste(face,(o,o),fm)
    save(finish(img,size,size), name)

tile("tile_pink.png", PINK); tile("tile_peach.png", PEACH); tile("tile_lavender.png", LAVENDER)

def slot(name, dashed):
    size=128; W=size*SS; r=30*SS
    img = Image.new("RGBA",(W,W),(0,0,0,0)); d=ImageDraw.Draw(img)
    if not dashed:
        d.rounded_rectangle([0,0,W-1,W-1], r, fill=(0x33,0x27,0x6E,255))
    else:
        # dashed rounded outline in peach
        tmp = Image.new("L",(W,W),0); td=ImageDraw.Draw(tmp)
        td.rounded_rectangle([8,8,W-9,W-9], r, outline=255, width=6*SS)
        cut = Image.new("L",(W,W),255); cd=ImageDraw.Draw(cut)
        n=16
        for i in range(n):
            a0=2*math.pi*i/n; a1=a0+math.pi/n
            cx=cy=W/2; R=W
            cd.polygon([(cx,cy),(cx+R*math.cos(a0),cy+R*math.sin(a0)),(cx+R*math.cos(a1),cy+R*math.sin(a1))], fill=0)
        mask = Image.composite(tmp, Image.new("L",(W,W),0), cut)
        col = Image.new("RGBA",(W,W),PEACH+(255,))
        img = Image.composite(col, img, mask)
    save(finish(img,size,size), name)

slot("tile_slot_empty.png", False); slot("tile_slot_dashed.png", True)

# ---- missing icons (white): infinity, vibration ----
def icon_canvas(): 
    W=256*SS; return Image.new("RGBA",(W,W),(0,0,0,0)), W

img,W = icon_canvas(); d=ImageDraw.Draw(img)
rad=13*SS
for i in range(1600):
    t=2*math.pi*i/1600; sn=math.sin(t); c=math.cos(t)
    x=W/2 + W*0.36*c/(1+sn*sn); y=W/2 + W*0.36*sn*c/(1+sn*sn)
    d.ellipse([x-rad,y-rad,x+rad,y+rad], fill=(255,255,255,255))
save(finish(img,256,256),"icon_infinity.png")

img,W = icon_canvas(); d=ImageDraw.Draw(img)
w=18*SS
d.rounded_rectangle([W*0.37,W*0.20,W*0.63,W*0.80], 24*SS, outline=(255,255,255,255), width=w)
d.line([(W*0.47,W*0.70),(W*0.53,W*0.70)], fill=(255,255,255,255), width=12*SS)
for side,start in ((-1,135),(1,-45)):
    for k,rr in enumerate((0.22,0.31)):
        box=[W/2-W*rr, W/2-W*rr, W/2+W*rr, W/2+W*rr]
        d.arc(box, start, start+90, fill=(255,255,255,255), width=12*SS)
save(finish(img,256,256),"icon_vibration.png")

# ---- sparkle (4-point star) ----
img,W = icon_canvas(); d=ImageDraw.Draw(img)
c=W/2; R=W*0.48; r=W*0.09
pts=[]
for i in range(8):
    a=math.pi/2*i/2 - math.pi/2
    rad = R if i%2==0 else r
    pts.append((c+rad*math.cos(a), c+rad*math.sin(a)))
d.polygon(pts, fill=(255,255,255,255))
save(finish(img,256,256).filter(ImageFilter.SMOOTH),"sparkle.png")

# ---- logo: "MIND" + tiles T I L E S ----
fred = os.path.join(FONTS,"Fredoka-Bold.ttf")
def logo(name):
    w,h=1400,650
    W,H=w*SS,h*SS
    img = Image.new("RGBA",(W,H),(0,0,0,0))
    font = ImageFont.truetype(fred, 300*SS)
    text="MIND"
    bbox = font.getbbox(text)
    tw,th = bbox[2]-bbox[0], bbox[3]-bbox[1]
    tx = (W-tw)//2 - bbox[0]; ty = 40*SS - bbox[1]
    # outline + drop
    outl = Image.new("L",(W,H),0); ImageDraw.Draw(outl).text((tx,ty+14*SS),text,font=font,fill=255,stroke_width=34*SS,stroke_fill=255)
    img.paste(Image.new("RGBA",(W,H),darken(OUTLINE,0.25)+(255,)),(0,0),outl)
    outl2 = Image.new("L",(W,H),0); ImageDraw.Draw(outl2).text((tx,ty),text,font=font,fill=255,stroke_width=30*SS,stroke_fill=255)
    img.paste(Image.new("RGBA",(W,H),OUTLINE+(255,)),(0,0),outl2)
    fillm = Image.new("L",(W,H),0); ImageDraw.Draw(fillm).text((tx,ty),text,font=font,fill=255)
    grad = vgrad(W,H,(255,255,255),(0xD8,0xC8,0xFF))
    # gradient only across the glyph height
    g2 = vgrad(W, th, (255,250,255), (0xD6,0xC6,0xFF))
    gfull = Image.new("RGBA",(W,H),(255,255,255,255)); gfull.paste(g2,(0,ty+bbox[1]))
    img.paste(gfull,(0,0),fillm)
    # tiles
    tilefont = ImageFont.truetype(fred, 120*SS)
    cols=[PINK,PEACH,LAVENDER,PINK,PEACH]; letters="TILES"; rots=[-6,4,-3,5,-4]
    size=230; gap=26
    total = 5*size + 4*gap
    x0 = (w-total)//2; y0 = 370
    for i,(L,col,rot) in enumerate(zip(letters,cols,rots)):
        T = Image.new("RGBA",(size*SS,size*SS),(0,0,0,0))
        W2=size*SS; o=10*SS; lip=14*SS; r=46*SS
        d=ImageDraw.Draw(T)
        d.rounded_rectangle([0,0,W2-1,W2-1], r, fill=OUTLINE+(255,))
        d.rounded_rectangle([o,o+lip,W2-1-o,W2-1-o], r-o, fill=darken(col,0.3)+(255,))
        face=vgrad(W2-2*o, W2-2*o-lip, lighten(col,0.5), col)
        fm=Image.new("L",face.size,0); ImageDraw.Draw(fm).rounded_rectangle([0,0,face.size[0]-1,face.size[1]-1], r-o, fill=255)
        T.paste(face,(o,o),fm)
        lb = tilefont.getbbox(L)
        lx=(W2-(lb[2]-lb[0]))//2-lb[0]; ly=(W2-lip-(lb[3]-lb[1]))//2-lb[1]
        ImageDraw.Draw(T).text((lx,ly),L,font=tilefont,fill=OUTLINE+(255,))
        T = T.rotate(rot, resample=Image.BICUBIC, expand=True)
        px = (x0 + i*(size+gap))*SS - (T.size[0]-W2)//2
        py = y0*SS - (T.size[1]-W2)//2 + (8*SS if i%2 else 0)
        img.alpha_composite(T,(px,py))
    # sparkle near the D
    sp = Image.open(os.path.join(OUT,"sparkle.png")).resize((110*SS,110*SS), Image.LANCZOS)
    tint = Image.new("RGBA",sp.size,PEACH+(255,)); tint.putalpha(sp.getchannel("A"))
    img.alpha_composite(tint,((tx+tw)+10*SS, 0))
    save(finish(img,w,h), name)

logo("logo_mindtiles.png")
