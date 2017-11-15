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

module DnnHost

open System
open System.Drawing
open canopy
open DnnAddToRole
open DnnUserLogin

let gotoExtensionsPage() =
    goto "/Host/Extensions"
    waitPageLoad()

let getAsync (url:string) (timeout: TimeSpan) = 
    async {
        let httpClient = new System.Net.Http.HttpClient()
        httpClient.Timeout <- timeout
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
        response.EnsureSuccessStatusCode () |> ignore
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return content
    }

let clearAppCacheAndRecycleApp() =
    loginAsHost()
    openPBServers()
    let clearCacheBtn = "div#servers-container>div>div>div>div>div>button:first-of-type"
    let restartAppBtn = "div#servers-container>div>div>div>div>div>button:last-of-type"
    click clearCacheBtn
    waitForAjax()
    waitForElementPresent clearCacheBtn
    click restartAppBtn
    waitForAjax()
    waitForElementPresent restartAppBtn

/// <summary>
/// Removes modules from a page
/// </summary>
/// <param name="pageUrl"></param>
/// <remarks>The page must be open in the Edit mode </remarks>
let removeAllModulesFromPage pageUrl =
    let menuAnchor = ".actionMenuAdmin > a"

    let deleteModule() =
        let handles = element "#Body" |> elementsWithin "//div[contains(@class,'dnnDragHint ui-sortable-handle')]" //handle elements
        let handleElement = handles.Item(0)
        scrollToPoint (Point(handleElement.Location.X,handleElement.Location.Y-100))
        sleep 0.5
        let moduleNumber = (handleElement |> parent |> elementWithin "a").GetAttribute("name")
        let moduleSpace = sprintf "//div[contains(@id,'dnn_ctr%s_ModuleContent')]" (moduleNumber.ToString())
        hoverOver moduleSpace
        let gearIconSelector = "//*[contains(@id,'moduleActions-" + moduleNumber + "')]/ul/li[@class='actionMenuAdmin']/a"
        waitForElementPresent gearIconSelector
        click gearIconSelector
        let modActionsSettingsSelector = "//li[contains(@id,'moduleActions-" + moduleNumber + "-Delete')]"
        waitForElementPresent modActionsSettingsSelector
        let modActionsDelete = element modActionsSettingsSelector
        click modActionsDelete
        click "button.dnnPrimaryAction"
        sleep 0.5

    goto pageUrl
    let modulesCount = elements menuAnchor |> List.length
    for i in 1 .. modulesCount do
        deleteModule()

//'pageUrl' is the relative Url of the page where the given module is installed.
//Each module, in Edit mode, contains a handle/div whose class contains 'dnnDragHint ui-sortable-handle'. Any single page might have multiple modules/handles.
//'handleNumber' is the position (starting at 0, from the top) of the given module's handle on the page.
let openModuleSettings ( pageUrl : string ) ( handleNumber : int ) =   
    if not(String.IsNullOrEmpty(pageUrl)) then 
        loginAsHost()
        closePersonaBarIfOpen()
        goto pageUrl 
    let mutable moduleNumber = ""
    openEditMode() 
    let doOpenSettings()=
        try            
            let handles = element "#Body" |> elementsWithin "//div[contains(@class,'dnnDragHint ui-sortable-handle')]" //handle elements
            let handleElement = handles.Item(handleNumber)
            scrollToPoint (Point(handleElement.Location.X,handleElement.Location.Y-100))
            sleep 0.5
            moduleNumber <- (parent handleElement |> elementWithin "a").GetAttribute("name")
            let gearIcon = "//*[contains(@id,'moduleActions-" + moduleNumber + "')]/ul/li[@class='actionMenuAdmin']/a"
            let settingsLink = "//li[@class='actionMenuAdmin']/ul/li[contains(@id,'moduleActions-" + moduleNumber + "-Settings')]"
            hoverOver handleElement
            waitClick gearIcon
            waitForElementPresent settingsLink
            clickDnnPopupLink settingsLink
            true
        with _ ->
            reloadPage()
            false
    let openSucess = retryWithWait 3 0.5 doOpenSettings
    if not openSucess then
        failwithf "  FAIL: Module Settings could not be opened successfully."
    try
        waitForElementPresent moduleSettingsDiv
    with _ -> ()
    moduleNumber

/// <summary>Deletes a module from a page</summary>
/// <param name="handleNumber">Position of the module on the page, starting at 0. Search for 'dnnDragHint ui-sortable-handle'.</param>
let deleteModule handleNumber =
    let handles = element "#Body" |> elementsWithin "//div[contains(@class,'dnnDragHint ui-sortable-handle')]" //handle elements
    let handleElement = handles.Item(handleNumber)
    scrollToPoint (Point(handleElement.Location.X,handleElement.Location.Y-100))
    sleep 0.5
    let moduleNumber = (handleElement |> parent |> elementWithin "a").GetAttribute("name")
    let gearIconSelector = "//*[contains(@id,'moduleActions-" + moduleNumber + "')]/ul/li[@class='actionMenuAdmin']/a"
    waitForElementPresent gearIconSelector
    click gearIconSelector
    let modActionDelete = "//li[contains(@id,'moduleActions-" + moduleNumber + "-Delete')]"
    waitForElementPresent modActionDelete
    //let modActionsDelete = element modActionsSettingsSelector
    click modActionDelete
    waitForElementPresent "button.dnnPrimaryAction"
    click "button.dnnPrimaryAction"
    sleep 0.5
    waitPageLoad()

//'pageUrl' is the relative Url of the page where the given module is installed.
//Each module, in Edit mode, contains a handle/div whose class contains 'dnnDragHint ui-sortable-handle'. Any single page might have multiple modules/handles.
//'handleNumber' is the position (starting at 0, from the top) of the given module's handle on the page.
//'roleRowNumber' is the row number of the role in the permission matrix (starting at 1)
//'permissionColumnNumber' is the column number of the permission in the permission matrix (starting at 1)
//'actionType' is the desired permission state (0 for clear, 1 for allow, 2 for deny)
let changeModulePermission ( pageUrl : string ) ( handleNumber : int ) ( roleRowNumber: int) ( permissionColumnNumber : int ) ( actionType: int) =
    let moduleNumber = openModuleSettings pageUrl handleNumber
    click "//a[contains(@href,'#msPermissions')]"
    waitForAjax()
    //uncheck inherit view permission checkbox
    let inheritCB = "//input[contains(@id,'ModuleSettings_chkInheritPermissions')]"
    let isChecked = inheritCB + "/../span[contains(@class,'dnnCheckbox-checked')]"    
    if existsAndVisible isChecked then 
       let inheritCBImg = element (isChecked + "/span/img")
       clickCboxImage inheritCBImg
       waitForAjax()
    //change permission
    let tableCell = (sprintf "//table[@class='dnnPermissionsGrid']/tbody/tr[%i]/td[%i]" roleRowNumber permissionColumnNumber).ToString()
    let checkboxImg = tableCell + "/img"
    let checkboxImgElm = element checkboxImg
    let checkboxInputElm = element (tableCell + "/input")

    if (actionType=0 || actionType=1 || actionType=2) then
        let reqActionName = match actionType with | 0 -> "Null" | 1 -> "True" | 2 -> "False" | _ -> ""
        if (checkboxInputElm.GetAttribute("value") <> reqActionName) then click checkboxImg
        waitForAjax()
        if (checkboxInputElm.GetAttribute("value") <> reqActionName) then click checkboxImgElm
    let updateBtn = "//a[contains(@id,'dnn_ctr" + moduleNumber + "_ModuleSettings_cmdUpdate')]"  
    click updateBtn
    waitForSpinnerDone()
    waitPageLoad()
    try
        closeEditMode()
    with _ -> () 

///'pageUrl' is the relative Url of the page
///'roleRowNumber' is the row number of the role in the permission matrix (starting at 1)
///'permissionColumnNumber' is the column number of the permission in the permission matrix (starting at 1)
///'actionType' is the desired permission state (0 for clear, 1 for allow, 2 for deny)
let changePagePermission ( pageUrl: string ) ( roleRowNumber: int) ( permissionColumnNumber : int ) ( actionType: int) =
    if not(String.IsNullOrEmpty(pageUrl)) then 
        loginAsHost()
        goto pageUrl
    openPagePermissions()
    let mutable permCheckBox = sprintf "div.role-permissions-grid>div:nth-of-type(%i)" (roleRowNumber+2) //ignoring 2 header rows
    permCheckBox <- permCheckBox + (sprintf ">div:nth-of-type(%i)>a" (permissionColumnNumber+1)) //ignoring 1 index column
    let reqState = match actionType with | 0 -> "unchecked" | 1 -> "checkbox" | 2 | _ -> "denied"
    if (element permCheckBox).GetAttribute("aria-label") <> reqState then
        click (permCheckBox + ">*")
    if (element permCheckBox).GetAttribute("aria-label") <> reqState then
        click (permCheckBox + ">*")
    let saveBtn = "div.buttons-box>button[role=primary]"
    scrollTo saveBtn
    click saveBtn
    waitForAjax()
    closePersonaBar()
    closeEditMode() 

//'pageUrl' is the relative Url of the page where the given module is installed.
//Each module, in Edit mode, contains a handle/div whose class contains 'dnnDragHint ui-sortable-handle'. Any single page might have multiple modules/handles.
//'handleNumber' is the position (starting at 0, from the top) of the given module's handle on the page.
//'actionType' is the desired state for Enable Comments Checkbox (0 for disable, 1 for enable)
let changeCommentsStatusWiki ( pageUrl : string ) ( handleNumber : int ) ( actionType: int) =
    let moduleNumber = openModuleSettings pageUrl handleNumber
    let settingsSelector = "#dnn_ctr" + moduleNumber.ToString() + "_ModuleSettings_hlSpecificSettings"
    click settingsSelector
    waitForAjax()
    let enableCB = element ("#dnn_ctr" + moduleNumber.ToString() + "_ModuleSettings_Settings_chkEnableComments")
    let enableCBSpan = element ("//div[@id='dnn_ctr" + moduleNumber.ToString() + "_ModuleSettings_Settings_ScopeWrapper']/fieldset/div[3]/span")
    let enableCBSpanClass = enableCBSpan.GetAttribute("class")
    let enableCBImgSelector = "//img[@alt='checkbox']"
    if enableCBSpanClass="dnnCheckbox" && actionType<>0 then
        click enableCBImgSelector
    if enableCBSpanClass="dnnCheckbox dnnCheckbox-checked" && actionType=0 then
        click enableCBImgSelector
    waitForAjax()
    click "//a[contains(text(),'Update')]"
    waitForSpinnerDone()
    waitPageLoad()
    try
        closeEditMode()
    with _ -> ()

/// <summary>
/// Installs a Forge module from the list of available extensions.
/// </summary>
/// <param name="moduleName">Name of the forge module to install.</param>
let installForgeModule moduleName =
    loginAsHost()
    openPBExtenstions()
    click "div.extensions-app>div>div>div>div>div>ul>li:nth-of-type(2)" //Available Extensions tab
    waitForAjax()
    let modSpan = sprintf "//span[contains(.,\"%s\")]" moduleName
    if not(existsAndVisible modSpan) then
        printfn "  INFO: Module %A is not visible. Its either not available, or already installed." moduleName
    else
        scrollTo modSpan
        let modInstallBtn = modSpan + "/../../div[4]/button"
        click modInstallBtn
        waitForAjax()
        let nextBtn = "div.modal-footer>button:last-of-type"
        waitForElementPresent nextBtn
        scrollTo nextBtn //long screen
        click nextBtn
        waitForAjax()
        click nextBtn
        let acceptCB = "div.dnn-checkbox-container>div.checkbox>label"
        waitForElementPresent acceptCB
        click acceptCB
        click nextBtn
        waitForAjax()
        let doneBtn = "div.modal-footer>button[role=primary]"
        click doneBtn
        waitForAjax()
        let installExtBtn = sprintf "//button[.=\"%s\"]" installExtensionAction
        waitForElementPresentXSecs installExtBtn 30.0    

/// <summary>Sets the SMTP server in Host Settings</summary>
/// <param name="server">The name or url of the server, with or without port</param>
let setupSMTPServer server =
    goto "/Host/Host-Settings"
    click "//a[@href='#advancedSettings']"
    expandSectionsByLink "//div[@id='advancedSettings']/div[@class='dnnFormExpandContent']"
    let smtpTextBox = "//input[contains(@name,'SMTPServer')]"
    scrollTo smtpTextBox
    smtpTextBox << server
    let updBtn = "//a[contains(@id,'HostSettings_cmdUpdate')]"
    scrollTo updBtn
    click updBtn
    waitForSpinnerDone()
    waitPageLoad()

/// <summary>Bulk add multiple pages</summary>
/// <param name="listOfPages">List of page names to add</param>
let addMultiplePages (listOfPages:Collections.Generic.List<String>) =
    loginAsAdmin() |> ignore
    openPBPages()
    let addMultiplePagesBtn = "div.dnn-persona-bar-page-header>div>button[role=secondary]"
    click addMultiplePagesBtn
    waitForAjax()
    let bulkInput ="textarea.bulk-page-input"
    waitForElementPresent bulkInput
    let mutable inputString = ""
    for i in listOfPages do
        inputString <- inputString + i + "\n"
    bulkInput << inputString
    waitForAjax()
    //do not display in menu
    click "span.dnn-switch-active>span"
    waitForAjax()
    click "div.buttons-box>button[role=primary]"
    waitForAjax()
    waitLoadingBar()
    waitForElementPresentXSecs addMultiplePagesBtn 30.0
    waitPageSettleDown() //loading page previews
    printfn "  INFO: Bulk Added %i pages" (listOfPages.Count)

/// <summary>Picks the site to import/export</summary>
/// <param name="siteName">Name of the site</param>
let pickSiteImpExp (siteName:string) =
    let oneSiteOnly = "div.site-selection>div.disabled"
    if exists oneSiteOnly then
        let chosenSite = (element "div.site-selection>div>div>span.dropdown-prepend").Text
        if not(chosenSite.ToUpper().Contains(siteName.ToUpper())) then
            failwithf "  FAIL: Site %A does not exist" siteName
    else
        let siteDiv = "//div[contains(@class,'site-selection')]/div"
        click siteDiv
        let siteItem = siteDiv + (sprintf "//div[contains(@class,'open')]/div[contains(@class,'open')]/div/ul/li[.=\"%s\"]" siteName)
        try
            waitForElementPresent siteItem
        with _ -> ()
        if not(existsAndVisible siteItem) then
            failwithf "  FAIL: Site %A does not exist" siteName
        click siteItem
        waitForAjax()

/// <summary>
/// Exports Site Data for a website
/// </summary>
/// <param name="siteName">Name of the site</param>
/// <param name="exportMode">Export mode - differential or full</param>
/// <param name="exportItems">Items to export</param>
let exportSite siteName (exportMode:ExportMode) (exportItems:ExportItems) =
    pickSiteImpExp siteName
    let exportDataBtn = "div.top-panel>div>div.action-buttons>button:first-of-type"
    click exportDataBtn
    let exportNameInp = "div.export-site-container>div>div>div.dnn-single-line-input-with-error>div>input"
    waitForElementPresent exportNameInp
    //export name
    let exportName = "Export" + getRandomId()
    exportNameInp << exportName
    let beginExportBtn = "div.action-buttons>button[role=primary]"
    scrollTo beginExportBtn
    //export mode
    match exportMode with
    | DIFFERENTIAL ->
        let diffBtn = "input.radio-button-Differential-Differential"
        if existsAndEnabled diffBtn then
            click "label[for=radio-button-Differential-Differential]"
            waitForAjax()
    | FULL ->
        let fullBtn = "input.for=radio-button-Full-Full"
        if existsAndEnabled fullBtn then
            click "input[for=radio-button-Full-Full]"
            waitForAjax()
    //export items
    let expSwitch = "div.export-switches>div:nth-of-type(Num)>div.dnn-switch-container>div>span"
    if not(exportItems.Content) then click (expSwitch.Replace("Num", "2"))
    if not(exportItems.Assets) then click (expSwitch.Replace("Num", "3"))
    if not(exportItems.Users) then click (expSwitch.Replace("Num", "4"))
    if not(exportItems.Roles) then click (expSwitch.Replace("Num", "5"))
    if not(exportItems.Vocabularies) then click (expSwitch.Replace("Num", "6"))
    if not(exportItems.PageTemplates) then click (expSwitch.Replace("Num", "7"))
    if not(exportItems.ProfileProperties) then click (expSwitch.Replace("Num", "8"))
    if not(exportItems.Permissions) then click (expSwitch.Replace("Num", "9"))
    if not(exportItems.Extensions) then click (expSwitch.Replace("Num", "10"))
    if exportItems.IncludeDeletions then click (expSwitch.Replace("Num", "11"))
    if not(exportItems.RunNow) then click (expSwitch.Replace("Num", "12"))
    let firstPageCB = "div.pages-container>ul>li.page-item>div.page-value>div:first-of-type>*"
    if not(exportItems.Pages) then click firstPageCB
    //export
    click beginExportBtn
    waitForAjax()
    waitForElementPresent exportDataBtn
    //return export name
    exportName

/// <summary>
/// Import an exported package into the site
/// </summary>
/// <param name="siteName">Name of the site</param>
/// <param name="package">Name of the export package to import</param>
let importSite siteName package =
    pickSiteImpExp siteName
    let importDataBtn = "div.top-panel>div>div.action-buttons>button:last-of-type"
    click importDataBtn
    let packTitle = sprintf "div.package-name>div[title=\"%s\"]" package
    hoverOver packTitle
    waitClick "div.package-card-overlay"
    waitForElement "div.package-card.selected" //package selected check
    let continueBtn = "div.dnn-grid-cell.action-buttons>button[role=primary]:enabled"
    click continueBtn
    waitForAjax()
    //wait for package check
    waitForElementPresentXSecs continueBtn 30.0
    click continueBtn
    waitLoadingBar()
    waitForElementPresentXSecs importDataBtn 30.0
