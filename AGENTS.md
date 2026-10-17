# AGENTS – pravidla pro AI agenty

Platí pro všechny agenty pracující na tomto projektu (Claude, Codex).

## Role
- **Claude** píše C# kód a staví scény v Unity. Dělá **všechny commity a větve**.
- **Codex** dělá **review a grafiku**. Assety generuje podle **STYLE.md** a zpracuje je přes `Tools/ArtPipeline`. Zapisuje **jen do `Assets/Art/` a `Tools/ArtPipeline/`**.
- **Claude do `Assets/Art/` nezapisuje.** Assety odtud jen používá v kódu a ve scénách.

## Před prací
- Vždy si nejdřív přečti **GDD.md** (zdroj pravdy o hře) a **STYLE.md** (vizuální styl a pravidla pro assety).

## Jak pracovat
- Postupuj **po malých krocích**.
- Po každé změně **zkontroluj konzoli Unity** (chyby a varování) a vyřeš je, než budeš pokračovat.
- **Nemaž soubory bez potvrzení** uživatele.

## Git
- Každý úkol dělej **na vlastní větvi**, nikdy necommituj přímo do `main`.
- **Codex nemá přístup ke gitu.** Své změny nechává v pracovní složce. Commituje je a větve pro ně zakládá Claude.
