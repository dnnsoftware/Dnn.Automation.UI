module PersonaBarUITests

open System.IO
open DnnCanopyContext
open DnnAddToRole
open DnnExtensions
open DnnPrompt

let mutable private sectionNamePBArch = ""
let mutable private sectionExistsPBArch = false

/// <summary>Checks if the item is a PB section header</summary>
/// <param name="item">The item to be checked</param>
let private isSectionHeader item =
    item="div.personabarLogo" || item="#Content" || item="#Manage" || item="#Settings"

/// <summary>Returns the main sectionLink of the PB Section</summary>
/// <param name="section">The Persona Bar Section</param>
let private getSectionMainLink section =
    match section with
    | "div.personabarLogo" -> "#Logout"
    | "#Content" -> "//li[@id='Dnn.Pages']"
    | "#Manage" -> "//li[@id='Dnn.Users']"
    | "#Settings" -> "//li[@id='Dnn.Connectors']"
    | _ -> "div.personabarLogo"

/// <summary>Opens a hover menu in Persona Bar</summary>
/// <param name="hoverIcon">The PB icon to hover over</param>
/// <param name="sectionLink">The link in PB menu to verify with</param>
let private openPBMenu hoverIcon sectionLink =
    let doOpenMenu()=
        try
            hoverOver hoverIcon
            waitForElementPresentXSecs sectionLink 5.0
            true
        with _ ->
            reloadPage()
            waitForElementPresent hoverIcon
            false
    if not(existsAndVisible sectionLink) then
        retryWithWait 2 0.5 doOpenMenu |> ignore  

/// <summary>Checks the item's visibility against expected visibility</summary>
/// <param name="item">The item to be checked</param>
/// <param name="shouldBeVisible">If the item should be visible</param>
/// <returns>Empty string if passed, otherwise a reason for failure</returns>
let private checkVisibility item shouldBeVisible = 
    if existsAndVisible item <> shouldBeVisible then
        reloadPage()
        openPBMenu sectionNamePBArch (getSectionMainLink sectionNamePBArch)
    if existsAndVisible item <> shouldBeVisible then 
        let sectionPath = if item=sectionNamePBArch then item else sprintf "%s > %s" sectionNamePBArch item
        if shouldBeVisible then sprintf "\n\tUI element %A is not visible." sectionPath
        else sprintf "\n\tUI element %A is visible." sectionPath
    else ""

/// <summary>Verifies if an item is visible</summary>
/// <param name="item">The selector of the item</param>
/// <param name="prodVisible">Bool: Whether or not item is visible in product being tested</param>
/// <param name="roleVisible">Bool: Whether of not the item is visible for current user role</param>
/// <returns>Empty string if passed, otherwise a reason for failure</returns>
let private verifyItemVisibility item roleVisible =   
    let shouldBeVisible = roleVisible   
    if isSectionHeader item then
        sectionNamePBArch <- item
        sectionExistsPBArch <- existsAndVisible item
        if sectionExistsPBArch then
            openPBMenu item (getSectionMainLink item)
        checkVisibility item shouldBeVisible
    else ""

/// <summary>Test Persona Bar Information Architecture for a user role</summary>
/// <param name="userRole">The user role to be tested for</param>
let private testPersonaBarUIInfoArch (userRole:APIRoleName) =
    let mutable failedReasons = ""
    waitForElementPresent (personaBarInfoArch.Head.ItemSelector)
    waitPageSettleDown()
    failedReasons <- personaBarInfoArch |> List.fold (
        fun acc item -> 
            acc + verifyItemVisibility item.ItemSelector (if userRole = APIRoleName.HOSTUSER then item.Host
                                                          elif userRole = APIRoleName.ADMINISTRATORS then item.Admin
                                                          else false)
        ) failedReasons
    if failedReasons <> "" then failwithf "  FAIL: %s" failedReasons

let private bvtPBFeaturesVerify _ =
    context "Persona Bar Sections UI : Platform"

    "Persona Bar | Host | Open Recycle Bin" @@@ fun _ ->
        loginAsHost()
        openPBRecycleBin()

    "Persona Bar | Host | Verify Themes UI" @@@ fun _ ->
        openPBThemes()
        if not(existsAndVisible "div.current-theme") || not(existsAndVisible "div.theme-skin.selected") then
            failwith "  FAIL: One or more UI elements not visible in Themes"

    "Persona Bar | Host | Verify Sites UI" @@@ fun _ ->
        openPBSites()
        let portal = "div.portal-name-info"
        if not(existsAndVisible portal) then
            failwith "  FAIL: One or more UI elements not visible in Sites"

    "Persona Bar | Host | Verify Admin Logs UI" @@@ fun _ ->
        openPBAdminLogs()
        let sitesDdn = "div.toolbar>div.adminlogs-filter-container:nth-of-type(1)"
        let typesDdn = "div.toolbar>div.adminlogs-filter-container:nth-of-type(2)"
        let clearLogBtn = "div.toolbar>div.toolbar-button:nth-of-type(5)"
        let deleteBtn = "div.toolbar>div.toolbar-button:nth-of-type(4)"
        let emailBtn = "div.toolbar>div.toolbar-button:nth-of-type(3)"
        if not(existsAndVisible sitesDdn) || not(existsAndVisible typesDdn) || not(existsAndVisible clearLogBtn)
            || not(existsAndVisible deleteBtn) || not(existsAndVisible emailBtn) then
            failwith "  FAIL: One or more UI elements not visible in Admin Logs"

    "Persona Bar | Host | Verify Site Settings UI" @@@ fun _ ->
        openPBSiteSettings()
        if not(existsAndVisible "div.siteSettings-app>div>div>div>div.dnn-tabs>ul") || not(existsAndVisible "div.siteSettings-app>div>div>div>div.dnn-tabs>div:first-of-type") then
                failwith "  Fail: One or more UI elements not visible in Site Settings"

    "Persona Bar | Host | Verify Security UI" @@@ fun _ ->
        openPBSecurity()
        if not(existsAndVisible "div.securitySettings-app>div>div>div>div.dnn-tabs>ul") || not(existsAndVisible "div.securitySettings-app>div>div>div>div.dnn-tabs>div:first-of-type") then
                failwith "  Fail: One or more UI elements not visible in Security"

    "Persona Bar | Host | Verify SEO UI" @@@ fun _ ->
        openPBSeo()
        if not(existsAndVisible "div.seo-app>div>div>div>div.dnn-tabs>ul") || not(existsAndVisible "div.seo-app>div>div>div>div.dnn-tabs>div:first-of-type") then
                failwith "  Fail: One or more UI elements not visible in SEO"

    "Persona Bar | Host | Verify Servers UI" @@@ fun _ ->
        openPBServers()
        let clearCacheBtn = "div#servers-container>div>div>div>div>div>button:first-of-type"
        let restartAppBtn = "div#servers-container>div>div>div>div>div>button:last-of-type"
        if not(existsAndVisible clearCacheBtn) || not(existsAndVisible restartAppBtn) then
            failwith "  Fail: Clear Cache or Restart App button not visible"
        if not(existsAndVisible "div.servers-app>div>div>div>div.dnn-tabs>ul") || not(existsAndVisible "div.servers-app>div>div>div>div.dnn-tabs>div:first-of-type") then
            failwith "  Fail: One or more UI elements not visible in Servers"

    "Persona Bar | Host | Run Basic Prompt Commands" @@@ fun _ ->
        openPBPrompt()
        runPromptCommand CLH ""
        runPromptCommand CLS ""
        runPromptCommand CONFIG "50%"
        runPromptCommand CONFIG "100%"

let private bvtPBFeaturesModules _ =
    context "Persona Bar: Install and Uninstall Extensions"

    "Persona Bar | Extensions | Host | Install Hello World Module" @@@ fun _ ->
        loginAsHost()
        let zipPath = Path.Combine(additionalFilesLocation, "Dnn.PersonaBar.HelloWorld_01.00.00_Install.zip")
        let moduleName = "Hello World"
        canopy.configuration.skipRemainingTestsInContextOnFailure <- true
        installHostExtension zipPath moduleName
        waitForAjax()
        if not(exists "//span[.='Dnn.PersonaBar.HelloWorld']") then
            failwithf "  FAIL: After Install, %A module does not exist in the list of Installed Extensions" moduleName
        canopy.configuration.skipRemainingTestsInContextOnFailure <- false
        reloadPage()
        openPBSection "#Settings" "#HelloWorld" "//h3[contains(.,'Hello World')]" 10       

    "Persona Bar | Extensions | Host | UnInstall Hello World Module"  @@@ fun _ ->
        loginAsHost()
        let moduleName = "Hello World"
        uninstallHostExtension personaBarText "Dnn.PersonaBar.HelloWorld"
        reloadPage()
        openPBExtenstions()
        changeExtSection personaBarText
        if exists "//span[.='Dnn.PersonaBar.HelloWorld']" then
            failwithf "  FAIL: After UnInstall, %A module exists in the list of Installed Extensions" moduleName

let private infoArchTestsAll _ =
    context "Persona Bar: UI Information Architecture tests"

    "PB Information Architecture | Host | Verify UI Elements" @@@ fun _ ->
        loginAsHost()
        goto "/"
        closeEditMode()
        testPersonaBarUIInfoArch APIRoleName.HOSTUSER

    "PB Information Architecture | Admin | Verify UI Elements" @@@ fun _ ->
        loginAsAdmin() |> ignore
        testPersonaBarUIInfoArch APIRoleName.ADMINISTRATORS

let all _ = 
    //PB Info Arch tests
    infoArchTestsAll()

    //Open PB Sections tests
    bvtPBFeaturesVerify()
    bvtPBFeaturesModules()
