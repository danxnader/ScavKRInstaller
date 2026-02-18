# ScavKRInstaller Fork

## Francais

Ce projet est un fork de [danxnader/ScavKRInstaller](https://github.com/danxnader/ScavKRInstaller).

### Ce qui change par rapport a la version officielle

- Suppression du GUI de l'installeur: execution 100% automatique des le lancement.
- Dossier d'installation force: `C:\Users\<user>\Downloads\scavMULTI`.
- Structure de fichiers aplatit au maximum pour correspondre a `CasualtiesUnknownDemo` (pas de sous-dossiers inutiles).
- Telechargements techniques dans `C:\Users\<user>\Downloads\ScavKRInstaller` puis suppression automatique des fichiers `.zip` et temporaires en fin d'installation.
- Installation automatique de:
  - le jeu (archive demo),
  - BepInEx,
  - le mod multijoueur,
  - ChangeSkin.
- Patch automatique de `BepInEx\plugins\KrokoshaCasualtiesMP.dll`:
  - texte du menu principal personnalise (FR + EN),
  - valeurs de connexion par defaut:
    - serveur: `26.35.34.177:7790`
    - nom joueur: `grosFemboyFurry`
    - mot de passe: vide
- Lancement automatique du jeu apres installation avec auto-connexion.
- Creation d'un script local `Launch_AutoConnect.bat` dans le dossier du jeu.
- Creation d'un raccourci bureau `scavMULTI.lnk` pour connexion rapide guest.
  - Le launcher lance une connexion client directe avec le nom `grosFemboyFurry`.
- Installation automatique d'une alternative open source a Radmin VPN:
  - OpenVPN Community (installation silencieuse).
  - Le launcher demarre OpenVPN en meme temps que le jeu et le ferme a la fermeture du jeu.
  - Fichiers generes dans le jeu:
    - `VPN_GUEST_INFO.txt` (adresse `26.35.34.177`, mot de passe `123`)
    - `vpn\credentials.txt` (guest/123)
    - `vpn\guest.ovpn.template` (template de profil)

### Notes

- L'installation OpenVPN depend des droits machine (admin Windows).
- Ce fork est oriente usage automatise, pas interface utilisateur.

## English

This project is a fork of [danxnader/ScavKRInstaller](https://github.com/danxnader/ScavKRInstaller).

### What changed compared to the official version

- Installer GUI removed: fully automatic execution on launch.
- Forced install directory: `C:\Users\<user>\Downloads\scavMULTI`.
- Folder layout is flattened as much as possible to match `CasualtiesUnknownDemo` (no unnecessary nested folders).
- Technical downloads are stored in `C:\Users\<user>\Downloads\ScavKRInstaller`, then `.zip` and temp files are deleted automatically at the end.
- Automatic installation of:
  - game demo archive,
  - BepInEx,
  - multiplayer mod,
  - ChangeSkin.
- Automatic patching of `BepInEx\plugins\KrokoshaCasualtiesMP.dll`:
  - custom main menu text (FR + EN),
  - default connection values:
    - server: `26.35.34.177:7790`
    - player name: `grosFemboyFurry`
    - password: empty (guests choose their own)
- Game is launched automatically after install with auto-connect arguments.
- Creates `Launch_AutoConnect.bat` in the game folder.
- Creates desktop shortcut `scavMULTI.lnk` for quick guest connection.
  - The launcher starts direct client connection with name `grosFemboyFurry`.
- Automatically installs an open-source alternative to Radmin VPN:
  - OpenVPN Community (silent install).
  - The launcher starts OpenVPN together with the game and stops it when the game closes.
  - Generated game files:
    - `VPN_GUEST_INFO.txt` (address `26.35.34.177`, password `123`)
    - `vpn\credentials.txt` (guest/123)
    - `vpn\guest.ovpn.template` (profile template)

### Notes

- OpenVPN installation depends on machine privileges (Windows admin rights).
- This fork is focused on automation, not interactive UI.
