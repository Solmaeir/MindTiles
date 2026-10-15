# Generates the two social icons used on the Info panel (white glyphs, tinted in Unity).
# Simple geometric versions, not the official brand artwork.
from PIL import Image, ImageDraw
import os

OUT = "/home/ZeynOS/Github/MindTiles/Assets/MindTiles/UI/Sprites"
SIZE, SS = 256, 4
W = SIZE * SS


def canvas():
    img = Image.new("RGBA", (W, W), (255, 255, 255, 0))
    return img, ImageDraw.Draw(img)


def p(v):
    return v * SS


def finish(img, name):
    img = img.resize((SIZE, SIZE), Image.LANCZOS)
    img.save(os.path.join(OUT, name))
    print("wrote", name)


# Instagram: rounded square outline, lens ring, flash dot
img, d = canvas()
white = (255, 255, 255, 255)
d.rounded_rectangle([p(26), p(26), p(230), p(230)], radius=p(66), outline=white, width=p(22))
d.ellipse([p(128 - 52), p(128 - 52), p(128 + 52), p(128 + 52)], outline=white, width=p(22))
d.ellipse([p(178 - 13), p(78 - 13), p(178 + 13), p(78 + 13)], fill=white)
finish(img, "icon_instagram.png")


# X: one thick and one thin diagonal stroke with round ends
def stroke(d, a, b, width):
    d.line([(p(a[0]), p(a[1])), (p(b[0]), p(b[1]))], fill=white, width=p(width))
    for x, y in (a, b):
        r = width / 2
        d.ellipse([p(x - r), p(y - r), p(x + r), p(y + r)], fill=white)


img, d = canvas()
stroke(d, (48, 40), (208, 216), 34)
stroke(d, (208, 40), (48, 216), 16)
finish(img, "icon_x.png")
