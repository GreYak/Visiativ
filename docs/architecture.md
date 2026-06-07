# Architecture

## Généralités

### Contexte

Mini application e-commerce permettant à un utilisateur de consulter un catalogue de produits, d'ajouter des articles à un panier et de visualiser son contenu. L'exercice démontre la cohabitation d'une **stack moderne** (ASP.NET Core 10) et d'une **stack legacy** (.NET Framework 4.8) au sein d'un même système orchestré.

### Approche retenue — .NET Aspire

[.NET Aspire](https://learn.microsoft.com/fr-fr/dotnet/aspire/get-started/aspire-overview) a été retenu comme solution d'orchestration locale pour les raisons suivantes :

- **Service discovery automatique** : les services se référencent par nom (`http://catalogservice`, `http://basketservice`) sans configuration manuelle de ports.
- **Provisioning des ressources** : le conteneur SQL Server (et ses deux bases) est déclaré dans l'AppHost et démarré automatiquement.
- **Health checks et démarrage ordonné** : le BFF attend la disponibilité du CatalogService avant de démarrer (`WaitFor`).
- **Dashboard de supervision intégré** : traces, logs et métriques centralisés via OpenTelemetry sans infrastructure supplémentaire.
- **Gestion du service legacy** : BasketService est enregistré comme ressource Docker (`AddDockerfile`) — Aspire gère son cycle de vie comme n'importe quel service.

---

## Présentation des briques

| Composant | Rôle | Technologie | Port local |
|---|---|---|---|
| **Visiativ.AppHost** | Orchestrateur Aspire — déclare et relie toutes les ressources | .NET Aspire 10 | — |
| **Visiativ.ServiceDefaults** | Bibliothèque partagée — OpenTelemetry, health checks, service discovery, middleware d'exception catch-all | ASP.NET Core 10 (lib) | — |
| **CatalogService** | Service catalogue — expose les produits en lecture | ASP.NET Core 10 · EF Core 10 · SQL Server | dynamique (Aspire) |
| **BasketService** | Service panier — stocke et gère les lignes du panier | ASP.NET .NET Framework 4.8.1 · ADO.NET · SQL Server · Mono 6.12 / XSP4 | 8080 (Docker) |
| **Visiativ.ApiService** | BFF — orchestre CatalogService et BasketService, point d'entrée unique du frontend | ASP.NET Core 10 Minimal API | dynamique (Aspire) |
| **Visiativ.Web** | Interface utilisateur *(non branché dans cette livraison)* | Blazor Server | — |

---

## Schéma global

```mermaid
graph TB
    subgraph Client
        User([Utilisateur])
        Web[Visiativ.Web\nBlazor Server]
    end

    subgraph Aspire["Visiativ.AppHost (Aspire)"]
        BFF["Visiativ.ApiService\nBFF · ASP.NET Core 10"]
        Cat["CatalogService\nASP.NET Core 10 · EF Core"]
        Bas["BasketService\n.NET Framework 4.8\nMono 6.12 / Docker"]
        SQL[("SQL Server\nconteneur Aspire")]
    end

    subgraph Shared[Visiativ.ServiceDefaults]
        Defaults["OpenTelemetry · Health Checks\nService Discovery · ExceptionHandlingMiddleware"]
    end

    User --> Web
    Web --> BFF
    BFF --> Cat
    BFF --> Bas
    Cat --> SQL
    Bas --> SQL
    Cat -.-> Defaults
    BFF -.-> Defaults
```

### Dépendances entre composants

```mermaid
graph LR
    AppHost --> CatalogService
    AppHost --> BasketService
    AppHost --> ApiService
    AppHost --> Web

    ApiService --> CatalogService
    ApiService --> BasketService

    CatalogService --> ServiceDefaults
    ApiService --> ServiceDefaults

    CatalogService --> CatalogTests[CatalogService.Tests]
    ApiService --> ApiTests[Visiativ.ApiService.Tests]
    BasketService --> BasketTests[BasketService.Tests]
```

> **Note** : BasketService (.NET Framework 4.8.1) ne peut pas référencer Visiativ.ServiceDefaults (net10). Il dispose de ses propres mécanismes : `GlobalExceptionFilter` (ASP.NET WebAPI) pour la gestion des erreurs, `System.Diagnostics.Trace` pour le logging.

---

## Use Cases — Diagrammes de séquence

### 1. Consulter le catalogue produits

```mermaid
sequenceDiagram
    actor User as Utilisateur
    participant Web as Visiativ.Web
    participant BFF as ApiService (BFF)
    participant Cat as CatalogService

    User->>Web: Ouvre la liste des produits
    Web->>BFF: GET /products
    BFF->>Cat: GET /products
    Cat-->>BFF: 200 OK · ProductResponse[]
    BFF-->>Web: 200 OK · ProductResponse[]
    Web-->>User: Affiche la liste

    note over BFF,Cat: En cas d'indisponibilité CatalogService
    Cat--xBFF: Exception réseau → ServiceUnavailableException
    BFF-->>Web: 503 Service Unavailable
```

---

### 2. Ajouter un produit au panier

C'est le flux le plus riche : le BFF orchestre deux appels en séquence et gère tous les cas d'erreur métier.

```mermaid
sequenceDiagram
    actor User as Utilisateur
    participant Web as Visiativ.Web
    participant BFF as ApiService (BFF)
    participant Cat as CatalogService
    participant Bas as BasketService

    User->>Web: Clique "Ajouter au panier" (productId, quantité)
    Web->>BFF: POST /basket/items {productId, quantity}

    BFF->>Cat: GET /products/{productId}

    alt CatalogService indisponible
        Cat--xBFF: Exception réseau → ServiceUnavailableException
        BFF-->>Web: 503 Service Unavailable "CatalogService indisponible"

    else Produit non trouvé
        Cat-->>BFF: 404 Not Found → null
        BFF-->>Web: 400 Bad Request "Le produit '{id}' est introuvable."

    else Produit trouvé
        Cat-->>BFF: 200 OK {id, name, price, stock}

        alt Stock insuffisant (stock < quantité demandée)
            BFF-->>Web: 400 Bad Request "Stock insuffisant. Disponible : N, demandé : M."

        else Stock suffisant
            BFF->>Bas: POST /api/basket/add {productId, name, price, quantity}

            alt BasketService indisponible
                Bas--xBFF: Exception réseau → ServiceUnavailableException
                BFF-->>Web: 503 Service Unavailable "BasketService indisponible"

            else Quantité invalide (≤ 0, rejeté par la couche domaine)
                Bas-->>BFF: 400 Bad Request → RemoteValidationException
                BFF-->>Web: 400 Bad Request (message propagé)

            else Ajout réussi
                Bas-->>BFF: 200 OK
                BFF-->>Web: 200 OK
                Web-->>User: Confirmation ajout
            end
        end
    end
```

---

### 3. Consulter le panier

```mermaid
sequenceDiagram
    actor User as Utilisateur
    participant Web as Visiativ.Web
    participant BFF as ApiService (BFF)
    participant Bas as BasketService

    User->>Web: Ouvre le panier
    Web->>BFF: GET /basket
    BFF->>Bas: GET /api/basket

    alt BasketService indisponible
        Bas--xBFF: Exception réseau → ServiceUnavailableException
        BFF-->>Web: 503 Service Unavailable

    else Panier accessible
        Bas-->>BFF: 200 OK · BasketItem[]
        BFF-->>Web: 200 OK · BasketItem[]
        Web-->>User: Affiche les lignes + total
    end
```

---

### 4. Vider le panier

```mermaid
sequenceDiagram
    actor User as Utilisateur
    participant Web as Visiativ.Web
    participant BFF as ApiService (BFF)
    participant Bas as BasketService

    User->>Web: Clique "Vider le panier"
    Web->>BFF: DELETE /basket
    BFF->>Bas: DELETE /api/basket

    alt BasketService indisponible
        Bas--xBFF: Exception réseau → ServiceUnavailableException
        BFF-->>Web: 503 Service Unavailable

    else Vidage réussi
        Bas-->>BFF: 204 No Content
        BFF-->>Web: 204 No Content
        Web-->>User: Panier vide affiché
    end
```
