from pathlib import Path
import sys

from PIL import Image, ImageEnhance, ImageFilter


SOURCE = Path(sys.argv[1])
OUTPUT = Path(sys.argv[2])
CONTACT = Path(sys.argv[3])

WIDTH = 1920
HEIGHT = 1080
SCALE = 0.92
FEATHER = 42


def main() -> None:
    source = Image.open(SOURCE).convert("RGB")
    foreground_size = (round(WIDTH * SCALE), round(HEIGHT * SCALE))
    offset = (
        round((WIDTH - foreground_size[0]) / 2),
        round((HEIGHT - foreground_size[1]) / 2),
    )

    background = source.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS)
    background = background.filter(ImageFilter.GaussianBlur(34))
    background = ImageEnhance.Color(background).enhance(0.72)
    background = ImageEnhance.Brightness(background).enhance(0.62)

    foreground = source.resize(foreground_size, Image.Resampling.LANCZOS)
    mask = Image.new("L", foreground_size, 255)
    pixels = mask.load()
    max_x = foreground_size[0] - 1
    max_y = foreground_size[1] - 1

    for y in range(foreground_size[1]):
        for x in range(foreground_size[0]):
            distance = min(x, y, max_x - x, max_y - y)
            t = max(0.0, min(1.0, distance / FEATHER))
            pixels[x, y] = round(t * t * (3 - 2 * t) * 255)

    background.paste(foreground, offset, mask)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    background.save(OUTPUT, optimize=True)

    original_preview = source.resize((960, 540), Image.Resampling.LANCZOS)
    revised_preview = background.resize((960, 540), Image.Resampling.LANCZOS)
    contact = Image.new("RGB", (1920, 540), "#111319")
    contact.paste(original_preview, (0, 0))
    contact.paste(revised_preview, (960, 0))
    CONTACT.parent.mkdir(parents=True, exist_ok=True)
    contact.save(CONTACT, optimize=True)

    print(
        {
            "output": str(OUTPUT),
            "contact": str(CONTACT),
            "scale": SCALE,
            "foreground": foreground_size,
            "offset": offset,
            "feather": FEATHER,
        }
    )


if __name__ == "__main__":
    main()
