# Documentation Technique — Visiativ.ApiService (BFF)

---

## 1. Présentation du service

`Visiativ.ApiService` est le Backend For Frontend (BFF) exposé au frontend Blazor. Il orchestre CatalogService et BasketService, consolide leurs données, et traduit leurs erreurs en codes HTTP cohérents pour le frontend.

Responsabilités : proxy catalogue, gestion du panier (ajout avec contrôle de stock, lecture consolidée, suppression), traduction d'erreurs.

---

## 2. Stack technique

| Composant | Technologie |
|---|---|
| Framework | ASP.NET Core 10 Minimal API |
| Communication services | `HttpClient` (named/typed) |
| Documentation API | Scalar (OpenAPI) |
| Tests BFF | NUnit · NSubstitute · WebApplicationFactory |
| Tests Blazor | bUnit · NSubstitute |

---

## 3. Architecture interne

```
Visiativ.ApiService/
├── Program.cs
├── Abstractions/
│   ├── IBasketClient.cs              # Interface du client BasketService
│   └── ICatalogClient.cs             # Interface du client CatalogService
├── Clients/
│   ├── BasketClient.cs               # HttpClient → BasketService
│   └── CatalogClient.cs              # HttpClient → CatalogService
├── Endpoints/
│   ├── BasketEndpoints.cs            # GET/DELETE /basket, POST /basket/items
│   └── CatalogEndpoints.cs           # GET /products, GET /products/{id}
├── Exceptions/
│   ├── ServiceUnavailableException.cs
│   ├── RemoteValidationException.cs  # Wrapping d'un 400 reçu d'un service backend
│   └── RemoteConflictException.cs    # Wrapping d'un 409 reçu de BasketService
└── Models/
    ├── AddItemRequest.cs
    ├── BasketItem.cs
    ├── BasketItemDto.cs
    └── ProductResponse.cs
```

### Clients HTTP

**`BasketClient`** (implémente `IBasketClient`) — gère les contraintes de compatibilité Mono/XSP4 (voir section 8) et traduit les réponses en exceptions typées :

| Code reçu | Action |
|---|---|
| 400 | Lit le message → `RemoteValidationException(message)` |
| 409 | Lit le message → `RemoteConflictException(message)` |
| `HttpRequestException` / 5xx | `ServiceUnavailableException("BasketService")` |

**`CatalogClient`** (implémente `ICatalogClient`) :

| Code reçu | Action |
|---|---|
| 200 | Désérialise et retourne |
| 404 | Retourne `null` |
| `HttpRequestException` / 5xx | `ServiceUnavailableException("CatalogService")` |

---

## 4. Modèle de données / base de données

Le BFF n'a pas de base de données propre. Il manipule les modèles suivants :

| Modèle | Description |
|---|---|
| `AddItemRequest` | Corps de `POST /basket/items` : `{ productId, quantity }` |
| `BasketItem` | Miroir du modèle BasketService : `{ productId, quantity }` |
| `ProductResponse` | DTO catalogue : `{ id, name, description, price, stock }` |
| `BasketItemDto` | Vue consolidée panier + catalogue (voir tableau ci-dessous) |

**`BasketItemDto` — sources des champs :**

| Champ | Source |
|---|---|
| `ProductId` | BasketService |
| `Quantity` | BasketService |
| `Name` | CatalogService |
| `Description` | CatalogService |
| `Price` | CatalogService |
| `Stock` | CatalogService |

---

## 5. API / interfaces exposées

### `GET /products`

Proxy transparent vers `CatalogService GET /products`.

| Code | Description |
|---|---|
| 200 · `ProductResponse[]` | Liste des produits |
| 503 | CatalogService indisponible |

### `POST /basket/items`

Orchestration en 3 étapes :

1. `GET /products/{productId}` → CatalogService (infos produit + stock)
2. Vérification du stock côté BFF (`stock < quantity` → 400)
3. `POST /api/basket/add { productId, quantity, limitMax: stock }` → BasketService

Le `limitMax` transmis au BasketService est le stock catalogue au moment de l'ajout, garantissant que la quantité totale accumulée ne dépasse pas le stock disponible même en cas d'ajouts successifs.

| Code | Condition |
|---|---|
| 200 | Ajout réussi |
| 400 | Produit introuvable |
| 400 | Stock insuffisant (`quantity > stock catalogue`) |
| 400 | Quantité invalide propagée depuis BasketService |
| 409 | Dépassement du stock accumulé (`panier existant + nouvelle quantité > stock`) |
| 503 | CatalogService ou BasketService indisponible |
| 500 | Erreur technique inattendue |

### `GET /basket`

Consolidation panier + catalogue :

1. `GET /api/basket` → BasketService → `BasketItem[]`
2. `GET /products` → CatalogService → `ProductResponse[]`
3. Jointure sur `ProductId` → `BasketItemDto[]`
4. Items absents du catalogue → ignorés, `isPartial = true`

| Code | Description |
|---|---|
| 200 · `BasketItemDto[]` | Panier complet, tous les items trouvés dans le catalogue |
| 207 · `BasketItemDto[]` | Panier partiel — un ou plusieurs items absents du catalogue (ignorés) |
| 503 | BasketService ou CatalogService indisponible |

### `DELETE /basket`

Proxy vers `DELETE /api/basket` du BasketService.

| Code | Description |
|---|---|
| 204 | Panier vidé |
| 503 | BasketService indisponible |

### Swagger UI

Interface interactive disponible en mode développement :

```
http://<apiservice>/swagger
```

Spec OpenAPI brute : `/openapi/v1.json`

---

## 6. Gestion des erreurs

Le BFF utilise un système d'exceptions typées pour isoler les erreurs des services backend et les traduire en codes HTTP cohérents :

| Exception | Origine | Code HTTP retourné |
|---|---|---|
| `ServiceUnavailableException` | `HttpRequestException` ou 5xx d'un backend | 503 |
| `RemoteValidationException` | 400 reçu de BasketService | 400 |
| `RemoteConflictException` | 409 reçu de BasketService | 409 |

Les vérifications métier propres au BFF (stock insuffisant, produit introuvable) sont gérées directement dans les endpoints et retournent un 400.

---

## 7. Logs & monitoring

Aucune instrumentation spécifique documentée. Le logging par défaut ASP.NET Core (`ILogger`) est actif.

---

## 8. Spécificités techniques

### Compatibilité Mono/XSP4 dans `BasketClient`

BasketService tourne sous Mono 6.12 / XSP4, ce qui impose deux contraintes appliquées dans `BasketClient` :

- **`Content-Length` explicite** : le corps de requête est envoyé avec une longueur fixe (pas de chunked transfer encoding) car XSP4 ne gère pas correctement le transfert chunked en mode asynchrone.
- **`Expect: 100-continue` désactivé** : l'en-tête est supprimé pour éviter une attente bloquante.

### Panier partiel (207)

Quand un ou plusieurs `ProductId` du panier ne correspondent à aucun produit du catalogue (produit supprimé, désynchronisation), le BFF ignore ces items et retourne un `207 Multi-Status` au lieu d'un `200`. Le flag `isPartial = true` est inclus dans la réponse.

---

## 9. Tests

### BFF — `Visiativ.ApiService.Tests`

**Projet :** `tests/Visiativ.ApiService.Tests/` — NUnit · NSubstitute · WebApplicationFactory

**Approche :** tests d'intégration — l'hôte ASP.NET Core complet est démarré en mémoire via `WebApplicationFactory<Program>`. Les seules couches remplacées sont `IBasketClient` et `ICatalogClient` (les frontières externes du BFF).

**Infrastructure — `ApiServiceWebApplicationFactory`** — étend `WebApplicationFactory<Program>` et remplace les implémentations réelles par des mocks NSubstitute exposés en propriétés publiques :

```csharp
public ICatalogClient CatalogClient { get; } = Substitute.For<ICatalogClient>();
public IBasketClient  BasketClient  { get; } = Substitute.For<IBasketClient>();
```

Chaque test configure les mocks via `.Returns(...)` ou `.Throws(...)` avant d'émettre une requête HTTP. Pas d'état partagé entre tests.

**Organisation :**

```
Visiativ.ApiService.Tests/
├── ApiServiceWebApplicationFactory.cs   # Infrastructure partagée
├── GetProductTests.cs                   # GET /products
├── AddItemToBasketTests.cs              # POST /basket/items
├── GetBasketTests.cs                    # GET /basket
└── ClearBasketTests.cs                  # DELETE /basket
```

### Frontend Blazor — `Visiativ.Web.Tests`

**Projet :** `tests/Visiativ.Web.Tests/` — bUnit · NSubstitute

Les composants Blazor (`Basket.razor`, `Products.razor`) sont testés en isolation via **bUnit**. `IVisiativApiClient` est mocké avec NSubstitute et injecté dans le conteneur de services bUnit. Les tests vérifient le rendu HTML produit par le composant en fonction des données retournées par le mock.

```
Visiativ.Web.Tests/
├── BasketPageTests.cs      # Tests composant Basket.razor
└── ProductsPageTests.cs    # Tests composant Products.razor
```
