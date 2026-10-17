# ArtPipeline

Python CLI pro přípravu vygenerovaných obrázků podle `STYLE.md`, kap. 3 a 9.
Python 3.11+, Pillow, NumPy a SciPy. Spouští se nezávisle na Unity.

```powershell
python -m venv Tools/ArtPipeline/.venv
Tools/ArtPipeline/.venv/Scripts/python -m pip install -r Tools/ArtPipeline/requirements.txt
Tools/ArtPipeline/.venv/Scripts/python Tools/ArtPipeline/art_pipeline.py source.png --kind furniture-small --name chair --era 70s
```

Výsledek: `Assets/Art/Furniture/furn_chair_70s.png`. Kořen projektu se standardně
odvodí od umístění skriptu, nezávisle na aktuálním adresáři. Lze jej změnit
pomocí `--project-root`. Existující soubory ani zdroj nástroj nepřepisuje.

| `--kind` | Rozměry | Cesta / název |
|---|---|---|
| `tile` | plátno 128 × 64 | `Tiles/tile_floor_wood_70s.png` |
| `furniture-small` | nejvýše 128 × 128 | `Furniture/furn_chair_70s.png` |
| `furniture-large` | nejvýše 256 × 192 | `Furniture/furn_desk_computer_70s.png` |
| `character` | nejvýše 128 × 128; při dostatečném zdroji výška 128 | `Characters/char_dev_male_70s_se.png` |
| `icon` | plátno 64 × 64 | `UI/Icons/icon_genre_arcade.png` |
| `platform` | plátno 256 × 256 | `Platforms/plat_astari_vcs.png` |

`--name` je anglický popis bez prefixu, přípony a éry. Povolená jsou malá ASCII
písmena, číslice a podtržítka. `--era 70s` je volitelné pro varianty podle éry.
Postava vyžaduje `--direction ne|nw|se|sw` (SV, SZ, JV, JZ).
Pro všechny čtyři směry spusť příkaz nad odpovídajícími zdroji samostatně.

Obrázek se ořízne podle alfa kanálu a zmenší se zachováním poměru stran.
Malé zdroje se nezvětšují. U široké postavy může limit šířky vést k výšce pod
96 px; zvol zdroj vhodných proporcí. Dlaždice, ikony a platformy se doplní
průhledným okrajem na přesné rozměry; jejich obsah se centruje. Nástroj nemění
perspektivu obrázku ani z něj nedělá izometrickou dlaždici.

## Pozadí a čisté okraje

Zdroj generuj na jednobarevném pozadí, které se nevyskytuje v objektu,
s volným okrajem a bez stínu na podlaze. Nejlépe funguje sytá magenta nebo
zelená. Pozadí se odstraní i v uzavřených mezerách (např. mezi nohami židle).

- `--background auto`: medián barvy obvodu; alespoň 95 % obvodu musí odpovídat
  jednobarevnému pozadí. Pokud zdroj již obsahuje plně průhledné pixely,
  zachová se jeho alfa kanál a barvy.
- `--background '#ff00ff'`: explicitní barva klíče, vhodná při jiném obvodu.
- `--background keep`: pouze ořez a zmenšení existujícího RGBA obrázku.
- `--tolerance 18`: největší odchylka kanálu v rozsahu 0–255 považovaná za pozadí.
- `--edge-width 3`: šířka pásma ve zdrojových pixelech pro obnovu barev hran.
  U širšího barevného lemu ji zvyš; pro tenké detaily ji sniž.
- `--min-component 16`: odstraní oddělené skvrny menší než 16 zdrojových pixelů.
  Pro zachování drobných oddělených detailů použij `--min-component 1`.

Na hraně se odhadne barva nejbližšího vnitřního pixelu objektu a rozloží se
směs `C = alfa × objekt + (1 − alfa) × pozadí`. Z RGB se pak odečte příspěvek
pozadí. Lanczos zmenšení pracuje s přednásobenou alfou, takže do hran
nepřimíchává barvy neviditelných pixelů. Výstup je skutečné PNG RGBA.

Jednobarevný klíč nemůže rozlišit stejnou barvu uvnitř objektu od pozadí.
Průhledné materiály, barevný odraz pozadí hluboko uvnitř objektu a velmi
rozmazané či vícebarevné pozadí vyžadují jiný zdroj nebo ruční masku.
Náhled na světlém i tmavém podkladu je součást kontroly každého nového assetu.

PNG samo neukládá Unity pivot, PPU ani filtr. Po importu nastav Sprite,
Bilinear, PPU 128, bez mipmap a pivot `(0.5, 0)` podle STYLE.md kap. 8.
Automatický Unity `AssetPostprocessor` zůstává úkolem M4; tento nástroj
nemění importní nastavení ani existující `.meta` soubory.

## Ověření

```powershell
python -m unittest discover -s Tools/ArtPipeline -v
python Tools/ArtPipeline/art_pipeline.py Tools/ArtPipeline/examples/chair_source.png --kind furniture-small --name chair_test --era 70s
```

Druhý příkaz je reprodukce ukázky, takže pokud ukázkový výstup již existuje,
použij jiné `--name` nebo dočasný `--project-root`.

Testy ověřují alfa kanál, uzavřenou mezeru, obnovu známých barev a pokrytí
antialiasovaných hran, rozměry všech kategorií, cesty, názvy, zachování
existující průhlednosti, odmítnutí přepsání a chybné vstupy. Zvlášť zpracují
skutečný generovaný zdroj a hledají magenta lem ve viditelných pixelech.

## Generovaná ukázka

- Zdroj: `examples/chair_source.png` (1254 × 1254, vestavěný imagegen).
- Výstup: `Assets/Art/Furniture/furn_chair_test_70s.png`.
- Kontrolní náhled na světlém a tmavém pozadí: `examples/chair_preview.png`.
- Přesný prompt: `examples/prompt.txt`.

Židle je testovací asset pro pipeline, nikoli schválený finální herní asset.
