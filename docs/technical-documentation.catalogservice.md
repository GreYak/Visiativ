# Documentation Technique — CatalogService

> Service catalogue exposant les produits en lecture. Stack : ASP.NET Core 10 · EF Core 10 · SQL Server.

---

## Modèle de données — table `Products`

Gérée par EF Core (Fluent API dans `ProductConfiguration`).

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

---

## Migrations EF Core

### Migration existante

`20260606125644_InitialCreate` — crée la table `Products`.

### Application automatique

La migration est appliquée automatiquement au démarrage **en environnement `Development`** :

```csharp
// CatalogService/Program.cs
using var sp = app.Services.CreateScope();
sp.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.Migrate();
```

Un **seeder** est également exécuté en environnement `Development` pour pré-charger 6 produits informatiques (Laptop, Souris, Clavier, Moniteur, Casque, Webcam) avec des stocks aléatoires entre 0 et 15.

### Créer une nouvelle migration

```bash
cd src/CatalogService
dotnet ef migrations add <NomDeLaMigration>
```

### Rollback

```bash
dotnet ef database update <NomDeLaMigrationPrécédente>
```

---

## Structure du projet

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

---

## API — `GET /products`

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

| Réponse | Description |
|---|---|
| 200 | Liste des produits (vide si aucun) |
| 503 | Service indisponible (erreur interne non catchée → middleware) |

---

## API — `GET /products/{id:guid}`

Retourne un produit par son identifiant.

| Réponse | Description |
|---|---|
| 200 | Produit trouvé |
| 404 `{ "Message": "Product '{id}' not found." }` | Produit inexistant |

---

## OpenAPI

Documentation interactive disponible en mode développement à :

```
http://<catalogservice>/openapi
```

---

## Tests

**Projet :** `tests/CatalogService.Tests/` — NUnit · NSubstitute · WebApplicationFactory

### Approche

Les tests sont des **tests d'intégration** : l'hôte ASP.NET Core complet est démarré en mémoire via `WebApplicationFactory<Program>`. Chaque test envoie de vraies requêtes HTTP et reçoit de vraies réponses JSON. La seule couche remplacée est la base de données.

### Stratégie de substitution — `CatalogWebApplicationFactory`

`CatalogWebApplicationFactory` étend `WebApplicationFactory<Program>` et remplace le `CatalogDbContext` EF Core réel par une base **EF Core InMemory** :

- La connection string SQL Server est court-circuitée (`Server=fake`).
- Le `DbContext` et ses options sont retirés du conteneur DI, puis réinscrits avec `UseInMemoryDatabase(DatabaseName)` où `DatabaseName` est un `Guid` unique par instance de factory — ce qui garantit l'isolation entre les tests.
- La méthode `SeedAsync(Action<CatalogDbContext>)` permet d'insérer des données de test avant un appel HTTP.

Pour les tests de cas d'erreur (simulation d'une exception DB), NSubstitute est utilisé en mode **partial mock** (`Substitute.ForPartsOf<CatalogDbContext>()`) pour faire lever une exception sur `Set<Product>()`. La factory accepte un `Action<IServiceCollection>?` optionnel pour injecter ce faux contexte.

### Organisation des tests

```
CatalogService.Tests/
├── CatalogWebApplicationFactory.cs    # Infrastructure partagée
├── GetAllProductsTests.cs             # 3 tests — GET /products
└── GetProductByIdTests.cs             # 3 tests — GET /products/{id}
```

Une classe par endpoint. Chaque classe crée et dispose une instance de factory dans `[SetUp]` / `[TearDown]`.

### Cas couverts

| Classe | Test | Scénario |
|---|---|---|
| `GetAllProductsTests` | `GetAll_ReturnsOk_WithEmptyList` | Base vide → 200 liste vide |
| | `GetAll_ReturnsOk_WithProducts` | 2 produits seedés → 200, count = 2 |
| | `GetAll_Returns500_WhenDbThrows` | Contexte partiel qui lève → 500 |
| `GetProductByIdTests` | `GetById_ReturnsOk_WhenProductExists` | Produit seedé → 200, données correctes |
| | `GetById_ReturnsNotFound_WhenProductDoesNotExist` | Id inconnu → 404 |
| | `GetById_Returns500_WhenDbThrows` | Contexte partiel qui lève → 500 |
