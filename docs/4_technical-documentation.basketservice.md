# Documentation Technique — BasketService

---

## 1. Présentation du service

BasketService gère le panier d'achat. Il stocke les lignes de panier sous la forme `(ProductId, Quantity)` et est consommé exclusivement par le BFF (`Visiativ.ApiService`).

Responsabilités : lire le panier, ajouter/mettre à jour un item, vider le panier.

Les informations produit (nom, prix, description, stock) ne sont pas stockées ici — elles sont récupérées par le BFF auprès du CatalogService au moment de la consultation du panier. Cette séparation évite la duplication et la désynchronisation des données.

---

## 2. Stack technique

| Composant | Technologie |
|---|---|
| Framework | ASP.NET Web API (.NET Framework 4.8.1) |
| Accès données | ADO.NET (pas d'ORM) |
| Base de données | SQL Server |
| Exécution (Docker) | Mono 6.12 / XSP4 |
| Tests | NUnit · NSubstitute · HttpServer (in-process) |

> ⚠️ `mono:6.12` repose sur Debian Buster (EOL). Acceptable pour la démonstration, à adresser en production.

---

## 3. Architecture interne

```
BasketService/
├── App_Start/
│   └── WebApiConfig.cs               # Configuration WebAPI + enregistrement GlobalExceptionFilter
├── Controllers/
│   └── BasketController.cs           # GET /api/basket, POST /api/basket/add, DELETE /api/basket
├── Domain/
│   ├── AddItemToBasket.cs            # Cas d'utilisation ajout (validation quantité + limitMax)
│   ├── GetBasket.cs
│   ├── DeleteBasket.cs
│   └── Ports/Spi/
│       └── IBasketItemRepository.cs  # Port SPI — permet le mock dans les tests
├── Filters/
│   └── GlobalExceptionFilter.cs      # ExceptionFilterAttribute global (log + JSON 500)
├── Infrastructure/
│   ├── BasketItemRepository.cs       # ADO.NET + MERGE SQL
│   └── DatabaseInitializer.cs        # Création et migration de la table BasketItems
├── Models/
│   ├── BasketItem.cs                 # (ProductId, Quantity)
│   └── AddItemRequest.cs             # Corps POST add (ProductId, Quantity, LimitMax?)
└── Dockerfile                         # Build Mono 6.12 / XSP4
```

### Logique domaine — `AddItemToBasket`

```
1. Valide Quantity > 0            → ArgumentException        → 400
2. Lit l'item existant en base    (Get())
3. Calcule la quantité finale     = existante + nouvelle
4. Si limitMax défini
   et finalQty > limitMax         → InvalidOperationException → 409
5. Persiste via EnsureBasketItem  (MERGE SQL)
```

---

## 4. Modèle de données / base de données

### Table `BasketItems`

Gérée manuellement via ADO.NET. Créée au démarrage par `DatabaseInitializer.Initialize()` si elle n'existe pas.

| Colonne | Type SQL | Contraintes |
|---|---|---|
| `ProductId` | `UNIQUEIDENTIFIER` | PK, NOT NULL |
| `Quantity` | `INT` | NOT NULL |

`ProductId` est la clé primaire : un même produit ne peut avoir qu'une seule ligne dans le panier. L'opération d'ajout utilise un **`MERGE SQL`** — si le produit est déjà présent, `Quantity` est mise à jour ; sinon, une nouvelle ligne est insérée.

### Migration de schéma

Si la table a été créée avec l'ancien schéma (colonnes `Name` et `Price` présentes), `DatabaseInitializer.Initialize()` les supprime automatiquement au démarrage :

```sql
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BasketItems' AND COLUMN_NAME = 'Name')
    ALTER TABLE BasketItems DROP COLUMN Name;
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BasketItems' AND COLUMN_NAME = 'Price')
    ALTER TABLE BasketItems DROP COLUMN Price;
```

---

## 5. API / interfaces exposées

### `GET /api/basket`

Retourne le contenu du panier.

**Réponse 200 :**
```json
[
  {
    "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "quantity": 2
  }
]
```

| Code | Description |
|---|---|
| 200 | Contenu du panier (liste vide si vide) |
| 500 `{ "status": 500, "error": "..." }` | Erreur technique |

### `POST /api/basket/add`

Ajoute ou met à jour un item. La quantité est **accumulée** si le produit est déjà présent.

**Corps de la requête :**
```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "quantity": 2,
  "limitMax": 10
}
```

`limitMax` est optionnel. Quand il est fourni, la quantité finale accumulée ne peut pas dépasser cette valeur. Le BFF passe systématiquement le stock catalogue comme `limitMax`.

| Code | Description |
|---|---|
| 200 | Ajout ou mise à jour réussi |
| 400 `"Requête invalide."` | Corps de requête null |
| 400 `"Le paramètre limitMax doit être strictement positif."` | `limitMax <= 0` |
| 400 `"La quantité doit être supérieure à zéro."` | `quantity <= 0` (validation domaine) |
| 409 `{ "message": "Oversize the limit: final quantity (N) exceeds the maximum allowed (M)." }` | Quantité accumulée > `limitMax` |
| 500 `{ "status": 500, "error": "..." }` | Erreur technique |

### `DELETE /api/basket`

Vide le panier (supprime toutes les lignes).

| Code | Description |
|---|---|
| 204 | Panier vidé |
| 500 | Erreur technique |

---

## 6. Gestion des erreurs

| Mécanisme | Portée | Comportement |
|---|---|---|
| `BasketController` (try/catch inline) | `InvalidOperationException` → 409, `ArgumentException` → 400 | Erreurs métier gérées explicitement |
| `GlobalExceptionFilter` | Toute exception non gérée | Log `System.Diagnostics.Trace` + JSON `{ status: 500, error }` |

Le `GlobalExceptionFilter` est un `ExceptionFilterAttribute` enregistré globalement dans `WebApiConfig.Register()`.

---

## 7. Logs & monitoring

Le logging est assuré via `System.Diagnostics.Trace` dans le `GlobalExceptionFilter`. Aucune instrumentation structurée ou externe n'est documentée.

---

## 8. Spécificités techniques

### Contraintes Mono 6.12 / XSP4

Le BasketService est compilé pour `net4.7.2` (compatibilité Mono) et packagé dans un conteneur Linux via l'image `mono:6.12`. Le serveur HTTP est `xsp4`.

Deux contraintes de compatibilité impactent les clients HTTP qui communiquent avec ce service :

- **`Content-Length` explicite obligatoire** : XSP4 ne gère pas correctement le chunked transfer encoding en mode asynchrone. Tout client doit envoyer les corps de requête avec un `Content-Length` fixe.
- **`Expect: 100-continue` désactivé** : l'en-tête doit être supprimé côté client pour éviter une attente bloquante.

Ces deux contraintes sont appliquées dans le `BasketClient` du BFF.

---

## 9. Tests

**Projet :** `tests/BasketService.Tests/` — NUnit · NSubstitute · HttpServer (in-process)

### Approche

`WebApplicationFactory` n'est pas disponible sur .NET Framework 4.8. Les tests utilisent **`HttpServer`** (`System.Web.Http.HttpServer`) : le pipeline WebAPI complet est instancié en mémoire sans socket TCP. Les requêtes sont envoyées via un `HttpClient` wrappant directement ce serveur.

### Stratégies de substitution

**NSubstitute (mock classique)** — utilisé quand le test ne nécessite pas d'état accumulé (vérifier un code HTTP, simuler une exception). Le repository est créé avec `Substitute.For<IBasketItemRepository>()` et configuré via `.Returns(...)` ou `.Throws(...)`.

**`InMemoryBasketItemRepository` (faux réel)** — utilisé quand la logique dépend de l'état courant du panier (ex. : ajouter deux fois le même produit et vérifier l'accumulation). Ce faux implémente réellement `IBasketItemRepository` avec une liste en mémoire dont l'état persiste entre les appels HTTP d'un même test.

### Infrastructure — `BasketControllerTestBase`

Classe de base abstraite partagée par toutes les classes de test. `CreateClient(IBasketItemRepository)` construit un `HttpServer` configuré avec `WebApiConfig.Register()` et un `TestDependencyResolver` qui crée une **nouvelle instance de `BasketController` à chaque requête** — nécessaire car `ApiController` implémente `IDisposable` : Web API dispose le contrôleur après chaque appel, et une instance réutilisée lèverait `ObjectDisposedException`.

### Organisation

```
BasketService.Tests/
├── BasketControllerTestBase.cs       # HttpServer, TestDependencyResolver, PostItem()
├── InMemoryBasketItemRepository.cs   # Faux réel — IBasketItemRepository en mémoire
├── BasketControllerGetTests.cs       # 4 tests — GET /api/basket
├── BasketControllerAddTests.cs       # 10 tests — POST /api/basket/add
└── BasketControllerDeleteTests.cs    # 3 tests — DELETE /api/basket
```

Les tests d'erreur DB vérifient à la fois le code HTTP et le format JSON de la réponse (`{ "status": 500, "error": "..." }`), garantissant le comportement du `GlobalExceptionFilter`.
