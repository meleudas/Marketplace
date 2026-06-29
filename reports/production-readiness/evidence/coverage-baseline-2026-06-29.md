# Coverage baseline — 2026-06-29

Evidence для P1-1 (фазове підняття coverage gates).

## Поточні вимірювання (Release, local)

| Gate | Filter / Include | Line % | CI threshold (після P1) |
|------|------------------|--------|-------------------------|
| Global | `Suite!=API&...` | **10.27%** | **10** |
| CartCheckout | Cart/Carts/Checkout assemblies | TBD scoped | **14** |
| Payments | Payment/Refund assemblies | **14.38%** | **14** |
| Orders | Order assemblies | ~14% (scoped) | **14** |
| IdentityAccess | Auth/Identity assemblies | TBD scoped | **14** |
| Reviews | Review assemblies | **11.98%** | **11** |
| Інші домени | Suite only | ~2% whole-solution | **12** (unchanged) |

## Фазовий план до target (executive)

| Фаза | Global | P0 domains (scoped) | Статус |
|------|--------|---------------------|--------|
| A | 10 | 14 | **Застосовано** |
| B | 15 | 18 | Planned |
| C | 25 | 25 | Target public launch |

## Команди відтворення

```powershell
dotnet test backend/tests/Marketplace.Tests.Unit -c Release \
  --filter "Suite!=API&Suite!=Contract&Suite!=Security&Suite!=Performance&Suite!=Architecture" \
  /p:CollectCoverage=true /p:Threshold=0 /p:ThresholdType=line
```

## Нотатки

- Domain gates з `Include=` вимірюють scoped assemblies; gates без `Include` — whole-solution (низький %).
- P1 додано `Include` для CartCheckout та IdentityAccess.
