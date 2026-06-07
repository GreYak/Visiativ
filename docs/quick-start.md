# Quick Start

## Prérequis

### Obligatoires

| Outil | Version minimale | Lien |
|---|---|---|
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| Aspire workload | inclus dans .NET 10 SDK | `dotnet workload install aspire` |
| Docker Desktop | 4.x (Linux containers) | https://www.docker.com/products/docker-desktop |
| WSL 2 | activé | Paramètres Docker → *Use WSL 2 based engine* |
| Visual Studio 2022 | 17.9+ | https://visualstudio.microsoft.com/ |

> **Visual Studio Code** est également supporté avec l'extension C# DevKit et l'extension .NET Aspire.

### Optionnel

| Outil | Utilité |
|---|---|
| SQL Server Management Studio (SSMS) | Inspecter les bases de données pendant l'exécution |
| .NET Aspire dashboard | Accessible automatiquement au lancement (URL affichée dans la console) |

---

## Note sur BasketService et Mono

Le **BasketService** est un projet ASP.NET ciblant **.NET Framework 4.8.1**. Pour le faire tourner sous Linux (Docker), la solution utilise l'image Docker **`mono:6.12`** avec le serveur web **`xsp4`** (serveur ASP.NET de Mono).

**Aucune installation de Mono n'est nécessaire sur votre machine locale.** Docker télécharge et build l'image automatiquement au premier lancement via l'AppHost Aspire.

> ⚠️ `mono:6.12` est basé sur Debian Buster (EOL). Les sources APT sont redirigées vers les archives Debian dans le `Dockerfile` pour permettre l'installation de `mono-xsp4`. Cette configuration est fonctionnelle pour un contexte de démonstration.

---

## Lancement pas à pas

### 1. Cloner le repository

```bash
git clone https://github.com/GreYak/visiativ.git
cd visiativ
```

### 2. Configurer Docker Desktop

Vérifier que Docker Desktop est en mode **Linux containers** :

```
Docker Desktop → Settings → General → "Use WSL 2 based engine" ✓
```

### 3. Ouvrir la solution

Ouvrir `Visiativ.slnx` dans Visual Studio 2022.

### 4. Définir le projet de démarrage

Clic droit sur **`Visiativ.AppHost`** → *Définir comme projet de démarrage*.

### 5. Lancer

Appuyer sur **F5** (ou `Ctrl+F5` sans débogage).

Au premier lancement, Docker télécharge l'image `mono:6.12` et build le conteneur BasketService (~2-3 minutes). Les lancements suivants sont instantanés.

### 6. Accéder aux services

Le dashboard Aspire s'ouvre automatiquement dans le navigateur. Les URLs de chaque service sont affichées :

| Service | URL (exemple) |
|---|---|
| Dashboard Aspire | `http://localhost:18888` |
| BFF (ApiService) | `http://localhost:5XXX` |
| CatalogService | `http://localhost:5XXX` |
| BasketService | `http://localhost:8080` (via Docker) |

---

## Données de démo

### Catalogue produits (CatalogService)

La base `catalogdb` est créée et migrée automatiquement au démarrage en environnement `Development`. La table `Products` est vide par défaut. Insérer des produits de démo via l'endpoint OpenAPI du CatalogService (disponible en mode développement à `/openapi`) :

```http
POST http://<catalogservice>/products
Content-Type: application/json

[
  { "name": "Laptop Pro 15", "description": "Ordinateur portable haute performance", "price": 1299.99, "stock": 5 },
  { "name": "Souris sans fil", "description": "Souris ergonomique 2.4 GHz", "price": 29.99, "stock": 50 },
  { "name": "Clavier mécanique", "description": "Switch Cherry MX Blue, rétroéclairé", "price": 89.99, "stock": 20 },
  { "name": "Moniteur 27 pouces", "description": "QHD 165 Hz, dalle IPS", "price": 399.99, "stock": 0 }
]
```

> Le produit *Moniteur 27 pouces* avec `stock: 0` permet de tester le cas d'erreur *stock insuffisant*.

### Panier (BasketService)

La table `BasketItems` est créée automatiquement au démarrage si elle n'existe pas (`DatabaseInitializer.Initialize()`). Aucune donnée initiale — le panier est vide.

---

## Vérification rapide

Une fois lancé, le parcours utilisateur complet peut être testé via le BFF :

```bash
# 1. Lister les produits
curl http://<bff>/products

# 2. Ajouter un produit au panier (remplacer <guid> par un Id réel)
curl -X POST http://<bff>/basket/items \
     -H "Content-Type: application/json" \
     -d '{"productId": "<guid>", "quantity": 2}'

# 3. Consulter le panier
curl http://<bff>/basket

# 4. Vider le panier
curl -X DELETE http://<bff>/basket
```
