# Frontend coverage map

Мапа endpoint → UI. Використовуйте при заповненні секції **«Де на фронті»** у `Docs/Endpoints/*.md`.

| Endpoint | Frontend module | Screen / feature | Status |
|----------|-----------------|------------------|--------|
| `POST /auth/register` | `frontend/src/features/auth/api/auth.api.ts` | RegisterForm | `implemented` |
| `POST /auth/login` | `frontend/src/features/auth/api/auth.api.ts` | LoginForm | `implemented` |
| `POST /auth/refresh` | `frontend/src/shared/api/http.client.ts` | HTTP interceptor | `implemented` |
| `POST /auth/logout` | `frontend/src/features/auth/api/auth.api.ts` | Settings / header | `implemented` |
| `GET /auth/google` | `frontend/src/features/auth/api/auth.api.ts` | Google OAuth redirect | `implemented` |
| `GET /catalog/companies` | `frontend/src/features/storefront/api/catalog.api.ts` | CompaniesScreen | `implemented` |
| `GET /catalog/categories` | `frontend/src/features/storefront/api/catalog.api.ts` | HomeScreen, ProductsScreen | `implemented` |
| `GET /catalog/products` | `frontend/src/features/storefront/api/catalog.api.ts` | HomeScreen, ProductsScreen | `implemented` |
| `GET /catalog/products/{slug}` | `frontend/src/features/storefront/api/catalog.api.ts` | Product detail (planned route) | `partial` |
| `GET /companies/{companyId}/products` | `frontend/src/features/workspace/api/products.api.ts` | WorkspaceProductsScreen | `implemented` |
| `POST /companies/{companyId}/products` | `frontend/src/features/workspace/api/products.api.ts` | WorkspaceProductsScreen | `implemented` |
| `GET /companies/{companyId}/members` | `frontend/src/features/workspace/api/members.api.ts` | Workspace members (planned UI) | `partial` |
| `GET /companies/{companyId}/inventory/*` | `frontend/src/features/workspace/api/inventory.api.ts` | Workspace inventory | `partial` |
| `GET /me/cart` | — | Cart | `planned` |
| `POST /me/cart/items` | — | Product add-to-cart | `planned` |
| `POST /me/cart/checkout` | — | Checkout | `planned` |
| `GET /me/orders` | — | My Orders | `planned` |
| `GET /me/favorites` | — | Favorites | `planned` |
| `GET /me/in-app-notifications` | — | Notifications bell | `planned` |
| `POST /me/web-push/subscriptions` | — | Push opt-in | `planned` |
| `GET /admin/*` | — | AdminShell routes | `partial` |
| `POST /reports` | — | Report dialog | `planned` |
| `POST /me/chats` | — | Chat inbox / product seller chat | `backend-only` |
| `GET /me/chats` | — | Chat inbox | `backend-only` |
| `POST /me/chats/{chatId}/messages` | — | Chat thread | `backend-only` |
| `POST /admin/chats/{chatId}/moderate` | — | Admin moderation | `backend-only` |
| `WS /hubs/chat` | — | Realtime chat | `backend-only` |
| `POST /support/tickets` | — | Support ticket form | `backend-only` |
| `GET /me/support/tickets` | — | My support inbox | `backend-only` |
| `POST /admin/support/tickets/{id}/status` | — | Admin support queue | `backend-only` |
| `POST /integrations/support/helpdesk/webhook` | — | Helpdesk provider | `backend-only` |
| `POST /analytics/*` | — | Storefront tracking hooks | `planned` |

**Статуси:**

- `implemented` — виклик є у frontend API-шарі та використовується екраном
- `partial` — модуль/API є, але не всі сценарії покриті UI
- `planned` — endpoint задокументований, UI/API ще не реалізовано
