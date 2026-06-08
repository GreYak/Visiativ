# Documentation Technique — CatalogService

---

## 1. Présentation du service

CatalogService expose le catalogue produits en lecture seule. Il est consommé exclusivement par le BFF (`Visiativ.ApiService`) ; aucun frontend ne l'appelle directement.

Responsabilités : lister les produits, retourner un produit par identifiant.

---

## 2. Stack technique

| Composant | Technologie |
|---|---|
| Framework | ASP.NET Core 10 Minimal API |
| ORM | EF Core 10 (Fluent API) |
| Base de données | SQL Server |
| Tests | NUnit · NSubstitute · WebApplicationFactory |

---

## 3. Architecture interne

```
CatalogService/
├── Program.cs
├── Domain/
│   └── Product.cs                    # Entité domaine avec validations (factory Create())
└── Infrastructure/
    ├── Api/
    │   ├── ProductEndpoints.cs       # GET /products, GET /products/{id}
    │   └── ProductResponse.cs        # DTO de sortie
    └── Persistence/
        ├── CatalogDbContext.cs
        ├── Configuration/
        │   └── ProductConfiguration.cs
        └── Migrations/
            └── 20260606125644_InitialCreate.cs
```

L'entité domaine `Product` est créée via la factory `Product.Create()`, qui centralise les validations. Les endpoints sont déclarés dans `ProductEndpoints.cs` et mappés dans `Program.cs`.

---

## 4. Modèle de données / base de données

### Table `Products`

Gérée par EF Core (Fluent API dans `ProductConfiguration`).

| Colonne | Type SQL | Contraintes |
|---|---|---|
| `Id` | `uniqueidentifier` | PK, NOT NULL |
| `Name` | `nvarchar(200)` | NOT NULL |
| `Description` | `nvarchar(1000)` | NOT NULL |
| `Price` | `decimal(18,2)` | NOT NULL |
| `Stock` | `int` | NOT NULL |

### Validations domaine (`Product.Create()`)

- `Name` ne peut pas être vide ou null.
- `Price` ne peut pas être négatif.
- `Stock` ne peut pas être négatif.

### Migrations EF Core

Migration existante : `20260606125644_InitialCreate` — crée la table `Products`.

**Application automatique** — en environnement `Development` uniquement :

```csharp
// CatalogService/Program.cs
using var sp = app.Services.CreateScope();
sp.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.Migrate();
```

Un **seeder** est également exécuté en `Development` pour pré-charger 6 produits (Laptop, Souris, Clavier, Moniteur, Casque, Webcam) avec des stocks aléatoires entre 0 et 15.

**Créer une migration :**

```bash
cd src/CatalogService
dotnet ef migrations add <NomDeLaMigration>
```

**Rollback :**

```bash
dotnet ef database update <NomDeLaMigrationPrécédente>
```

---

## 5. API / interfaces exposées

### `GET /products`

Retourne la liste complète des produits, triée par nom.

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

| Code | Description |
|---|---|
| 200 | Liste des produits (vide si aucun) |
| 503 | Service indisponible (exception non catchée → middleware) |

### `GET /products/{id:guid}`

Retourne un produit par son identifiant.

| Code | Description |
|---|---|
| 200 | Produit trouvé |
| 404 `{ "Message": "Product '{id}' not found." }` | Produit inexistant |

### OpenAPI

Documentation interactive disponible en mode développement :

```
http://<catalogservice>/openapi
```

---

## 6. Gestion des erreurs

Les erreurs non catchées remontent via le middleware ASP.NET Core et produisent un `503`. Les erreurs de validation domaine (ex. : produit inexistant) sont gérées directement dans les endpoints et retournent un `404` structuré.

---

## 7. Logs & monitoring

Aucune instrumentation spécifique documentée. Le logging par défaut ASP.NET Core (`ILogger`) est actif.

---

## 8. Spécificités techniques

- **Migration automatique au démarrage** : uniquement en environnement `Development`. En production, les migrations doivent être appliquées manuellement.
- **Seeder de données** : actif en `Development` uniquement. Insère 6 produits de démonstration avec stocks aléatoires.
- Le service est **lecture seule** : aucun endpoint d'écriture n'est exposé. Les modifications de catalogue passent par des outils externes (migrations, seeder).

---

## 9. Tests

**Projet :** `tests/CatalogService.Tests/` — NUnit · NSubstitute · WebApplicationFactory

### Approche

Tests d'intégration : l'hôte ASP.NET Core complet est démarré en mémoire via `WebApplicationFactory<Program>`. Chaque test envoie de vraies requêtes HTTP et reçoit de vraies réponses JSON. La seule couche remplacée est la base de données.

### Infrastructure — `CatalogWebApplicationFactory`

Étend `WebApplicationFactory<Program>`. Remplace le `CatalogDbContext` EF Core réel par une base **EF Core InMemory** :

- La connection string SQL Server est court-circuitée (`Server=fake`).
- Le `DbContext` est réinscrit avec `UseInMemoryDatabase(DatabaseName)`, où `DatabaseName` est un `Guid` unique par instance — garantit l'isolation entre tests.
- `SeedAsync(Action<CatalogDbContext>)` permet d'insérer des données avant un appel HTTP.

Pour les tests de cas d'erreur DB, NSubstitute est utilisé en **partial mock** (`Substitute.ForPartsOf<CatalogDbContext>()`) pour lever une exception sur `Set<Product>()`. La factory accepte un `Action<IServiceCollection>?` optionnel pour injecter ce faux contexte.

### Organisation

```
CatalogService.Tests/
├── CatalogWebApplicationFactory.cs    # Infrastructure partagée
├── GetAllProductsTests.cs             # 3 tests — GET /products
└── GetProductByIdTests.cs             # 3 tests — GET /products/{id}
```

### Cas couverts

| Classe | Test | Scénario |
|---|---|---|
| `GetAllProductsTests` | `GetAll_ReturnsOk_WithEmptyList` | Base vide → 200 liste vide |
| | `GetAll_ReturnsOk_WithProducts` | 2 produits seedés → 200, count = 2 |
| | `GetAll_Returns500_WhenDbThrows` | Contexte partiel qui lève → 500 |
| `GetProductByIdTests` | `GetById_ReturnsOk_WhenProductExists` | Produit seedé → 200, données correctes |
| | `GetById_ReturnsNotFound_WhenProductDoesNotExist` | Id inconnu → 404 |
| | `GetById_Returns500_WhenDbThrows` | Contexte partiel qui lève → 500 |
