# PLAN – Vertical slice (GDD kap. 18)

> Plán první hratelné verze. Před prací si přečti **GDD.md**, **STYLE.md** a **AGENTS.md**.
> Milníky jdou po sobě. Každý dílčí krok má vlastní **Ověření**. Dokud neprojde, na další krok se nepokračuje.

**Cíl slice:** ověřit, že jádro hry je zábavné. Éra 1972–1985, jen garáž, hráč sám, celý cyklus malé hry (koncept → fáze → QA → vydání → post-mortem), hodnocení a prodeje podle kap. 6.4, odhalování vah, peníze, fanoušci, bankrot, placeholder grafika.

**Technologie:** Unity 6000.6, URP 2D, C#, Unity Test Framework (EditMode i PlayMode testy), UI Toolkit, Unity Localization (CZ/EN).

---

## Pravidla pro všechny milníky
- Každý milník má vlastní větev `feature/mX-...`. Commity jsou malé a po každé změně se kontroluje konzole Unity. Na konci milníku udělá Codex review, pak následuje merge do `main`.
- Herní logika je v čistém C# (`Game.Core`, bez MonoBehaviour), vrstvy Unity jsou zvlášť. Logika jde testovat EditMode testy bez scény.
- Žádné texty pro hráče v kódu. Logika vrací klíče a čísla, texty doplňuje až UI přes Localization.
- Simulace je deterministická. Veškerá náhoda jde jen přes `IRandom` se seedem.
- Herní data (žánry, témata, platformy, balanc) jsou v ScriptableObjects, ne natvrdo v kódu.
- **Milník je hotový**, když všechny testy prochází, konzole je bez chyb a varování z našeho kódu a jsou splněna všechna Ověření.

## Struktura projektu
```
Assets/Game/Core/        Game.Core.asmdef    – simulace (čas, peníze, vývoj, hodnocení, prodeje)
Assets/Game/Data/        Game.Data.asmdef    – ScriptableObject definice + převod do Core
Assets/Game/Content/     .asset soubory žánrů, témat, platforem, balancu
Assets/Game/Runtime/     Game.Runtime.asmdef – MonoBehaviour vrstva (GameRunner)
Assets/Game/UI/          Game.UI.asmdef      – UI Toolkit (UXML, USS, controllery)
Assets/Game/World/       izometrická garáž
Assets/Tests/EditMode/   Game.Tests.EditMode.asmdef
Assets/Tests/PlayMode/   Game.Tests.PlayMode.asmdef
Assets/Art/              grafika podle STYLE.md (zapisuje jen Codex přes Tools/ArtPipeline)
```

---

## M1 – Datová kostra, čas a peníze ✅ HOTOVO

> Dokončeno 2026-10-17 na větvi `feature/m1-data-core`, prošlo review (Codex). 62 EditMode testů prochází, konzole Unity je čistá.

**1.1 Projekt a testy**
Asmdefy podle struktury, jeden prázdný EditMode test.
*Ověření:* Test Runner je zelený, konzole je čistá.

**1.2 Datové definice**
ScriptableObjects `GenreDefinition`, `ThemeDefinition`, `PlatformDefinition` a `BalanceConfig`. Každý má id, lokalizační klíč a parametry:
- **Žánr:** rok vzniku, váhy 10 oblastí vývoje (kap. 6.2), shoda s publikem (Děti / Všichni / Dospělí).
- **Téma:** shoda s každým žánrem (výborná / dobrá / špatná).
- **Platforma:** rok vydání, konec podpory, licenční poplatek, cena dev kitu, křivka instalované základny po letech, technický strop, oblíbenost žánrů.
- `GameDatabase` a validátor. Hlídá unikátní id, váhy v povoleném rozsahu a to, že každé téma má shodu se všemi žánry.

*Ověření:* EditMode testy validátoru nad testovacími i skutečnými daty.

**1.3 Obsah**
- **Žánry:** celý katalog (GDD 6.1). Rok vzniku každého žánru je potřeba ověřit podle skutečné historie: Arkáda, Akce, Puzzle, Sport, Adventura, Závodní, Střílečka (shoot 'em up), RPG, Simulace, Strategie, Plošinovka, Roguelike, Bojovka, FPS, Survival horor, Rytmická/hudební, Sandbox/survival, MOBA, Battle royale, Karetní/deckbuilder. Ve slice jsou hratelné žánry s rokem ≤ 1985.
- **Témata (6):** Vesmír, Fantasy, Sport, Válka, Detektivka, Piráti.
- **Platformy (4):** Magnafox Odyssé, Astari VCS, Pear II, Komodor 64. Data je potřeba ověřit podle skutečné historie (GDD kap. 5).

*Ověření:* validátor projde. Test „v roce 1972 jsou dostupné jen žánry a platformy s rokem ≤ 1972“ projde a stejně tak test pro rok 1985.

> **Stav po M1 (rozhodnutí a odhady k revizi):**
> - Instalovaná základna je seznam bodů (rok → miliony prodaných kusů kumulativně), ne `AnimationCurve`. Lépe se čte v review.
> - Roční prodeje platforem jsou **odhad**. Zdroje se rozcházejí, ověřené jsou jen celkové počty: Odyssey ~350 tis., VCS ~30 mil., Apple II 5–6 mil., C64 12,5–17 mil.
> - Magnafox Odyssé je dostupná do roku 1977 jako celá řada Odyssey (originál skončil 1975). Jinak by v roce 1976 nebyla žádná platforma.
> - Adventura má rok vzniku 1976 (Colossal Cave Adventure), ne 1972. GDD 6.1 ji uvádí mezi startovními žánry, rozhodnout, co platí.
> - Licenční poplatky, ceny dev kitů, technický strop, váhy žánrů a shody témat jsou herní návrh, ne historická data. Doladí se v M2.6.

**1.4 Čas**
`GameClock` počítá týdny od 1. týdne roku 1972 a vysílá události nového týdne, měsíce a roku. Pauzu a 3 rychlosti řeší až runner v Unity, Core má jen `AdvanceWeek()`.
*Ověření:* testy přechodu měsíce a roku a test, že po 52 týdnech nastane rok 1973.

**1.5 Peníze a stav studia**
`StudioState` drží peníze a fanoušky. Měsíční náklady tvoří nájem garáže. Bankrot nastane, když je zůstatek v mínusu déle než X měsíců (X je v `BalanceConfig`).
*Ověření:* testy strhávání nákladů, bankrotu a záchrany těsně před bankrotem.

**1.6 Save/load**
Stav hry se serializuje do JSON a zpět, včetně stavu `SeededRandom`.
*Ověření:* test, že uložený a znovu načtený stav je stejný. Test, že po načtení dá stejný seed stejné výsledky jako hra, která běžela bez ukládání.

**1.7 Runner**
`GameRunner` (MonoBehaviour ve scéně) posouvá čas podle zvolené rychlosti a vypisuje do konzole datum a peníze.
*Ověření:* v Play mode běží v konzoli týdny a peníze ubývají o nájem.

---

## M2 – Logika vývoje hry a hodnocení (bez grafiky) 🔍 ČEKÁ NA REVIEW

> Implementováno 2026-10-17 na větvi `feature/m2-dev-logic`. 115 EditMode testů prochází, konzole Unity je čistá. Čeká na review (Codex).
>
> **Rozhodnutí a odhady k revizi:**
> - Délka malé hry: pre-produkce 3 + produkce 5 + dokončení 3 týdny + QA 0–5 týdnů = 11–16 týdnů. Validátor hlídá rozsah 8–16.
> - Skóre oblasti = body^0,5 (klesající výnos). Díky tomu se vyplatí rozdělit body podle vah žánru, nejlepší je podíl úměrný váze². Bez toho by bylo optimální dát vše do jedné oblasti.
> - Technologie = engine (vlastní 1,0 / licencovaný 1,2 za 1500) × (1 + 2 × technický strop platformy). Dovednost hráče roste o 0,1 za vydanou hru (max 3).
> - Recenze = 1 + 9 × (1 − e^(−kvalita/očekávání)) + šum. Šum je ořezaný na ±0,5 v kódu, i kdyby balanc chtěl víc. Očekávání trhu roste o 4 % ročně.
> - Prodeje: faktor fanoušků má strop (max ×2), jinak se prodeje a fanoušci navzájem nafukovali do miliard. Týden prodá nejvýš 2 % instalované základny.
> - Trendy (GDD 9) jsou nad rámec původního plánu slice (kap. 18 je měla mimo rozsah), přidány na přání. Historická křivka v datech žánru/tématu × generovaný výkyv ±10 % za rok podle vlastního seedu (nespotřebovává RNG simulace). Zprávy z trhu ohlašují změnu o ≥ 15 % rok dopředu.
> - Post-mortem po každé hře odhalí kombinaci žánr + téma, nejdůležitější a nejméně důležitou dosud skrytou váhu žánru (●●● / ●○○). Všech 10 vah je známých po 5 hrách stejného žánru.
> - Dev kit se platí jednou za platformu, licenční poplatek platformy při každém vydání.
> - Save má verzi 2, saves z M1 se nenačtou.
> - Odhad recenze s rozptylem (kap. 7) zatím není, je mimo rozsah slice.
> - Kalibrace (10 let, 5 seedů): bot podle vah má průměrnou recenzi ~8,3 a ~40 mil. peněz, náhodný bot ~6,7 a 4–7 mil. Ekonomika je štědrá, žádný bot nezbankrotuje. Doladit při playtestu v M3.
> - Debug příkaz: menu **Game → Debug → Simulate One Game** (bot podle vah / náhodný bot / náhodný seed) vypíše do konzole koncept, rozdělení bodů, rozpad recenze, post-mortem a prodeje po týdnech.

**2.1 Koncept**
`GameConcept` obsahuje název, žánr, téma, platformu, velikost (ve slice jen Malá), publikum a engine. Validace hlídá, že žánr a platforma jsou v daném roce dostupné, a stahuje cenu dev kitu.
*Ověření:* testy validního a nevalidního konceptu.

**2.2 Fáze vývoje**
`DevelopmentProject` má 4 fáze. V každé hráč rozděluje body mezi oblasti (součet 100 %) a fáze postupuje po týdnech. Výsledek oblasti = čas × dovednost hráče × technologie. Bugy během vývoje přibývají, týdny QA je snižují.
*Ověření:* testy součtu 100 %, délky fází (malá hra trvá 8–16 týdnů) a snižování bugů.

**2.3 Kvalita a recenze**
Vzorce z kap. 6.4:
- **Kvalita** = Σ(výsledek oblasti × váha žánru) × shoda téma/žánr × shoda publikum/žánr × postih za bugy.
- **Recenze (1–10)** se počítá relativně k očekávání trhu v daném roce, šum je nejvýš ±0,5.

Každý faktor se uloží do `ReviewBreakdown`, aby ho post-mortem uměl vysvětlit.

*Ověření:* testy, že:
- rozdělení bodů podle vah žánru dá lepší recenzi než rozdělení opačné,
- stejný seed dá stejnou recenzi,
- šum nikdy nepřekročí ±0,5,
- stejná hra vydaná o 5 let později dostane horší recenzi.

**2.4 Prodeje**
Týdenní prodeje = recenze × instalovaná základna × trend žánru × hype × fanoušci × cena, s postupným útlumem. Ve slice jsou trend a hype konstanty z `BalanceConfig`. Příjem jde do peněz a úspěšná hra přinese fanoušky.
*Ověření:* testy, že lepší recenze dá vyšší prodeje a větší základna také. Test, že prodeje časem klesají až k nule.

**2.5 Post-mortem a znalosti**
`PostMortemReport` se staví z `ReviewBreakdown` a obsahuje jen klíče textů a parametry, žádné hotové věty. `MarketKnowledge` postupně odhaluje váhy žánrů (●○○ → ●●○ → ●●●) a hodnocení kombinací žánr + téma. Znalosti se ukládají do save.
*Ověření:* testy, že po vydání se odhalí správná váha a kombinace a že znalosti přežijí uložení a načtení.

**2.6 Simulační test**
EditMode test odehraje 10 let s jednoduchým botem.
*Ověření:* nevyhodí výjimku, se stejným seedem dá vždy stejný výsledek a bot, který hraje podle vah, vydělá víc než bot náhodný. Test zároveň slouží k prvnímu vyvážení čísel.

---

## M3 – UI a lokalizace (UI Toolkit + Unity Localization CZ/EN)

**3.1 Lokalizace**
Balíček `com.unity.localization`, jazyky cs a en, String Tables `UI`, `Content` (žánry, témata, platformy) a `PostMortem`. Jazyk jde přepnout za běhu.
*Ověření:* přepnutí jazyka v Play mode změní všechny texty. Editor test, že každý lokalizační klíč z dat existuje v obou jazycích.

**3.2 HUD**
Datum, peníze, fanoušci, pauza, 3 rychlosti, tlačítka Nová hra, Uložit a Načíst a přepínač jazyka CZ/EN.
*Ověření:* ruční test v Play mode, hodnoty sedí s logikou.

**3.3 Dialog nové hry**
Výběr názvu, žánru, tématu, platformy, publika a enginu. Nabízí jen možnosti dostupné v aktuálním roce.
*Ověření:* ruční test a PlayMode test, že dialog vytvoří platný `GameConcept`.

**3.4 Panel vývoje**
Aktuální fáze, posuvníky se součtem 100 %, průběh vývoje a volba počtu týdnů QA.
*Ověření:* posuvníky vždy drží součet 100 % a vývoj doběhne až do vydání.

**3.5 Výsledky a post-mortem**
Recenze, prodeje v čase, post-mortem s nápovědami, encyklopedie odhalených vah a kombinací a obrazovka bankrotu. UI musí zvládnout delší české texty.
*Ověření:* celý cyklus koncept → vývoj → vydání → post-mortem projde v obou jazycích. Layout vydrží nejdelší text.

**3.6 Ukládání a nastavení**
Napojení save/load z M1.6 na UI. Jeden slot a automatické uložení na začátku každého měsíce, soubory v `Application.persistentDataPath`. Při pokusu načíst save ze starší verze se zobrazí lokalizovaná hláška a hra nespadne. Zvolený jazyk si hra pamatuje mezi spuštěními.
*Ověření:* PlayMode test, že po uložení, restartu a načtení sedí datum, peníze, fanoušci a rozpracovaná hra. Ruční test, že jazyk zůstane i po restartu.

---

## M4 – Placeholder izometrická garáž

**4.1 Import grafiky**
`AssetPostprocessor` pro `Assets/Art/` podle STYLE kap. 8: Sprite, Bilinear, PPU 128, bez mipmap. URP 2D Renderer s Transparency Sort Mode = Custom Axis (0, 1, 0).
*Ověření:* EditMode test, že importovaný PNG dostane správná nastavení.

**4.2 Garáž**
Izometrická Tilemap 2:1 s dlaždicemi 128 × 64. Placeholder podlaha a stěny, stůl s počítačem a židle. Jsou to jednoduché tvary ve velikostech ze STYLE kap. 3 a s pojmenováním podle kap. 9.
*Ověření:* snímek scény přes Unity MCP. Objekty níž na obrazovce překrývají ty výš.

**4.3 Postava**
Placeholder postava u stolu. Během vývoje má stav „pracuje“, jinak „odpočívá“. Kamera míří na garáž a UI je přes scénu.
*Ověření:* v Play mode stav postavy sleduje vývoj hry.

---

## M4b – Grafika garáže (dělá Codex)

> Grafiku dělá **Codex** podle STYLE.md a AGENTS.md. Assety generuje, zpracuje přes `Tools/ArtPipeline` a ukládá jen do `Assets/Art/`. Claude je pak jen napojí do scény z M4.

**4b.1 Assety garáže**
Dlaždice podlahy a stěn, stůl s počítačem, židle a další vybavení garáže v éře 70s. Velikosti podle STYLE kap. 3, barvy podle kap. 4, pojmenování a složky podle kap. 9.
*Ověření:* všechny soubory prošly `Tools/ArtPipeline`, mají správné rozměry a názvy a splňují právní pravidla STYLE kap. 6.

**4b.2 Postava**
Postava vývojáře ve 4 směrech (SV, SZ, JV, JZ) podle STYLE kap. 5.
*Ověření:* všechny 4 směry jsou v `Assets/Art/Characters/` a mají jednotný styl.

**4b.3 Napojení do scény (Claude)**
Claude nahradí placeholdery z M4 assety z `Assets/Art/`. Do `Assets/Art/` nic nezapisuje.
*Ověření:* snímek scény přes Unity MCP, objekty se správně překrývají a konzole je čistá.

---

## Mimo rozsah slice
Odhad hodnocení s rozptylem (kap. 7), konkurence (kap. 12), náhodné události (kap. 13), zaměstnanci, kanceláře, výzkum, další éry a finální grafika. Přidávají se až po ověření, že slice baví.
