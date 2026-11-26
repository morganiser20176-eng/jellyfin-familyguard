# Privacy notice / Notice de confidentialité

FamilyGuard is designed to run entirely inside a Jellyfin server.

- The plugin makes no outbound network request.
- It does not collect telemetry or usage analytics.
- It does not transmit media files, images, titles, ratings, genres, user accounts, or decision logs.
- It stores its profiles, rules, and up to 500 recent decisions in Jellyfin's plugin configuration on the local server.
- Administrators are responsible for their server backups, user access, and the tags chosen for protected profiles.

FamilyGuard only reads Jellyfin library metadata needed for the configured review queue or a manual scan. It writes only Jellyfin tags and its local decision log. Removing the plugin does not remove tags previously added to media.

## Français

FamilyGuard fonctionne entièrement dans le serveur Jellyfin.

- Le plugin n'effectue aucune requête réseau sortante.
- Il ne collecte aucune télémétrie ni analyse d'usage.
- Il ne transmet aucun fichier média, image, titre, classement, genre, compte utilisateur ni journal de décision.
- Il conserve ses profils, règles et au plus 500 décisions récentes dans la configuration locale du plugin Jellyfin.
- Les administrateurs restent responsables des sauvegardes, des accès utilisateurs et des étiquettes affectées aux profils protégés.

FamilyGuard lit uniquement les métadonnées nécessaires à la file de revue ou à une analyse manuelle configurée. Il écrit uniquement des étiquettes Jellyfin et son journal local. La suppression du plugin ne retire pas les étiquettes déjà ajoutées aux médias.
