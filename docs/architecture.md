# Architecture

## Généralités

### Contexte

Mini application e-commerce permettant à un utilisateur de consulter un catalogue de produits, d'ajouter des articles à un panier et de visualiser son contenu. L'exercice démontre la cohabitation d'une **stack moderne** (ASP.NET Core 10) et d'une **stack legacy** (.NET Framework 4.8) au sein d'un même système orchestré.

### Approche retenue — .NET Aspire + Docker

[.NET Aspire](https://learn.microsoft.com/fr-fr/dotnet/aspire/get-started/aspire-overview) a été retenu comme solution d'orchestration locale pour les raisons suivantes :

- **Service discovery automatique** : les services se référencent par nom (`http://catalogservice`, `http://basketservice`) sans configuration manuelle de ports.
- **Provisioning des ressources** : le conteneur SQL Server (et ses deux bases) est déclaré dans l'AppHost et démarré automatiquement.
- **Health checks et démarrage ordonné** : le BFF attend la disponibilité du CatalogService avant de démarrer (`WaitFor`).
- **Dashboard de supervision intégré** : traces, logs et métriques centralisés via OpenTelemetry sans infrastructure supplémentaire.
- **Gestion du service legacy** : BasketService est enregistré comme ressource Docker (`AddDockerfile`) — Aspire gère son cycle de vie comme n'importe quel service.
- **Confort pour les développeurs** : n'importe quel développeur souhaitant explorer la solution n'a besoin que de Docker Desktop et du SDK .NET 10. Un seul F5 démarre l'intégralité de la stack (SQL Server, BasketService sous Mono, CatalogService, BFF, frontend). Pas de configuration manuelle de base de données, de ports ou de variables d'environnement.

---

## Présentation des briques

| Composant | Rôle | Technologie | Port local |
|---|---|---|---|
| **Visiativ.AppHost** | Orchestrateur Aspire — déclare et relie toutes les ressources | .NET Aspire 10 | — |
| **Visiativ.ServiceDefaults** | Bibliothèque partagée — OpenTelemetry, health checks, service discovery, middlewares | ASP.NET Core 10 (lib) | — |
| **CatalogService** | Service catalogue — expose les produits en lecture | ASP.NET Core 10 · EF Core 10 · SQL Server | dynamique (Aspire) |
| **BasketService** | Service panier — stocke les lignes du panier (ProductId + Quantity) | ASP.NET .NET Framework 4.8.1 · ADO.NET · SQL Server · Mono 6.12 / XSP4 | 8080 (Docker) |
| **Visiativ.ApiService** | BFF — orchestre CatalogService et BasketService, point d'entrée unique du frontend | ASP.NET Core 10 Minimal API | dynamique (Aspire) |
| **Visiativ.Web** | Interface utilisateur | Blazor Server | dynamique (Aspire) |

---

## Schéma global

```mermaid
graph TB
    User([Utilisateur])

    subgraph Client
        Web[Visiativ.Web\nBlazor Server]
    end

    subgraph Aspire["Visiativ.AppHost (Aspire)"]
        BFF["Visiativ.ApiService\nBFF · ASP.NET Core 10"]
        Bas["BasketService\n.NET Framework 4.8\nMono 6.12 / Docker"]
        Cat["CatalogService\nASP.NET Core 10 · EF Core"]
        SQL[("SQL Server\nbasketdb · catalogdb")]
    end

    User --> Web
    Web --> BFF
    BFF --> Bas
    BFF --> Cat
    Bas --> SQL
    Cat --> SQL
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
    Web --> ServiceDefaults
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
        Cat-->>BFF: 200 OK {id, name, description, price, stock}

        alt Stock insuffisant (stock < quantité demandée)
            BFF-->>Web: 400 Bad Request "Stock insuffisant. Disponible : N, demandé : M."

        else Stock suffisant
            BFF->>Bas: POST /api/basket/add {productId, quantity, limitMax: stock}

            alt BasketService indisponible
                Bas--xBFF: Exception réseau → ServiceUnavailableException
                BFF-->>Web: 503 Service Unavailable "BasketService indisponible"

            else Quantité invalide (≤ 0, rejeté par la couche domaine)
                Bas-->>BFF: 400 Bad Request → RemoteValidationException
                BFF-->>Web: 400 Bad Request (message propagé)

            else Dépassement du stock accumulé (panier existant + nouvelle qté > stock)
                Bas-->>BFF: 409 Conflict → RemoteConflictException
                BFF-->>Web: 409 Conflict
                Web-->>User: "Dépassement du stock : la quantité totale dépasserait le stock disponible."

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

Le BFF consolide les données du panier (ProductId + Quantity) avec le catalogue (Name, Description, Price, Stock) pour construire une vue enrichie. Si un article du panier n'est plus référencé dans le catalogue, il est ignoré et une réponse partielle est retournée.

```mermaid
sequenceDiagram
    actor User as Utilisateur
    participant Web as Visiativ.Web
    participant BFF as ApiService (BFF)
    participant Bas as BasketService
    participant Cat as CatalogService

    User->>Web: Ouvre le panier
    Web->>BFF: GET /basket

    BFF->>Bas: GET /api/basket
    BFF->>Cat: GET /products

    alt BasketService indisponible
        Bas--xBFF: Exception réseau → ServiceUnavailableException
        BFF-->>Web: 503 Service Unavailable

    else CatalogService indisponible
        Cat--xBFF: Exception réseau → ServiceUnavailableException
        BFF-->>Web: 503 Service Unavailable

    else Les deux services répondent
        Bas-->>BFF: 200 OK · BasketItem[] (ProductId, Quantity)
        Cat-->>BFF: 200 OK · ProductResponse[]

        note over BFF: Consolidation : jointure sur ProductId
        note over BFF: Items absents du catalogue → ignorés

        alt Tous les articles trouvés dans le catalogue
            BFF-->>Web: 200 OK · BasketItemDto[]
            Web-->>User: Affiche le panier

        else Un ou plusieurs articles absents du catalogue
            BFF-->>Web: 207 Multi-Status · BasketItemDto[] (articles trouvés uniquement)
            Web-->>User: Affiche le panier + avertissement "Certains items ont été retirés du panier pour épuisement de stock"
        end
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
