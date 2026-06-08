# Visiativ — Mini Application E-Commerce

Démonstration de la cohabitation d'une stack moderne (.NET 10) et d'une stack legacy (.NET Framework 4.8) orchestrées par [.NET Aspire](https://learn.microsoft.com/fr-fr/dotnet/aspire/get-started/aspire-overview).

---

## Survol des composants

| Bloc | Rôle | Technologie |
|---|---|---|
| **Visiativ.AppHost** | Orchestrateur Aspire — démarre et relie tous les services | .NET Aspire 10 |
| **CatalogService** | Catalogue produits (API REST) | ASP.NET Core 10 · EF Core · SQL Server |
| **BasketService** | Gestion du panier (API REST) | ASP.NET .NET Framework 4.8 · ADO.NET · Mono 6.12 |
| **Visiativ.ApiService** | BFF — façade exposée au frontend | ASP.NET Core 10 Minimal API |
| **Visiativ.Web** | Interface utilisateur | Blazor Server |

```
Navigateur → BFF (ApiService) → CatalogService ─→ SQL Server (catalogdb)
                             ↘→ BasketService  ─→ SQL Server (basketdb)
```

---

## Lancer l'application

Voir [docs/quick-start.md](docs/quick-start.md) pour les prérequis et les étapes détaillées.

En résumé :

```bash
git clone https://github.com/GreYak/visiativ.git
cd visiativ
# Ouvrir Visiativ.slnx dans Visual Studio 2026
# Définir Visiativ.AppHost comme projet de démarrage → F5
```

---

## Documentation

| Document | Contenu |
|---|---|
| [quick-start.md](docs/quick-start.md) | Prérequis, installation, lancement pas à pas |
| [architecture.md](docs/architecture.md) | Présentation des briques, schéma global, diagrammes de séquence |
| [technical-documentation.catalogservice.md](docs/technical-documentation.catalogservice.md) | Modèle, EF Core, migrations, APIs CatalogService |
| [technical-documentation.basketservice.md](docs/technical-documentation.basketservice.md) | Modèle, DB ADO.NET, APIs BasketService |
| [technical-documentation.apiservice.md](docs/technical-documentation.apiservice.md) | Clients HTTP, endpoints BFF, modèles, traduction des erreurs |

