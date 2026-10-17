# STYLE – vizuální styl a pravidla pro grafiku

> Platí pro všechnu grafiku ve hře (generovanou i ruční). Agenti si to čtou před prací s assety.

---

## 1. Celkový dojem
**Kreslená, líbivá, polorealistická ilustrace.** Proporce a předměty věrné realitě (skutečné stoly, počítače, kabely, kávovary), ale zpracování jako malovaná ilustrace: měkké stínování, čisté tvary, teplá atmosféra. Ne pixel art, ne fotorealismus, ne „chibi“ karikatura.

Klíčová slova pro generování: *hand-painted 2D illustration, semi-realistic, clean shapes, soft shading, cozy, warm lighting, isometric*

---

## 2. Perspektiva
- **Izometrie 2:1** (dimetrická, jako většina 2D izometrických her).
- Základní dlaždice podlahy: **128 × 64 px**.
- Světlo vždy **zleva shora**, stíny padají doprava dolů.
- Všechny objekty kreslené ze stejného úhlu, žádné míchání perspektiv.

---

## 3. Velikosti assetů

| Typ | Velikost | Pozn. |
|---|---|---|
| Dlaždice podlahy | 128 × 64 px | průhledné pozadí |
| Nábytek malý (židle, rostlina) | do 128 × 128 px | |
| Nábytek velký (stůl s PC) | do 256 × 192 px | |
| Postava | výška cca 96–128 px | 4 směry (SV, SZ, JV, JZ) |
| Ikony UI | 64 × 64 px | čitelné i ve 32 px |
| Konzole / platformy | 256 × 256 px | vlastní parodický design |

Vždy **PNG s průhledným pozadím**. Pivot objektů = střed spodní hrany (místo, kde objekt stojí na podlaze).

---

## 4. Barvy podle éry
Paleta se mění s dobou, hráč tak pozná posun času i podle vzhledu studia:

| Éra | Paleta | Prostředí |
|---|---|---|
| 1972–1982 | hnědá, oranžová, hořčicová, olivová | dřevo, koberce, béžové počítače |
| 1983–1989 | béžová, šedá + neonové akcenty | plakáty, CRT monitory |
| 1990–1999 | šedá, tmavě modrá, tyrkysová | velké CRT, sklo, kancelářské boxy |
| 2000–2012 | bílá, stříbrná, sytě modrá | ploché monitory, open space |
| 2013+ | světlé dřevo, zelené rostliny, pastely | moderní startup kanceláře |

UI zůstává napříč érami jednotné a čisté (neutrální tmavé panely, jedna akcentní barva).

---

## 5. Postavy
- Realistické proporce, mírně zjednodušené rysy, přátelské výrazy.
- Oblečení odpovídá éře (70. léta: svetry, kalhoty do zvonu; 90.: flanel, mikiny…).
- Animace: sezení u PC, psaní, chůze, přestávka. Stačí málo snímků, důležitá je čitelnost.

---

## 6. Právní pravidla (povinné)
- **Žádná skutečná loga, názvy značek ani věrné kopie designu** konzolí, počítačů nebo produktů.
- Konzole jen jako **parodie**: rozpoznatelný „duch“ doby, ale vlastní tvar a název (viz GDD kap. 5).
- Žádné postavy z existujících her.

---

## 7. Šablona promptu pro generování

```
Isometric 2:1 [OBJEKT], hand-painted 2D illustration, semi-realistic,
clean shapes, soft shading, light from top-left, [ÉRA] era style,
[BARVY ÉRY] color palette, transparent background, no text, no logos,
game asset, centered
```

Příklad:
```
Isometric 2:1 wooden desk with a beige 1970s home computer and CRT monitor,
hand-painted 2D illustration, semi-realistic, clean shapes, soft shading,
light from top-left, 1970s era style, brown orange mustard color palette,
transparent background, no text, no logos, game asset, centered
```

Každý nový asset porovnej s již schválenými. Pokud se stylem liší, vygeneruj znovu.

---

## 8. Nastavení v Unity
- Texture Type: **Sprite (2D and UI)**, Sprite Mode: Single (u spritesheetů Multiple)
- Filter Mode: **Bilinear** (není to pixel art)
- Pixels Per Unit: **128** (1 dlaždice = 1 jednotka na šířku)
- Compression: Normal Quality, Generate Mip Maps: vypnuto
- Řazení izometrie: v URP 2D Rendereru nastav **Transparency Sort Mode = Custom Axis, osa (0, 1, 0)**, aby objekty níž na obrazovce překrývaly ty výš.

Tyto hodnoty má automaticky nastavovat `AssetPostprocessor` pro složku `Assets/Art/`.

---

## 9. Složky a pojmenování
```
Assets/Art/Tiles/        tile_floor_wood_70s.png
Assets/Art/Furniture/    furn_desk_computer_70s.png
Assets/Art/Characters/   char_dev_male_70s_se.png
Assets/Art/Platforms/    plat_astari_vcs.png
Assets/Art/UI/Icons/     icon_genre_arcade.png
```
Malá písmena, podtržítka, anglicky, bez diakritiky. Éra jako přípona tam, kde má objekt více verzí.
