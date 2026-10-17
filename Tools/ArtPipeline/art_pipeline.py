"""Prepare generated sprites according to STYLE.md (chapters 3 and 9)."""
from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path
import re

import numpy as np
from PIL import Image, ImageOps
from scipy import ndimage


@dataclass(frozen=True)
class Preset:
    folder: str
    prefix: str
    size: tuple[int, int]
    fixed_canvas: bool = False


PRESETS = {
    "tile": Preset("Tiles", "tile", (128, 64), True),
    "furniture-small": Preset("Furniture", "furn", (128, 128)),
    "furniture-large": Preset("Furniture", "furn", (256, 192)),
    "character": Preset("Characters", "char", (128, 128)),
    "icon": Preset("UI/Icons", "icon", (64, 64), True),
    "platform": Preset("Platforms", "plat", (256, 256), True),
}
PROJECT_ROOT = Path(__file__).resolve().parents[2]


def slug(value: str) -> str:
    if not re.fullmatch(r"[a-z0-9]+(?:_[a-z0-9]+)*", value):
        raise ValueError("Use lowercase ASCII words separated by underscores.")
    return value


def background_color(image: Image.Image, value: str) -> np.ndarray:
    if value != "auto":
        if not re.fullmatch(r"#[0-9a-fA-F]{6}", value):
            raise ValueError("Background must be auto, keep or #RRGGBB.")
        return np.array([int(value[i:i + 2], 16) for i in (1, 3, 5)], dtype=np.float32)
    rgb = np.asarray(image, dtype=np.float32)[..., :3]
    border = np.concatenate((rgb[0], rgb[-1], rgb[1:-1, 0], rgb[1:-1, -1]))
    color = np.median(border, axis=0)
    if np.mean(np.max(np.abs(border - color), axis=1) <= 18) < 0.95:
        raise ValueError("Border is not a uniform background; specify --background '#RRGGBB'.")
    return color


def remove_background(image: Image.Image, background: str = "auto", tolerance: float = 18,
                      edge_width: int = 3, min_component: int = 16) -> Image.Image:
    """Recover straight RGB and alpha using a local foreground estimate.

    C = alpha * F + (1-alpha) * B. A nearest opaque interior pixel estimates F;
    projection estimates alpha, then unmixing removes B from edge RGB. Keying
    includes enclosed gaps, not only background connected to the image border.
    """
    image = image.convert("RGBA")
    source = np.asarray(image, dtype=np.float32)
    if background == "keep" or (background == "auto" and np.any(source[..., 3] == 0)):
        return image
    bg = background_color(image, background)
    rgb = source[..., :3]
    distance = np.max(np.abs(rgb - bg), axis=2)
    candidate = (distance > tolerance) & (source[..., 3] > 0)
    # Generated/JPEG backgrounds can contain isolated off-color specks.
    labels, _ = ndimage.label(candidate, structure=np.ones((3, 3)))
    sizes = np.bincount(labels.ravel())
    keep = sizes >= min_component
    keep[0] = False
    candidate = keep[labels]
    if not candidate.any():
        raise ValueError("No foreground remains; check background color and tolerance.")
    core = ndimage.binary_erosion(candidate, iterations=edge_width, border_value=0)
    if not core.any():
        raise ValueError("Foreground is too thin; reduce --edge-width or use a larger source.")
    nearest = ndimage.distance_transform_edt(~core, return_distances=False, return_indices=True)
    foreground = rgb[nearest[0], nearest[1]]
    direction = foreground - bg
    denominator = np.sum(direction * direction, axis=2)
    alpha = np.clip(np.sum((rgb - bg) * direction, axis=2) / np.maximum(denominator, 1), 0, 1)
    alpha[core] = 1
    alpha[~candidate] = 0
    clean = np.clip((rgb - (1 - alpha[..., None]) * bg) / np.maximum(alpha[..., None], 1 / 255), 0, 255)
    # Existing opacity multiplies coverage; do not make translucent source pixels opaque.
    alpha *= source[..., 3] / 255
    result = np.concatenate((clean, alpha[..., None] * 255), axis=2)
    result[alpha < 1 / 255] = 0
    return Image.fromarray(np.rint(result).astype(np.uint8))


def resize_clean(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Lanczos in premultiplied alpha avoids bringing invisible RGB into edges."""
    data = np.asarray(image, dtype=np.float32) / 255
    alpha = data[..., 3]
    channels = [data[..., i] * alpha for i in range(3)] + [alpha]
    resized = [np.asarray(Image.fromarray(c).resize(size, Image.Resampling.LANCZOS)) for c in channels]
    a = np.clip(resized[3], 0, 1)
    rgb = np.stack(resized[:3], axis=2) / np.maximum(a[..., None], 1 / 255)
    out = np.concatenate((np.clip(rgb, 0, 1), a[..., None]), axis=2)
    out[a < 1 / 255] = 0
    return Image.fromarray(np.rint(out * 255).astype(np.uint8))


def prepare(image: Image.Image, kind: str, background: str = "auto", tolerance: float = 18,
            edge_width: int = 3, min_component: int = 16) -> Image.Image:
    if not 0 <= tolerance < 255 or not 1 <= edge_width <= 32 or min_component < 1:
        raise ValueError("Tolerance must be 0..<255; edge width 1..32; min component >=1.")
    preset = PRESETS[kind]
    cutout = remove_background(ImageOps.exif_transpose(image), background, tolerance, edge_width, min_component)
    box = cutout.getchannel("A").getbbox()
    if box is None:
        raise ValueError("Image has no visible foreground.")
    cutout = cutout.crop(box)
    scale = min(1, preset.size[0] / cutout.width, preset.size[1] / cutout.height)
    size = (max(1, round(cutout.width * scale)), max(1, round(cutout.height * scale)))
    result = resize_clean(cutout, size)
    if preset.fixed_canvas:
        canvas = Image.new("RGBA", preset.size)
        canvas.alpha_composite(result, ((canvas.width - result.width) // 2, (canvas.height - result.height) // 2))
        return canvas
    return result


def destination(kind: str, name: str, era: str | None, direction: str | None,
                root: Path = PROJECT_ROOT) -> Path:
    slug(name)
    if era:
        slug(era)
    if direction not in (None, "ne", "nw", "se", "sw"):
        raise ValueError("Direction must be ne, nw, se or sw.")
    if (kind == "character") != (direction is not None):
        raise ValueError("Use --direction only for characters; characters require it.")
    preset = PRESETS[kind]
    parts = [preset.prefix, name] + ([era] if era else []) + ([direction] if direction else [])
    return root / "Assets" / "Art" / preset.folder / ("_".join(parts) + ".png")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("--kind", choices=PRESETS, required=True)
    parser.add_argument("--name", type=slug, required=True, help="Subject without prefix, era or extension.")
    parser.add_argument("--era", type=slug, help="For example 70s.")
    parser.add_argument("--direction", choices=("ne", "nw", "se", "sw"))
    parser.add_argument("--background", default="auto", help="auto, keep, or '#RRGGBB'.")
    parser.add_argument("--tolerance", type=float, default=18)
    parser.add_argument("--edge-width", type=int, default=3, help="Source-pixel band for edge color recovery.")
    parser.add_argument("--min-component", type=int, default=16, help="Remove disconnected specks smaller than this many source pixels; 1 preserves all.")
    parser.add_argument("--project-root", type=Path, default=PROJECT_ROOT)
    args = parser.parse_args(argv)
    try:
        target = destination(args.kind, args.name, args.era, args.direction, args.project_root)
        if target.resolve() == args.source.resolve():
            raise ValueError("Source and destination must differ.")
        if target.exists():
            raise ValueError(f"Destination already exists: {target}. Choose another name.")
        with Image.open(args.source) as source:
            result = prepare(source, args.kind, args.background, args.tolerance, args.edge_width, args.min_component)
        target.parent.mkdir(parents=True, exist_ok=True)
        # Exclusive creation also prevents accidental overwrites by concurrent invocations.
        with target.open("xb") as output:
            result.save(output, format="PNG")
    except (ValueError, OSError) as error:
        parser.exit(2, f"Error: {error}\n")
    print(f"{target} ({result.width}x{result.height}, RGBA)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
