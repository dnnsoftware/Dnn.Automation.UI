[<AutoOpen>]
module DnnCommonPB

open System.Drawing
open System.Diagnostics
open canopy

let mutable formsLoaded = false
let mutable sContentLoaded = false

/// <summary>Checks if an error message is displayed in Persona Bar</summary>
let checkPBErrorMsg()=
    let error = "div#notification-dialog.errorMessage"
    if existsAndVisible error then
        let msg = (element (error+">div>p")).Text
        failwithf "  FAIL: Error message displayed: %s" msg

/// <summary>Waits for the Loading Bar in Persona Bar to disappear, for a max of 1 minute</summary>
let waitLoadingBar()=
    let waitingBar = "div.waiting-message"
    let loadingBar = "div#personaBar-loadingbar[style*='display: block']"
    let loadingBarSpan = "div#personaBar-loadingbar>span"    
    if existsAndVisible waitingBar || existsAndVisible loadingBar || existsAndVisible loadingBarSpan then
        waitForAjax()
        let sw = Stopwatch.StartNew()
        printf "  Waiting for PB Loading Bar to complete"
        let mutable retries = 60
        while (existsAndVisible waitingBar || existsAndVisible loadingBar || existsAndVisible loadingBarSpan) && retries > 0 do
            sleep 1
            retries <- retries - 1
        sw.Stop()
        printfn " (%i ms)" (sw.ElapsedMilliseconds)
    waitForAjax()
    //check if error message is displayed
    if existsAndVisible "div.load-error" then
        failwith "  FAIL: Error message displayed in Loading Bar"
    checkPBErrorMsg()

//scrolldownorup: boolen: true for down, false for up
//scrollpercentage: integer: how much to scroll in terms of percentage of scroll bar height
let scrollPB scrolldownorup (scrollpercentage : int) =
    let pageScrollBarSelector = "div.jspDrag"
    if exists pageScrollBarSelector then
        let pageScrollBar = element pageScrollBarSelector
        let mutable scrollHeight = ( pageScrollBar.Size.Height / 100 ) * scrollpercentage
        if not(scrolldownorup) then scrollHeight <- -(scrollHeight)
        dragElementBy pageScrollBarSelector (Point(0,scrollHeight))

/// <summary>Opens a section in Persona Bar</summary>
/// <param name="hoverIcon">The PB icon to hover over</param>
/// <param name="sectionLink">The link in PB to open the sections</param>
/// <param name="sectionElm">The element in the section to verify/wait for</param>
/// <param name="sectionLoadTime">The time (in seconds) to wait for the section element</param>
let openPBSection hoverIcon sectionLink sectionElm sectionLoadTime =
    let doOpenSection()=
        try
            hoverOver hoverIcon
            waitForElementPresentXSecs sectionLink 5.0
            click sectionLink
            waitForAjax()
            hideHoverMenu()
            waitForElementPresentXSecs sectionElm (float sectionLoadTime)
            waitForAjax()
            scrollToOrigin()            
            checkPBErrorMsg()
            waitLoadingBar()
            true
        with _ ->
            reloadPage()
            waitForElementPresent hoverIcon            
            false
    if not(existsAndVisible sectionElm) then
        let openSucess = retryWithWait 3 0.5 doOpenSection
        if not openSucess then
            failwithf "  FAIL: PB Section %A > %A > %A could not be opened successfully." hoverIcon sectionLink sectionElm

let openPBPages()=
    openPBSection "#Content" "//li[@id='Dnn.Pages']" addPageBtn 10

let openPBRecycleBin()=
    openPBSection "#Content" "//li[@id='Dnn.Recyclebin']" "//li/a[@href='#pages']" 10

let openPBUsers()=
    openPBSection "#Manage" "//li[@id='Dnn.Users']" (sprintf "//button[.=\"%s\"]" addUserText) 10

let openPBRoles()=
    openPBSection "#Manage" "//li[@id='Dnn.Roles']" createRoleBtn 10

let openPBThemes()=
    openPBSection "#Manage" "//li[@id='Dnn.Themes']" "div.restore-theme>button" 10

let openPBSites()=
    openPBSection "#Manage" "//li[@id='Dnn.Sites']" addSiteBtn 10

let openPBAdminLogs()=
    openPBSection "#Manage" "//li[@id='Dnn.AdminLogs']" "div.adminlogs-app>div>div>div>div>ul>li:nth-child(1)" 10

let openPBSiteSettings()=
    openPBSection "#Settings" "//li[@id='Dnn.SiteSettings']" "div.siteSettings-app>div>div>div>div>ul>li:nth-child(1)" 10

let openPBSecurity()=
    openPBSection "#Settings" "//li[@id='Dnn.Security']" "div.securitySettings-app>div>div>div>div>ul>li:nth-child(1)" 10

let openPBSeo()=
    openPBSection "#Settings" "//li[@id='Dnn.Seo']" "div.seo-app>div>div>div>div>ul>li:nth-child(1)" 10

let openPBConnectors() =
    openPBSection "#Settings" "//li[contains(@id, '.Connectors')]" "table#connectionstbl" 10

let openPBExtenstions()=
    openPBSection "#Settings" "//li[@id='Dnn.Extensions']" (sprintf "//button[.=\"%s\"]" installExtensionAction) 10

let openPBServers()=
    openPBSection "#Settings" "//li[@id='Dnn.Servers']" "div#servers-container>div>div>div>div>div>button:first-of-type" 10

let openPBImportExport()=
    openPBSection "#Settings" "//li[@id='Dnn.SiteImportExport']" "div.top-panel>div>div.action-buttons>button:first-of-type" 10

let openPBScheduler()=
    openPBSection "#Settings" "//li[@id='Dnn.TaskScheduler']" "div.taskScheduler-app>div>div>div>div.primary>ul>li:nth-child(1)" 10

let openPBCustomCss()=
    openPBSection "#Settings" "//li[@id='Dnn.CssEditor']" customCssEditor 10

let openPBSqlConsole()=
    openPBSection "#Settings" "//li[@id='Dnn.SqlConsole']" "div.query-form>div.actions>button.create-page" 10

let openPBPrompt()=
    openPBSection "#Settings" "//li[@id='Dnn.Prompt']" promptInput 10

/// <summary>Toggles the CDF/CRM feature to on or off. Increments the current host version.</summary>
let toggleCdf () =
    openPBServers()
    click "div.dnn-servers-tab-panel>ul>li:last-child" //Server Settings tab
    let perfTab = "div.dnn-servers-tab-panel>div>div>ul>li:nth-child(2)" //Performance tab
    let enableCompFiles = "div.performanceSettingTab>div:nth-of-type(5)>div>div.rightPane>div:nth-of-type(2)>div>div>span>span"
    let cssSection = "div.performanceSettingTab>div:nth-of-type(5)>div>div.rightPane>div:nth-of-type(3)>div>div"
    let jsSection = "div.performanceSettingTab>div:nth-of-type(5)>div>div.rightPane>div:nth-of-type(4)>div>div"
    let minifyCSS = cssSection + ">span>span"    
    let minifyJS = jsSection + ">span>span"    
    let incVersion = "//div[@class='currentHostVersion']/../button"
    let saveBtn = "div.buttons-panel>button"
    let confirmBtn = "#confirmbtn"
    waitForElementPresent perfTab
    click perfTab
    waitForAjax()
    scrollTo enableCompFiles
    click enableCompFiles
    waitForAjax()
    let minifyCSSLabel = element (cssSection + ">label")
    let minifyJSLabel = element (jsSection + ">label")
    if minifyCSSLabel.Text <> "On" then click minifyCSS
    if minifyJSLabel.Text <> "On" then click minifyJS
    click saveBtn
    waitForAjax()
    click incVersion
    waitForElementPresent confirmBtn
    click confirmBtn
    waitForAjax()
    reloadPage()

let clearStoredFlags()=
    formsLoaded <- false
    sContentLoaded <- false

/// <summary>Execute SQL Query</summary>
/// <param name="query>The SQL query to execute</param>
let executeSqlQuery query  =
    openPBSqlConsole()
    let resultsTab = "div.result-tabs>ul>li>a"
    //If results tab already visible, reload page
    if existsAndVisible resultsTab then
        reloadPage()
        openPBSqlConsole()
    //enter query
    let codeMirrorElement = element "div#sqlconsole-bodyPanel>div>div>div.CodeMirror"
    click codeMirrorElement
    execJs ("arguments[0].CodeMirror.setValue(\"" + query + "\");") codeMirrorElement |> ignore // Add SQL query into the CodeMirror
    // execute the SQL query
    click "button.create-page" 
    try
        waitForElementPresent resultsTab
        waitForAjax()
    with _ -> ()
    //check for error
    if existsAndVisible "div#notification-dialog>img.notify-error" then
        click "button#close-notification"
        waitForAjax()
        failwithf "  FAIL: There was an error in SQL query:\n\t%A" query
    if not(existsAndVisible resultsTab) then
        failwithf "  FAIL: Results Tab not visible after running SQL query:\n\t%A" query
