# Domain Production Readiness (Gap Domains)

Цей розділ описує цільовий production-ready стан для gap-доменів Marketplace:

1. Shipping
2. Coupons
3. Reports
4. Behavior/Analytics
5. Chats
6. Support

Документація орієнтована на кінцевий стан `100/100` за методикою readiness-аудиту, з перевірним Definition of Done, тестовим контуром і операційними вимогами.

## Індекс

- [00-methodology-and-definition-of-done.md](./00-methodology-and-definition-of-done.md)
- [01-shipping-production-spec.md](./01-shipping-production-spec.md)
- [02-coupons-production-spec.md](./02-coupons-production-spec.md)
- [03-reports-production-spec.md](./03-reports-production-spec.md)
- [04-behavior-analytics-production-spec.md](./04-behavior-analytics-production-spec.md)
- [05-chats-production-spec.md](./05-chats-production-spec.md)
- [06-support-production-spec.md](./06-support-production-spec.md)
- [07-cross-domain-rollout-plan.md](./07-cross-domain-rollout-plan.md)

## Вхідні джерела

- [reports/production-readiness/domain-gaps-and-proposals.md](../../reports/production-readiness/domain-gaps-and-proposals.md)
- [reports/production-readiness/scoring-methodology.md](../../reports/production-readiness/scoring-methodology.md)
- [reports/production-readiness/domain-map.md](../../reports/production-readiness/domain-map.md)
- [backend/src/Marketplace.API/Docs/Endpoints/README.md](../../backend/src/Marketplace.API/Docs/Endpoints/README.md)

## Як використовувати

1. Вибрати домен-файл.
2. Виконати імплементацію за секціями Domain -> Application -> API -> Infrastructure -> Ops.
3. Закрити всі пункти Definition of Done.
4. Зафіксувати тести й quality gates в CI до merge.
