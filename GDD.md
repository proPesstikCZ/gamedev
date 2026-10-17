# GDD – Simulátor herního studia (pracovní název: **Garage to Glory**)

> Tento dokument je zdroj pravdy pro všechny agenty (Claude, Codex). Před každou úlohou si ho přečti.
> Kód, názvy tříd a proměnných piš anglicky. Texty pro hráče nikdy nepiš natvrdo do kódu (viz Lokalizace).

---

## 1. Pitch

Hráč začíná v roce **1972** sám v garáži a vyvíjí první jednoduché hry. Během desetiletí skutečné historie herního průmyslu (s parodovanými názvy firem a konzolí) buduje studio až po nadnárodní korporaci s AAA tituly, MMO, vlastním R&D na konzole a technologickou divizí.

Inspirace: **Game Dev Tycoon**. Rozdíl: **méně náhody, víc čitelné logiky**. Hráč má vždy možnost pochopit, *proč* hra uspěla nebo propadla, a příště to udělat líp.

---

## 2. Pilíře hry

1. **Čitelnost místo náhody** – výsledek hry je z 90 % dán rozhodnutími hráče. Náhoda je malá a vždy viditelná.
2. **Učení se trhu** – hráč postupně odhaluje, co který žánr potřebuje (arkáda = hratelnost, RPG = příběh a svět…). Znalost je hlavní progres.
3. **Skutečná historie** – platformy vycházejí ve skutečných letech a jejich úspěch kopíruje realitu (parodované názvy).
4. **Růst** – garáž → kancelář → korporace; malá hra → AAA → MMO; jeden člověk → stovky zaměstnanců a divize.
5. **Řízení kapacit** – tým má omezený čas a dovednosti; dobré rozdělení práce podle žánru je jádro hry.

---

## 3. Hlavní herní smyčka

```
Průzkum trhu → Koncept hry → Vývoj (fáze) → Testování → Vydání → Prodeje + recenze → Post-mortem → (znalosti, peníze, fanoušci) → další hra / rozšíření studia / výzkum
```

Čas běží v **týdnech** (pauza a 3 rychlosti). Jedna malá hra v garáži trvá cca 8–16 týdnů.

---

## 4. Éry a čas

| Éra | Roky | Charakter |
|---|---|---|
| Úsvit | 1972–1982 | arkády, první konzole a domácí počítače, malé hry |
| Krach a obnova | 1983–1989 | krach trhu 1983, nástup 8bit konzolí |
| 16bit a CD | 1990–1999 | 16bit válka, 3D, CD-ROM, první online hry |
| Online a HD | 2000–2012 | MMO, HD konzole, digitální distribuce, mobily od 2007 |
| Moderní | 2013–dnes | live service, indie boom, free-to-play |
| Budoucnost | dnes+ | volná hra, fiktivní platformy |

Hra nemá pevný konec. Po roce ~2030 pokračuje jako sandbox s fiktivními platformami.

---

## 5. Platformy (parodie skutečných)

Data v tabulce jsou **rok prvního vydání na západním trhu**. Podrobná data (podíl na trhu v čase, prodaná základna, konec podpory, oblíbenost žánrů) půjdou do datového souboru `Platforms` (ScriptableObject/JSON) a **musí se ověřit podle skutečné historie**, než se použijí.

| Parodie | Předloha | Rok |
|---|---|---|
| Magnafox Odyssé | Magnavox Odyssey | 1972 |
| Astari VCS | Atari 2600 | 1977 |
| Pear II | Apple II | 1977 |
| Intelivize | Intellivision | 1979 |
| IBN PC | IBM PC | 1981 |
| Komodor 64 | Commodore 64 | 1982 |
| ZX Spektrum | ZX Spectrum | 1982 |
| KolekoVision | ColecoVision | 1982 |
| Nintendu FES | NES | 1985 |
| Amigo | Amiga | 1985 |
| Saga Master System | Sega Master System | 1986 |
| Nintendu Pocket Boy | Game Boy | 1989 |
| Saga MegaDrive | Sega Genesis/Mega Drive | 1989 |
| Nintendu Super FES | SNES | 1991 |
| PC (Okna 95) | PC s Windows 95 | 1995 |
| Sonya PlayStage | PlayStation | 1995 |
| Saga Saturnus | Sega Saturn | 1995 |
| Nintendu 64 | Nintendo 64 | 1996 |
| Saga Dreamcaster | Dreamcast | 1999 |
| Sonya PlayStage 2 | PlayStation 2 | 2000 |
| MicroWorks Ybox | Xbox | 2001 |
| Nintendu CubeGame | GameCube | 2001 |
| MicroWorks Ybox 360 | Xbox 360 | 2005 |
| Nintendu Wiz | Wii | 2006 |
| Sonya PlayStage 3 | PlayStation 3 | 2006 |
| Pear jPhone (mobil) | iPhone | 2007 |
| Nintendu Wiz U | Wii U | 2012 |
| Sonya PlayStage 4 | PlayStation 4 | 2013 |
| MicroWorks Ybox One | Xbox One | 2013 |
| Nintendu Swap | Switch | 2017 |
| Sonya PlayStage 5 | PlayStation 5 | 2020 |
| MicroWorks Ybox Series | Xbox Series X/S | 2020 |
| Nintendu Swap 2 | Switch 2 | 2025 |

Každá platforma má: licenční poplatek, náklady na dev kit, instalovanou základnu v čase (křivka), technický strop (max. kvalita grafiky/zvuku), oblíbenost žánrů u publika.

Události trhu: **krach 1983** (prodeje konzolí prudce klesnou na ~2 roky), nástup CD, internetu, mobilů, digitální distribuce.

**Nikdy nepoužívej skutečné názvy, loga ani tvary designu konzolí.**

---

## 6. Vývoj hry

### 6.1 Koncept
Hráč zvolí:
- **Název**
- **Žánr** (na začátku: Arkáda, Akce, Puzzle, Sport, Adventura; další odemyká výzkum)
- **Téma** (vesmír, středověk, sport, zombie… odemykají se postupně)
- **Platformu**
- **Velikost** (Malá → Střední → Velká → AAA → MMO, viz 6.5)
- **Cílové publikum** (Děti / Všichni / Dospělí)
- **Engine** (vlastní nebo licencovaný)

### 6.2 Fáze vývoje
Každá fáze má **oblasti**, mezi které hráč rozděluje body pozornosti (posuvníky, součet 100 %):

| Fáze | Oblasti |
|---|---|
| 1. Pre-produkce | Design hratelnosti, Příběh a svět, Technologie/engine |
| 2. Produkce | Hratelnost, Grafika, Zvuk, Level design |
| 3. Dokončení | AI a systémy, Dialogy a texty, Vyladění (polish) |
| 4. Testování (QA) | počet týdnů testování → snižuje bugy |

Ve větším studiu se zaměstnanci **přiřazují k oblastem**. Výsledek oblasti = čas × dovednost přiřazených lidí × technologie.

### 6.3 Žánrové váhy (jádro „méně náhody“)
Každý žánr má **skryté váhy oblastí**, např.:

| Žánr | Hratelnost | Grafika | Příběh | Zvuk | Level design | AI |
|---|---|---|---|---|---|---|
| Arkáda | **vysoká** | střední | nízká | střední | střední | nízká |
| RPG | střední | střední | **vysoká** | střední | střední | střední |
| Strategie | vysoká | nízká | nízká | nízká | střední | **vysoká** |
| Adventura | nízká | střední | **vysoká** | střední | vysoká | nízká |

Váhy jsou **zpočátku skryté** a hráč je odhaluje (kap. 8). Konkrétní čísla v datovém souboru `Genres`.

### 6.4 Hodnocení a prodeje
**Kvalita** = Σ (výsledek oblasti × váha žánru) × shoda tématu a žánru × shoda žánru s publikem × postih za bugy

**Recenze (1–10)** = kvalita *relativně* k očekávání trhu v daném roce (očekávání s časem rostou → stará technologie = horší recenze) + malý šum **max ±0,5**.

**Prodeje** = recenze × instalovaná základna platformy × trend žánru × hype × marketing × fanoušci × cena.

Hra **nesmí** generovat výsledky čistě náhodně. Každý faktor musí jít hráči vysvětlit v post-mortem.

### 6.5 Velikosti her

| Velikost | Odemčení | Tým | Délka vývoje | Poznámka |
|---|---|---|---|---|
| Malá | start | 1 | 2–4 měsíce | |
| Střední | výzkum + kancelář | 2–6 | 4–9 měsíců | |
| Velká | výzkum + větší kancelář | 6–20 | 9–18 měsíců | |
| AAA | výzkum + velké studio | 20+ | 1,5–3 roky | vyžaduje marketing a vlastní/silný engine |
| MMO | výzkum + online infrastruktura | 30+ | 2–4 roky | po vydání **live service**: servery, aktualizace, předplatitelé |

### 6.6 Post-mortem
Po každé vydané hře přijde report: co fungovalo, co ne, a **nápověda ke skrytým vahám** („Hráči arkád ocenili hratelnost, příběh téměř nikoho nezajímal.“). Tohle je hlavní nástroj učení.

---

## 7. Odhad úspěchu před vydáním
Testování a **beta** ukážou **odhad hodnocení s rozptylem** (např. 6–8). Čím víc znalostí a lepší QA tým, tím užší rozptyl. Hráč tak může zvážit, jestli hru ještě vylepšit, nebo vydat.

---

## 8. Znalosti trhu
- **Kombinace žánr + téma**: po vydání se ukáže hodnocení kombinace (výborná / dobrá / špatná).
- **Žánrové váhy**: odhalují se postupně přes post-mortem a výzkum „Analýza trhu“ (zobrazení ●○○ → ●●○ → ●●●).
- **Encyklopedie studia**: všechno odhalené je přehledně k dispozici.

---

## 9. Trendy
Každý žánr i téma má **popularitu v čase** (vlny). Trendy kopírují historii (např. boom adventur v 80.–90. letech, FPS v 90. letech, MMO kolem 2004) + menší generované výkyvy.
- Trend se ohlašuje dopředu ve **zprávách z trhu** → hráč může plánovat.
- Výzkum „Tržní analýza“ ukazuje graf popularity.

---

## 10. Studio a růst

### 10.1 Lokace
| Lokace | Kapacita | Odemyká |
|---|---|---|
| Garáž | 1 (hráč) | malé hry |
| Malá kancelář | 4 | nábor, střední hry |
| Kancelář | 12 | velké hry, trénink |
| Patro / budova | 40 | AAA, divize |
| Kampus | 100+ | MMO, R&D, více týmů současně |

### 10.2 Zaměstnanci
Atributy: **Design, Technologie, Grafika, Zvuk, Rychlost, Výdrž**, + specializace (programátor, grafik, designér, zvukař, scenárista, tester, producent).
- **Nábor**: inzerát (cena, čas), kandidáti s viditelnými statistikami a platem.
- **Trénink**: stojí peníze a čas, zvyšuje atributy.
- **Únava**: přetěžování snižuje výkon → dovolená.
- Plat roste s úrovní, nespokojený zaměstnanec může odejít.

### 10.3 Divize (pozdní hra)
- **Technologická divize**: vyvíjí enginy a technologie (3D, fyzika, online, AI). Lze je použít ve vlastních hrách nebo **licencovat** jiným studiím.
- **R&D hardware**: vývoj **vlastní konzole** (návrh parametrů, cena, výrobní náklady, launch titul). Technologie z tech divize lze do konzole implementovat.

---

## 11. Výzkum
Strom výzkumu ve větvích:
- **Žánry a témata**
- **Velikosti her**
- **Technologie enginu** (2D grafika → 3D → stínování → fyzika → online…), závislé na éře
- **Produkce** (QA procesy, marketing, analýza trhu)
- **Organizace** (lepší nábor, trénink, management)
- **Divize** (tech divize, R&D hardware, online infrastruktura)

Výzkum stojí **body výzkumu** (získávají se vývojem her) + peníze. Některé technologie se odemknou až po skutečném roce (3D ne dřív než cca 1992).

---

## 12. Konkurence
AI studia (fiktivní) vydávají hry, zabírají trendy a mají vlastní reputaci. Velká konkurence ve stejném žánru a měsíci snižuje prodeje. Zprávy oznamují jejich připravované hry.

---

## 13. Náhodné události
Události jsou **řídké a vždy s volbou**, nikdy jen „ztratil jsi peníze“:
- Nabídka zakázky od vydavatele (jistý příjem vs. čas)
- Zaměstnanec dostal nabídku od konkurence (přidat plat / pustit)
- Únik buildu, výpadek serverů, kontroverze
- Herní veletrh (prezentace = hype)

Události se ohlašují předem, kde dává smysl (např. veletrh).

---

## 14. Ekonomika a stav studia
- **Peníze** (měsíční náklady: nájem, platy, licence, servery)
- **Fanoušci** (zvyšují prodeje)
- **Reputace** (ovlivňuje nábor a zájem vydavatelů)
- **Body výzkumu**

**Prohra**: bankrot (záporný zůstatek déle než X měsíců).

---

## 15. Obrazovky / UI
- Hlavní pohled: izometrická scéna studia (viz STYLE.md)
- Dialog nové hry (koncept)
- Panel vývoje (fáze, posuvníky, přiřazení lidí)
- Výsledky vydání + post-mortem
- Trh (platformy, trendy, konkurence)
- Zaměstnanci (nábor, trénink)
- Výzkum (strom)
- Finance (grafy)
- Encyklopedie znalostí

---

## 16. Lokalizace
- Jazyky: **čeština a angličtina**, přepínatelné v nastavení i za běhu.
- Použij balíček **Unity Localization** (String Tables). Žádný text pro hráče natvrdo v kódu.
- Klíče textů anglicky, např. `ui.newgame.title`, `genre.arcade.name`.
- Počítej s tím, že čeština bývá delší → UI musí zvládnout delší texty.

---

## 17. Technické zásady
- Unity 6.3+, URP 2D, C#.
- **Data-driven**: žánry, témata, platformy, výzkum, události jako ScriptableObjects (nebo JSON), ne natvrdo v kódu.
- Herní logika (simulace) oddělená od UI → jde testovat bez scény (Unity Test Framework, EditMode testy).
- Save/load od začátku (JSON).
- Simulace deterministická při stejném seedu (kvůli testování a vyvažování).

---

## 18. První verze (vertical slice)
Cíl: ověřit, že jádro je zábavné. **Jen tohle:**
- Éra 1972–1985, pouze **garáž**, hráč sám
- 4 žánry, 6 témat, 4 platformy
- Celý vývojový cyklus malé hry (koncept → fáze → QA → vydání → post-mortem)
- Hodnocení a prodeje podle kap. 6.4, odhalování vah
- Peníze, fanoušci, bankrot
- Placeholder grafika

Až tohle baví, přidávají se kanceláře, zaměstnanci, výzkum a další éry.

---

## 19. Otevřené otázky
- Finální název hry
- Délka celé kampaně v hodinách
- Fiktivní platformy po roce ~2030
- Možnost vydat hru přes vydavatele vs. self-publishing
