module DnnSiteSetup

open System
open canopy

let mutable private retryClicks = 0

let createAndInstallSite (mode : DnnInstallationMode) beforeContinueCallback
    afterContinueCallback beforeVisitSiteCallback afterVisitSiteCallback = 
    let databaseName = portalAlias.Split('.').[0]

    let installSite() = 
        let checkPermission() = 
            // sometimes a security popup appears - waiting a few
            // more seconds may - hopefully - prevent it from appearing.
            printfn "Checking Permission..."
            let sleepTime = 15
            let mutable retries = 60 / sleepTime
            let installPermissionDialogImg = @"//img[@src='..\403-3.gif']"
            let installPermissionRecheckBtn = 
                match installationLanguage with
                | English -> "//a[.='Recheck']"
                | German ->  "//a[.='Recheck']"
                | Spanish -> "//a[.='Verificar de nuevo']"
                | French ->  "//a[.='Relancer la vérification']"
                | Italian -> "//a[.='Ricontrolla']"
                | Dutch ->   "//a[.='Recheck']"

            let permissionDlgVisible() = existsAndVisible installPermissionRecheckBtn || existsAndVisible installPermissionDialogImg

            if permissionDlgVisible() then
                let dismissPermissionDialog() =
                    printf " Permission dialog is visible. Retrying to dismiss it and wait a bit!"
                    if existsAndVisible installPermissionRecheckBtn then
                        click installPermissionRecheckBtn
                    else
                        // sometimes the above might not be visible (I18N issues, so we find the button in a different way
                        let btn = element installPermissionDialogImg |> parent |> elementWithin ".dnnPrimaryAction"
                        click btn
                    waitPageLoad()
                    permissionDlgVisible()

                if false = retryWithWait retries sleepTime dismissPermissionDialog then
                    failwith "No permission to run the wizard on this machine"

            while existsAndVisible installPermissionRecheckBtn || existsAndVisible installPermissionDialogImg do
                if retries = 0 then failwith "No permission to run the wizard on this machine"
                sleep sleepTime
                if existsAndVisible installPermissionRecheckBtn then
                    click installPermissionRecheckBtn
                else
                    // sometimes the above might not be visible (I18N issues, so we find the button in a different way
                    let btn = element installPermissionDialogImg |> parent |> elementWithin ".dnnPrimaryAction"
                    click btn
                waitPageLoad()
                retries <- retries - 1

        let hasWizardError() = 
            if existsAndVisible wizardProgressSelector then
                let progressText = (element wizardProgressSelector).Text
                let hasError = progressText.Contains(wizardProgressErrorText)
                // DNN-7933: work around this issue; appears randomly in TC automated tests
                if hasError then
                    if retryClicks < 3 then
                        retryClicks <- retryClicks + 1
                        printfn "  Retry #%d after detecting installation error: %A" retryClicks progressText
                        click "#retry" // the retry button in this case
                        false
                    else true
                else false
            else false

        let continueWizardInstallation() = 
            beforeContinueCallback()
            scrollTo "#continueLink"
            click "#continueLink"
            waitForAjax() // two ajax calls go out: verify input and verify db connection
            sleep 0.005
            waitForAjax()
            checkPermission()
            sleep 1
            afterContinueCallback()
            waitPageLoad()
            sleep 1 // needed to give the next step of the wizard to start its progress
            let visitSiteDisabledButton = "//a[@class='dnnPrimaryAction visitSiteLink dnnDisabledAction']"
            waitUntil waitForInstallProgressToAppear "Wizard progress screen didn't appear!" 
                (fun _ -> existsAndVisible visitSiteDisabledButton || existsAndVisible wizardProgressSelector)
            let visitSiteEnabledButton = "//a[@class='dnnPrimaryAction visitSiteLink']"
            let stepError = "//p[@class='step-error']"
            waitUntil waitForInstallProgressToFinish "Wizard progress screen didn't finish!" 
                (fun _ -> existsAndEnabled visitSiteEnabledButton || existsAndVisible stepError || hasWizardError())
            // the caller should verify as well
            beforeVisitSiteCallback()
            waitForWebConfigUnlocked() //wait for lock on web.config
            // at end visit the site
            //click "a#visitSite"
            goto "/"

        let wizardNew qualifier = 
            let selectLanguageIcon() = 
                // the following click refreshes the page then sends 3 AJAX requests
                click localeElement
                waitPageLoad()

            let selectInstallLanguage() =
                click "#languageList"
                let langOption = sprintf "//option[contains(.,'%s')]" ddlistLangKeyword
                waitForElementPresent langOption
                click langOption                               

            let fillupInstallForm() =                 
                sleep 5 // needed to work around file security/permission
                click "#txtUsername"
                "#txtUsername" << hostUsername
                click "#txtPassword"
                "#txtPassword" << defaultPassword
                click "#txtConfirmPassword"
                waitForAjax() // when password input loses focus it sends ajax call
                "#txtConfirmPassword" << defaultPassword
                click "#txtEmail"
                waitForAjax() // when confirm password input loses focus it sends ajax call
                "#txtEmail" << hostEmail
                click "#txtWebsiteName"
                "#txtWebsiteName" << parentWebSiteName
                //database information
                scrollTo "table#databaseSetupType"                
                clickNthRadioButton "#databaseSetupType" 2
                waitForAjax()
                scrollTo "table#databaseType"
                clickNthRadioButton "#databaseType" 2
                click "#txtDatabaseServerName"
                "#txtDatabaseServerName" << "(local)"
                click "#txtDatabaseName"
                "#txtDatabaseName" << databaseName
                click "input#txtDatabaseObjectQualifier"
                "#txtDatabaseObjectQualifier" << qualifier
                clickNthRadioButton "#databaseSecurityType" 1

            checkPermission()
            if installationLanguage <> English then
                selectLanguageIcon()
                checkPermission()
                selectInstallLanguage()
            fillupInstallForm()
            continueWizardInstallation()

        let wizardUpgrade() = 
            let fillupUpgradeForm() = 
                printfn "  SuperUser name [%s]" hostUsername
                printfn "  SuperUser pass [%s]" defaultPassword
                "#txtUsername" << hostUsername
                "#txtPassword" << defaultPassword

            fillupUpgradeForm()
            continueWizardInstallation()

        // Note: all database settings should be already added to the web.config file
        //       specifically for a new installation for this option to work properly.
        let autoNewAndUpgrade() = 
            // Note: the folowing waits are useless since the auto-install page streams
            // its progress and we don't receive control until the process finishes.
            (*
            waitUntil waitForInstallProgressToAppear "Auto-install progress didn't appear!" (fun _ -> 
                match mode with
                | AutoNew -> existsAndVisible installingText
                | AutoUpgrade -> existsAndVisible upgradingText
                | _ -> false)
            waitUntil waitForInstallProgressToFinish "Auto-install progress didn't finish!" (fun _ -> 
               exists autoInstallErrorElement || exists autoInstallError2Element || existsAndEnabled autoVisitSiteLinkText)
            *)
            beforeVisitSiteCallback()
            waitForWebConfigUnlocked()
            scrollTo autoVisitSiteLinkText
            click autoVisitSiteLinkText

        let setup() = 
            let restartLocalIIS() =
                if not config.Site.IsRemoteSite then
                    // requires elevated rights and might not work always
                    try
                        execCommandShell "iisreset /restart" |> ignore
                    with _ -> ()

            let gotoInstallPage path =
                // first try to load the site's root page using direct link; not through Selenium
                let tout = int(canopy.configuration.pageTimeout * 2.0)
                readWebPage root tout |> ignore
                printfn "  Browsing page %s" path
                goto path

            let path = 
                match mode with
                | AutoNew -> "/install/install.aspx?mode=auto"
                | AutoUpgrade -> "/Install/Install.aspx?mode=upgrade"
                | WizardNew -> "/install/installwizard.aspx"
                | WizardUpgrade -> "/install/UpgradeWizard.aspx"
            match mode with
            | WizardNew ->
                glbDbQualifier <- sprintf "%s_" (getRandomStr 3)
                printfn " Database qualifier = %s" glbDbQualifier
                try
                    gotoInstallPage path
                    wizardNew glbDbQualifier
                with ex ->
                    if not config.Site.IsRemoteSite && hasWizardError() then
                        printfn "  -x-x-x- Retrying after installation wizard error - %s -x-x-x-" ex.Message
                        retryClicks <- 0
                        restartLocalIIS()
                        gotoInstallPage path
                        wizardNew glbDbQualifier
                    else
                        reraise()

            | WizardUpgrade ->
                gotoInstallPage path
                wizardUpgrade()
            | AutoNew
            | AutoUpgrade ->
                // we don't use our own goto page here; the auto-install page streams the progress
                let tout = int(canopy.configuration.pageTimeout * 2.0)
                readWebPage root tout |> ignore
                let link = sprintf "%s%s" root path
                url link
                autoNewAndUpgrade()

        retryClicks <- 0
        printfn "  Installing site %s started" root
        setup()
        printfn "  Installing site %s finished" root
        sleep 1 // needed to start page loading
        waitPageLoad()        
        afterVisitSiteCallback()                

    canopy.configuration.skipAllTestsOnFailure <- true
    let oldTimeout = canopy.configuration.pageTimeout
    canopy.configuration.pageTimeout <- waitForInstallProgressToFinish
    browser.Manage().Timeouts().PageLoad <- TimeSpan.FromMinutes waitForInstallProgressToFinish
    installSite()
    canopy.configuration.pageTimeout <- oldTimeout
    browser.Manage().Timeouts().PageLoad <- TimeSpan.FromSeconds oldTimeout
    canopy.configuration.skipAllTestsOnFailure <- false // if error thrown during installation the flag will be already set
