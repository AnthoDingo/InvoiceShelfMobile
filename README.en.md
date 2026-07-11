# InvoiceShelf Mobile — .NET 10 MAUI

🇫🇷 [Version française](./README.md)

Cross-platform mobile app for [InvoiceShelf](https://invoiceshelf.com).

## 📱 Features

| Module | Description |
|--------|-------------|
| **Authentication** | Login, forgot password, server configuration |
| **Invoices** | List, create, edit, delete, status changes |
| **Customers** | Full management with billing/shipping addresses |
| **Payments** | Recording and tracking payments |
| **Expenses** | Expense tracking by category |
| **Items** | Product/service catalog |
| **Taxes** | Tax types (simple, compound, grouped) |
| **Account & Company** | User settings and company information |

## 🏗️ Architecture

```
src/InvoiceShelf/
├── Models/            # Data models (Invoice, Customer, Payment…)
├── Services/          # API layer + business services
├── ViewModels/        # MVVM with CommunityToolkit.Mvvm
├── Views/             # XAML pages per module
│   ├── Auth/
│   ├── Invoices/
│   ├── Customers/
│   ├── Payments/
│   ├── Expenses/
│   └── More/
├── Converters/        # XAML value converters
└── Resources/
    ├── Fonts/         # Poppins (Regular, Medium, SemiBold, Bold, Light)
    ├── Images/
    └── Styles/        # Colors.xaml + Styles.xaml
```

## 🛠️ Technologies

- **.NET 10** — Target framework
- **MAUI** — Cross-platform UI (Android, iOS, macOS, Windows)
- **CommunityToolkit.Maui** — Additional components
- **CommunityToolkit.Mvvm** — MVVM source generators
- **System.Text.Json** — JSON serialization

## ⚙️ Prerequisites

- .NET 10 SDK
- Visual Studio 2022 (17.12+) with the MAUI workload, or Rider
- For iOS: macOS with Xcode 16+
- For Android: Android SDK (API 21+)

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

On first launch, enter the URL of your InvoiceShelf server (e.g. `https://invoices.mycompany.com`).

The app then connects to your instance's REST API via `/api/v1/`.

## 📄 License

AGPL-3.0 — see [LICENSE](./LICENSE) (same as the upstream InvoiceShelf project).
