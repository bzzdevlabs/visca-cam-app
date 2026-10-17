using System;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.Core.Localization;

/// <summary>
/// Every text the user sees, in English and French side by side. The language is chosen once at
/// start-up, before the windows are built; changing it takes a restart. The log and the debug
/// console stay in English, so that a log reads the same whoever sends it.
/// </summary>
public static class Strings
{
    public static AppLanguage Language { get; set; } = AppLanguage.French;

    /// <summary>The language's own name, as listed in the settings.</summary>
    public static string NameOf(AppLanguage language) => language == AppLanguage.English ? "English" : "Français";

    /// <summary>
    /// Picks the text of the current language. French puts a non-breaking space before
    /// <c>? : ! ;</c>, so these never wrap to a line of their own.
    /// </summary>
    private static string T(string english, string french) =>
        Language == AppLanguage.English
            ? english
            : french.Replace(" ?", " ?").Replace(" :", " :").Replace(" !", " !").Replace(" ;", " ;");

    // Status line and prompts (presenter).
    public static string ChooseCameraThenConnect => T("Choose the camera and how to control it, then press Connect.", "Choisissez la caméra et le mode de contrôle, puis cliquez sur Connecter.");
    public static string CouldNotListDevices(string error) => T("Could not list devices: ", "Impossible de lister les appareils : ") + error;
    public static string ConnectionFailed(string error) => T("Connection failed: ", "Échec de la connexion : ") + error;
    public static string Connected(string description, bool preview) =>
        T("Connected: ", "Connecté : ") + description
        + (preview ? "." : T(". Preview off: other apps such as Zoom can use the camera.", ". Aperçu désactivé : d'autres applications comme Zoom peuvent utiliser la caméra."));
    public static string Disconnected => T("Disconnected.", "Déconnecté.");
    public static string CouldNotSaveSettings(string error) => T("Could not save settings: ", "Impossible d'enregistrer les paramètres : ") + error;
    public static string CouldNotSavePresets(string error) => T("Could not save presets: ", "Impossible d'enregistrer les positions : ") + error;
    public static string NotConnected => T("Not connected. Press Connect first.", "Non connecté. Cliquez d'abord sur Connecter.");
    public static string NoVideo(string controller) => controller + T(", no video", ", sans vidéo");

    public static string ConnectBeforeSavingPreset => T("Connect to the camera before saving a preset.", "Connectez la caméra avant d'enregistrer une position.");
    public static string NewPresetTitle => T("New preset", "Nouvelle position");
    public static string NewPresetPrompt => T("Name of the current camera position:", "Nom de la position actuelle de la caméra :");
    public static string DefaultPresetName(int number) => T("Preset ", "Position ") + number;
    public static string SavedPreset(string name) => T($"Saved preset \"{name}\".", $"Position « {name} » enregistrée.");
    public static string GoingToPreset(string name) => T($"Going to \"{name}\".", $"Vers « {name} ».");
    public static string NoPresetNumber(int number) => T($"No preset #{number}.", $"Aucune position n° {number}.");
    public static string ReplacePreset(string name) => T($"Replace \"{name}\" with the current camera position?", $"Remplacer « {name} » par la position actuelle de la caméra ?");
    public static string UpdatedPreset(string name) => T($"Updated preset \"{name}\".", $"Position « {name} » mise à jour.");
    public static string RenamePresetTitle => T("Rename preset", "Renommer la position");
    public static string RenamePresetPrompt => T("New name:", "Nouveau nom :");
    public static string RenamedPreset(string name) => T($"Renamed preset to \"{name}\".", $"Position renommée en « {name} ».");
    public static string DeletePreset(string name) => T($"Delete preset \"{name}\"?", $"Supprimer la position « {name} » ?");
    public static string DeletedPreset(string name) => T($"Deleted preset \"{name}\".", $"Position « {name} » supprimée.");
    public static string PresetNameRequired => T("A preset name is required.", "Le nom de la position est obligatoire.");
    public static string PresetSlotsFull(int count) => T($"All {count} preset slots are in use.", $"Les {count} emplacements de position sont tous utilisés.");
    public static string PresetSavedInVisca(string name) =>
        T($"Preset \"{name}\" was saved in VISCA mode; save it again in UVC mode to use it here.",
          $"La position « {name} » a été enregistrée en mode VISCA ; enregistrez-la de nouveau en mode UVC pour l'utiliser ici.");

    public static string SettingsSaved => T("Settings saved.", "Paramètres enregistrés.");
    public static string ConfirmReset =>
        T("Restore every setting to its default? This disconnects the camera and resets the camera choice, speed, keys, theme, language and other options. Presets are kept.",
          "Rétablir tous les paramètres par défaut ? La caméra est déconnectée, et le choix de la caméra, la vitesse, les touches, le thème, la langue et les autres options sont réinitialisés. Les positions enregistrées sont conservées.");
    public static string ResetDone => T("All settings were reset to their defaults. Presets were kept.", "Tous les paramètres ont été réinitialisés. Les positions enregistrées sont conservées.");
    public static string RestartForLanguage(AppLanguage language) =>
        language == AppLanguage.English
            ? T("Restart Tenveo PTZ now to show it in English?", "Redémarrer Tenveo PTZ maintenant pour l'afficher en anglais ?")
            : T("Restart Tenveo PTZ now to show it in French?", "Redémarrer Tenveo PTZ maintenant pour l'afficher en français ?");
    public static string LanguageAtNextStart => T("Settings saved. The new language shows the next time Tenveo PTZ starts.", "Paramètres enregistrés. La nouvelle langue s'affichera au prochain démarrage de Tenveo PTZ.");

    public static string UpdateAvailableMessage(Version version, Version current) =>
        T($"Version {version} of Tenveo PTZ is available (you have {current}). Download and install it now? The app restarts when it is done.",
          $"La version {version} de Tenveo PTZ est disponible (vous avez la {current}). La télécharger et l'installer maintenant ? L'application redémarre une fois l'installation terminée.");
    public static string DownloadingVersion(Version version) => T($"Downloading version {version}...", $"Téléchargement de la version {version}...");
    public static string UpdateFailed(string error, Uri page) =>
        T($"Update failed: {error} You can download it from {page}", $"Échec de la mise à jour : {error} Vous pouvez la télécharger depuis {page}");
    public static string UpdateNotWindowsProgram => T("The downloaded update is not a Windows program.", "La mise à jour téléchargée n'est pas un programme Windows.");
    public static string UpdateHasNoApplication => T("The downloaded update does not contain the application.", "La mise à jour téléchargée ne contient pas l'application.");
    public static string MoveToApplicationsFirst => T("Move Tenveo PTZ to the Applications folder first: it cannot update itself where it is.", "Déplacez d'abord Tenveo PTZ dans le dossier Applications : il ne peut pas se mettre à jour là où il se trouve.");

    // Camera sessions.
    public static string VideoDeviceNotConnected(string name) => T($"Video device \"{name}\" is not connected.", $"L'appareil vidéo « {name} » n'est pas branché.");
    public static string VideoDeviceDisconnected => T("The video device was disconnected.", "L'appareil vidéo a été débranché.");
    public static string SelectComPort => T("Select the COM port the camera's VISCA cable is connected to.", "Choisissez le port COM auquel est relié le câble VISCA de la caméra.");
    public static string UvcNeedsVideoDevice => T("UVC control needs the camera's video device: select it in the Camera list.", "Le contrôle UVC a besoin de l'appareil vidéo de la caméra : choisissez-le dans la liste des caméras.");
    public static string NoUvcControls => T("This video device does not expose UVC pan/tilt/zoom controls. Use VISCA instead.", "Cet appareil vidéo ne propose pas de commandes UVC de rotation, d'inclinaison et de zoom. Utilisez plutôt VISCA.");
    public static string NoUsbControl(string axis) => T($"This camera does not expose {axis} control over USB.", $"Cette caméra ne propose pas de commande de {axis} en USB.");
    public static string AxisName(string axis) => axis switch
    {
        "Pan" => T("pan", "rotation"),
        "Tilt" => T("tilt", "inclinaison"),
        "Zoom" => T("zoom", "zoom"),
        "Focus" => T("focus", "mise au point"),
        _ => axis.ToLowerInvariant(),
    };
    public static string CameraInUse(string name) => T($"\"{name}\" is in use by another application.", $"« {name} » est utilisée par une autre application.");
    public static string CannotOpenCamera(string name, string? error) => T($"Cannot open \"{name}\": {error ?? "unknown error"}.", $"Impossible d'ouvrir « {name} » : {error ?? "erreur inconnue"}.");
    public static string CameraAccessDenied => T("Tenveo PTZ may not use the camera. Allow it in System Settings > Privacy & Security > Camera, then restart the app.", "Tenveo PTZ n'a pas accès à la caméra. Autorisez-le dans Réglages Système > Confidentialité et sécurité > Caméra, puis redémarrez l'application.");
    public static string AllowCameraThenConnect => T("Allow Tenveo PTZ to use the camera, then press Connect again.", "Autorisez Tenveo PTZ à utiliser la caméra, puis cliquez de nouveau sur Connecter.");
    public static string CannotWatchKeypad(int error) => T($"Cannot watch the numeric keypad (Windows error {error}).", $"Impossible de surveiller le pavé numérique (erreur Windows {error}).");
    public static string CameraRejectedCommand(string code) => T($"The camera rejected the command (IOKit error {code}).", $"La caméra a refusé la commande (erreur IOKit {code}).");

    // Main window.
    public static string NoVideoTitle => T("No video", "Pas de vidéo");
    public static string NoVideoHint => T("Choose the camera above, then select Connect.", "Choisissez la caméra ci-dessus, puis cliquez sur Connecter.");
    public static string PreviewOffTitle => T("Preview off", "Aperçu désactivé");
    public static string PreviewOffHint => T("The camera is free for other apps such as Zoom. Turn Preview on to see it here.", "La caméra est libre pour d'autres applications comme Zoom. Activez Aperçu pour la voir ici.");
    public static string NoCameraFound => T("No camera found", "Aucune caméra trouvée");
    public static string ControlPlaceholder => T("Control", "Contrôle");
    public static string NoComPort => T("No COM port", "Aucun port COM");
    public static string NoSerialPort => T("No serial port", "Aucun port série");
    public static string BaudRatePlaceholder => T("Baud rate", "Débit");
    public static string AddressPlaceholder => T("Address", "Adresse");
    public static string Baud(int rate) => T($"{rate} baud", $"{rate} bauds");
    public static string Address(int address) => T("Address ", "Adresse ") + address;
    public static string UsbControl => T("USB control (UVC)", "Contrôle USB (UVC)");
    public static string SerialControl => T("Serial control (VISCA)", "Contrôle série (VISCA)");
    public static string Connect => T("Connect", "Connecter");
    public static string Disconnect => T("Disconnect", "Déconnecter");
    public static string Preview => T("Preview", "Aperçu");
    public static string Settings => T("Settings", "Paramètres");
    public static string VideoDeviceTip => T("Video device used for the live preview", "Appareil vidéo utilisé pour l'aperçu en direct");
    public static string ModeTip => T("USB: control through the camera's USB cable. Serial: VISCA through an RS-232/RS-485 cable.", "USB : contrôle par le câble USB de la caméra. Série : VISCA par un câble RS-232/RS-485.");
    public static string ComPortTip => T("COM port of the VISCA serial cable or adapter", "Port COM du câble ou de l'adaptateur série VISCA");
    public static string SerialPortTip => T("Serial port of the VISCA cable or adapter", "Port série du câble ou de l'adaptateur VISCA");
    public static string BaudRateTip => T("VISCA baud rate (must match the camera, usually 9600)", "Débit VISCA (identique à celui de la caméra, en général 9600)");
    public static string AddressTip => T("VISCA camera address (usually 1)", "Adresse VISCA de la caméra (en général 1)");
    public static string PreviewTip => T("Show the camera's video here. Turn off to use the camera in another app (Zoom, Teams...) while controlling it from here.", "Affiche ici la vidéo de la caméra. Désactivez-le pour utiliser la caméra dans une autre application (Zoom, Teams...) tout en la pilotant d'ici.");
    public static string PinTip => T("Keep this window on top of other windows", "Garder cette fenêtre au-dessus des autres");
    public static string KeepOnTop => T("Keep on top", "Garder au premier plan");
    public static string SettingsTip(string shortcut) => T($"Settings ({shortcut})", $"Paramètres ({shortcut})");
    public static string ConnectTip => T("Open the video and camera control", "Ouvrir la vidéo et le contrôle de la caméra");
    public static string RefreshTip => T("Refresh the device lists", "Actualiser les listes d'appareils");

    public static string PanAndTilt => T("Pan and tilt", "Orientation");
    public static string Speed => T("Speed", "Vitesse");
    public static string Precise => T("Precise", "Précis");
    public static string PadTip => T("Hold a direction to move (or use the arrow keys). Centre: home position.", "Maintenez une direction pour déplacer la caméra (ou utilisez les flèches). Centre : position d'origine.");
    public static string SpeedTip => T("Pan, tilt and zoom speed", "Vitesse de rotation, d'inclinaison et de zoom");
    public static string PreciseTip => T("Much slower moves for fine framing (or hold Shift)", "Mouvements beaucoup plus lents pour un cadrage fin (ou maintenez Maj)");

    public static string ZoomAndFocus => T("Zoom and focus", "Zoom et mise au point");
    public static string Zoom => T("Zoom", "Zoom");
    public static string Autofocus => T("Autofocus", "Mise au point auto");
    public static string AutofocusTip => T("Let the camera focus by itself", "Laisser la caméra faire la mise au point");
    public static string Near => T("Near", "Près");
    public static string Far => T("Far", "Loin");
    public static string ZoomOutTip => T("Zoom out (hold, or - / Page Down)", "Zoom arrière (maintenir, ou - / Page suivante)");
    public static string ZoomInTip => T("Zoom in (hold, or + / Page Up)", "Zoom avant (maintenir, ou + / Page précédente)");
    public static string FocusNearTip => T("Focus nearer (hold)", "Mise au point plus près (maintenir)");
    public static string FocusFarTip => T("Focus farther (hold)", "Mise au point plus loin (maintenir)");

    public static string Presets => T("Presets", "Positions");
    public static string NewPreset => T("New", "Nouvelle");
    public static string NoPresetsYet => T("No presets yet. Frame a shot, then select \"New\".", "Aucune position pour l'instant. Cadrez un plan, puis cliquez sur « Nouvelle ».");
    public static string NewPresetTip => T("Save the current camera position as a new preset", "Enregistrer la position actuelle de la caméra comme nouvelle position");
    public static string RecallPresetTip(string enterKey) => T($"Go to the selected preset (double-click, {enterKey} or 1-9)", $"Aller à la position sélectionnée (double-clic, {enterKey} ou 1-9)");
    public static string OverwritePresetTip => T("Replace the selected preset with the current position", "Remplacer la position sélectionnée par la position actuelle");
    public static string RenamePresetTip => T("Rename the selected preset", "Renommer la position sélectionnée");
    public static string DeletePresetTip => T("Delete the selected preset", "Supprimer la position sélectionnée");
    public static string MoveUpTip => T("Move up (changes its number key)", "Monter (change sa touche numérique)");
    public static string MoveDownTip => T("Move down (changes its number key)", "Descendre (change sa touche numérique)");
    public static string EnterKey => T("Enter", "Entrée");
    public static string ReturnKey => T("Return", "Retour");

    // Dialogs.
    public static string AreYouSure => T("Are you sure?", "Confirmer ?");
    public static string Yes => T("Yes", "Oui");
    public static string Ok => T("OK", "OK");
    public static string Save => T("Save", "Enregistrer");
    public static string Cancel => T("Cancel", "Annuler");
    public static string UpdateAvailable => T("Update available", "Mise à jour disponible");
    public static string Update => T("Update", "Mettre à jour");
    public static string NotNow => T("Not now", "Plus tard");
    public static string UnexpectedErrorTitle => T("Tenveo PTZ - unexpected error", "Tenveo PTZ - erreur inattendue");
    public static string DetailsInLog => T("Details were written to the log (Ctrl+Shift+D opens the debug console).", "Les détails ont été écrits dans le journal (Ctrl+Maj+D ouvre la console de débogage).");

    // Settings panel.
    public static string Camera => T("Camera", "Caméra");
    public static string ReverseLeftRight => T("Reverse left and right", "Inverser gauche et droite");
    public static string ReverseUpDown => T("Reverse up and down", "Inverser haut et bas");
    public static string ContinuousUsbMoves => T("Continuous USB moves (experimental)", "Mouvements USB continus (expérimental)");
    public static string ContinuousUsbMovesNote =>
        T("USB (UVC) only: smoother moves using the camera's own motion. Some cameras ignore it or report wrong positions afterwards; turn it off if the controls stop working or presets come back wrong.",
          "USB (UVC) uniquement : mouvements plus fluides grâce au moteur de la caméra. Certaines caméras l'ignorent ou indiquent ensuite de mauvaises positions ; désactivez-le si les commandes ne répondent plus ou si les positions reviennent mal.");
    public static string Keyboard => T("Keyboard", "Clavier");
    public static string BackgroundNumpad => T("Numeric keypad works while another app is in front", "Pavé numérique actif quand une autre application est au premier plan");
    public static string BackgroundNumpadNote =>
        T("Windows takes the keypad keys from the app in front while the camera is connected. Num Lock off: 8 4 6 2 move, 7 home, 9 3 zoom. Num Lock on: 1-9 presets. + and - zoom.",
          "Windows prend les touches du pavé à l'application au premier plan tant que la caméra est connectée. Verr. Num désactivé : 8 4 6 2 déplacent, 7 origine, 9 3 zoom. Verr. Num activé : 1-9 positions. + et - zoom.");
    public static string Window => T("Window", "Fenêtre");
    public static string KeepWindowOnTop => T("Keep the window on top of other windows", "Garder la fenêtre au-dessus des autres");
    public static string Theme => T("Theme", "Thème");
    public static string ThemeSameAsSystem(string system) => T("Same as ", "Comme ") + system;
    public static string ThemeLight => T("Light", "Clair");
    public static string ThemeDark => T("Dark", "Sombre");
    public static string LanguageLabel => T("Language", "Langue");
    public static string LanguageNote => T("Applies when Tenveo PTZ restarts.", "S'applique au redémarrage de Tenveo PTZ.");
    public static string Updates => T("Updates", "Mises à jour");
    public static string CheckForUpdates => T("Look for a new version on start-up", "Rechercher une nouvelle version au démarrage");
    public static string SettingsAndPresets => T("Settings and presets", "Paramètres et positions");
    public static string StoredIn(string folder) => T($"Stored in {folder}.", $"Enregistrés dans {folder}.");
    public static string OpenFolder => T("Open folder", "Ouvrir le dossier");
    public static string ResetAllSettings => T("Reset all settings...", "Tout réinitialiser...");

    // macOS menus and settings.
    public static string About => T("About Tenveo PTZ", "À propos de Tenveo PTZ");
    public static string SettingsMac => T("Settings", "Réglages");
    public static string SettingsMenu => T("Settings...", "Réglages...");
    public static string Hide => T("Hide Tenveo PTZ", "Masquer Tenveo PTZ");
    public static string HideOthers => T("Hide Others", "Masquer les autres");
    public static string ShowAll => T("Show All", "Tout afficher");
    public static string Quit => T("Quit Tenveo PTZ", "Quitter Tenveo PTZ");
    public static string Edit => T("Edit", "Édition");
    public static string Undo => T("Undo", "Annuler");
    public static string Redo => T("Redo", "Rétablir");
    public static string Cut => T("Cut", "Couper");
    public static string Copy => T("Copy", "Copier");
    public static string Paste => T("Paste", "Coller");
    public static string SelectAll => T("Select All", "Tout sélectionner");
    public static string Minimize => T("Minimize", "Placer dans le Dock");
    public static string ZoomWindow => T("Zoom", "Réduire/agrandir");
    public static string Close => T("Close", "Fermer");
    public static string SettingsKeptIn(string folder) => T($"Settings and presets are kept in {folder}.", $"Les réglages et les positions sont enregistrés dans {folder}.");
    public static string ResetAllSettingsMac => T("Reset All Settings...", "Tout réinitialiser...");
    public static string ThemeMenuItem(string theme) => T("Theme: ", "Thème : ") + theme;
    public static string LanguageMenuItem(string language) => T("Language: ", "Langue : ") + language;
}
