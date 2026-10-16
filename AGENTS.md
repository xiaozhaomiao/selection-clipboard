# Public edition maintenance

This repository contains only the offline public edition. Do not add account, authentication, registration, cloud sync, telemetry, network update checks, production endpoints, credentials, user data, private backups, or private operational notes.
Keep the public edition independently buildable. Use its own data directory and process mutex. Fixes shared with a separate personal edition must be explicitly ported and checked; never copy private directories wholesale.
Run 构建.ps1 after changes and relevant persistence regression checks when storage changes.
