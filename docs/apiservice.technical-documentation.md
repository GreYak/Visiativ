# Documentation Technique — Visiativ.ApiService (BFF)

> Façade exposée au frontend. Orchestre CatalogService et BasketService, traduit les erreurs, et consolide les données. Stack : ASP.NET Core 10 Minimal API.

---

## Structure du projet

```
Visiativ.ApiService/
├── Program.cs
├── Abstractions/
│   ├── IBasketClient.cs              # Interface du client BasketService
│   └── ICatalogClient.cs             # Interface du client CatalogService
├── Clients/
│   ├── BasketClient.cs               # HttpClient → BasketService (gère 400→RemoteValidation,
│   │                                 # 409→RemoteConflict, 5xx→ServiceUnavailable)
│   └── CatalogClient.cs              # HttpClient → CatalogService (gère 404→null,
│                                     # 5xx→ServiceUnavailable)
├── Endpoints/
│   ├── BasketEndpoints.cs            # GET/DELETE /basket, POST /basket/items
│   └── CatalogEndpoints.cs           # GET /products, GET /products/{id}
├── Exceptions/
│   ├── ServiceUnavailableException.cs
│   ├── RemoteValidationException.cs  # Wrapping d'un 400 reçu d'un service backend
│   └── RemoteConflictException.cs    # Wrapping d'un 409 reçu de BasketService
└── Models/
    ├── AddItemRequest.cs             # Corps de POST /basket/items {ProductId, Quantity}
    ├── BasketItem.cs                 # Miroir de BasketService.Models.BasketItem (ProductId, Quantity)
    ├── BasketItemDto.cs              # Vue consolidée : ProductId + Quantity (panier) + Name/Description/Price/Stock (catalogue)
    └── ProductResponse.cs            # DTO catalogue
```

---

## Modèles

### `AddItemRequest`
Corps de la requête `POST /basket/items` envoyée par le frontend :
```json
{ "productId": "<guid>", "quantity": 2 }
```

### `BasketItem`
Miroir du modèle BasketService — ne contient que `ProductId` et `Quantity`.

### `BasketItemDto`
Vue enrichie construite par le BFF lors de la consolidation panier + catalogue :

| Champ | Source |
|---|---|
| `ProductId` | BasketService |
| `Quantity` | BasketService |
| `Name` | CatalogService |
| `Description` | CatalogService |
| `Price` | CatalogService |
| `Stock` | CatalogService |

### `ProductResponse`
DTO retourné par CatalogService : `Id, Name, Description, Price, Stock`.

---

## Endpoints

### `GET /products`

Proxy transparent vers `CatalogService GET /products`.

| Réponse | Description |
|---|---|
| 200 · `ProductResponse[]` | Liste des produits |
| 503 | CatalogService indisponible |

---

### `POST /basket/items`

Flux principal — orchestration en 3 étapes :

1. `GET /products/{productId}` → CatalogService pour récupérer les infos produit et le stock
2. Vérification du stock côté BFF (`stock < quantity` → 400)
3. `POST /api/basket/add {productId, quantity, limitMax: stock}` → BasketService

Le `limitMax` passé au BasketService est le stock catalogue au moment de l'ajout, ce qui garantit que la quantité totale accumulée en panier ne dépasse pas le stock disponible, même si des ajouts successifs ont eu lieu.

| Réponse | Condition |
|---|---|
| 200 | Ajout réussi |
| 400 | Produit introuvable |
| 400 | Stock insuffisant (quantity > stock catalogue) |
| 400 | Quantité invalide propagée depuis BasketService |
| 409 | Dépassement du stock accumulé (panier existant + nouvelle quantité > stock) |
| 503 | CatalogService ou BasketService indisponible |
| 500 | Erreur technique inattendue |

---

### `GET /basket`

Consolidation panier + catalogue :

1. `GET /api/basket` → BasketService → `BasketItem[]` (ProductId, Quantity)
2. `GET /products` → CatalogService → `ProductResponse[]`
3. Jointure sur `ProductId` → `BasketItemDto[]`
4. Items absents du catalogue → ignorés, `isPartial = true`

| Réponse | Description |
|---|---|
| 200 · `BasketItemDto[]` | Panier complet, tous les items trouvés dans le catalogue |
| 207 · `BasketItemDto[]` | Panier partiel — un ou plusieurs items absents du catalogue (ignorés) |
| 503 | BasketService ou CatalogService indisponible |

---

### `DELETE /basket`

Proxy vers `DELETE /api/basket` du BasketService.

| Réponse | Description |
|---|---|
| 204 | Panier vidé |
| 503 | BasketService indisponible |

---

## Clients HTTP

### `BasketClient`

Implémente `IBasketClient`. Gère les contraintes de compatibilité Mono/XSP4 :
- Corps de requête envoyé avec `Content-Length` explicite (pas de chunked transfer encoding)
- En-tête `Expect: 100-continue` désactivé

Traduction des réponses :

| Code reçu | Action |
|---|---|
| 400 | Lit le message → `RemoteValidationException(message)` |
| 409 | Lit le message → `RemoteConflictException(message)` |
| `HttpRequestException` / 5xx | `ServiceUnavailableException("BasketService")` |

### `CatalogClient`

Implémente `ICatalogClient`.

| Code reçu | Action |
|---|---|
| 200 | Désérialise et retourne |
| 404 | Retourne `null` |
| `HttpRequestException` / 5xx | `ServiceUnavailableException("CatalogService")` |

---

## Swagger / OpenAPI

Interface interactive **Scalar** disponible en mode développement à :

```
http://<apiservice>/scalar/v1
```

Spec OpenAPI brute : `/openapi/v1.json`
