[<AutoOpen>]
module DnnConfig

open System
open System.IO
open System.Text.RegularExpressions
open FSharp.Configuration
open canopy
open canopy.configuration
open reporters

//===================================================
// The file entries define test site parameters
//===================================================
type SiteConfig = YamlConfig< "Config.yaml" >

let config = SiteConfig() // defines the type and its properties

config.Load( Path.Combine(exeLocation, "Config.yaml") ) // loads config from runner location

let siteDomain =
    let s = config.Site.SiteAlias.ToLower()
    if s.EndsWith("/") then s.Substring(0, s.Length) else s

let root = "http://" + siteDomain
let portalAlias = Uri(root).DnsSafeHost
let isRemoteSite : bool = config.Site.IsRemoteSite
let hostUsername = Regex.Replace(config.Site.HostUserName, @"[^0-9A-Za-z.-]", "")
let defaultPassword = config.Site.DefaultPassword.Replace(" ", "")
let hostDisplayName = config.Site.HostDisplayName

let installationLanguage : InstallationLanguageas = 
    match config.Site.InstallationLanguage.ToUpperInvariant() with
    | "ENGLISH" -> English
    | "GERMAN" -> German
    | "SPANISH" -> Spanish
    | "FRENCH" -> French
    | "ITALIAN" -> Italian
    | "DUTCH" -> Dutch
    | _ -> failwithf "Unknown installation language: %A" config.Site.InstallationLanguage

// this should be set at the beginning to define what we are testing
let siteSettings = ProductSettings().init ""

if config.Settings.DevMode then canopy.configuration.wipSleep <- 0.2

// canopy configurations (timeouts and others as set in from YAML configuration file)
canopy.configuration.elementTimeout <- if isRemoteSite then (float config.Settings.ElementTimeout.Remote)
                                       else (float config.Settings.ElementTimeout.Local)
canopy.configuration.compareTimeout <- if isRemoteSite then (float config.Settings.CompareTimeout.Remote)
                                       else (float config.Settings.CompareTimeout.Local)
canopy.configuration.pageTimeout <- if isRemoteSite then (float config.Settings.PageTimeout.Remote)
                                    else (float config.Settings.PageTimeout.Local)
canopy.configuration.autoPinBrowserRightOnLaunch <- false
canopy.configuration.optimizeByDisablingCoverageReport <- true
canopy.configuration.failScreenshotPath <- DirectoryInfo(exeLocation + @"\..\ScreenShots").FullName
canopy.configuration.disableSuggestOtherSelectors <- config.Settings.HideSuggestedSelectors
canopy.configuration.failureScreenshotsEnabled <- not config.Settings.DontCaptureImages

let FramerowkLogFilesFolder = DirectoryInfo(exeLocation + @"\..\LogFiles").FullName
// the following values represent seconds (added as real numbers)
let waitForInstallProgressToAppear = 
    if isRemoteSite then (float config.Settings.WaitForInstallProgressToAppear.Remote)
    else (float config.Settings.WaitForInstallProgressToAppear.Local)

let waitForInstallProgressToFinish = 
    if isRemoteSite then (float config.Settings.WaitForInstallProgressToFinish.Remote)
    else (float config.Settings.WaitForInstallProgressToFinish.Local)

let childSiteCreationTimeout = 
    if isRemoteSite then (float config.Settings.WaitForChildSiteCreation.Remote)
    else (float config.Settings.WaitForChildSiteCreation.Local)

let waitForPageCreation = 
    if isRemoteSite then (float config.Settings.WaitForPageCreation.Remote)
    else (float config.Settings.WaitForPageCreation.Local)

let hostEmail = hostUsername + "@test.com"
let parentWebSiteName = "Automation Main WebSite"
let childWebSiteName = "Automation Child WebSite"
// set this to anything before calling the tests to experiment with various names
let mutable childSiteAlias = config.Site.ChildSitePrefix // "childsite"
let mutable useChildPortal = false
let mutable glbDbQualifier = config.Site.DevQualifier

let userAccountsPage = "/Admin/User-Accounts"
let adminsRoleName = siteSettings.specialRoles.Head
// these are the selectors that are applicable to all products
let popupFormSelector = "div.dnnFormPopup.ui-resizable"
let SkinMsgErrorSelector = 
    "//div[contains(@id,'_dnnSkinMessage') and (@class='dnnFormMessage dnnFormValidationSummary')]"

//open and close edit mode selectors
let editBar = "div#edit-bar"
let publishButton = "div#edit-bar>div.right-section>div>button[data-bind*='button_Publish']"
let closeButton = "div#edit-bar>div.right-section>ul>li>button"
let closedEditMode = "//body[@id='Body' and not(contains(@class,'dnnEditState'))]"
let closeBtnPB = "#showsite"

//persona bar selectors
let addSiteBtn = "div#sites-container>div>div>div>div>div>div>button"
let installExtBtn = "div.extensions-app>div>div>div>div>button[role='primary']"
let createRoleBtn = "div.roles-app>div>div>div>button"
let customCssEditor = "div.cssForm>div>div.CodeMirror"
let addPageBtn = "div#pages-container>div.pages-app>div>div>div>button[role='primary']"

//DnnPrompt selectors
let promptScreen = "div.dnn-prompt"
let promptInput = "div.dnn-prompt>div>input.dnn-prompt-input"
let promptOkMsg = "span.dnn-prompt-ok"
let promptOkMsgLast = "span.dnn-prompt-ok:last-of-type"
let promptErrorMsg = "span.dnn-prompt-error"
let promptOutputTable = "table.dnn-prompt-tbl>tbody"
let promptOutputs = "div.dnn-prompt>div.dnn-prompt-output>*"
let promptOutputCmd = "div.dnn-prompt-output>span.dnn-prompt-cmd"

//module selectors
let moduleSettingsDiv = "div#msModuleSettings>div.msmsContent"

//html module
let htmlInlineEditMask = "div.dnnInlineEditingMask" //inline mask reads Click here to edit content
let htmlLeftPaneAddText = "div#dnn_LeftPane>div>div.QuickAddTextHandler"
let htmlTopHeroPaneAddText = "div#dnn_topHero>div>div.QuickAddTextHandler"
let htmlTopPaneAddText = "div#dnn_TopPane>div>div.QuickAddTextHandler"
let htmlTopHeroDarkPaneAddText = "div#dnn_TopHeroDark>div>div.QuickAddTextHandler"

//user profile
let uploadProfilePicBtn = "input[type=button][name='uploadFileButton']"

let homePageName = 
    match installationLanguage with
    | English -> "Home"
    | German -> "Start"
    | Spanish -> "Inicio"
    | French -> "Accueil"
    | Italian -> "Home"
    | Dutch -> "Home"

// login popup and page controls
let loginText = 
    match installationLanguage with
    | English -> "Login"
    | German -> "anmelden"
    | Spanish -> "Iniciar"
    | French -> "Connexion"
    | Italian -> "Login"
    | Dutch -> "Inloggen"

let logoutText = 
    match installationLanguage with
    | English -> "Logout"
    | German -> "abmelden"
    | Spanish -> "Salir"
    | French -> "Déconnexion"
    | Italian -> "Logout"
    | Dutch -> "Uitloggen"

let registerText = 
    match installationLanguage with
    | English -> "Register"
    | German -> "registrieren"
    | Spanish -> "Registro"
    | French -> "Inscription"
    | Italian -> "Registrazione"
    | Dutch -> "Registreren"

let addNewPageMenuTitle = 
    match installationLanguage with
    | English -> "Pages"
    | German -> "Seiten"
    | Spanish -> "Páginas"
    | French -> "Pages"
    | Italian -> "Pagine"
    | Dutch -> "Pagina's"

let pageDoesNotExistText = 
    match installationLanguage with
    | English -> "The page was not found."
    | German -> "Seite nicht gefunden"
    | Spanish -> "La página no se ha encontrado."
    | French -> "Page introuvable"
    | Italian -> "La pagina non può essere trovata"
    | Dutch -> "De gevraagde pagina kan niet worden gevonden"

let pageNotFoundText = "//*[contains(@text,'Page cannot be found')]"

(*
    match installationLanguage with
    | English -> "//*[contains(@text,'Page cannot be found')]"
    | German  -> "//*[contains(@text,'Seite nicht gefunden')]"
    | Spanish -> "//*[contains(@text,'La página no se ha encontrado.')]"
    | French  -> "//*[contains(@text,'Page introuvable')]"
    | Italian -> "//*[contains(@text,'La pagina non può essere trovata')]"
    | Dutch   -> "//*[contains(@text,'De gevraagde pagina kan niet worden gevonden')]"
    *)

let localeElement = 
    match installationLanguage with
    | English -> "#lang_en_US"
    | German -> "#lang_de_DE"
    | Spanish -> "#lang_es_ES"
    | French -> "#lang_fr_FR"
    | Italian -> "#lang_it_IT"
    | Dutch -> "#lang_nl_NL"

let ddlistLangKeyword = 
    match installationLanguage with
    | English -> "English"
    | German -> "Deutsch"
    | Spanish -> "internacional"
    | French -> "France"
    | Italian -> "Italia"
    | Dutch -> "Nederland"

let ddlistLangName = 
    match installationLanguage with
    | English -> "English (United States)"
    | German -> "Deutsch (Deutschland)"
    | Spanish -> "español (España, alfabetización internacional)"
    | French -> "français (France)"
    | Italian -> "italiano (Italia)"
    | Dutch -> "Nederlands (Nederland)"

let welcomeToSiteText = 
    match installationLanguage with
    | English -> "//span[.='Welcome to your website']"
    | German -> "//span[.='Willkommen auf Ihrer neuen Website']"
    | Spanish -> "//span[.='Bienvenido a su sitio web']"
    | French -> "//span[.='Bienvenue sur votre site Internet']"
    | Italian -> "//span[.='Benvenuto nel tuo sito web']"
    | Dutch -> "//span[.='Welkom op je website']"

let manageRolesSelector = 
    match installationLanguage with
    | English -> "//img[@title='Manage Roles']"
    | German -> "//img[@title='Benutzergruppen verwalten']"
    | Spanish -> "//img[@title='Administrar roles']"
    | French -> "//img[@title='Gestion des rôles et groupes de rôles']"
    | Italian -> "//img[@title='Gestione Ruoli ']" // note the title string has an extra space at the end
    | Dutch -> "//img[@title='Rollen beheren']"

let editIconSelector = 
    match installationLanguage with
    | English -> "//img[@title='Edit']"
    | German -> "//img[@title='bearbeiten']"
    | Spanish -> "//img[@title='Editar']"
    | French -> "//img[@title='Modifier']"
    | Italian -> "//img[@title='Modifica']"
    | Dutch -> "//img[@title='Bewerken']"

let addNewUserMenuTitle = 
    match installationLanguage with
    | English -> "Users"
    | German -> "Benutzer"
    | Spanish -> "Usuarios"
    | French -> "Utilisateurs"
    | Italian -> "Utenti"
    | Dutch -> "Gebruikers"

let authorizeUserText = 
    match installationLanguage with
    | English -> "Authorize User"
    | German -> "Benutzer autorisieren"
    | Spanish -> "Autorizar usuario"
    | French -> "Autoriser l'utilisateur"
    | Italian -> "Autorizza Utente"
    | Dutch -> "Machtig gebruiker"

let deleteUserText = 
    match installationLanguage with
    | English -> "Delete User"
    | German -> "Benutzer löschen"
    | Spanish -> "Eliminar usuario"
    | French -> "Supprimer l’utilisateur"
    | Italian -> "Elimina Utente"
    | Dutch -> "Verwijder gebruiker"

let unAuthorizeUserText = 
    match installationLanguage with
    | English -> "UnAuthorize User"
    | German -> "Autorisierung des Benutzers widerrufen"
    | Spanish -> "Desautorizar usuario"
    | French -> "Interdire l'accès"
    | Italian -> "Utente Non Autorizzato"
    | Dutch -> "Gebruiker toegang ontzeggen"

let userRolesText = 
    match installationLanguage with
    | English -> "User Roles"
    | German -> "Benutzergruppen"
    | Spanish -> "Roles del usuario"
    | French -> "Gérer les rôles de l'utilisateur."
    | Italian -> "Ruoli Utente"
    | Dutch -> "Gebruikersrollen."

let addPageText = 
    match installationLanguage with
    | English -> "Add Page"
    | German -> "Seite(n) hinzufügen"
    | Spanish -> "Añadir página"
    | French -> "Ajout de page"
    | Italian -> "Aggiungi Pagina"
    | Dutch -> "Toevoegen Pagina"

let collapseText=
    match installationLanguage with
    | English -> "[COLLAPSE]"
    | German -> "[COLLAPSE]"
    | Spanish -> "[COLLAPSE]"
    | French -> "[COLLAPSE]"
    | Italian -> "[COLLAPSE]"
    | Dutch -> "[COLLAPSE]"

let expandText=
    match installationLanguage with
    | English -> "[EXPAND]"
    | German -> "[EXPAND]"
    | Spanish -> "[EXPAND]"
    | French -> "[EXPAND]"
    | Italian -> "[EXPAND]"
    | Dutch -> "[EXPAND]"

let pageContextMenuAddPageText=
    match installationLanguage with
    | English -> "Add Page"
    | German -> "Seite(n) hinzufügen"
    | Spanish -> "Añadir página"
    | French -> "Ajout de page"
    | Italian -> "Aggiungi Pagina"
    | Dutch -> "Toevoegen Pagina"

let pageContextMenuViewText=
    match installationLanguage with
    | English -> "View"
    | German -> "anzeigen"
    | Spanish -> "Ver"
    | French -> "Voir"
    | Italian -> "Vista"
    | Dutch -> "Weergave"

let pageContextMenuEditText=
    match installationLanguage with
    | English -> "Edit"
    | German -> "bearbeiten"
    | Spanish -> "Editar"
    | French -> "Modifier"
    | Italian -> "Modifica"
    | Dutch -> "Bewerken"

let pageContextMenuDuplicatetText=
    match installationLanguage with
    | English -> "Duplicate"
    | German -> "Duplicate"
    | Spanish -> "Duplicate"
    | French -> "Duplicate"
    | Italian -> "Duplicate"
    | Dutch -> "Duplicate"

let pageContextMenuAnalyticsText=
    match installationLanguage with
    | English -> "Analytics"
    | German -> "Analytics"
    | Spanish -> "Analytics"
    | French -> "Analytics"
    | Italian -> "Analytics"
    | Dutch -> "Analytics"

let buttonSaveText=
    match installationLanguage with
    | English -> "Save"
    | German -> "speichern"
    | Spanish -> "Guardar"
    | French -> "Enregistrer"
    | Italian -> "Salva"
    | Dutch -> "Opslaan"

let pageDetailStandardType=
    match installationLanguage with
    | English -> "Standard"
    | German -> "Standard"
    | Spanish -> "Estándar"
    | French -> "Standard."
    | Italian -> "Standard"
    | Dutch -> "Standaard"

let pageDetailExistType=
    match installationLanguage with
    | English -> "Existing"
    | German -> "bestehend"
    | Spanish -> "Existente"
    | French -> "Existant"
    | Italian -> "Esistente"
    | Dutch -> "Bestaande"

let pageDetailURLType=
    match installationLanguage with
    | English -> "URL"
    | German -> "Link-URL"
    | Spanish -> "URL"
    | French -> "URL"
    | Italian -> "URL"
    | Dutch -> "URL"

let pageDetailFileType=
    match installationLanguage with
    | English -> "File"
    | German -> "Datei"
    | Spanish -> "Archivo"
    | French -> "Fichier"
    | Italian -> "File"
    | Dutch -> "Bestand"

let pageDetailWorkflowText=
    match installationLanguage with
    | English -> "Workflow"
    | German -> "Workflow"
    | Spanish -> "Flujo de trabajo"
    | French -> "Workflow"
    | Italian -> "Workflow"
    | Dutch -> "Workflow"

let pageDetailDescriptionText=
    match installationLanguage with
    | English -> "Description"
    | German -> "Beschreibung"
    | Spanish -> "Descripción"
    | French -> "Description"
    | Italian -> "Descrizione"
    | Dutch -> "Omschrijving"

let pageDetailNameText=
    match installationLanguage with
    | English -> "Name*"
    | German -> "Name*"
    | Spanish -> "Nombre*"
    | French -> "Nom*"
    | Italian -> "Nome*"
    | Dutch -> "Naam*"

let pageDetailTitleText=
    match installationLanguage with
    | English -> "Title"
    | German -> "Überschrift"
    | Spanish -> "Título"
    | French -> "Titre"
    | Italian -> "Titolo"
    | Dutch -> "Titel"

let permanentRedirect=
    match installationLanguage with
    | English -> "Permanent Redirect"
    | German -> "Überschrift"
    | Spanish -> "Título"
    | French -> "Titre"
    | Italian -> "Titolo"
    | Dutch -> "Titel"

let editPageText= 
    match installationLanguage with
    | English -> "Save"
    | German -> "Save"
    | Spanish -> "Salvar"
    | French -> "Save"
    | Italian -> "Save"
    | Dutch -> "Save"

let changePasswordText = 
    match installationLanguage with
    | English -> "Change Password"
    | German -> "Change Password"
    | Spanish -> "Change Password"
    | French -> "Change Password"
    | Italian -> "Change Password"
    | Dutch -> "Change Password"

let emailDetailsSentText = 
    match installationLanguage with
    | English -> 
        "An e-mail with your details has been sent to the website administrator for verification. Once your registration has been approved an e-mail will be sent to your e-mail address. In the meantime you can continue to browse this site by closing the popup."
    | German -> 
        "Eine E-Mail mit Ihren Anmeldedaten wurde zur Überprüfung an den Verwalter der Website geschickt. Sobald dieser Ihre Anmeldung freigeschaltet hat, erhalten Sie eine Benachrichtigung per E-Mail. Bis dahin können Sie unsere Website weiter betrachten, nachdem Sie dieses Popup geschlossen haben."
    | Spanish -> 
        "Se ha enviadao un email con los detalles de su registro al administrador de la web para su verificación. Una vez que su registro haya sido aprobado se le enviará un email a su dirección. Mientras tanto puede cerrar esta ventana y continuar navegado por el sitio."
    | French -> 
        "Un message contenant vos informations d'inscription a été envoyé à l'administrateur pour vérification.&lt;br&gt;Lorsque votre inscription aura été approuvée, vous recevrez un courriel avec un code de vérification sur votre adresse de courriel. En attendant, vous pouvez continuer à naviguer sur ce site en fermant simplement cette fenêtre d'information."
    | Italian -> 
        "All'amministratore viene inviato un messaggio contenente le informazioni di registrazione per la verifica. Quando la registrazione è stata approvata, riceverai una e-mail con un codice di controllo all'indirizzo e-mail. Nel frattempo è possibile continuare a navigare questo sito chiudendo il popup."
    | Dutch -> 
        "Opmerking: Lidmaatschap van deze website is prive. Als uw account informatie is ingediend, zal de website beheerder in kennis gesteld en uw aanvraag zal worden onderworpen aan een screening procedure. Als uw aanvraag wordt goedgekeurd, ontvangt u melding van uw toegang tot de website."

// Note the Duch above has different resource format that the rest. Check {Website}\App_GlobalResources\SharedResources.nl-NL.resx
let userNameExistsText = 
    match installationLanguage with
    | English -> "A User Already Exists For the Username Specified. Please Register Again Using A Different Username."
    | German -> 
        "Es gibt bereits einen Benutzer mit dem angegebenen Namen. Bitte registrieren Sie sich unter einem anderen Benutzernamen."
    | Spanish -> "Ya existe un usuario con este mismo nombre. Regístrese nuevamente utilizando otro nombre."
    | French -> "Ce compte utilisateur existe déjà. Veuillez choisir un autre compte"
    | Italian -> "Questo account utente esiste già. Si prega di selezionare un altro account."
    | Dutch -> "Deze gebruikersnaam is reeds in gebruik. Registreer alstublieft met een andere gebruikersnaam."

// these are not localized
let installingText = "//h1[.='Installing DotNetNuke']"
let upgradingText = "//h1[.='Upgrading DotNetNuke']"
let autoVisitSiteLinkText = "//a[.='Click Here To Access Your Site']"
let wizardProgressSelector = "#percentage"

let wizardProgressErrorText = 
    match installationLanguage with
    | English -> "ERROR occured - "
    | German -> "Es ist ein Fehler aufgetreten: "
    | Spanish -> "Ha ocurrido un ERROR - "
    | French -> "Une erreur s'est produite - "
    | Italian -> 
        if config.Site.IsUpgrade then "Si è verificato un ERRORE - "
        else "ERRORE si è verificato - "
    | Dutch -> 
        if config.Site.IsUpgrade then "Er is een fout opgetreden - "
        else "Er heeft zich een fout voorgedaan - "

// Note: checking the error page coming from the browser itself is
// browser specific and might not work in all browsers and/or languages
let browserServerAppError = "//h1[.=\"Server Error in '/' Application.\"]"
let loginUserNameTextBoxId = "#dnn_ctr_Login_Login_DNN_txtUsername"
let loginPasswordTextBoxId = "#dnn_ctr_Login_Login_DNN_txtPassword"
let loginButtonId = "#dnn_ctr_Login_Login_DNN_cmdLogin"
let registrationFormId = "#dnn_ctr_Register_userForm"
let registerNewUserBtn = "#dnn_ctr_Register_registerButton"
let registerCancelBtn = "#dnn_ctr_Register_cancelButton"

//skin specific elements are part of the "siteSettings" object
let createNewPagePartialLink = "/ctl/Tab/action/edit/activeTab/settingTab"
let editPageSettingsPartialLink = "/ctl/Tab/action/edit/activeTab/settingTab"
let editPagePermissionPartialLink = "/ctl/Tab/action/edit/activeTab/permissionsTab"
let editPageAppearancePartialLink = "/ctl/Tab/action/edit/activeTab/advancedTab"

// Preparations reporting for all tests
// Default is console output. enable Either of the next two for others kinds
// for live HTML reports in a browser
if config.Settings.Reports.Html then reporter <- new LiveHtmlReporter() :> IReporter
// for reports compatible with TeamCity
if config.Settings.Reports.TeamCity then reporter <- new TeamCityReporter(false) :> IReporter

let directoryText =
    match installationLanguage with
    | English -> "Directory"
    | German ->  "Verzeichnis"
    | Spanish -> "Carpeta"
    | French ->  "Répertoire"
    | Italian -> "Directory"
    | Dutch ->   "Directorie"

let installExtensionAction =
    match installationLanguage with
    | English -> "Install Extension"
    | German ->  "Installationsassistent für Erweiterungen"
    | Spanish -> "Instalar extensión"
    | French ->  "Installer une nouvelle extension"
    | Italian -> "Installa Estensione"
    | Dutch ->   "Installeer Extensie"

let addUserText =
    match installationLanguage with
    | English -> "Add User"
    | German ->  "Benutzer anlegen"
    | Spanish -> "Añadir usuario"
    | French ->  "Ajouter un utilisateur"
    | Italian -> "Aggiungi Utente"
    | Dutch ->   "Toevoegen gebruiker"

let hostGUIDText =
    match installationLanguage with
    | English -> "HOST GUID"
    | German ->  "GUID"
    | Spanish -> "GUID SISTEMA"
    | French ->  "HÔTE GUID"
    | Italian -> "HOST GUID"
    | Dutch ->   "HOST GUID"

let UnauthorizedText =
    match installationLanguage with
    | English -> "Unauthorized"
    | German ->  "nicht autorisiert"
    | Spanish -> "No autorizados"
    | French ->  "Non autorisés"
    | Italian -> "Non Autorizzato"
    | Dutch ->   "Ongemachtigd"

let detailsText =
    match installationLanguage with
    | English -> "Details"
    | German ->  "Details"
    | Spanish -> "Detalles"
    | French ->  "Détails"
    | Italian -> "Dettagli"
    | Dutch ->   "Details"

let permissionText =
    match installationLanguage with
    | English -> "Permissions"
    | German ->  "Berechtigungen"
    | Spanish -> "Permisos"
    | French ->  "Permissions"
    | Italian -> "Permessi"
    | Dutch ->   "Toestemmingen"

let advancedText =
    match installationLanguage with
    | English -> "Advanced"
    | German ->  "erweitert"
    | Spanish -> "Avanzado"
    | French ->  "Avancé"
    | Italian -> "Avanzate"
    | Dutch ->   "Geavanceerd"

let personaBarText = 
    match installationLanguage with
    | English -> "Persona Bar"
    | German ->  "persönliche Leiste"
    | Spanish -> "Persona Bar"
    | French ->  "Persona Bar"
    | Italian -> "Persona Bar"
    | Dutch ->   "Persona Bar"

let deleteText = 
    match installationLanguage with
    | English -> "Delete"
    | German ->  "löschen"
    | Spanish -> "Eliminar"
    | French ->  "Supprimer"
    | Italian -> "Elimina"
    | Dutch ->   "Verwijderen"

//PB Content - Edit a Content Item
let editText = 
    match installationLanguage with
    | English -> "Edit"
    | German ->  "Edit"
    | Spanish -> "Edit"
    | French ->  "Edit"
    | Italian -> "Edit"
    | Dutch ->   "Edit"

let pbDescriptionLabelText = 
    match installationLanguage with
    | English -> "Description"
    | German -> "Beschreibung"
    | Spanish -> "Descripción"
    | French -> "Description"   //TODO: add correct localized string
    | Italian -> "Descrizione"
    | Dutch -> "Omschrijving"

//let xxxXXX =
//    match installationLanguage with
//    | English -> ""
//    | German ->  ""
//    | Spanish -> ""
//    | French ->  ""
//    | Italian -> ""
//    | Dutch ->   ""

configuration.failScreenshotFileName <- (fun test suite -> 
    if canopy.configuration.failureScreenshotsEnabled then 
        let cleanName = (sanitizeFileName test.Description).Replace(' ', '_')
        let stamp = DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss")
        let fname = sprintf "%s_%s" stamp cleanName
        printfn "  Saving screenshot to: %s/%s.png" canopy.configuration.failScreenshotPath fname
        fname
    else "")

let userDisplayNameSelector = "#dnn_dnnUser_enhancedRegisterLink"

let fileSpanSelectorDam = "//table[contains(@class,'rgMasterTable rgClipCells')]/tbody/tr[1]/td[2]/div"

let platformHostSchedulers = [
        {Name = "Messaging Dispatch"; Enabled = true; Frequency = "Every 1 Minute"; RetryTime = "Every 30 Seconds"};
        {Name = "Purge Cache"; Enabled = false; Frequency = "Every 2 Hours"; RetryTime = "Every 30 Minutes"};
        {Name = "Purge Client Dependency Files"; Enabled = false; Frequency = "Every 1 Day"; RetryTime = "Every 6 Hours"};
        {Name = "Purge Log Buffer"; Enabled = false;  Frequency = "Every 1 Minute"; RetryTime = "Every 30 Seconds"};
        {Name = "Purge Module Cache"; Enabled = true; Frequency = "Every 1 Minute"; RetryTime = "Every 30 Seconds"};
        {Name = "Purge Output Cache"; Enabled = false; Frequency = "Every 1 Minute"; RetryTime = "Every 30 Seconds"};
        {Name = "Purge Schedule History"; Enabled = true; Frequency = "Every 1 Day";  RetryTime = "Every 2 Hours"};
        {Name = "Search: Site Crawler"; Enabled = true; Frequency = "Every 1 Minute"; RetryTime = "Every 30 Seconds"};
        {Name = "Send Log Notifications"; Enabled = false; Frequency = "Every 5 Minutes"; RetryTime = "Every 2 Minutes"};
        {Name = "Site Import/Export"; Enabled = false; Frequency = "Every 1 Day"; RetryTime = "Every 1 Hour"}
    ]

let listAnalyticsCaption = 
    [
        "Page Traffic"; "Page Views"; "Unique Visitors"; "Unique Sessions"; "Time On Page";
        "Bounce Rate"; "Page Activities"; "Top Referrers"; "Top OS's"; "Page Events";
        "Channels"; "Devices";
    ]

let listDashboardCaption = 
    [
        "Site Traffic"; "Page Views"; "Unique Visitors"; "Unique Sessions"; "Time On Page";
        "Bounce Rate"; "Site Activities"; "Top Referrers"; "Top Pages"; "Site Events";
        "Channels"; "Devices"; "Direct"; "Search"; "Social"; "Mobile"; "Tablet";
    ]

let listSecurityAuditCheck = 
    [ 
        ("CheckDebug",1)
        ("CheckTracing",1)
        ("CheckBiography",0)
        ("CheckSiteRegistration",1)
        ("CheckRarelyUsedSuperuser",1)
        ("CheckSuperuserOldPassword",1)
        ("CheckUnexpectedExtensions",1)
        ("CheckDefaultPage",1)
        ("CheckModuleHeaderAndFooter",1)
        ("CheckPasswordFormat",1)
        ("CheckDiskAccess",2)
        ("CheckSqlRisk",2)
        ("CheckAllowableFileExtensions",1)
        ("CheckHiddenSystemFiles",3)
    ]

// This list captures the information architecture visibility settings for the new persona bar in Platform 9.0
// This list is divided in sections. 
// The first item in each section should be the PB icon, which upon hovering on, should display the remaining items of that section.
// For e.g., hovering on div.personabarLogo, should display li.framework and items below it through #Logout
let personaBarInfoArch =
    [
        //Content
        { ItemSelector = "#Content"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Pages']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Recyclebin']"; Host = true; Admin = true }
        //Manage
        { ItemSelector = "#Manage"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Users']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Roles']"; Host = true; Admin = true }
        { ItemSelector = "#SiteAssets"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Themes']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.AdminLogs']"; Host = true; Admin = true }
        //Settings
        { ItemSelector = "#Settings"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.SiteSettings']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Security']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Seo']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Vocabularies']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Connectors']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Extensions']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.Servers']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.SiteImportExport']"; Host = true; Admin = false }
        { ItemSelector = "//li[@id='Dnn.TaskScheduler']"; Host = true; Admin = false }
        { ItemSelector = "//li[@id='Dnn.CssEditor']"; Host = true; Admin = true }
        { ItemSelector = "//li[@id='Dnn.SqlConsole']"; Host = true; Admin = false }
        { ItemSelector = "//li[@id='Dnn.ConfigConsole']"; Host = true; Admin = false }
        { ItemSelector = "//li[@id='Dnn.Prompt']"; Host = true; Admin = false }
        { ItemSelector = "//li[@id='Dnn.Licensing']"; Host = true; Admin = false }
    ]
