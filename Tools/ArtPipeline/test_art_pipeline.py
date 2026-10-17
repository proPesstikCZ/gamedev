"""Run with python -m unittest discover -s Tools/ArtPipeline -v."""
import tempfile
import unittest
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from art_pipeline import PRESETS, destination, main, prepare, remove_background


class PipelineTests(unittest.TestCase):
    def source(self):
        mask = Image.new("L", (256, 256))
        draw = ImageDraw.Draw(mask)
        draw.ellipse((40, 24, 216, 232), fill=255)
        draw.ellipse((100, 100, 156, 156), fill=0)
        # Antialiased synthetic ground truth with enclosed background gap.
        mask = mask.resize((128, 128), Image.Resampling.LANCZOS)
        alpha = np.asarray(mask, dtype=np.float32) / 255
        fg = np.array([100, 140, 30])
        bg = np.array([255, 0, 255])
        rgb = alpha[..., None] * fg + (1 - alpha[..., None]) * bg
        return Image.fromarray(np.rint(rgb).astype(np.uint8)), alpha

    def test_recovers_antialiased_edges_without_magenta(self):
        source, truth = self.source()
        result = np.asarray(remove_background(source))
        edge = (truth > 0.2) & (truth < 0.9)
        self.assertGreater(edge.sum(), 10)
        self.assertLess(np.max(np.abs(result[..., 3][edge] / 255 - truth[edge])), 0.03)
        self.assertLess(np.max(np.abs(result[..., :3][edge].astype(float) - [100, 140, 30])), 8)
        self.assertEqual(result[64, 64, 3], 0)  # interior hole
        self.assertEqual(result[0, 0, 3], 0)

    def test_preserves_existing_transparency(self):
        source = Image.new("RGBA", (20, 20))
        ImageDraw.Draw(source).rectangle((5, 5, 14, 14), fill=(230, 20, 10, 128))
        self.assertTrue(np.array_equal(np.asarray(source), np.asarray(remove_background(source))))

    def test_all_sizes_and_no_upscaling(self):
        source, _ = self.source()
        for kind, preset in PRESETS.items():
            with self.subTest(kind=kind):
                result = prepare(source, kind)
                self.assertEqual(result.mode, "RGBA")
                self.assertLessEqual(result.width, preset.size[0])
                self.assertLessEqual(result.height, preset.size[1])
                if preset.fixed_canvas:
                    self.assertEqual(result.size, preset.size)
        tiny = Image.new("RGBA", (8, 12), (100, 30, 20, 255))
        self.assertEqual(prepare(tiny, "character", "keep").size, (8, 12))

    def test_names_and_paths(self):
        for kind, preset in PRESETS.items():
            direction = "se" if kind == "character" else None
            path = destination(kind, "test", "70s", direction, Path("project"))
            self.assertEqual(path.parent, Path("project/Assets/Art") / preset.folder)
            self.assertTrue(path.name.startswith(preset.prefix + "_"))
        self.assertEqual(destination("character", "dev_male", "70s", "se").name, "char_dev_male_70s_se.png")
        for name in ("../escape", "Bad Name", "židle", "test.png"):
            with self.assertRaises(ValueError):
                destination("tile", name, None, None)

    def test_cli_writes_png_and_refuses_overwrite(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "source.png"
            self.source()[0].save(source)
            args = [str(source), "--kind", "icon", "--name", "test", "--project-root", directory]
            self.assertEqual(main(args), 0)
            output = root / "Assets/Art/UI/Icons/icon_test.png"
            before = output.read_bytes()
            with Image.open(output) as image:
                self.assertEqual((image.format, image.mode, image.size), ("PNG", "RGBA", (64, 64)))
            with self.assertRaises(SystemExit):
                main(args)
            self.assertEqual(output.read_bytes(), before)

    def test_empty_and_nonuniform_background_fail(self):
        with self.assertRaises(ValueError):
            prepare(Image.new("RGB", (30, 30), "white"), "icon")
        source = Image.new("RGB", (30, 30), "red")
        ImageDraw.Draw(source).rectangle((0, 0, 14, 29), fill="blue")
        with self.assertRaises(ValueError):
            prepare(source, "icon")

    def test_generated_fixture_has_no_colored_fringe(self):
        with Image.open(Path(__file__).parent / "examples/chair_source.png") as source:
            result = prepare(source, "furniture-small")
        self.assertEqual(result.height, 128)
        data = np.asarray(result).astype(float)
        visible = data[..., 3] > 32
        magenta = (data[..., 0] > data[..., 1] + 35) & (data[..., 2] > data[..., 1] + 35)
        self.assertFalse((visible & magenta).any())
        self.assertTrue((data[..., 3] == 0).any())
        self.assertTrue(((data[..., 3] > 0) & (data[..., 3] < 255)).any())


if __name__ == "__main__":
    unittest.main()
