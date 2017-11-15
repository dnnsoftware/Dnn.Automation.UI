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

module DnnManager

open System
open System.IO
open System.Drawing
open canopy

//PB Pages should already be open
let testLevel0PageExistsPBBySearching pagename =
    let thePage = sprintf "//span[.='%s']" pagename
    if existsAndVisible thePage then true
    else
        waitLoadingBar()
        "div.search-input>input" << pagename
        sleep 0.5 //wait for search to begin
        waitLoadingBar()    
        try
            waitForElementPresent thePage
        with _ -> ()
        existsAndVisible thePage

let createUserPB (userInfo : CreateUserInfo) =
    click (sprintf "//button[.=\"%s\"]" addUserText)
    "//input[@tabindex='1']" << userInfo.FirstName
    "//input[@tabindex='2']" << userInfo.LastName
    "//input[@tabindex='3']" << userInfo.UserName
    "//input[@tabindex='4']" << userInfo.EmailAddress
    "//input[@tabindex='7']" << defaultPassword
    "//input[@tabindex='8']" << defaultPassword
    click "div.modal-footer>button:nth-child(2)"    
    try
        waitForElementPresent "#notification-dialog"
    with _ ->
        waitForAjax()

//In order for the search to work, user should be already indexed by the search system
let addRoleToUserPB username rolename =
    let searchBox = "div.users-filter-container>div>div.search-filter>div>input"
    searchBox << username
    sleep 0.5
    waitForAjax()
    click "//div[@class='username']"
    waitForAjax()
    let roleInput = element "#token-input-add-input"
    scrollToPoint (Point(roleInput.Location.X,roleInput.Location.Y-300))
    roleInput.Click()  
    roleInput.SendKeys(rolename)
    waitForAjax()
    roleInput.SendKeys(enter)
    waitForAjax()
    let addBtn = (element "div.addpanel") |> elementWithin "a"
    click addBtn
    waitForAjax()

/// <summary>Creates a new page from Persona Bar</summary>
/// <param name="pageInfo">Parameters of the page like name, title etc.</param>
/// <param name="grandParentPage">The name of the parent page of the parent page</param>
/// <returns>Url of the created page</returns>
let createPagePB (pageInfo : NewPageInfo) grandParentPage =  
    openPBPages()
    let  pageName = pageInfo.Name
    let title = pageInfo.Title
    let includeInMenu = not pageInfo.RemoveFromMenu
    let parentPage = pageInfo.ParentPage
    let addPageBtnSelector = sprintf "//button[.=\"%s\"]" addPageText
    waitClick addPageBtnSelector
    sleep 0.5
    waitLoadingBar()
    let pageDescriptionSelector = sprintf "//label[.='%s']/../../div[2]/textarea" pbDescriptionLabelText
    try
        waitForElementPresent pageDescriptionSelector
    with _ ->
        closePersonaBar()
        reloadPage()
        openPBPages()
        click addPageBtnSelector
        waitForElementPresent pageDescriptionSelector
    let pageDescription = element pageDescriptionSelector
    scrollToPoint (Point(pageDescription.Location.X,pageDescription.Location.Y-1000))
    waitPageLoad()
    let pageNameElement = element "div.left-column>div>div.input-tooltip-container>input"
    if not(existsAndVisible pageNameElement) then
        failwith "  FAIL: PB Add Page: Page Name field is not visible"
    pageNameElement << pageName
    waitForAjax()
    "div.dnn-grid-system>div:nth-child(2)>div>div>div>input" << title
    pageDescription << title
    //display in menu
    let includeInMenuSwitch = "span.dnn-switch-active>span"
    scrollTo includeInMenuSwitch
    scrollByPoint(Point(0,-200))
    if not(includeInMenu) then click includeInMenuSwitch
    waitPageLoad() //wait for template preview image to load
    //permissions
    scrollToOrigin()
    let permissionsTab = sprintf "//li[.=\"%s\"]" permissionText
    click permissionsTab
    waitForAjax()
    if pageInfo.GrantToRegisteredUsers then
        click "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='Registered Users']/../../div[2]/a/*"
    if pageInfo.GrantToAllUsers then
        click "//div[contains(@class,'role-permissions-grid')]/div/div/span[@title='All Users']/../../div[2]/a/*"
    //advanced
    let advancedTab = sprintf "//li[.=\"%s\"]" advancedText
    click advancedTab
    waitForAjax()
    let theme = sprintf "//div[@title='%s']/../span/*[1]" (pageInfo.Theme)
    if exists theme then
        click (theme + "/../span[@class='hoverLayer']")
        waitForAjax()
    //Page Container
    scrollTo "div.dnn-simple-tab-item>div>div:nth-child(3)"
    let container = sprintf "//div[@title='%s']/../span/*[1]" (pageInfo.Container)
    if exists container then
        click (container + "/../span[@class='hoverLayer']")
        waitForAjax()
    //add page
    let addBtnSelector = sprintf "//div[@class='buttons-box']/button[.='%s']" addPageText
    scrollTo addBtnSelector
    click addBtnSelector
    sleep 0.5
    waitPageLoad()
    waitForElementPresentXSecs closeButton 20.0
    currentUrl()

//PB Pages should already be open
let deletePagePB pagename =
    let searchBox = element "div.search-filter>div.dnn-search-box>input"
    searchBox << pagename
    let pageItemSelector = sprintf "//span[text()='%s']" pagename
    try
        waitForElementPresent pageItemSelector
    with _ -> ()
    if existsAndVisible pageItemSelector then
        hoverOver pageItemSelector
        click "//span[@title='Delete']"
        waitForElementPresentXSecs "#confirmbtn" 3.0
        click "#confirmbtn"
        searchBox << pagename
        sleep 0.5
        waitForAjax()
    else
        failwithf "  FAIL: Could not find PB Page to delete: %A" pagename

//PB Recycle Bin should already be open
let restorePageFromPBRecycleBin pagename =
    if not(existsAndVisible "//div[@id='pages']") then click "//a[contains(text(),'Pages')]"
    waitPageLoad()
    let firstRowSelector = "//div[@id='pages']/div[@id='pageList']/div/div/table/tbody[@class='pages-list-container']/tr[1]"
    scrollTo firstRowSelector
    let mutable rownumber = 1
    let mutable pageRowSelector = "//div[@id='pages']/div[@id='pageList']/div/div/table/tbody[@class='pages-list-container']/tr[1]"
    let mutable found = false
    while exists pageRowSelector && not found do
        let readPageName = ((element pageRowSelector) |> elementWithin "div.pagename").Text.ToString()
        if readPageName = pagename then  
            found <- true
        if not(found) then
            rownumber <- rownumber + 1
            pageRowSelector <- "//div[@id='pages']/div[@id='pageList']/div/div/table/tbody[@class='pages-list-container']/tr[" + rownumber.ToString() + "]"
    if not(found) then
        failwithf "  FAIL: Page %A not found in Recycle Bin" pagename
    let pageDiv = (element pageRowSelector) |> elementWithin "div.pagename"
    if existsAndVisible pageDiv then
        //hoverOver pageDiv
        click (pageRowSelector + "/td[1]/input")
        click "#RestoreSelectedPages"
        waitForElementPresentXSecs "#confirmbtn" 3.0
        click "#confirmbtn"
        sleep 0.5
        waitPageLoad()
    openPBPages()
    if testLevel0PageExistsPBBySearching pagename then
        printfn "  Page %A was restored successfully from Persona Bar > Recycle Bin" pagename  
    else
        failwithf "  FAIL: Could not restore Page %A from Persona Bar > Recycle Bin" pagename

/// <summary>Adds a module to page</summary>
/// <param name="moduleName">Name of the module to add</param>
let addModuleToPageWizard moduleName =
    let editBarAddModBtn = "#menu-AddModule"
    click editBarAddModBtn
    let searchTB = "#AddModule_SearchModulesInput"
    waitForElementPresent searchTB
    //wait for Module cards to load
    waitForElement "li.dnnModuleItem:first-of-type" //first module card
    waitForAjax()    
    let moduleCardSelector = sprintf "//div[contains(@class,'listAll')]/ul/li/span[.='%s']/.." moduleName
    if not(existsAndVisible moduleCardSelector) then
        searchTB << moduleName
        sleep 0.5 //wait for search to start
        waitForElementPresent moduleCardSelector
    click moduleCardSelector
    if moduleName="Grids" then
        hoverOver "//span[text()='50% + 50%']"
        click "//li[3]/a/span[2]"
    let dragBoxSelector = "//div[contains(@class,'floating')]/div[contains(@class,'dnnDragHint')]"
    try
        waitForElementPresentXSecs dragBoxSelector 20.0
        dragAndDrop dragBoxSelector "#dnn_ContentPane"
        waitForSpinnerDone()
        waitPageLoad()
        sleep 1 //wait forpage to settle down
        waitForAjax()
    with ex -> ()
    if checkForErrorMessageExists() then
        failwithf "  FAIL: Module %A shows error when deployed on a new page (in Edit Mode)" moduleName

//An admin role should be logged in and already on the page
let addModuleToPagePB modulename =   
    openEditMode()
    addModuleToPageWizard modulename
    closeEditMode()
    sleep 0.5
    if checkForErrorMessageExists() then
        failwithf "  FAIL: Module %A shows error when deployed on a new page (in Published Mode)" modulename

let getFolderTypeName foldertype =
    let mutable ftype = ""
    match foldertype with
    | STANDARD -> ftype <- "Standard"
    | SECURE -> ftype <- "Secure"
    | DATABASE -> ftype <- "Database"
    ftype

/// <summary>
/// Opens a folder in Asset manager
/// </summary>
/// <param name="folderName">Name of the folder</param>
let openFolderAssetMgr folderName =
    let folderCard = sprintf "//div[contains(@class,'item card')]/div[@class='text-card']/div/p[.=\"%s\"]" folderName
    click folderCard
    waitForAjax()

let uploadFileAssetMgr fpath = 
    let mutable success = false
    if File.Exists(fpath) then
        waitForElementPresent "//a[.='Add Asset']"
        click "//a[.='Add Asset']"
        waitForAjax()
        let chooseFile = element "//input[@name='postfile']"  
        chooseFile << (fixfilePath fpath)
        waitForAjax()
        if existsAndVisible "//a[@class='fu-file-already-exists-prompt-button-replace']" then
            click "//a[@class='fu-file-already-exists-prompt-button-replace']"
        if existsAndVisible "//div[@class='item card highlight']" then
            success <- true  
    else
        failwithf "  ERROR: File does not exist: %A\n" fpath
    success

/// <summary>
/// Opens an item in Asset Manager for editing
/// </summary>
/// <param name="itemName">Name of the folder or asset</param>
let openItemEditAssetMgr itemName =
    let item = sprintf "//p[.='%s']/../.." itemName
    hoverOver item
    let pencilSelector = item + "/../div[@class='actions']/div[@class='edit']"
    waitForElementPresent pencilSelector
    sleep 0.1
    click pencilSelector
    waitForAjax()

/// <summary>Edits a file in PB Asset Manager</summary>
/// <param name="fileName">The name of the file to be edited</param>
/// <param name="fileTitle">The title to be entered for the file</param>
/// <param name="fileDescription">The description to be entered for the file</param>
let editFileAssetMgr fileName fileTitle fileDescription =
    openItemEditAssetMgr fileName
    waitForElementPresent "input#title" //wait for title input field
    "input#title" << fileTitle
    "#description" << fileDescription
    let saveBtn = "div#fileDetailsPanel>div.save>a.primary"
    scrollTo saveBtn
    click saveBtn
    waitForElement "div.item.card.highlight" //wait for file details div to collapse
    waitForAjax()
    //check title and description
    openItemEditAssetMgr fileName
    waitForElementPresent "input#title" //wait for title input field
    waitForAjax()
    sleep 0.5 //wait for JS to load
    let readTitle = getJavaScriptValue "title.value"
    let readDescription = getJavaScriptValue "description.value"
    let cancelBtn = "div#fileDetailsPanel>div.cancel>a.secondary"
    scrollTo cancelBtn
    click cancelBtn
    if readTitle<>fileTitle || readDescription<>fileDescription then
        let failReason = 
            "  FAIL: File Details (title, description) were not saved successfully."
            + sprintf "\n\tRead title: %A, Expected title: %A" readTitle fileTitle
            + sprintf "\n\tRead description: %A, Expected description: %A" readDescription fileDescription
        failwith failReason

let private backlogOfSitePages = new System.Collections.Generic.List<String>()

/// <summary>Picks a new page from the backlog of bulk-added pages and opens it in edit mode</summary>
/// <param name="returnUrl">Bool - Set to true to get the Url of the page, False to get the page name</param>
/// <returns>Page Url or Page Name</returns>
let openNewPage returnUrl =
    let pageName = "NewTestPage" + getRandomId()
    let pageInfo = getPageInfo pageName ""    
    let newPageUrl = createPagePB pageInfo null
    if returnUrl then newPageUrl else pageName

let clearBacklogSitePages()=
    backlogOfSitePages.Clear()
