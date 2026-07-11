# GitHub Actions — Invoice Shelf Mobile

## Secrets requis

Pour que le workflow `release.yml` fonctionne, les secrets suivants doivent
être configurés dans **Settings → Secrets and variables → Actions** :

| Secret | Description |
|--------|-------------|
| `KEYSTORE_BASE64` | Keystore Android encodé en base64 |
| `KEYSTORE_PASSWORD` | Mot de passe du keystore |
| `KEY_ALIAS` | Alias de la clé dans le keystore |
| `KEY_PASSWORD` | Mot de passe de la clé |

### Générer le keystore et l'encoder

```bash
# 1. Générer le keystore (à faire une seule fois)
keytool -genkey -v \
  -keystore invoiceshelf.keystore \
  -alias invoiceshelf \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000

# 2. Encoder en base64 pour le secret GitHub
base64 -w 0 invoiceshelf.keystore
```

Coller la sortie de la commande `base64` dans le secret `KEYSTORE_BASE64`.

## Workflows

### `build.yml` — Build de vérification

- **Déclencheur :** push ou PR sur `main` et `dev/**`
- **Action :** compile en Debug, upload l'APK 7 jours
- **But :** vérifier que le code compile à chaque modification

### `release.yml` — Release de production

- **Déclencheur :** création d'un tag `v*.*.*`
- **Action :** compile en Release, signe l'APK, crée une GitHub Release

```bash
# Créer une release
git tag v1.0.0
git push origin v1.0.0
```

> Un tag contenant un `-` (ex: `v1.0.0-beta.1`) sera marqué
> automatiquement comme **pre-release**.
