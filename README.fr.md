# FamilyGuard pour Jellyfin

[English](README.md) · Français

FamilyGuard est un flux local de validation parentale pour Jellyfin. Il permet à un administrateur de décider quels films et séries sont accessibles à chaque profil protégé, sans déplacer, supprimer, téléverser ou analyser les fichiers médias.

## Fonctionnalités

- Plusieurs profils protégés, chacun avec une étiquette autorisée et une étiquette « à vérifier ».
- Validation ou refus par lot depuis une file de revue.
- Règles prudentes, facultatives et déclenchées manuellement pour les médias récemment ajoutés.
- Journal local des 500 dernières décisions de marquage.
- Raccourci FamilyGuard dans l’administration Jellyfin.

FamilyGuard complète le contrôle parental natif de Jellyfin. Pour chaque compte enfant, conservez les restrictions natives et n'autorisez que l'**étiquette autorisée** configurée pour son profil.

## Confidentialité et sécurité

- Aucun compte cloud, télémétrie, analyse, API externe ou classifieur distant.
- Aucun fichier média ni métadonnée de bibliothèque n'est transmis hors du serveur Jellyfin.
- FamilyGuard ne déplace, ne renomme et ne supprime aucun fichier média.
- Le plugin ne modifie que les étiquettes Jellyfin et sa propre configuration locale.
- Les décisions exigent une session Jellyfin administrateur avec élévation.

Consultez la [notice de confidentialité](PRIVACY.md) et la [politique de sécurité](SECURITY.md) avant l'utilisation pour un profil familial.

## Installation

Lorsqu'une version publique sera disponible, ajoutez cette adresse dans **Tableau de bord → Plugins → Gérer les dépôts → Ajouter** :

```text
https://github.com/VOTRE_COMPTE_GITHUB/jellyfin-familyguard/releases/latest/download/manifest.json
```

Redémarrez Jellyfin si demandé, puis ouvrez **Tableau de bord → Plugins → FamilyGuard**.

## Première configuration

1. Créez un profil FamilyGuard avec son étiquette autorisée et son étiquette à vérifier.
2. Dans le contrôle parental natif de Jellyfin, restreignez le compte enfant à l'étiquette autorisée.
3. Marquez les médias incertains avec l'étiquette à vérifier, ou utilisez d'abord **Prévisualiser** avant d'appliquer les règles manuelles.
4. Autorisez ou refusez la file de revue avec un compte administrateur.

## Compatibilité

La première version publique cible Jellyfin `10.11.11` et .NET 9. Les plugins Jellyfin dépendent de l'ABI : n'installez pas cette version sur un serveur en dehors de la plage déclarée.

## Licence

FamilyGuard est distribué sous [GNU GPL v3.0](LICENSE).
