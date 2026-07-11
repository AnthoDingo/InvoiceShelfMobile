# InvoiceShelf Mobile — .NET 10 MAUI

🇬🇧 [English version](./README.en.md)

Application mobile cross-platform pour [InvoiceShelf](https://invoiceshelf.com).

## 📱 Fonctionnalités

| Module | Description |
|--------|-------------|
| **Authentification** | Connexion, mot de passe oublié, configuration du serveur |
| **Factures** | Liste, création, édition, suppression, changement de statut |
| **Clients** | Gestion complète avec adresses de facturation/livraison |
| **Paiements** | Enregistrement et suivi des paiements |
| **Dépenses** | Suivi des dépenses par catégorie |
| **Articles** | Catalogue de produits/services |
| **Taxes** | Types de taxes (simples, composées, collectives) |
| **Compte & Entreprise** | Paramètres utilisateur et informations entreprise |

## 🏗️ Architecture

```
src/InvoiceShelf/
├── Models/            # Modèles de données (Invoice, Customer, Payment…)
├── Services/          # Couche API + services métier
├── ViewModels/        # MVVM avec CommunityToolkit.Mvvm
├── Views/             # Pages XAML par module
│   ├── Auth/
│   ├── Invoices/
│   ├── Customers/
│   ├── Payments/
│   ├── Expenses/
│   └── More/
├── Converters/        # Value converters XAML
└── Resources/
    ├── Fonts/         # Poppins (Regular, Medium, SemiBold, Bold, Light)
    ├── Images/
    └── Styles/        # Colors.xaml + Styles.xaml
```

## 🛠️ Technologies

- **.NET 10** — Framework cible
- **MAUI** — UI cross-platform (Android, iOS, macOS, Windows)
- **CommunityToolkit.Maui** — Composants supplémentaires
- **CommunityToolkit.Mvvm** — MVVM source generators
- **System.Text.Json** — Sérialisation JSON

## ⚙️ Prérequis

- .NET 10 SDK
- Visual Studio 2022 (17.12+) avec workload MAUI, ou Rider
- Pour iOS : macOS avec Xcode 16+
- Pour Android : Android SDK (API 21+)

## 🚀 Installation

```bash
git clone https://github.com/FrApp42/InvoiceShelfMobile.git
cd InvoiceShelfMobile
dotnet restore
```

### Android
```bash
dotnet build -f net10.0-android -c Debug
dotnet run -f net10.0-android
```

### iOS
```bash
dotnet build -f net10.0-ios -c Debug
dotnet run -f net10.0-ios
```

## 📡 Configuration

Au premier démarrage, entrez l'URL de votre serveur InvoiceShelf (ex. `https://invoices.monentreprise.com`).

L'application se connecte ensuite à l'API REST de votre instance InvoiceShelf via `/api/v1/`.

## 📄 Licence

AGPL-3.0 — voir [LICENSE](./LICENSE) (identique au projet source InvoiceShelf).
