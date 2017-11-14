module InstallationTests

open System
open System.IO
open canopy
open DnnCanopyContext
open DnnSiteSetup
open DnnUserLogin

let private updateWebConfig() =
    if not isRemoteSite then
        let sitePath = config.Site.WebsiteFolder
        let psfile = FileInfo(exeLocation + @"\..\PowershellScripts\UpdateConfigFileAfterInstall.ps1 ").FullName
        let args = sprintf @"-NonInteractive -ExecutionPolicy ByPass -File ""%s"" -sitePath ""%s"" " psfile sitePath
        waitForWebConfigUnlocked() //wait for lock on web.config
        execApplication "powershell.exe" args |> ignore

// change this to perform something before clicking continue
let private beforeContinue = fun () -> ()

// perform something after clicking continue (validates some items)
let private afterContinue() = 
    // check all possible error messages that can apear after clicking "Continue"
    // but before installation progress starts
    let isDbErrMsg = existsAndVisible "#databaseError"
    if isDbErrMsg then 
        let e = element "#databaseError"
        failwithf "Wizard contains DATABASE error message(s)! Error: %s" e.Text
    let span1 = someElement "//span[contains(@class,'dnnFormMessage')]"
    match span1 with
    | Some(e) -> 
        if not (String.IsNullOrEmpty e.Text) then 
            failwithf "Wizard contains FORM error message(s)!\n\t%s\n" e.Text
    | None -> ()
    let span2 = someElement "#lblAdminInfoError"
    match span2 with
    | Some(e) -> 
        if not (String.IsNullOrEmpty e.Text) then 
            failwithf "Wizard contains ADMIN error message(s)!\n\t%s\n" span2.Value.Text
    | None -> ()

// perform something before clicking visit site or check for errors
let private beforeVisitSiteWizard() = 
    let percentage = element wizardProgressSelector
    if percentage.Text.Contains(wizardProgressErrorText) then 
        failwithf "Wizard Installtion Error. %s" percentage.Text
    let stepError = "//p[@class='step-error']"
    if existsAndVisible stepError then failwithf "Wizard Installtion Error. %s" percentage.Text
    let visitSiteEnabledButton = "a#visitSite"
    if not (existsAndEnabled visitSiteEnabledButton) then 
        failwith "Wizard Installtion Error: Visit Site button never got enabled!"
    updateWebConfig()

let private beforeVisitSiteAuto() = 
    if exists autoInstallErrorElement || exists autoInstallError2Element then
        failwithf "Error in installing/upgrading the site"
    updateWebConfig()

// change this to perform something after clicking visit site
let private afterVisitSite() = 
    dismissWelcomePopup()
    reloadPage() //This is for PB icons to load properly on an upgraded site with a trial-license
    isOnRoot()

let wizardNew _ = 
    context "New site installation using wizard"
    "Complete new site installation using wizard" @@@ fun _ -> 
        createAndInstallSite WizardNew beforeContinue afterContinue beforeVisitSiteWizard afterVisitSite
        // make sure the host is logged in when using the wizard
        displayed siteSettings.loggedinUserImageLinkId

let wizardUpgrade _ = 
    context "Existing site upgrade using wizard"
    "Upgrading existing site using wizard" @@@ fun _ ->
        createAndInstallSite WizardUpgrade beforeContinue afterContinue beforeVisitSiteWizard afterVisitSite

let autoNew _ = 
    context "New site installation using auto-mode"
    "Complete new site installation using auto-mode" @@@ fun _ -> 
        createAndInstallSite AutoNew beforeContinue afterContinue beforeVisitSiteAuto afterVisitSite

let autoUpgrade _ = 
    context "Existing site upgrade using auto-mode"
    "Upgrading existing site using auto-mode" @@@ fun _ -> 
        createAndInstallSite AutoUpgrade beforeContinue afterContinue beforeVisitSiteAuto afterVisitSite

// this is actually not enable but toggle CDF setting
// assumeing CDF is disabled by default after a clean install
let enableCdf _ =
    context "Toggle CDF setting"
    "Enabling CDF mode" @@@ fun _ ->
        loginOnPageAs Host
        //displayed "#ControlNav"
        toggleCdf()

let all _ = failwith "There should be no all here; these tests don't work with each other"

