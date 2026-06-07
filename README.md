# Visiativ — Mini Application E-Commerce

Test technique Lead Developer .NET — démonstration de la cohabitation d'une stack moderne (.NET 10) et d'une stack legacy (.NET Framework 4.8) orchestrées par [.NET Aspire](https://learn.microsoft.com/fr-fr/dotnet/aspire/get-started/aspire-overview).

---

## Architecture en un coup d'œil

| Bloc | Rôle | Technologie |
|---|---|---|
| **Visiativ.AppHost** | Orchestrateur Aspire — démarre et relie tous les services | .NET Aspire 10 |
| **CatalogService** | Catalogue produits (API REST) | ASP.NET Core 10 · EF Core · SQL Server |
| **BasketService** | Gestion du panier (API REST) | ASP.NET .NET Framework 4.8 · ADO.NET · Mono 6.12 |
| **Visiativ.ApiService** | BFF — façade exposée au frontend | ASP.NET Core 10 Minimal API |
| **Visiativ.Web** | Interface utilisateur | Blazor Server *(non branché dans cette livraison)* |

```
Navigateur → BFF (ApiService) → CatalogService ─→ SQL Server (catalogdb)
                             ↘→ BasketService  ─→ SQL Server (basketdb)
```

---

## Lancer l'application

Voir [docs/quick-start.md](https://github.com/GreYak/visiativ/blob/main/docs/quick-start.md) pour les prérequis et les étapes détaillées.

En résumé :

```bash
git clone https://github.com/GreYak/visiativ.git
cd visiativ
# Ouvrir Visiativ.sln dans Visual Studio 2022
# Définir Visiativ.AppHost comme projet de démarrage → F5
```

---

## Documentation

| Document | Contenu |
|---|---|
| [quick-start.md](https://github.com/GreYak/visiativ/blob/main/docs/quick-start.md) | Prérequis, installation, lancement pas à pas |
| [architecture.md](https://github.com/GreYak/visiativ/blob/main/docs/architecture.md) | Présentation des briques, schéma global, diagrammes de séquence |
| [technical-documentation.md](https://github.com/GreYak/visiativ/blob/main/docs/technical-documentation.md) | Structure solution, modèles DB, APIs, gestion des erreurs, choix techniques et limites |

---

## Choix techniques — résumé

- **Aspire** comme orchestrateur : service discovery, health checks, dashboard de supervision intégrés sans configuration manuelle.
- **Mono 6.12 + XSP4** pour faire tourner le BasketService (.NET Framework 4.8) dans un conteneur Linux sans recourir à des Windows containers.
- **BFF pattern** : le frontend ne connaît qu'un seul point d'entrée ; les services backend restent internes.
- **Minimal API** pour CatalogService et le BFF : moins de cérémonie, endpoints déclaratifs.
- **Gestion des erreurs en couches** : validation domaine (400), indisponibilité service (503), exceptions techniques (500 via middleware partagé).

## Limites connues

- Pas de données de démo pré-chargées dans le catalogue (table vide au premier démarrage).
- Frontend Blazor non branché dans l'AppHost pour cette livraison.
- Panier unique partagé (pas d'authentification / session utilisateur).
- Race condition possible sur le stock (vérification dans le BFF non atomique avec l'ajout).
