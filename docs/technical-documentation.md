# Documentation Technique

## Structure de la solution

```
Visiativ/
├── Visiativ.slnx                          # Solution Visual Studio (format .slnx)
├── aspire.config.json                     # Configuration Aspire
├── README.md                              # Point d'entrée documentation Git
│
├── Visiativ.AppHost/                      # Orchestrateur Aspire
│   └── AppHost.cs                         # Déclaration des ressources (SQL, services, Docker)
│
├── Visiativ.ServiceDefaults/              # Bibliothèque partagée (net10)
│   ├── Extensions.cs                      # AddServiceDefaults, UseExceptionHandlingMiddleware, MapDefaultEndpoints
│   └── Middlewares/
│       └── ExceptionHandlingMiddleware.cs # Middleware catch-all partagé (log + JSON 500)
│
├── CatalogService/                        # Service catalogue (net10)
│   ├── Program.cs
│   ├── Domain/
│   │   └── Product.cs                     # Entité domaine avec validations
│   └── Infrastructure/
│       ├── Api/
│       │   ├── ProductEndpoints.cs        # GET /products, GET /products/{id}
│       │   └── ProductResponse.cs         # DTO de sortie
│       └── Persistence/
│           ├── CatalogDbContext.cs
│           ├── Configuration/
│           │   └── ProductConfiguration.cs
│           └── Migrations/
│               └── 20260606125644_InitialCreate.cs
│
├── BasketService/                         # Service panier (net48 / ASP.NET WebAPI)
│   ├── App_Start/
│   │   └── WebApiConfig.cs               # Configuration WebAPI + enregistrement GlobalExceptionFilter
│   ├── Controllers/
│   │   └── BasketController.cs           # GET /api/basket, POST /api/basket/add, DELETE /api/basket
│   ├── Domain/
│   │   ├── AddItemToBasket.cs            # Cas d'utilisation ajout (validation quantité)
│   │   ├── GetBasket.cs
│   │   ├── DeleteBasket.cs
│   │   └── Ports/Spi/
│   │       └── IBasketItemRepository.cs
│   ├── Filters/
│   │   └── GlobalExceptionFilter.cs      # ExceptionFilterAttribute global (log + JSON 500)
│   ├── Infrastructure/
│   │   ├── BasketItemRepository.cs       # ADO.NET + MERGE SQL
│   │   └── DatabaseInitializer.cs        # Création table BasketItems si absente
│   ├── Models/
│   │   └── BasketItem.cs
│   └── Dockerfile                         # Build Mono 6.12 / XSP4
│
├── Visiativ.ApiService/                   # BFF (net10)
│   ├── Program.cs
│   ├── Abstractions/
│   │   ├── IBasketClient.cs
│   │   └── ICatalogClient.cs
│   ├── Clients/
│   │   ├── BasketClient.cs               # HttpClient → BasketService (gère 400→RemoteValidation, 5xx→ServiceUnavailable)
│   │   └── CatalogClient.cs             # HttpClient → CatalogService (gère 404→null, 5xx→ServiceUnavailable)
│   ├── Endpoints/
│   │   ├── BasketEndpoints.cs            # GET/DELETE /basket, POST /basket/items (try/catch inline)
│   │   └── CatalogEndpoints.cs           # GET /products
│   ├── Exceptions/
│   │   ├── ServiceUnavailableException.cs
│   │   └── RemoteValidationException.cs
│   └── Models/
│       ├── AddItemRequest.cs
│       ├── BasketItem.cs
│       └── ProductResponse.cs
│
├── Visiativ.Web/                          # Frontend Blazor (non branché)
│
└── tests/
    ├── Visiativ.ApiService.Tests/         # Tests intégration BFF (WebApplicationFactory + NSubstitute)
    │   ├── AddItemToBasketTests.cs
    │   ├── GetBasketTests.cs
    │   ├── ClearBasketTests.cs
    │   └── GetProductTests.cs
    └── BasketService.Tests/               # Tests intégration BasketService (HttpServer WebAPI)
        └── BasketControllerTests.cs
```

---

## Modèles de données

### CatalogService — table `Products`

Gérée par EF Core. Schéma défini dans `ProductConfiguration` (Fluent API).

| Colonne | Type SQL | Contraintes |
|---|---|---|
| `Id` | `uniqueidentifier` | PK, NOT NULL |
| `Name` | `nvarchar(200)` | NOT NULL |
| `Description` | `nvarchar(1000)` | NOT NULL |
| `Price` | `decimal(18,2)` | NOT NULL |
| `Stock` | `int` | NOT NULL |

**Entité domaine `Product`** — validations dans la factory `Product.Create()` :
- `Name` ne peut pas être vide ou null.
- `Price` ne peut pas être négatif.
- `Stock` ne peut pas être négatif.

### BasketService — table `BasketItems`

Gérée manuellement via ADO.NET. Créée au démarrage par `DatabaseInitializer.Initialize()` si elle n'existe pas.

| Colonne | Type SQL | Contraintes |
|---|---|---|
| `ProductId` | `UNIQUEIDENTIFIER` | PK, NOT NULL |
| `Name` | `NVARCHAR(200)` | NOT NULL |
| `Price` | `DECIMAL(18,2)` | NOT NULL |
| `Quantity` | `INT` | NOT NULL |

> `ProductId` est la clé primaire : un même produit ne peut avoir qu'une ligne dans le panier. L'opération d'ajout utilise un `MERGE SQL` — si le produit est déjà présent, `Quantity` et `Price` sont mis à jour ; sinon, une nouvelle ligne est insérée.

---

## Migrations EF Core (CatalogService)

### Migration existante

`20260606125644_InitialCreate` — crée la table `Products`.

### Application automatique

La migration est appliquée automatiquement au démarrage **en environnement `Development`** :

```csharp
// CatalogService/Program.cs
using var sp = app.Services.CreateScope();
sp.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.Migrate();
```

En cas d'échec de la migration, l'application s'arrête proprement avec un message d'erreur dans la console.

### Créer une nouvelle migration

```bash
cd CatalogService
dotnet ef migrations add <NomDeLaMigration>
```

### Rollback

```bash
dotnet ef database update <NomDeLaMigrationPrécédente>
```

---

## Description des APIs

### CatalogService — `http://catalogservice`

Documentation OpenAPI disponible en mode développement à `/openapi`.

#### `GET /products`

Retourne la liste complète des produits, triée par nom.

| Paramètre | Type | Description |
|---|---|---|
| — | — | — |

**Réponse 200 :**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Laptop Pro 15",
    "description": "Ordinateur portable haute performance",
    "price": 1299.99,
    "stock": 5
  }
]
```

#### `GET /products/{id:guid}`

Retourne un produit par son identifiant.

| Réponse | Description |
|---|---|
| 200 | Produit trouvé |
| 404 `{ "Message": "Product '{id}' not found." }` | Produit inexistant |

---

### BasketService — `http://basketservice` (Docker, port 8080)

Pas de documentation OpenAPI (stack legacy). Testable via `GET /api/basket/test` *(⚠️ à supprimer avant toute mise en production — expose la connection string).*

#### `GET /api/basket`

Retourne le contenu du panier.

**Réponse 200 :**
```json
[
  {
    "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Laptop Pro 15",
    "price": 1299.99,
    "quantity": 2
  }
]
```

#### `POST /api/basket/add`

Ajoute ou met à jour un item dans le panier (MERGE SQL sur `ProductId`).

**Corps de la requête :**
```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Laptop Pro 15",
  "price": 1299.99,
  "quantity": 2
}
```

| Réponse | Description |
|---|---|
| 200 | Ajout ou mise à jour réussi |
| 400 `"Item invalide."` | Corps de requête null |
| 400 `"La quantité doit être supérieure à zéro."` | Quantité ≤ 0 (validation dans `AddItemToBasket`) |
| 500 `{ "status": 500, "error": "..." }` | Erreur technique (base de données, etc.) |

#### `DELETE /api/basket`

Vide le panier (supprime toutes les lignes).

| Réponse | Description |
|---|---|
| 204 | Panier vidé |
| 500 | Erreur technique |

---

### BFF Visiativ.ApiService — `http://apiservice`

Documentation OpenAPI disponible en mode développement à `/openapi`.

#### `GET /products`

Proxy vers `CatalogService GET /products`.

| Réponse | Description |
|---|---|
| 200 | Liste des produits |
| 503 | CatalogService indisponible |

#### `POST /basket/items`

Flux principal : vérification du produit + stock dans le catalogue, puis ajout dans le panier.

**Corps de la requête :**
```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "quantity": 2
}
```

| Réponse | Condition |
|---|---|
| 200 | Ajout réussi |
| 400 | Produit introuvable |
| 400 | Stock insuffisant |
| 400 | Quantité invalide (propagée depuis BasketService) |
| 503 | CatalogService ou BasketService indisponible |
| 500 | Erreur technique inattendue (middleware catch-all) |

#### `GET /basket`

| Réponse | Description |
|---|---|
| 200 | Contenu du panier |
| 503 | BasketService indisponible |

#### `DELETE /basket`

| Réponse | Description |
|---|---|
| 204 | Panier vidé |
| 503 | BasketService indisponible |

---

## Gestion des erreurs

### Vue d'ensemble

La gestion des erreurs est organisée en trois couches distinctes, chacune avec une responsabilité précise.

**Couche 1 — Validation domaine (BasketService)**
`AddItemToBasket.HandleAsync()` valide que `Quantity > 0` et lève une `ArgumentException`. Le `BasketController` la capture et retourne `400 Bad Request` avec le message. Le `GlobalExceptionFilter` (enregistré dans `WebApiConfig`) intercepte toute exception non gérée, la journalise via `System.Diagnostics.Trace`, et retourne `{ status: 500, error: "..." }` en JSON uniforme.

**Couche 2 — Clients HTTP (BFF)**
`CatalogClient` et `BasketClient` convertissent les erreurs réseau et HTTP en exceptions typées :
- `HttpRequestException` (service injoignable) → `ServiceUnavailableException(serviceName)`
- Réponse 400 du BasketService → `RemoteValidationException(message)`
- Réponse 5xx d'un service → `ServiceUnavailableException(serviceName)`
- Réponse 404 du CatalogService → `null` (produit inexistant, géré explicitement)

**Couche 3 — Endpoints BFF (inline) + middleware**
Chaque endpoint BFF exposé aux appels de service wrap ses appels clients dans des blocs `try/catch` explicites :
- `ServiceUnavailableException` → `Results.Problem(503, title: "Le service '{name}' est temporairement indisponible.")`
- `RemoteValidationException` → `Results.BadRequest(message)`

Le `ExceptionHandlingMiddleware` (partagé via `Visiativ.ServiceDefaults`) est positionné en tête du pipeline des services ASP.NET Core (CatalogService et BFF). Il constitue le filet de sécurité pour toute exception technique inattendue non catchée par les endpoints, et retourne `{ status: 500, error: "Une erreur inattendue s'est produite." }` avec un log `LogError`.

### Tableau de synthèse

| Cas d'erreur | Détecté par | HTTP retourné |
|---|---|---|
| Produit inexistant | BFF endpoint (catalog retourne null) | 400 |
| Stock insuffisant | BFF endpoint (comparaison explicite) | 400 |
| Quantité ≤ 0 | `AddItemToBasket` → `BasketController` → `BasketClient` → BFF endpoint | 400 propagé |
| CatalogService injoignable | `CatalogClient` → `ServiceUnavailableException` → BFF endpoint | 503 |
| BasketService injoignable | `BasketClient` → `ServiceUnavailableException` → BFF endpoint | 503 |
| Erreur DB non catchée (BasketService) | `GlobalExceptionFilter` | 500 JSON uniforme |
| Exception technique inattendue (BFF) | `ExceptionHandlingMiddleware` | 500 JSON uniforme |
| Exception technique inattendue (CatalogService) | `ExceptionHandlingMiddleware` | 500 JSON uniforme |

---

## Choix techniques et justifications

### Aspire comme orchestrateur

Aspire élimine toute la configuration manuelle habituelle d'un environnement multi-services (ports, connection strings, ordre de démarrage, health checks). La totalité de l'orchestration est déclarative dans `AppHost.cs`. Le dashboard intégré offre une supervision immédiate sans infrastructure observabilité supplémentaire.

### Mono 6.12 / XSP4 pour BasketService

Le test impose .NET Framework 4.8 pour le BasketService. Pour s'intégrer dans un environnement Docker/Linux comme les autres services (et éviter les Windows containers qui nécessitent une configuration spécifique de Docker Desktop), l'image `mono:6.12` avec le serveur `xsp4` a été retenue. MSBuild compile le projet en ciblant `v4.7.2` (limitation de compatibilité Mono), ce qui est transparent pour le comportement applicatif.

### BFF Pattern

Le frontend ne connaît qu'un seul point d'entrée. Le BFF est responsable de l'orchestration (appel catalogue + vérification stock + appel panier) et de la traduction des erreurs des services backend en réponses cohérentes pour le client. Les services backend restent internes et non exposés directement.

### Minimal API pour CatalogService et le BFF

Les Minimal APIs d'ASP.NET Core réduisent la cérémonie pour des services dont les endpoints sont peu nombreux et clairement délimités. Le routage déclaratif reste lisible et les endpoints sont facilement testables via `WebApplicationFactory`.

### ADO.NET + MERGE SQL pour BasketService

Contrainte du test. L'opération `MERGE` garantit l'idempotence de l'ajout au panier (upsert sur `ProductId`) sans nécessiter de logique applicative de détection de doublon.

### Architecture hexagonale partielle sur BasketService

`IBasketItemRepository` (port SPI) permet de mocker le repository dans les tests unitaires sans toucher à l'infrastructure ADO.NET. Le domaine (`AddItemToBasket`, `GetBasket`, `DeleteBasket`) reste testable en isolation.

---

## Limites connues et pistes d'amélioration

### Limites

**Pas de données de démo pré-chargées** — La table `Products` est vide au premier lancement. Un seeder (migration EF Core avec `HasData` ou script SQL d'initialisation) serait à ajouter pour une démonstration out-of-the-box.

**Frontend Blazor non branché** — `Visiativ.Web` existe dans la solution mais est commenté dans `AppHost.cs`. L'intégration complète UI → BFF n'est pas finalisée dans cette livraison.

**Panier unique / pas d'authentification** — Le panier est une table plate sans notion de session ou d'utilisateur. Tous les visiteurs partagent le même panier. L'ajout d'une colonne `UserId` ou d'un système de session serait nécessaire pour une application réelle.

**Race condition sur le stock** — La vérification du stock est faite dans le BFF (lecture depuis CatalogService), mais l'ajout dans le panier est une opération séparée dans BasketService. Entre les deux appels, le stock peut avoir changé (achat concurrent). Une solution correcte impliquerait une réservation atomique côté CatalogService.

**Route de diagnostic exposée** — `GET /api/basket/test` retourne la connection string SQL en clair. Ce endpoint de debug doit être supprimé avant toute mise en production.

**Mono EOL** — L'image `mono:6.12` repose sur Debian Buster (fin de vie). Les sources APT sont redirigées vers les archives Debian dans le `Dockerfile`. Acceptable pour un contexte de démonstration, à adresser si le service est amené à vivre en production.

**Logging basique sur BasketService** — Le `GlobalExceptionFilter` utilise `System.Diagnostics.Trace` (pas d'intégration OpenTelemetry). En production, une intégration avec un logger structuré (Serilog ou NLog avec sink OpenTelemetry) serait recommandée.

### Pistes d'amélioration

- Ajouter un seeder EF Core (`HasData` ou migration dédiée) pour prépopuler le catalogue.
- Intégrer le frontend Blazor avec les appels BFF (pages Products + Basket avec total calculé côté client).
- Ajouter une gestion de session (cookie ou JWT léger) pour isoler les paniers par utilisateur.
- Introduire une réservation de stock dans CatalogService pour éliminer la race condition.
- Remplacer Mono/XSP4 par NANCYFX ou migrer le BasketService vers .NET 8+ dans une démarche de modernisation.
- Centraliser le logging de BasketService vers OpenTelemetry pour avoir une traçabilité unifiée dans le dashboard Aspire.
- Ajouter des tests d'intégration end-to-end (AppHost Aspire + vrai SQL Server en test) avec `Aspire.Hosting.Testing`.
