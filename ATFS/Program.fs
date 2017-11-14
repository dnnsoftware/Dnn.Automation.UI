module Program

open System
open OpenQA.Selenium
open canopy
open canopy.types
open DnnCanopyContext

let mutable browserLoaded = false

// Note: The order of files, F# uses top down compilation so your dependencies have to be in order in your proj solution.
//       To add folders to your project file you have to manually modify the .fsproj file, minor annoyance.
//=======================================================
// helpers
//=======================================================
let promptExit() = 
    // wait for user key when running under debugger
    if Diagnostics.Debugger.IsAttached then 
        printf "\nPress [Enter] key to exit ... "
        System.Console.ReadLine() |> ignore
        printf "\nExiting ... Please Wait!"

//=======================================================
// Exits application with error code
//=======================================================
let exitWithError ex =
    printfn "%A" ex
    promptExit()
    exit 1

//=======================================================
// Load browser and adjust its position
//=======================================================
let loadBrowser() = 
    let startBrowser() = 
        match config.Settings.Browser.ToLowerInvariant() with
        | "ff" | "firefox" ->
            if true then
                let profile = Firefox.FirefoxProfile()
                profile.SetPreference("browser.cache.disk.enable", false)
                profile.SetPreference("browser.cache.memory.enable", true)
                profile.SetPreference("browser.cache.offline.enable", false)
                profile.SetPreference("network.http.use-cache", false)
                let brwzr = FirefoxWithProfile(profile)
                start brwzr
            else
                let brwzr = FirefoxWithPathAndTimeSpan(null, TimeSpan.FromMinutes(3.))
                start brwzr
        | "chrome" -> 
            // chrome runs the tests much faster than firefox but has some elements clicking issues
            canopy.configuration.chromeDir <- exeLocation
            let options = Chrome.ChromeOptions()
            if not(config.Settings.DevMode) then
                options.AddArguments("--kiosk")
            let brwzr = ChromeWithOptionsAndTimeSpan(options, TimeSpan.FromMinutes(3.))
            start brwzr
        | "chromium" -> 
            // same as chrome
            canopy.configuration.chromiumDir <- exeLocation
            start chromium
        | "ie" | "internetexplorer" -> 
            canopy.configuration.ieDir <- exeLocation
            start ie
        | "edge" -> 
            // not working until this release of Selenium 3.0.0
            canopy.configuration.edgeDir <- exeLocation
            start edgeBETA
        | "safari" -> 
            canopy.configuration.safariDir <- exeLocation
            start safari
        | "phantomjs" ->
            // headless test driver; not useful for UI testing
            canopy.configuration.phantomJSDir <- exeLocation
            start phantomJS
        | _ -> failwith ("unsupported browser: " + config.Settings.Browser)
    try 
        startBrowser()
    with :? OpenQA.Selenium.WebDriverException as ex -> 
        // sometimes the browser fails to start so we retry once more
        printfn "Failed to start the browser. Error: %s" ex.Message
        printfn "Retrying in a few seconds ..."
        sleep 5
        startBrowser()

    browserLoaded <- true
    printfn "Running browser: %A" browser
    printfn "Browser instance handle: %s" (browser.CurrentWindowHandle)
    browser.Manage().Timeouts().PageLoad <- TimeSpan.FromSeconds canopy.configuration.pageTimeout

    // zoom only in debugging mode
    try
        if Diagnostics.Debugger.IsAttached then
            if config.Settings.ShowOnMonitor > 0 then
                pinToMonitor config.Settings.ShowOnMonitor
            else
                let size = browser.Manage().Window.Size
                let width = Math.Max(Math.Min(size.Width, 1400), 1000)
                let height = Math.Max(Math.Min(size.Height, 1000), 800)
                resize (width, height)
        elif config.Settings.DevMode then
            pin FullScreen
            // F11 will not have effect unless the browser is the focused window
            InputSimulatorHelper.pressF11()
    with ex -> 
        printfn "Error pinning the browser %s" ex.Message

//=======================================================
// Run all the registered tests here
//=======================================================
let private runTests() =
    try
        loadBrowser()
        run()
        0
    finally
        promptExit()
        if browserLoaded then quit()

//=======================================================
// Register the tests to run here
//=======================================================
let registerSetupTests() =
    if config.Site.DoInstallation then
        if config.Site.IsUpgrade then
            if config.Site.UseInstallWizard then InstallationTests.wizardUpgrade()
            else InstallationTests.autoUpgrade()
        else if config.Site.UseInstallWizard then InstallationTests.wizardNew()
        else InstallationTests.autoNew()

        if config.Site.EnableCDF then
            InstallationTests.enableCdf()

let registrOtherTests() = 
    let mutable tests2Run = config.Settings.TestsToRun

    if tests2Run.CoverageTests then
        tests2Run.BvtTests <- true
        tests2Run.P1ALL <- true
        tests2Run.API_Set_1 <- true
        tests2Run.API_Set_2 <- true
        tests2Run.RegressionTests <- true

    // run these tests for all language installations
    if tests2Run.BvtTests then
        LoginTests.all()

    // add API tests before anything else
    if tests2Run.API_Set_1 then
        WebAPIBVT.all()

    // continue with regular BVT tests
    if tests2Run.BvtTests then
        InspectPagesTests.all()
        RegistrationTests.all()
        CreatePagesTests.all()
        PersonaBarUITests.all()
        SecurityAnalyzer.all()
        Scheduler.bvtTests()

    if tests2Run.API_Set_2 then
        ()

    if tests2Run.P1ALL then
        tests2Run.P1_Set_01 <- true
        tests2Run.P1_Set_02 <- true
        tests2Run.P1_Set_03 <- true
        tests2Run.P1_Set_04 <- true
        tests2Run.P1_Set_05 <- true
        tests2Run.P1_Set_06 <- true
        tests2Run.P1_Set_07 <- true
        tests2Run.P1_Set_08 <- true
        tests2Run.P1_Set_09 <- true
        tests2Run.P1_Set_10 <- true
        tests2Run.P1_Set_11 <- true
        tests2Run.P1_Set_12 <- true
        tests2Run.P1_Set_13 <- true
        tests2Run.P1_Set_14 <- true
        tests2Run.P1_Set_15 <- true
        tests2Run.P1_Set_16 <- true

    //register P1 scripts here
    if tests2Run.P1_Set_01 then
        CreatePagesExtraTests.all()
        PageSettingsTests.all()
        AddModulesToPageTest.all()
        AddUsersToRoles.all()           

    if tests2Run.P1_Set_02 then
        Search.all()
        HtmlModule.all()

    if tests2Run.P1_Set_03 then
        Prompt.all()        

    if tests2Run.P1_Set_04 then
        UserProfile.all()

    if tests2Run.P1_Set_05 then
        SiteSettings.all()         

    if tests2Run.P1_Set_06 then
        PBPages.all()

    if tests2Run.P1_Set_07 then
        RecycleBin.all()

    if tests2Run.P1_Set_08 then
        PBTools.all()

    if tests2Run.P1_Set_09 then
        DAM.all()

    if tests2Run.P1_Set_10 then
        MvcSpaModules.all()    

    if tests2Run.P1_Set_11 then                
        Scheduler.all()

    if tests2Run.P1_Set_12 then
        ()

    if tests2Run.P1_Set_13 then
        UserRegistration.all()        

    if tests2Run.P1_Set_14 then
        PBRoles.all()        

    if tests2Run.P1_Set_15 then
        PBConnectors.all()

    if tests2Run.P1_Set_16 then
        Modules.all()

    if tests2Run.RegressionTests then 
        SupportIssues.all()
        ExportImport.all()
        PBConnectors.azureRegTests()

let registerChildSiteTests() =    
    if config.Settings.TestsToRun.RepeatTestsForChildSite then 
        CreateChildSiteTest.all()
        isChildSiteContext <- true
        registrOtherTests() // repeat all these for child site
        isChildSiteContext <- false

let registerAllTests() =
    printfn "Testing of *** %A - %A ***" "Platform" installationLanguage
    printfn "Testing site : %s" root

    if not config.Settings.TestsToRun.DevTestsOnly then
        registerSetupTests()
        registrOtherTests()
        // This MUST be the very last one to run as it will set the context to child site
        registerChildSiteTests()
        // we don't care about this whe performing specific tests
        if not config.Settings.TestsToRun.CoverageTests then 
            // this one will run at the end to check the log files. DO NOT REMOVE FROM HERE
            LogFileTest.all()
    else
        if config.Settings.TestsToRun.RepeatTestsForChildSite then
            isChildSiteContext <- true
            useChildPortal <- true

        //================================================================
        //
        // This area is for registering selective tests during development
        //
        // Replace the next line(s) with your test reegistration code; e.g.,
        // Myests.all()
        //
        //================================================================

        // replace the next code line(s) with your test reegistration
        // code; e.g., MyTests.all()
        ()

        //================================================================
        // end of registration area
        //================================================================

//=======================================================
// Main program body
//=======================================================
[<EntryPoint>]
let main _ = 
    try 
        registerAllTests()
        runTests()
    with ex -> 
        exitWithError ex
