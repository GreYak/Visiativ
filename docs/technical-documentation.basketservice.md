# Documentation Technique — BasketService

> Service panier stockant les lignes de panier (ProductId + Quantity). Stack : ASP.NET WebAPI · .NET Framework 4.8.1 · ADO.NET · SQL Server · Mono 6.12 / XSP4 (Docker).

---

## Modèle de données — table `BasketItems`

Gérée manuellement via ADO.NET. Créée au démarrage par `DatabaseInitializer.Initialize()` si elle n'existe pas.

| Colonne | Type SQL | Contraintes |
|---|---|---|
| `ProductId` | `UNIQUEIDENTIFIER` | PK, NOT NULL |
| `Quantity` | `INT` | NOT NULL |

`ProductId` est la clé primaire : un même produit ne peut avoir qu'une seule ligne dans le panier. L'opération d'ajout utilise un `MERGE SQL` — si le produit est déjà présent, `Quantity` est mise à jour ; sinon, une nouvelle ligne est insérée.

**Pourquoi seulement ProductId + Quantity ?**
Les informations produit (nom, prix, description, stock) viennent exclusivement du CatalogService au moment de la consultation du panier. Cette séparation évite la duplication et la désynchronisation des données.

### Migration du schéma

Si la table a été créée avec l'ancien schéma (colonnes `Name` et `Price` présentes), `DatabaseInitializer.Initialize()` les supprime automatiquement au démarrage :

```sql
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BasketItems' AND COLUMN_NAME = 'Name')
    ALTER TABLE BasketItems DROP COLUMN Name;
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BasketItems' AND COLUMN_NAME = 'Price')
    ALTER TABLE BasketItems DROP COLUMN Price;
```

---

## Structure du projet

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
│   └── AddItemRequest.cs             # Corps de la requête POST add (ProductId, Quantity, LimitMax?)
└── Dockerfile                         # Build Mono 6.12 / XSP4
```

---

## API — `GET /api/basket`

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

| Réponse | Description |
|---|---|
| 200 | Contenu du panier (liste vide si vide) |
| 500 `{ "status": 500, "error": "..." }` | Erreur technique (base de données, etc.) |

---

## API — `POST /api/basket/add`

Ajoute ou met à jour un item dans le panier. La quantité est **accumulée** si le produit est déjà présent.

**Corps de la requête :**
```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "quantity": 2,
  "limitMax": 10
}
```

`limitMax` est optionnel. Quand il est fourni, la quantité finale accumulée ne peut pas dépasser cette valeur. Le BFF passe systématiquement le stock catalogue comme `limitMax` pour éviter tout dépassement.

| Réponse | Description |
|---|---|
| 200 | Ajout ou mise à jour réussi |
| 400 `"Requête invalide."` | Corps de requête null |
| 400 `"Le paramètre limitMax doit être strictement positif."` | `limitMax <= 0` |
| 400 `"La quantité doit être supérieure à zéro."` | `quantity <= 0` (validation domaine) |
| 409 `{ "message": "Oversize the limit: final quantity (N) exceeds the maximum allowed (M)." }` | Quantité accumulée > `limitMax` |
| 500 `{ "status": 500, "error": "..." }` | Erreur technique |

---

## API — `DELETE /api/basket`

Vide le panier (supprime toutes les lignes).

| Réponse | Description |
|---|---|
| 204 | Panier vidé |
| 500 | Erreur technique |

---

## Logique domaine — `AddItemToBasket`

```
1. Valide Quantity > 0 → ArgumentException → 400
2. Lit l'item existant en base (Get())
3. Calcule la quantité finale = existante + nouvelle
4. Si limitMax défini et finalQty > limitMax → InvalidOperationException → 409
5. Persiste via EnsureBasketItem (MERGE SQL)
```

---

## Gestion des erreurs

| Mécanisme | Portée | Comportement |
|---|---|---|
| `BasketController` (try/catch inline) | `InvalidOperationException` → 409, `ArgumentException` → 400 | Erreurs métier gérées explicitement |
| `GlobalExceptionFilter` | Toute exception non gérée | Log `System.Diagnostics.Trace` + JSON `{ status: 500, error }` |

---

## Tests

**Projet :** `tests/BasketService.Tests/` — NUnit · NSubstitute · HttpServer (in-process WebAPI)

### Approche

`WebApplicationFactory` n'est pas disponible sur .NET Framework 4.8. Les tests utilisent à la place **`HttpServer`** (classe `System.Web.Http.HttpServer`) : le pipeline WebAPI complet est instancié en mémoire sans lancer de socket TCP. Les requêtes sont envoyées via un `HttpClient` wrappant directement ce serveur.

### Stratégie de substitution

Deux approches selon le scénario testé :

**NSubstitute (mock classique)** — utilisé quand le test n'a pas besoin d'état accumulé entre les appels (vérifier un code HTTP, simuler une exception). Le repository est créé avec `Substitute.For<IBasketItemRepository>()` et configuré via `.Returns(...)` ou `.Throws(...)`.

**`InMemoryBasketItemRepository` (faux réel)** — utilisé quand le test exerce une logique qui dépend de l'état courant du panier (ex. : ajouter deux fois le même produit et vérifier l'accumulation). Contrairement à un mock, ce faux implémente réellement `IBasketItemRepository` avec une liste en mémoire dont l'état persiste entre les appels HTTP d'un même test.

### `BasketControllerTestBase`

Classe de base abstraite partagée par toutes les classes de test.

`CreateClient(IBasketItemRepository)` construit un `HttpServer` configuré avec `WebApiConfig.Register()` et un `TestDependencyResolver` personnalisé. Ce resolver crée une **nouvelle instance de `BasketController` à chaque requête** — comportement indispensable car `ApiController` implémente `IDisposable` : Web API dispose le contrôleur après chaque appel, et une instance réutilisée lèverait `ObjectDisposedException`.

### Organisation des tests

```
BasketService.Tests/
├── BasketControllerTestBase.cs       # HttpServer, TestDependencyResolver, PostItem()
├── InMemoryBasketItemRepository.cs   # Faux réel — IBasketItemRepository en mémoire
├── BasketControllerGetTests.cs       # 4 tests — GET /api/basket
├── BasketControllerAddTests.cs       # 10 tests — POST /api/basket/add
└── BasketControllerDeleteTests.cs    # 3 tests — DELETE /api/basket
```

Une classe par endpoint. Les tests d'erreur DB vérifient à la fois le code HTTP et le format JSON de la réponse d'erreur (`{ "status": 500, "error": "..." }`), garantissant le comportement du `GlobalExceptionFilter`.

---

## Infrastructure Docker / Mono

Le BasketService est compilé pour `net4.7.2` (compatibilité Mono) et packagé dans un conteneur Linux via l'image `mono:6.12`. Le serveur HTTP est `xsp4` (implémentation Mono d'ASP.NET).

**Contraintes connues de Mono 6.12 / XSP4 :**
- Le body HTTP doit être envoyé avec un `Content-Length` explicite (pas de chunked transfer encoding) : `XSP4` ne gère pas correctement le transfert chunked en mode asynchrone.
- L'en-tête `Expect: 100-continue` est désactivé côté `BasketClient` pour éviter une attente bloquante.

> ⚠️ `mono:6.12` repose sur Debian Buster (EOL). Acceptable pour la démonstration, à adresser en production.
