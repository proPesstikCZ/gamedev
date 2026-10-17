# AGENTS – pravidla pro AI agenty

Platí pro všechny agenty pracující na tomto projektu (Claude, Codex).

## Role
- **Claude** píše C# kód a staví scény v Unity.
- **Codex** dělá review a opravy.
- **Codex** je zodpovědný i za **grafiku**. Generuje assety podle **STYLE.md**, zpracuje je přes `Tools/ArtPipeline` a ukládá je **jen do `Assets/Art/`**.
- **Claude do `Assets/Art/` nezapisuje.** Assety odtud jen používá v kódu a ve scénách.

## Před prací
- Vždy si nejdřív přečti **GDD.md** (zdroj pravdy o hře) a **STYLE.md** (vizuální styl a pravidla pro assety).

## Jak pracovat
- Postupuj **po malých krocích**.
- Po každé změně **zkontroluj konzoli Unity** (chyby a varování) a vyřeš je, než budeš pokračovat.
- **Nemaž soubory bez potvrzení** uživatele.

## Git
- Každý úkol dělej **na vlastní větvi**, nikdy necommituj přímo do `main`.
