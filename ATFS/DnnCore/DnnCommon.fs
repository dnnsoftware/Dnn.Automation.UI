// Automation UI Testing Framework - ATFS - http://www.dnnsoftware.com
// Copyright (c) 2015 - 2017, DNN Corporation
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

[<AutoOpen>]
module DnnCommon

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open canopy

// look for page skin and set variables accordingly
let mutable prevSkin = ""
let private setPageSkinSettings() =
    // look for skin in page source as in this example
    // <link href="/Portals/_default/Skins/Cavalier/skin.css?cdv=83" type="text/css" rel="stylesheet"/>
    let pattern = "/Portals/_default/Skins/([^/]+)/skin.css"
    let m = Regex.Match(browser.PageSource, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5.))
    if m.Success then
        let captures = m.Groups.Item(1).Captures
        let skin = captures.Item(0).Value
        if skin <> prevSkin then
            printfn "  Switching to %A skin settings" skin
            siteSettings.init skin |> ignore
            prevSkin <- skin
    else
        printfn "  Skin CSS not found in page source"

let private showWaitEventLog text =
    let u = currentUrl()
    let uri = new Uri(u)
    let pg =
        if uri.DnsSafeHost = siteDomain then uri.PathAndQuery
        else u
    printf "  %s %s" text pg

//User should be already logged in
let closeToastNotification() =
    let toastCloseSelector = "//div[@class='toast-item-close']"
    try
        if existsAndVisible toastCloseSelector then
            click toastCloseSelector
            printfn "  INFO: Closed Toast Notification."
            sleep 0.5
    with _ -> ()

let waitForSpinnerDone() =
    let sw = Stopwatch.StartNew()
    // I didn't add any counter to exit the loop
    // since the spinner should exit sooner or later
    // and I don't want it to work as a timed loop
    showWaitEventLog "waiting spinner to finish for"
    sleep 0.01
    while existsAndVisible "div.raDiv" do sleep 0.1
    sw.Stop()
    printfn " (%u ms)" sw.ElapsedMilliseconds

// this is striclty for use when jQuery library is used
let waitForAjax() = 
    let sw = Stopwatch.StartNew()
    sleep 0.01
    let active = 
        try 
            let jsval = js "return jQuery.active"
            match box jsval with
            | :? Int64 as l -> l
            | _ -> Convert.ToInt64(jsval)
        with _ -> 0L
    if active <> 0L then 
        printf "  waiting ajax-completion event (jQuery.active=%d)" active
        try 
            let eventWait = getBrowserWait canopy.configuration.elementTimeout
            eventWait.Until(
                fun _ ->
                    try
                        let jsval = js "return jQuery.active"
                        match box jsval with
                        | :? Int64 as l -> l = 0L
                        | _ -> Convert.ToInt64(jsval).Equals(0L)
                    with _ -> true
                ) |> ignore
        with _ -> ()
        sw.Stop()
        printfn " (%u ms)" sw.ElapsedMilliseconds

let waitPageLoad() = 
    let sw = Stopwatch.StartNew()
    showWaitEventLog "waiting page-load event for"
    try 
        sleep 0.01
        let pageWait = getBrowserWait canopy.configuration.pageTimeout
        pageWait.Until(fun _ -> try not (isNull (unreliableElement "//body")) with _ -> false) |> ignore
    with _ -> ()
    try 
        let eventWait = getBrowserWait canopy.configuration.elementTimeout
        eventWait.Until(fun _ -> try (js "return window.document.readyState").Equals("complete") with _ -> false) |> ignore
    with _ -> ()
    waitForAjax()
    sw.Stop()
    printfn " (%u ms)" sw.ElapsedMilliseconds
    setPageSkinSettings()

/// <summary>Wait till 3 sucessive WaitPageLoad() calls take less than 1 second each</summary>
let waitPageSettleDown()=
    let sw = Stopwatch.StartNew()
    let rec checkWaitTime settledTimes totalTimes  =
        if settledTimes < 3 && totalTimes <= 20 then
            let beforeTime = sw.Elapsed
            waitPageLoad()
            let afterTime = sw.Elapsed
            let waitMilliSeconds = (afterTime-beforeTime).TotalMilliseconds
            sleep 0.05
            //printfn "waitMilliSeconds: %f (wait#%i)" waitMilliSeconds totalTimes
            if waitMilliSeconds < 120.0 then checkWaitTime (settledTimes+1) (totalTimes+1)
            else checkWaitTime 0 (totalTimes+1)
    checkWaitTime 0 1

let waitUntilBy selector (seconds : float)=
    let wait = getBrowserWait seconds
    wait.Until(fun _ -> (element selector).Displayed) |> ignore
    existsAndEnabled selector

let waitForElementPresent selector = 
    printfn "  waiting for element present: %A" selector
    let wait = getBrowserWait canopy.configuration.elementTimeout
    wait.Until(fun _ -> (element selector).Displayed) |> ignore

let waitForElementPresentXSecs selector seconds = 
    printf "  waiting for element present: %A" selector
    let sw = Stopwatch.StartNew()

    let rec retryChecking() = 
        if existsAndVisible selector then true
        elif sw.Elapsed.TotalSeconds >= seconds then false
        else  
            sleep 0.1
            retryChecking()

    let found = retryChecking()
    sw.Stop()
    if found then printfn " (found in %i ms)" (sw.ElapsedMilliseconds)
    else failwithf "  Fail: Element %A not present after waiting %i ms" selector (sw.ElapsedMilliseconds)

/// <summary>Waits for a selector to be NOT visible for 10 seconds approx.</summary>
/// <param name="selector">Selector of the item to check</param>
let waitForElementNotPresent selector = 
    let mutable retries = 100
    while existsAndVisible selector && retries > 0 do
        retries <- retries - 1

/// <summary>Waits for an item to be visible, then clicks it</summary>
/// <param name="selector">Selector of the item to click</param>
let waitClick selector =
    waitForElementPresent selector
    click selector

//notDisplayed pageDoesNotExistText
// goes to the relative url according to effective portal
// to go to parent site without effective portal, use "url (root + pageUrl)
let goto (pageUrl : string) = 
    printfn "  visiting: %s" pageUrl
    let waitAndCheckPage url = 
        waitPageLoad()
        if existsAndVisible pageNotFoundText then failwithf "Page cannot be found: %s" url
        // Note: checking the error page coming from the browser itself is
        // browser specific and might not work in all browsers and/or languages
        if existsAndVisible browserServerAppError then failwithf "Server Error in application: %s" url
    if pageUrl.StartsWith("http") then 
        url pageUrl
        waitAndCheckPage pageUrl
    else 
        let s = 
            if pageUrl.StartsWith("/") then pageUrl
            else "/" + pageUrl

        let uri = 
            if useChildPortal then sprintf "%s/%s%s" root childSiteAlias s
            else sprintf "%s%s" root s

        url uri
        waitAndCheckPage uri

let isOnPage (pageUrl : string) = 
    if pageUrl.StartsWith("http") then on pageUrl
    else 
        let s = 
            if pageUrl.StartsWith("/") then pageUrl
            else "/" + pageUrl

        let uri = 
            if useChildPortal then sprintf "%s/%s%s" root childSiteAlias s
            else sprintf "%s%s" root s

        on uri

let isOnRoot() = 
    let u = currentUrl()
    if u.Contains("Default.aspx") then isOnPage "/Default.aspx"
    else isOnPage "/"

(*
let operateOnChildSite f = 
    let b = useChildPortal
    useChildPortal <- true
    try 
        goto "/"
        f()
    finally
        useChildPortal <- b
*)

let popupiFrame() = first popupFormSelector |> elementWithin "#iPopUp"

let maximizePopup() =
    // some products does not expose maximize button
    let maxTag = "a.dnnToggleMax"
    if existsAndVisible maxTag then
        try
            click maxTag
            waitForAjax()
        with _ -> ()

let closePopup() = 
    browser.SwitchTo().DefaultContent() |> ignore
    if existsAndVisible popupFormSelector then 
        let popup = first popupFormSelector
        let closeLinkSelector = "//a[@class='dnnModalCtrl']/button"
        let closeLink = popup |> someElementWithin closeLinkSelector
        match closeLink with
        | None -> ()
        | Some(btn) -> 
            scrollElementIntoView btn
            try
                click btn
            with ex -> 
                if existsAndVisible closeLinkSelector then click closeLinkSelector
            waitPageLoad() // popup disappears and page reloads
        |> ignore

// returns the full path of the captured screenshot file name
let captureScreenShot _ = 
    let path = canopy.configuration.failScreenshotPath
    let filename = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")
    screenshot path filename |> ignore
    Path.Combine(path, filename + ".png")

let CheckSkinValidationError errMsg = 
    if existsAndVisible SkinMsgErrorSelector then 
        printfn errMsg
        let e = element SkinMsgErrorSelector
        failwith e.Text

// timeout in seconds
let waitUntil timeout message f = 
    let tout = canopy.configuration.compareTimeout

    let t = 
        match box timeout with
        | :? float as i -> i
        | :? int as i -> Convert.ToDouble(i)
        | _ -> 
            puts (sprintf "Warning: %A is not a float or integer" timeout)
            tout // default to original
    canopy.configuration.compareTimeout <- t
    printf "  Waiting %0.2f seconds for processing to finish ." t
    let sw = Stopwatch.StartNew()
    try 
        waitFor2 message (fun _ -> 
            printf "."
            if f() then true
            else 
                sleep 0.1
                false)
    finally
        printfn ""
        printfn "  ! Finished after %.2f seconds" sw.Elapsed.TotalSeconds
        canopy.configuration.compareTimeout <- tout

//check for any module installation error on the page -- "<font color='red'>Error!</font><br>"
let autoInstallErrorElement = "//font[.='Error!']"
let autoInstallError2Element = "//h2[.='Upgrade Error: ERROR: Could not connect to database specified in connectionString for SqlDataProvider']"

// click a link that opens a DNN popup dialog (differs from jQuery dialog)
// first time a popup is clicked, it takes longer than succcessive ones
let clickDnnPopupLink selector = 
    let popupText = textFromSelector selector
    printfn "  Clicking POPUP link %A" popupText
    click selector
    waitForSpinnerDone()
    waitPageLoad()
    browser.SwitchTo().DefaultContent() |> ignore
    let mutable retries = 10
    while not (existsAndVisible "#iPopUp") && retries > 0 do
        sleep 0.3
        retries <- retries - 1
    displayed popupFormSelector

// opens a a DNN popup dialog in a page instead of as popup
// falls back to popup if it is not a popup link
let openDnnPopupLinkPage selector = 
    let e = elementFromSelector selector
    let mutable href = e.GetAttribute("href")
    let jsModalText = "javascript:dnnModal.show('"
    let jsPopupText = "?popUp=true"
    if href.StartsWith(jsModalText) && href.Contains(jsPopupText) then 
        href <- href.Substring(jsModalText.Length)
        let pos = href.IndexOf(jsPopupText)
        href <- href.Substring(0, pos)
        goto href
    else if href.StartsWith("http") then goto href
    else clickDnnPopupLink e
    CheckSkinValidationError "Page has validation error"

let extractControlId partialSelector = 
    let someE = someElement partialSelector
    match someE with
    | None -> 
        raise (System.ArgumentException(sprintf "No element for selector %A" partialSelector))
    | Some(e) -> 
        let id = e.GetAttribute("id")
        Regex.Match(id, "dnn_ctr(\d+)_").Result("$1")

// collects all hrefs under a specific element (div, span, etc.) that belong to our site
let collectLinks elementId = 
    match someElement elementId with
    | None -> []
    | Some(el) -> 
        let selector = sprintf "a[href*='://%s/']" config.Site.SiteAlias
        el
        |> elementsWithin "a"
        |> List.map (fun e -> e.GetAttribute("href"))
        |> List.filter (fun h -> h.IndexOf(config.Site.SiteAlias, StringComparison.InvariantCultureIgnoreCase) >= 0)

let expandlink elementId =
    match elementId with
    | "expandall" -> click "//a[contains(text(),'Expand All')]"
    | "collapseall" -> click "//a[contains(text(),'Collapse All')]"
    | _ -> ()

let reloadPage() =
    let sw = Stopwatch.StartNew()
    showWaitEventLog (sprintf "waiting Page Reload for %s" (currentUrl()))
    browser.Navigate().Refresh()
    js "history.go(0)" |> ignore
    sw.Stop()
    printfn " (%u ms)" sw.ElapsedMilliseconds

let getPageInfo pgName parentPage =
    { Name = pgName
      Title = pgName
      Description = sprintf "Test Page [/%s] description" pgName
      ParentPage = parentPage
      AfterPage = homePageName
      Position = ATEND
      HeaderTags = "PerfPageTag"
      Theme = "Xcillion"
      Container = "notitle"
      RemoveFromMenu = true
      GrantToAllUsers = true
      GrantToRegisteredUsers = true }

let hideHoverMenu() =
    if existsAndVisible closeBtnPB then hoverOver closeBtnPB
    elif existsAndVisible closeButton then hoverOver closeButton
    else
        let siteLogo = "div#logo"
        hoverOver siteLogo

let switchToCurrentContext()=
    let origWindow = browser.CurrentWindowHandle
    switchToWindow origWindow

/// <summary>Waits for Persona Bar to open, and then closes it.</summary>
let closePersonaBar()=
    try
        waitForElementPresent closeBtnPB
        waitForAjax()
    with _ -> ()
    if existsAndVisible closeBtnPB then
        let doClose()=
            try
                click closeBtnPB
                waitForAjax()
                if existsAndVisible closeBtnPB then 
                    raise (System.Exception "PB not closed")
                else true
            with _ ->
                reloadPage()
                waitForElementPresent closeBtnPB
                waitForAjax()
                false
        let closeSuccess = retryWithWait 3 0.5 doClose
        if not closeSuccess then
            printfn "  INFO: Persona Bar could not be closed."
        //switch context out of PB
        switchToCurrentContext()

/// <summary>
/// Closes persona bar if it is already open
/// </summary>
let closePersonaBarIfOpen()=
    if not(existsAndVisible closeBtnPB) then sleep 0.5
    if existsAndVisible closeBtnPB then closePersonaBar()
    else switchToCurrentContext()

let private isEditModeOpen()=
    not(exists closedEditMode)

/// <summary>Opens Edit Mode</summary>
/// Note: The selector "body.dnnEditState" does not work reliably
let openEditMode()=
    let doOpen()=
        try
            waitClick "#Edit"
            waitPageLoad()
            waitForElementPresent editBar
            waitForElementPresent "div#personabar"            
            hideHoverMenu() //hide edit button's lock message
            true
        with _ ->
            reloadPage()
            false
    if isEditModeOpen() then sleep 0.5 //wait to confirm again
    if not(isEditModeOpen()) then
        let openSuccess = retryWithWait 2 0.5 doOpen
        if not(openSuccess) then
            failwithf "  FAIL: Could not open edit mode for page %A" (currentUrl())

/// <summary>Closes Edit Mode</summary>
let closeEditMode()=
    let doClose()=
        try
            try
                waitClick closeButton
            with _ ->
                click publishButton
            waitPageLoad()
            waitForElement closedEditMode
            sleep 0.5 //wait for page to settle down
            true
        with _ ->
            reloadPage()
            false
    if not(isEditModeOpen()) then sleep 0.5 //wait to confirm again
    if isEditModeOpen() then
        let closeSuccess = retryWithWait 2 0.5 doClose
        if not(closeSuccess) then
            failwithf "  FAIL: Could not close edit mode for page %A" (currentUrl())

/// opens the page permissions from Edit Menu
let openPagePermissions()=
    openEditMode()
    let settingsBtn = "li#menu-PageSettings"
    click settingsBtn
    let permTab = "div.treeview-page-details>div>ul>li:nth-of-type(2)"
    waitClick permTab
    waitForAjax()

// must b logged in as HOST before calling this
//let toggleCdf () =
    //goto "/Host/Host-Settings"
    //click "//a[@href='#advancedSettings']"
    //clickExpandLink "a.expanded"
    //scrollTo "#Panel-ClientResourceManagement"
    //clickExpandLink (element "#Panel-ClientResourceManagement" |> elementWithin "a")
    //let ctrId = extractControlId "//a[contains(@id,'_HostSettings_IncrementCrmVersionButton')]"
    //let enableComposteFilesCheck = "#dnn_ctr" + ctrId + "_HostSettings_chkCrmEnableCompositeFiles"
    //let minifyCssCheck = "#dnn_ctr" + ctrId + "_HostSettings_chkCrmMinifyCss"
    //let minifyJsCheck = "#dnn_ctr" + ctrId + "_HostSettings_chkCrmMinifyJs"
    //let incrementVersionButton= "#dnn_ctr" + ctrId + "_HostSettings_IncrementCrmVersionButton"
    //let updateButton = "#dnn_ctr" + ctrId + "_HostSettings_cmdUpdate"
    //clickCboxImage enableComposteFilesCheck
    //waitForSpinnerDone()
    //if existsAndEnabled minifyCssCheck then
    //    clickCboxImage minifyCssCheck
    //    clickCboxImage minifyJsCheck
    //    printfn "  CDF Enabled"
    //else
    //    printfn "  CDF Disabled"
    //click updateButton
    //waitForSpinnerDone()
    //// now click the increment version so it takes effect
    //scrollTo incrementVersionButton
    //click incrementVersionButton
    //let confirmYesBtn = "button.dnnPrimaryAction"
    //waitForElementPresent confirmYesBtn
    //click confirmYesBtn // click the confirm popup
    //waitForSpinnerDone()

let expandSectonHeaders sectionnames =
    for i in sectionnames do
        let section = "//*[@class='dnnFormSectionHead']/a[contains(text(),\"" + i + "\")]"
        scrollTo section
        let sectionElement = element section
        if not(sectionElement.GetAttribute("class").Contains("dnnSectionExpanded")) then
            scrollTo section
            click section
            waitForAjax()
    scrollToOrigin()

let expandSectionsByLink parentSelector =
    let expandLinkSelector = parentSelector + "/a"
    let expandLink = element expandLinkSelector
    if expandLink.Text="Collapse All" then 
        click expandLinkSelector
        waitForAjax()
    click expandLinkSelector
    waitForAjax()

let checkForErrorMessageExists() =
    existsAndVisible SkinMsgErrorSelector

let checkForHTTPError404Exists() =
    existsAndVisible "//*[contains(text(),'HTTP Error 404')]"

let check404SitePageDisplayed()=
    existsAndVisible "*[contains(.,\"the page you are looking for cannot be found\")]"

let publishPublisherPost()=
    let publishBtnSelector = "div.publisher-editbar>div>div>button.primary-button[data-bind*=showPublish]"
    try
        waitForElementPresent publishBtnSelector
    with _ ->
        reloadPage()
        waitForElementPresent publishBtnSelector
    click publishBtnSelector
    try        
        waitForElementPresent "div#edit-bar>div.right-section>a.primary-button" //Edit Post btn
    with _ -> ()
    waitPageLoad()

let getConnectorName (connector : ConnectorsList) : String =
    match connector with
    | ConnectorsList.AZURE -> "Azure"
    | ConnectorsList.UNC -> "UNC"

/// <summary>Maximizes the browser window</summary>
let maximizeWindow()=
    try
        if browser.Manage().Window.Size.Height < 768 || browser.Manage().Window.Size.Width < 1024 then
            press OpenQA.Selenium.Keys.F11
    with _ -> ()

/// <summary>Returns the value of a JavaScript Variable</summary>
/// <param name="variableName">The name of the JavaScript Variable</param>
/// <returns>The value of the JavaScript Variable</returns>
let getJavaScriptValue variableName=
    let scriptToRun = sprintf "return %s;" variableName
    (js scriptToRun).ToString()

/// <summary>Opens the page settings for a page</summary>
let openPageSettings() =
    let pageSettings = 
        match installationLanguage with
        | English -> "//a[contains(.,'Page Settings')]"
        | German -> "//a[contains(.,'Seiteneinstellungen')]"
        | Spanish -> "//a[contains(.,'Editar')]"
        | French -> "//a[contains(.,'Paramètres de la page')]"
        | Italian -> "//a[contains(.,'Impostazioni Pagina')]"
        | Dutch -> "//a[contains(.,'Pagina instellingen')]"
    hoverOver "#ControlEditPageMenu"
    waitForElementPresentXSecs pageSettings 3.0
    clickDnnPopupLink pageSettings
    waitForElementPresent "//input[contains(@id,'ManageTabs_txtTabName')]"

/// <summary>Verifies a UI element's width and height</summary>
/// <param name="elmt">The element to be verified</param>
/// <param name="width">Expected width of the element</param>
/// <param name="height">Expected height of the element</param>
/// <returns>Reasons for failure, if any</returns>
let verifyElmWidthAndHeight (elmt, width, height) =
    let mutable fReasons = ""
    if not(existsAndPartiallyVisible elmt) then
        fReasons <- fReasons + sprintf "\tElement %A is not visible.\n" elmt
    else
        let eSize = (element elmt).Size
        let eWidth = eSize.Width
        let eHeight = eSize.Height
        //printfn "  INFO: Element %A is %ipx wide and %ipx high." elmt eWidth eHeight
        if eWidth < width then
            fReasons <- fReasons + sprintf "\tWidth of element %A was %ipx, expected at least %ipx.\n" elmt eWidth width
        if eHeight < height then
            fReasons <- fReasons + sprintf "\tHeight of element %A was %ipx, expected at least %ipx.\n" elmt eHeight height
    fReasons

/// <summary>Checks if a file is locked</summary>
/// <param name="file">The file to be checked</param>
let isFileLocked (file:FileInfo) =
    let mutable stream = null
    try
        stream <- file.Open(FileMode.Open, FileAccess.Read, FileShare.None)
        if isNull stream |> not then stream.Close()
        false        
    with _ ->
        if isNull stream |> not then stream.Close()
        true

/// <summary>Waits for a file to be unlocked for a specified time</summary>
/// <param name="file">The file to wait for to be unlocked</param>
/// <param name="seconds">Max number of seconds (5-60) to wait for unlocking</param>
let waitForFileUnlocked (file:FileInfo) (seconds:int) =
    printf "  waiting for file unlocked: %s" (file.FullName)  
    let sw = Stopwatch.StartNew()
    //wait min 5 secs and max 60 secs
    let mutable retries = if seconds < 5 then 5 elif seconds > 60 then 60 else seconds
    while retries > 0 && isFileLocked file do
        sleep 1
        retries <- retries - 1
    sw.Stop()
    if isFileLocked file then
        printfn " -- File %A still locked after waiting %i ms" (file.Name) (sw.ElapsedMilliseconds)
    else
        printfn " (unlocked in %i ms)" (sw.ElapsedMilliseconds)
        sleep 0.1

/// <summary>Waits for web.config to be unlocked for a max of 60 seconds</summary>
let waitForWebConfigUnlocked() =
    let sitePath = config.Site.WebsiteFolder
    let webConfigPath = FileInfo(sitePath + "\\web.config")
    if isFileLocked webConfigPath then
        waitForFileUnlocked webConfigPath 30

